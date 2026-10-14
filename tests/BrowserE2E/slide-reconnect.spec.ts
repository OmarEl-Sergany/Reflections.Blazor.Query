import { expect, test, type BrowserContext, type WebSocketRoute } from "@playwright/test";

test("page query state clears while layout query state survives navigation", async ({ page }) => {
  await page.goto("/navigation-state-test");
  await expect(page.getByTestId("navigation-state-ready")).toHaveText("styles-applied");
  await expect(page.locator(".navigation-state-page-target")).toHaveCSS("color", "rgb(255, 0, 0)");
  await expect(page.locator(".sidebar")).toHaveCSS("background-color", "rgb(0, 0, 0)");

  await page.getByRole("link", { name: "Counter" }).click();
  await expect(page).toHaveURL(/\/counter$/);
  await expect(page.locator('style[data-reflections-blazor-query="true"]').nth(1)).not.toContainText(".navigation-state-page-target { color: red; }");
  await expect(page.locator(".sidebar")).toHaveCSS("background-color", "rgb(0, 0, 0)");
});

test("disposing a QueryElement removes its selector metadata", async ({ page }) => {
  await page.goto("/disposal-test");
  await expect(page.locator(".disposal-test-target")).toBeVisible();
  await expect(page.getByTestId("disposal-match-count")).toHaveText("matches:0");
  await expect(page.locator(".disposal-test-target")).toHaveCount(0);
});

test("disposing a QueryElement cancels its active cancellation-aware queue callback", async ({ page }) => {
  await page.goto("/queue-disposal-test");
  await expect(page.getByTestId("queue-disposal-ready")).toHaveText("ready");
  await expect(page.locator(".queue-disposal-target")).toBeVisible();

  await page.getByTestId("start-disposal-queue").click();

  await expect(page.getByTestId("queue-disposal-status")).toHaveText("cancelled-on-dispose");
  await expect(page.locator(".queue-disposal-target")).toHaveCount(0);
});

test("an expired Server circuit does not resume its active queue", async ({ page, context }) => {
  test.setTimeout(120_000);
  const circuit = await controlCircuitExpiration(context);
  await page.goto("/circuit-queue-test");
  await expect(page.getByTestId("circuit-queue-ready")).toHaveText("ready");
  await page.getByTestId("start-circuit-queue").click();
  await expect(page.getByTestId("circuit-queue-status")).toHaveText("running");

  await circuit.disconnect();
  await expect(page.locator("#components-reconnect-modal")).toHaveJSProperty("open", true);
  await page.waitForTimeout(31_000);
  await context.setOffline(false);
  await expect(page.getByTestId("circuit-queue-ready")).toHaveText("ready");
  await expect(page.getByTestId("circuit-queue-status")).toHaveText("idle", { timeout: 30_000 });
});

test("a slide that is still running after reconnect completes normally", async ({ page, context }) => {
  const circuit = await controlCircuitReconnect(context);
  await page.goto("/reconnect-test?duration=8000");
  await expect(page.getByTestId("page-ready")).toHaveText("ready");
  await page.getByTestId("start-slide").click();
  await expect(page.getByTestId("slide-status")).toHaveText("running");
  await expect(page.locator(".reconnect-test-slide")).toHaveCSS("animation-name", "rbq-slide-down");
  await expect.poll(() => slidePlayState(page)).toBe("running");

  await circuit.disconnect();
  await expect(page.locator("#components-reconnect-modal")).toHaveJSProperty("open", true);
  await expect.poll(() => slidePlayState(page)).toBe("running");

  await circuit.allowReconnect();
  await expect(page.locator("#components-reconnect-modal")).toHaveJSProperty("open", false);
  await expect.poll(() => slidePlayState(page)).toBe("running");
  await expect(page.getByTestId("slide-status")).toHaveText("completed", { timeout: 15_000 });
});

test("a slide completed while disconnected settles after reconnect", async ({ page, context }) => {
  const circuit = await controlCircuitReconnect(context);
  await page.goto("/reconnect-test?duration=5000");
  await expect(page.getByTestId("page-ready")).toHaveText("ready");
  await trackSlideAnimationEnd(page);
  await page.getByTestId("start-slide").click();
  await expect(page.getByTestId("slide-status")).toHaveText("running");
  await expect(page.locator(".reconnect-test-slide")).toHaveCSS("animation-name", "rbq-slide-down");
  await expect.poll(() => slidePlayState(page)).toBe("running");

  await circuit.disconnect();
  await expect(page.locator("#components-reconnect-modal")).toHaveJSProperty("open", true);
  await expect.poll(() => slideAnimationEnded(page)).toBe(true);
  await expect(page.getByTestId("slide-status")).toHaveText("running");
  await expect.poll(() => slideEventCounts(page)).toEqual({ native: 1, synthetic: 0 });

  await circuit.allowReconnect();
  await expect(page.locator("#components-reconnect-modal")).toHaveJSProperty("open", false);
  await expect(page.getByTestId("slide-status")).toHaveText("completed", { timeout: 10_000 });
  await expect.poll(() => slideEventCounts(page)).toEqual({ native: 1, synthetic: 1 });
});

async function slidePlayState(page: import("@playwright/test").Page): Promise<string> {
  return page.locator(".reconnect-test-slide").evaluate(element => {
    const slide = element.getAnimations().find(animation =>
      animation instanceof CSSAnimation && animation.animationName === "rbq-slide-down");
    return slide?.playState ?? "absent";
  });
}

async function trackSlideAnimationEnd(page: import("@playwright/test").Page): Promise<void> {
  await page.locator(".reconnect-test-slide").evaluate(element => {
    (window as any).__rbqSlideAnimation = { native: 0, synthetic: 0 };
    element.addEventListener("animationend", event => {
      if (event instanceof AnimationEvent && event.animationName === "rbq-slide-down") {
        const counts = (window as any).__rbqSlideAnimation;
        counts[event.isTrusted ? "native" : "synthetic"]++;
      }
    });
  });
}

async function slideAnimationEnded(page: import("@playwright/test").Page): Promise<boolean> {
  return page.evaluate(() => (window as any).__rbqSlideAnimation.native === 1);
}

async function slideEventCounts(page: import("@playwright/test").Page): Promise<{ native: number; synthetic: number }> {
  return page.evaluate(() => (window as any).__rbqSlideAnimation);
}

async function controlCircuitReconnect(context: BrowserContext) {
  let reconnectAllowed = true;
  let active: { browser: WebSocketRoute; server: WebSocketRoute } | undefined;

  await context.routeWebSocket("**/_blazor**", socket => {
    if (!reconnectAllowed) {
      void socket.close({ code: 1012, reason: "Reconnect held by E2E test" });
      return;
    }

    const server = socket.connectToServer();
    active = { browser: socket, server };
  });

  return {
    async disconnect() {
      reconnectAllowed = false;
      if (!active)
        throw new Error("The Blazor circuit WebSocket was not established.");
      await context.setOffline(true);
      await Promise.all([
        active.browser.close({ code: 1012, reason: "Reconnect test disconnect" }),
        active.server.close({ code: 1012, reason: "Reconnect test disconnect" }),
      ]);
    },
    async allowReconnect() {
      reconnectAllowed = true;
      await context.setOffline(false);
    },
  };
}

async function controlCircuitExpiration(context: BrowserContext) {
  let active: { browser: WebSocketRoute; server: WebSocketRoute } | undefined;
  await context.routeWebSocket("**/_blazor**", socket => {
    const server = socket.connectToServer();
    active = { browser: socket, server };
  });

  return {
    async disconnect() {
      if (!active)
        throw new Error("The Blazor circuit WebSocket was not established.");
      await context.setOffline(true);
      await Promise.all([
        active.browser.close({ code: 1012, reason: "Circuit expiration test" }),
        active.server.close({ code: 1012, reason: "Circuit expiration test" }),
      ]);
    },
  };
}
