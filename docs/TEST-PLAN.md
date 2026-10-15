# Automated test plan

## Purpose

Define the repeatable validation required before producing release artifacts for Reflections.Blazor.Query. The Azure Pipeline is the enforcement point: any failed restore, build, test, packaging, or package-validation step fails the run. Package and documentation artifacts are produced only after the test gates succeed.

## Test layers

| Layer | Project / suite | Primary coverage | CI requirement |
| --- | --- | --- | --- |
| Unit | `tests/Reflections.Blazor.Query.Tests` | Selection and scope resolution; render-state behavior; registry lifecycle; measurement coordination; forms; classes and structure; effects and queues | All tests pass on the Release build |
| Component | `tests/Reflections.Blazor.Query.ComponentTests` | `QueryElement` rendering; host contracts and navigation/disposal; component scopes; form, structure, effects, and element relocation behavior | All tests pass on the Release build |
| Browser E2E — Server | `tests/BrowserE2E` with default host | Interactive Server lifecycle, reconnect behavior, and cross-render behavior | All Playwright tests pass |
| Browser E2E — WebAssembly | `tests/BrowserE2E` with `RBQ_E2E_HOST=wasm` | Client lifecycle, navigation, effects, queueing, delegation, measurement, and host performance scenarios | All Playwright tests pass |
| Browser E2E — Auto | `tests/BrowserE2E` with `RBQ_E2E_HOST=auto` | Auto-render-mode lifecycle and client-side behavior | All Playwright tests pass |
| Release validation | Pipeline packaging and documentation steps | Package identity, metadata, license, analyzer payload, generated API docs | All validation steps pass before artifacts are published |

## Release gate

The pipeline order is deliberate:

1. Restore and build the complete solution in Release configuration.
2. Run the unit and component test projects and publish their TRX results to the Azure DevOps run.
3. Run the Server, WebAssembly, and Auto browser suites and publish JUnit results.
4. Pack both NuGet packages and validate package contents and release metadata.
5. Build and validate the documentation site.
6. Publish build artifacts.

Azure Pipelines stops executing subsequent normal steps after a failure. Therefore a failed test prevents package creation and artifact publication. Test result publication uses `succeededOrFailed()` so diagnostics remain available for failed test runs. Do not mark a release candidate validated unless the exact source revision has a successful pipeline run with all required test suites green.

## Local execution

Run the .NET test plan:

```powershell
dotnet test Reflections.Blazor.Query.slnx --configuration Release
```

Run the browser suites from `tests/BrowserE2E` after restoring npm dependencies and installing Playwright Chromium:

```powershell
npm ci
npx playwright install chromium
npm test -- --reporter=list
$env:RBQ_E2E_HOST = 'wasm'
npm test -- --reporter=list
$env:RBQ_E2E_HOST = 'auto'
npm test -- --reporter=list
```

The Azure Pipeline remains the authoritative release gate because it validates the committed revision on the configured build agent and retains test and package artifacts.
