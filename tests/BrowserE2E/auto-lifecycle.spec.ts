import { expect, test } from "@playwright/test";

test("Interactive Auto switches to cached WebAssembly and clears page state on navigation", async ({ page }) => {
  test.setTimeout(120_000);
  const circuitConnections: string[] = [];
  page.on("websocket", socket => circuitConnections.push(socket.url()));

  await page.goto("/query-proof");
  await expect(page.getByRole("heading", { name: "Auto query proof" })).toBeVisible();
  await page.waitForLoadState("networkidle");

  const target = page.locator(".query-proof-text");
  await page.getByRole("button", { name: "Apply CSS" }).click();
  await expect(page.getByRole("status")).toContainText("Style applied in Interactive Auto.");
  await expect(target).toHaveCSS("color", "rgb(255, 0, 0)");
  await expect.poll(() => circuitConnections.length).toBeGreaterThan(0);

  const initialCircuitCount = circuitConnections.length;
  await page.reload();
  await expect(page.getByRole("heading", { name: "Auto query proof" })).toBeVisible();
  await page.waitForLoadState("networkidle");

  await page.getByRole("button", { name: "Apply CSS" }).click();
  await expect(page.getByRole("status")).toContainText("Style applied in Interactive Auto.");
  await expect(target).toHaveCSS("color", "rgb(255, 0, 0)");
  expect(circuitConnections).toHaveLength(initialCircuitCount);

  await page.getByRole("link", { name: "Counter" }).click();
  await expect(page).toHaveURL(/\/counter$/);
  await expect(page.locator('style[data-reflections-blazor-query="true"]')).toHaveCount(0);

  await page.goBack();
  await expect(page.getByRole("heading", { name: "Auto query proof" })).toBeVisible();
  await expect(target).not.toHaveCSS("color", "rgb(255, 0, 0)");
});
