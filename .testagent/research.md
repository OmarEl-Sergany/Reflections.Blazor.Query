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

---

# Replace operations — research

## Scope

Implement `ReplaceWith`, `ReplaceAll`, and `Replace` without bypassing the library-owned render-state and real-element relocation model.

## Ownership rule

The replacement is moved into the single target's registered sibling slot by the established `AttachSibling` path. The target is removed only after that move succeeds. Virtual targets are removed from state and registry; real targets use the existing render-owned removal state. The implementation deliberately does not clone for multi-target operations.

## Acceptance checklist

1. Replacing a virtual target moves the supplied replacement to its exact sibling position and removes the target from state and registry.
2. `ReplaceAll(target)` is the receiver-order equivalent of `target.ReplaceWith(this)`.
3. `Replace` is the documented compatibility alias of `ReplaceWith`.
4. A real Razor-declared replacement renders once at the target position while the real target no longer renders.
5. Self-replacement is rejected before it can remove the only selected element.

---

# WrapAll — research

## Scope

Implement `WrapAll` for a non-empty selection of owned siblings that share one registered parent. The operation creates one virtual wrapper before the first selected element, then moves every selected real or virtual element into it in render-order.

## Explicit boundary

`WrapInner` is not included in this slice. Arbitrary Razor `ChildContent` is an opaque `RenderFragment`; wrapping it correctly requires a separate render-slot design. Pretending that only virtual fragments were wrapped would violate caller expectations.

## Acceptance checklist

1. Virtual siblings become ordered children of exactly one generated wrapper.
2. Real Razor-declared siblings render once, in order, under that wrapper.
3. Mixed-parent selections fail before any wrapper or relocation state is created.
4. Generated metadata advertises `WrapAll`.
