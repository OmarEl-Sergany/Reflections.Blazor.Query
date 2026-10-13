# Batch 0 lifecycle hardening — research

## Scope

Add regression coverage for queued read-only browser observations that are invalidated while their JS bridge call is already in flight.

## Target inventory

- `src/Reflections.Blazor.Query/Measurement/QueryMeasurementCoordinator.cs`
- `src/Reflections.Blazor.Query/Measurement/QueryFormCoordinator.cs`
- Existing suites: `QueryMeasurementCoordinatorTests.cs` and `QueryFormSerializationTests.cs`

## Existing conventions

- xUnit tests use `TaskCompletionSource` with `RunContinuationsAsynchronously` for deterministic asynchronous bridge behavior.
- Coordinators are constructed directly with local fake bridges.
- The render-state object identity is the isolation boundary.

## Acceptance checklist

1. Invalidating a page measurement while its bridge call is in flight cancels the awaiting caller.
2. A late measurement bridge completion cannot repopulate the invalidated page cache.
3. A surviving layout render state remains independently usable after the page state is invalidated.
4. The same guarantees apply to in-flight form serialization snapshots.
