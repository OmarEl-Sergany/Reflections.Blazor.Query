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

---

# WrapInner — research

## Scope

Implement `WrapInner` only for mounted `QueryElement` instances. A component registers its own `ChildContent` render fragment with `QueryRenderState`; the operation creates an owned virtual wrapper, renders that fragment through the wrapper, and reparents existing library-owned direct children beneath it.

## Ownership rule

The query engine never reads or moves browser DOM nodes. It relocates a render fragment that the `QueryElement` component explicitly owns. Virtual nodes have no component-backed render fragment, so `WrapInner` rejects them rather than silently wrapping only part of their rendered output.

## Acceptance checklist

1. A real `QueryElement` with Razor `ChildContent` renders that content once under the generated wrapper after `WrapInner`.
2. Existing direct library-owned children are reparented beneath the wrapper in order.
3. A virtual target is rejected before a wrapper is created.
4. Generated metadata advertises `WrapInner`.

---

# Component query scopes — research

## Scope

Add lexical query isolation without creating nested DI service providers. `QueryScopeHost` creates an `IQueryScope`, cascades it through a component subtree, and `QueryElement` registers its stable key to that scope. The shared registry remains one scoped service, but every scoped selection filters keys to its own scope.

## Isolation rule

Scope is a registry predicate, not a DI lifetime. Resolution, traversal results, selector composition, and structural descendant cleanup must exclude other scope keys. This allows a component to orchestrate only its own registered elements while an explicit unscoped `Query` remains available for page/layout orchestration.

## Acceptance checklist

1. A scope resolves matching elements in its own component subtree and excludes an identical selector in another scope.
2. Traversal cannot escape the scope through `Parent` or sibling relationships.
3. Structural cleanup (`Empty`) does not remove nested elements owned by another scope.
4. QueryScopeHost cascades the same scope to child QueryElements without a nested DI scope.

---

# Events foundation — research

Library-owned handlers are stored by element key and normalized DOM event name. QueryElement renders an Action delegate for each registered event so Blazor owns browser binding and dispatch. `One` removes its handler after first invocation; `Off` removes registered callbacks; `Trigger` uses the same in-memory dispatch path for deterministic C# orchestration.

---

# Typed and delegated events — research

## Boundary

Blazor event callbacks carry typed event arguments but do not expose the browser target's library key. Delegation therefore needs a read-only JS function that walks from `event.target` to the closest `data-rbq-key`; it returns only that key. C# validates registry membership, scope, and selector matching before invoking a registered delegated handler.

## Constraints

- No JS-managed listener registration, mutation, selector matching, or handler storage.
- Typed handlers must render as Blazor `EventCallback<TEventArgs>` bindings.
- Delegated routing must reject a target outside the delegating root or scope.

---

# Typed event arguments — research

## Boundary

Typed browser callbacks must be rendered as the matching Blazor `EventCallback<TEventArgs>` determined by the normalized DOM event name. A generic runtime callback cannot safely replace that mapping without reflection or a loose `object` payload, both of which weaken the public contract.

## Acceptance checklist

1. `On<TEventArgs>` and `Trigger<TEventArgs>` retain the concrete event-argument object through C# dispatch.
2. Typed and untyped handlers registered for the same event both run from one rendered Blazor binding.
3. Typed registration rejects a mismatched event-argument type before mutating state.
4. Existing untyped event APIs remain source-compatible.

---

# Event registration lifecycle — research

## Boundary

Event registrations are keyed to an owned element identity. Destroying that identity must remove untyped, typed, and delegated-root registrations in the same state transition; registry removal alone cannot release callbacks retained by `QueryRenderState`.

## Acceptance checklist

1. Disposing a mounted real `QueryElement` removes every event registration for its key.
2. Removing a virtual node removes its own registrations and recursively removes those of virtual descendants.
3. Removing one key does not clear registrations for another live element.

---

# Native event descriptor completion — research

## Acceptance checklist

1. Use the native Blazor argument types for every supported event helper and reject unsupported typed event names.
2. Ensure `Submit` uses Blazor's actual submit argument type.
3. Define `Trigger<TEventArgs>` so it cannot silently discard the payload or unexpectedly invoke untyped handlers.
4. Test that each typed descriptor emits the expected callback type, including form and mouse/keyboard cases.

---

# Typed event helper APIs — research

## Acceptance checklist

1. Existing event helpers expose typed callback overloads for their matching Blazor event argument type.
2. Existing parameterless trigger helpers remain unchanged.
3. Component tests prove mouse, keyboard, and change helper overloads deliver native payloads.
4. Generator metadata and API docs identify the typed helper surface.

---

# Event compatibility aliases — research

## Acceptance checklist

1. `Bind` and `Unbind` support both untyped and typed callbacks and preserve `On`/`Off` removal semantics.
2. `TriggerHandler` has a typed overload that dispatches only typed callbacks for the selected elements.
3. Existing parameterless `TriggerHandler` behavior remains source-compatible.
4. Generated metadata, README, and project plan identify these compatibility APIs.

---

# Hover event helper completion — research

## Acceptance checklist

1. `Hover` accepts typed enter and leave callbacks using Blazor `MouseEventArgs`.
2. The typed overload composes with `mouseenter` and `mouseleave` dispatch without changing the existing `Action` overload.
3. Rendered component coverage proves both event payloads arrive through Blazor callbacks.
4. Generated metadata and the README event support checklist reflect the implementation.

---

# Select event helper completion — research

## Acceptance checklist

1. `SelectJS` exposes a typed `EventArgs` overload for Blazor's native `onselect` event.
2. The existing parameterless synthetic trigger remains unchanged.
3. A rendered component test proves native select-event dispatch.
4. Documentation describes this as a Blazor event helper, not a JavaScript escape hatch.

---

# Awaitable native event handlers — research

## Acceptance checklist

1. `On` and `One` accept `Func<Task>` and `Func<TEventArgs, Task>` handlers while preserving existing synchronous overloads.
2. `OffAsync` removes the exact asynchronous handler without making legacy `Off(eventName, null)` ambiguous; one-shot async handlers are removed before their first awaited dispatch.
3. Native Blazor event callbacks await asynchronous handlers and preserve native event arguments.
4. Existing synchronous `Trigger` behavior remains unchanged; explicit `TriggerAsync` dispatches async registrations.
5. Event-name discovery and element cleanup include asynchronous registrations.

## Bounded target inventory

- `QuerySelection`: public async event registration/removal and synthetic async dispatch.
- `QueryRenderState`: async handler storage, exact removal, one-shot lifecycle, discovery, dispatch, and cleanup.
- `QueryEventDescriptors` and `QueryElement`: await `Task` from native Blazor callbacks.
- Unit and bUnit suites: async dispatch, removal, one-shot behavior, native payload, and callback completion.
- Source-generator metadata and README/project plan: identify the awaitable event surface.

## Existing conventions and risk

Event handlers are split between parameterless `Action` and typed `Action<TEventArgs>` dictionaries. Native descriptors previously called synchronous `Trigger`. Async callbacks must not be adapted to `async void`; Blazor's `EventCallback<T>` must receive the task. Legacy synchronous `Trigger` must never block waiting for async handlers. Delegated event handlers are a separate API and remain outside this batch.
