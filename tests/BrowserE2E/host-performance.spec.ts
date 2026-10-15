import { expect, test, type Page } from "@playwright/test";
import { mkdir, writeFile } from "node:fs/promises";
import { dirname, resolve } from "node:path";

const host = process.env.RBQ_E2E_HOST ?? "server";
const sampleCount = 25;

type Sample = {
  runtime: "server" | "wasm";
  handlerMilliseconds: number;
  clickToRenderMilliseconds: number;
};

test("host measurement performance baseline", async ({ page }) => {
  const circuits: string[] = [];
  page.on("websocket", socket => {
    if (socket.url().includes("/_blazor"))
      circuits.push(socket.url());
  });

  await openRoute(page);
  const groups: Array<Record<string, unknown>> = [];

  if (host === "auto") {
    await expect.poll(() => circuits.length).toBeGreaterThan(0);
    expect(await page.getByTestId("host-perf-results").getAttribute("data-runtime")).toBe("server");
    groups.push(await collectRuntimeGroup(page, "server", sampleCount));

    const circuitCount = circuits.length;
    await page.reload();
    await openRoute(page);
    const wasmSamples = await collectRuntimeGroup(page, "wasm", sampleCount);
    expect(circuits).toHaveLength(circuitCount);
    groups.push(wasmSamples);
  } else {
    const expectedRuntime = host === "wasm" ? "wasm" : "server";
    if (host === "server")
      await expect.poll(() => circuits.length).toBeGreaterThan(0);
    else
      expect(circuits).toHaveLength(0);
    groups.push(await collectRuntimeGroup(page, expectedRuntime, sampleCount));
  }

  const report = {
    host,
    browser: await page.evaluate(() => navigator.userAgent),
    sampleCount,
    timingDefinitions: {
      handlerMilliseconds: {
        server: "Server handler elapsed; includes the JS interop request/response over the Blazor circuit, not pure server CPU or isolated SignalR latency.",
        wasm: "Client .NET handler elapsed, including JS interop and the in-browser measurement batch."
      },
      clickToRenderMilliseconds: "Browser capture-phase click to MutationObserver observation of the rendered result; end-to-end UI latency.",
      directLayoutRead: "Browser-only repeated getBoundingClientRect() baseline; not the library interop path."
    },
    groups
  };

  const json = JSON.stringify(report, null, 2);
  const reportPath = resolve(process.cwd(), "../../output/playwright", host, "host-performance.json");
  await mkdir(dirname(reportPath), { recursive: true });
  await writeFile(reportPath, json, "utf8");
  await test.info().attach("host-performance.json", { body: Buffer.from(json), contentType: "application/json" });
  console.log(`Host performance report: ${json}`);
});

async function openRoute(page: Page) {
  await page.goto("/host-performance");
  await expect(page.getByRole("heading", { name: "Query host performance" })).toBeVisible();
  await expect(page.getByTestId("measure-one")).toBeVisible();
  await expect(page.getByTestId("host-perf-results")).toHaveAttribute("data-completed", "0");
  await page.waitForLoadState("networkidle");
}

async function collectRuntimeGroup(page: Page, runtime: "server" | "wasm", count: number) {
  const output = page.getByTestId("host-perf-results");
  await expect(output).toHaveAttribute("data-runtime", runtime);
  await installTimingObserver(page);

  for (let index = 1; index <= count; index++) {
    await page.getByTestId("measure-one").click();
    await expect(output).toHaveAttribute("data-completed", String(index));
    await expect(output).toHaveAttribute("data-height", "110.00");
    await expect(output).toHaveAttribute("data-width", "150.00");
    await expect(output).toHaveAttribute("data-inner-height", "100.00");
    await expect(output).toHaveAttribute("data-inner-width", "140.00");
    await expect(output).toHaveAttribute("data-outer-height", "110.00");
    await expect(output).toHaveAttribute("data-outer-width", "150.00");
    await expect(output).toHaveAttribute("data-offset-left", /^-?\d+(\.\d+)?$/);
    await expect(output).toHaveAttribute("data-position-left", /^-?\d+(\.\d+)?$/);
    await expect(output).toHaveAttribute("data-valid", "true");
    await expect(output).toHaveAttribute("data-offset-parent", "host-perf-parent");
    await page.waitForFunction(expected => (window as PerformanceWindow).__rbqHostSamples?.length === expected, index);
  }

  const samples = await page.evaluate(() => (window as PerformanceWindow).__rbqHostSamples ?? []);
  expect(samples).toHaveLength(count);
  expect(samples.every(sample => sample.runtime === runtime)).toBe(true);
  expect(samples.every(sample => sample.handlerMilliseconds >= 0 && sample.clickToRenderMilliseconds > 0)).toBe(true);

  const directLayoutRead = await page.evaluate(() => {
    const target = document.getElementById("host-perf-target-0");
    if (!target)
      throw new Error("Performance target missing");

    const iterations = 10_000;
    let checksum = 0;
    const start = performance.now();
    for (let index = 0; index < iterations; index++)
      checksum += target.getBoundingClientRect().width;
    const elapsed = performance.now() - start;

    return { iterations, elapsedMilliseconds: elapsed, millisecondsPerRead: elapsed / iterations, checksum };
  });

  const report = {
    runtime,
    sampleCount: samples.length,
    handlerMilliseconds: summarize(samples.map(sample => sample.handlerMilliseconds)),
    clickToRenderMilliseconds: summarize(samples.map(sample => sample.clickToRenderMilliseconds)),
    rawSamples: samples,
    directLayoutRead
  };
  console.log(`${runtime} host measurements: ${JSON.stringify({
    handlerMilliseconds: report.handlerMilliseconds,
    clickToRenderMilliseconds: report.clickToRenderMilliseconds,
    directLayoutRead
  })}`);
  return report;
}

async function installTimingObserver(page: Page) {
  await page.evaluate(() => {
    const output = document.querySelector<HTMLOutputElement>('[data-testid="host-perf-results"]');
    if (!output)
      throw new Error("Performance result output missing");

    const state = window as PerformanceWindow;
    state.__rbqHostSamples = [];
    let clickStartedAt = 0;
    let lastCompleted = Number(output.dataset.completed);

    document.addEventListener("click", event => {
      const target = event.target;
      if (target instanceof Element && target.closest('[data-testid="measure-one"]'))
        clickStartedAt = performance.now();
    }, true);

    new MutationObserver(() => {
      const completed = Number(output.dataset.completed);
      if (completed <= lastCompleted)
        return;

      lastCompleted = completed;
      state.__rbqHostSamples?.push({
        runtime: output.dataset.runtime as "server" | "wasm",
        handlerMilliseconds: Number(output.dataset.handlerMs),
        clickToRenderMilliseconds: performance.now() - clickStartedAt
      });
    }).observe(output, { attributes: true, attributeFilter: ["data-completed"] });
  });
}

function summarize(values: number[]) {
  const sorted = [...values].sort((left, right) => left - right);
  const percentile = (fraction: number) => sorted[Math.max(0, Math.ceil(sorted.length * fraction) - 1)];
  return {
    min: Number(sorted[0].toFixed(3)),
    median: Number(percentile(0.5).toFixed(3)),
    p95: Number(percentile(0.95).toFixed(3)),
    max: Number(sorted.at(-1)!.toFixed(3))
  };
}

type PerformanceWindow = Window & {
  __rbqHostSamples?: Sample[];
};
