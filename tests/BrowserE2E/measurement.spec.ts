import { expect, test } from "@playwright/test";

const host = process.env.RBQ_E2E_HOST ?? "server";

test("read-only measurements return browser layout values without mutating owned elements", async ({ page }) => {
  const circuitConnections: string[] = [];
  page.on("websocket", socket => circuitConnections.push(socket.url()));

  await page.goto("/measurement-proof");
  await expect(page.getByRole("heading", { name: "Query measurement proof" })).toBeVisible();
  await page.waitForLoadState("networkidle");

  const target = page.getByTestId("measurement-target");
  const originalAttributes = await target.evaluate(element => ({
    style: element.getAttribute("style"),
    className: element.getAttribute("class"),
    key: element.getAttribute("data-rbq-key"),
  }));

  await page.getByTestId("measure-all").click();
  await assertMeasurementSnapshot(page);

  expect(await target.evaluate(element => ({
    style: element.getAttribute("style"),
    className: element.getAttribute("class"),
    key: element.getAttribute("data-rbq-key"),
  }))).toEqual(originalAttributes);

  if (host === "auto") {
    await expect.poll(() => circuitConnections.length).toBeGreaterThan(0);
    const initialCircuitCount = circuitConnections.length;
    await page.reload();
    await expect(page.getByRole("heading", { name: "Query measurement proof" })).toBeVisible();
    await page.waitForLoadState("networkidle");
    await page.getByTestId("measure-all").click();
    await assertMeasurementSnapshot(page);
    expect(circuitConnections).toHaveLength(initialCircuitCount);
  }
});

async function assertMeasurementSnapshot(page: import("@playwright/test").Page) {
  const result = page.getByTestId("measurement-results");
  await expect(result).toBeVisible();
  await expect(result).toHaveAttribute("data-height", "110");
  await expect(result).toHaveAttribute("data-width", "150");
  await expect(result).toHaveAttribute("data-inner-height", "100");
  await expect(result).toHaveAttribute("data-inner-width", "140");
  await expect(result).toHaveAttribute("data-outer-height", "110");
  await expect(result).toHaveAttribute("data-outer-width", "150");
  await expect(result).toHaveAttribute("data-position-left", "11");
  await expect(result).toHaveAttribute("data-position-top", "7");
  await expect(result).toHaveAttribute("data-offset-parent", "measurement-parent");
  await expect(result).toHaveAttribute("data-offset-left", /^\d+(\.\d+)?$/);
  await expect(result).toHaveAttribute("data-offset-top", /^\d+(\.\d+)?$/);
}
