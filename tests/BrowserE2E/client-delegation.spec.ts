import { expect, test } from "@playwright/test";

async function verifyDelegatedClick(page: import("@playwright/test").Page, host: "wasm" | "auto") {
  const status = page.getByTestId("delegation-status");
  await page.getByRole("button", { name: "Enable delegated click" }).click();
  await expect(status).toHaveText("enabled");

  await page.getByRole("button", { name: "Non-matching child" }).click();
  await expect(status).toHaveText("enabled");

  await page.getByRole("button", { name: "Delegated child" }).click();
  await expect(status).toHaveText(`delegated:${host}-delegation-child`);
}

test("pre-start custom-event initializer routes only matching nested owned elements", async ({ page }) => {
  test.setTimeout(120_000);
  const circuitConnections: string[] = [];
  page.on("websocket", socket => circuitConnections.push(socket.url()));
  const host = process.env.RBQ_E2E_HOST === "auto" ? "auto" : "wasm";
  const heading = host === "auto" ? "Auto query proof" : "WebAssembly query proof";

  await page.goto("/query-proof");
  await expect(page.getByRole("heading", { name: heading })).toBeVisible();
  await page.waitForLoadState("networkidle");
  await verifyDelegatedClick(page, host);

  if (host === "auto") {
    await expect.poll(() => circuitConnections.length).toBeGreaterThan(0);
    const initialCircuitCount = circuitConnections.length;

    await page.reload();
    await expect(page.getByRole("heading", { name: "Auto query proof" })).toBeVisible();
    await page.waitForLoadState("networkidle");
    await verifyDelegatedClick(page, host);
    expect(circuitConnections).toHaveLength(initialCircuitCount);
  }
});
