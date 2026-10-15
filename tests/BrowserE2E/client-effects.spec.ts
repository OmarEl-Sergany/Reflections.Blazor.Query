import { expect, test } from "@playwright/test";

test("measured slide toggle completes down and up in the client-capable host", async ({ page }) => {
  await page.goto("/query-proof");
  await expect(page.locator("h1")).toContainText(/query proof/i);
  await page.waitForLoadState("networkidle");

  const target = page.locator(".slide-target");
  const toggle = page.getByRole("button", { name: "Toggle slide" });
  await expect(target).toBeVisible();

  await toggle.click();
  await expect(target).toHaveCSS("animation-name", "rbq-slide-up");
  await expect(target).toBeHidden({ timeout: 10_000 });
  await expect(target).not.toHaveAttribute("style", /height|overflow|visibility|animation/i);

  await toggle.click();
  await expect(target).toHaveCSS("animation-name", "rbq-slide-down");
  await expect(target).toBeVisible({ timeout: 10_000 });
  await expect(target).not.toHaveAttribute("style", /height|overflow|visibility|animation/i);
});
