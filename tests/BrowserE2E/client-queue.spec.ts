import { expect, test } from "@playwright/test";

async function runQueueSequence(page: import("@playwright/test").Page) {
  const status = page.getByTestId("queue-status");
  await page.getByRole("button", { name: "Queue sequence", exact: true }).click();
  await expect(status).toHaveText("queued");
  await page.waitForTimeout(150);
  await expect(status).toHaveText("queued");

  await page.getByRole("button", { name: "Dequeue sequence" }).click();
  await expect(status).toHaveText("done:first-start,first-end,second-start,second-end");
}

test("queue callbacks wait for Dequeue and run sequentially in client-capable hosts", async ({ page }) => {
  test.setTimeout(120_000);
  const circuitConnections: string[] = [];
  page.on("websocket", socket => circuitConnections.push(socket.url()));

  await page.goto("/query-proof");
  await expect(page.locator("h1")).toContainText(/query proof/i);
  await page.waitForLoadState("networkidle");
  await runQueueSequence(page);

  if (process.env.RBQ_E2E_HOST === "auto") {
    await expect.poll(() => circuitConnections.length).toBeGreaterThan(0);
    const initialCircuitCount = circuitConnections.length;

    await page.reload();
    await expect(page.getByRole("heading", { name: "Auto query proof" })).toBeVisible();
    await page.waitForLoadState("networkidle");
    await runQueueSequence(page);
    expect(circuitConnections).toHaveLength(initialCircuitCount);
  }
});
