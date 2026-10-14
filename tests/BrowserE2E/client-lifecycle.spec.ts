import { expect, test } from "@playwright/test";

test("WebAssembly page state clears when navigating away and back", async ({ page }) => {
  await page.goto("/query-proof");
  await expect(page.getByRole("heading", { name: "WebAssembly query proof" })).toBeVisible();
  await page.waitForLoadState("networkidle");

  const target = page.locator(".query-proof-text");
  await page.getByRole("button", { name: "Apply CSS" }).click();
  await expect(page.getByRole("status")).toContainText("Style applied in Interactive WebAssembly.");
  await expect(target).toHaveCSS("color", "rgb(255, 0, 0)");

  await page.getByRole("link", { name: "Counter" }).click();
  await expect(page).toHaveURL(/\/counter$/);
  await expect(page.locator('style[data-reflections-blazor-query="true"]')).toHaveCount(0);

  await page.goBack();
  await expect(page.getByRole("heading", { name: "WebAssembly query proof" })).toBeVisible();
  await expect(target).not.toHaveCSS("color", "rgb(255, 0, 0)");
});
