import { defineConfig, devices } from "@playwright/test";

const host = process.env.RBQ_E2E_HOST ?? "server";
const hosts = {
  server: { project: "../../samples/Reflections.Blazor.Query.Demo/Reflections.Blazor.Query.Demo.csproj", port: 5184 },
  wasm: { project: "../../samples/Reflections.Blazor.Query.Demo.WebAssembly/Reflections.Blazor.Query.Demo.WebAssembly/Reflections.Blazor.Query.Demo.WebAssembly.csproj", port: 5185 },
  auto: { project: "../../samples/Reflections.Blazor.Query.Demo.Auto/Reflections.Blazor.Query.Demo.Auto/Reflections.Blazor.Query.Demo.Auto.csproj", port: 5186 },
} as const;
const selectedHost = hosts[host as keyof typeof hosts];
if (!selectedHost)
  throw new Error(`Unsupported RBQ_E2E_HOST '${host}'. Expected 'server', 'wasm', or 'auto'.`);

const baseURL = `http://127.0.0.1:${selectedHost.port}`;
const testIgnore = host === "server"
  ? ["**/client-lifecycle.spec.ts", "**/client-effects.spec.ts", "**/client-queue.spec.ts", "**/client-delegation.spec.ts", "**/auto-lifecycle.spec.ts"]
  : host === "wasm"
    ? ["**/slide-reconnect.spec.ts", "**/auto-lifecycle.spec.ts"]
    : ["**/slide-reconnect.spec.ts", "**/client-lifecycle.spec.ts"];

export default defineConfig({
  testDir: ".",
  testMatch: "**/*.spec.ts",
  testIgnore,
  outputDir: `../../output/playwright/${host}`,
  fullyParallel: false,
  timeout: 60_000,
  expect: { timeout: 15_000 },
  reporter: "list",
  use: {
    ...devices["Desktop Chrome"],
    baseURL,
    trace: "retain-on-failure",
  },
  webServer: {
    command: `dotnet run --no-launch-profile --project ${selectedHost.project} --urls ${baseURL}`,
    url: baseURL,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
    stdout: "pipe",
    stderr: "pipe",
    env: { ASPNETCORE_ENVIRONMENT: "Development", RBQ_E2E: "true" },
  },
});
