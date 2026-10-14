# Performance Baseline

## Scope

This is a reproducible baseline for the shared, in-process C# path only. It measures `QueryMeasurementCoordinator` request coalescing against an in-memory bridge and `QuerySelection.Animate` render-state updates. It does **not** measure browser layout, JavaScript interop, Blazor render dispatch, DOM work, SignalR transport, or WebAssembly execution. Consequently, these results are not Server-versus-client comparisons and are not end-to-end latency claims.

The measurement case queues one distinct `Width` read for each selected key, flushes once, and awaits all results. An iteration-level invariant verifies that the coordinator calls the bridge exactly once. The effect case alternates two target dictionaries over the selected elements and verifies the selection cardinality before timing.

## Reproduction

From the repository root:

```powershell
dotnet run -c Release --project benchmarks/Reflections.Blazor.Query.Benchmarks -- --filter "*" --artifacts output/benchmarks/latest --noOverwrite
```

BenchmarkDotNet writes raw reports under `output/benchmarks/`, which is ignored by Git. The checked-in tables below are a dated snapshot; rerun the command on the target machine before making release or regression decisions. For quick build/harness validation, add `--job Dry`; Dry timings are not valid performance comparisons.

## Snapshot — 2026-10-14

BenchmarkDotNet 0.15.8, default job; Windows 11 (10.0.26300.9711), Intel Core i7-10870H 2.20 GHz, .NET SDK 10.0.402, .NET runtime 10.0.13, x64 RyuJIT. `Mean` is per benchmark invocation. Allocations are managed bytes per invocation as reported by `MemoryDiagnoser`.

### Measurement request coalescing

| Distinct reads in one flush | Mean | StdDev | Allocated |
|---:|---:|---:|---:|
| 1 | 31.38 µs | 3.063 µs | 1.62 KB |
| 10 | 45.72 µs | 4.701 µs | 8.41 KB |
| 100 | 188.12 µs | 20.624 µs | 79.67 KB |
| 1,000 | 1,604.65 µs | 284.201 µs | 800.58 KB |

### `Animate` render-state updates

| Selected elements | Mean | StdDev | Allocated |
|---:|---:|---:|---:|
| 1 | 541.8 ns | 17.77 ns | 968 B |
| 10 | 2.251 µs | 25.17 ns | 1,760 B |
| 100 | 20.303 µs | 429.80 ns | 9,680 B |
| 1,000 | 219.933 µs | 6.353 µs | 96,080 B |

## Interpretation and remaining gate

The results establish a repeatable local baseline and show scaling/managed allocation for the two measured operations. The measurement numbers include coordinator, task/value-task bookkeeping, and the fake bridge's in-memory result materialization. They do not include actual JS invocation or browser work. The `Animate` benchmark covers state updates only, not the subsequent Blazor render or CSS transition in a browser.

The 1,000-read measurement case has relatively high run-to-run variation (284 µs standard deviation); treat it as a reference point, not a regression threshold. Hardware, runtime, power policy, and background load affect these figures.

## Browser-host snapshot — 2026-10-14

The Playwright `host measurement performance baseline` case ran 25 uncached batches per runtime. Each batch requests all nine dimensions/position metrics concurrently for a distinct generated key and asserts the returned values and owned offset-parent key. `Handler` is elapsed .NET event-handler time around query dispatch and awaited reads. `Click → render` is captured in the browser from capture-phase click to a `MutationObserver` seeing the rendered output update. `10k direct layout reads` is a separate browser-only repeated `getBoundingClientRect()` loop; it is not a measurement of library interop.

| Host/runtime path | Handler p50 / p95 (ms) | Click → render p50 / p95 (ms) | 10k direct layout reads (ms) |
|---|---:|---:|---:|
| Interactive Server | 13.351 / 14.957 | 14.6 / 16.9 | 8.6 |
| Interactive WebAssembly | 11.1 / 12.4 | 14.3 / 15.9 | 8.9 |
| Auto, initial Server | 12.318 / 14.881 | 13.9 / 15.9 | 8.5 |
| Auto, cached WebAssembly after reload | 11.5 / 12.9 | 14.6 / 16.0 | 8.6 |

Environment: Windows 11 Pro build 26300, Intel Core i7-10870H 2.20 GHz, .NET SDK 10.0.402, Chromium 156.0.8078.4. The Auto test confirms the initial circuit and absence of a new SignalR circuit after warm reload; the route reports which runtime handled each event. Raw 25-sample reports and JUnit files are generated under `output/playwright/{server,wasm,auto}` for CI publication.

These are local diagnostic baselines, not performance guarantees. Server handler elapsed includes coordinator work and the JS-interop request/response over the Blazor circuit; it is not pure server CPU or isolated SignalR latency. Browser click-to-render includes event delivery, rendering, and DOM application. WebAssembly handler time includes client .NET and browser JS interop. The direct layout loop isolates only local browser read cost. Do not add these columns or infer a standalone transport cost.

Sprint 5 host performance evidence is now recorded above. The Azure pipeline must still publish the JSON/JUnit evidence for the exact pushed source revision; CI timings should be retained as a distinct agent-environment snapshot.
