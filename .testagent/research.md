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

---

# Sprint 5 research — first increment

## Bounded scope

Start Sprint 5 with `QuerySelection.FadeTo(TimeSpan duration, double opacity)`. This is a synchronous, render-state-only effect: store a CSS transition and target opacity against each resolved generated element key. It does not add timers, JavaScript writes, queue semantics, or claim to implement the remaining Sprint 5 APIs.

## Current architecture and test conventions

- `QuerySelection` resolves concrete keys and writes per-element CSS through `QueryRenderState.SetCss`.
- `QueryRenderState.BeginBatch()` coalesces multiple state mutations into one `Changed` notification.
- `QueryHost` renders `QueryRenderState.Rules` into a Blazor-owned `<style>` element.
- `QueryGeneratedMetadata.SupportedOperations` is a generator-maintained public API inventory.
- Unit tests use xUnit and test state through internal-access helpers; `QueryClassesAndStructureTests` contains effect-adjacent fluent API/metadata coverage.
- Component rendering tests use bUnit in `Reflections.Blazor.Query.ComponentTests`.

## Acceptance checklist

1. `FadeTo` writes the target opacity and duration transition to each selected element's generated-key CSS rule.
2. A multi-element `FadeTo` operation emits one coalesced state-change notification.
3. Reject invalid opacity (non-finite or outside [0, 1]) and negative duration before mutating state.
4. Generated operation metadata advertises `FadeTo`.
5. Blazor component render output includes the state-owned transition and opacity rules without direct DOM mutation.
6. Empty selections remain no-ops, and generated CSS numeric values remain invariant under non-English cultures while preserving `TimeSpan` tick precision.
7. Both valid opacity endpoints (0 and 1) and zero duration are accepted.

## Constraints / non-goals

- No JS interop, timer, animation queue, cancellation, or promise behavior in this increment.
- No implementation claim for `Animate`, `FadeIn`, `FadeOut`, `FadeToggle`, `Slide*`, or queue APIs.
- Preserve existing user edits in README and `docs/PROJECT-PLAN.md`.

---

# Sprint 5 second increment research — FadeIn and FadeOut

## Bounded scope and observed architecture

Implement the next Sprint 5 slice: `QuerySelection.FadeIn(TimeSpan)` and `FadeOut(TimeSpan)`. `QueryHost` renders state-owned style rules, while `QueryRenderState` owns hidden keys and batches notifications. The effects must stay within these existing Blazor-owned paths; no browser DOM write, timer, or JS bridge is included.

`FadeOut` uses opacity/visibility keyframes and intentionally retains the element's layout slot. `FadeIn` clears the library's hidden flag before applying the animation. Explicit animation CSS must not be removed when the library cancels one of its own effects.

## Acceptance checklist

1. Both operations apply a per-generated-key CSS animation declaration and appear in generated operation metadata.
2. Multiple matched keys update in one render-state batch; zero matches remain a no-op.
3. `FadeIn` removes library hidden state; `FadeOut` does not hide/remove layout.
4. `Show` and `Hide` cancel an effect-owned animation, while explicit animation CSS remains user-owned.
5. Negative durations throw before any state mutation; zero duration and invariant CSS formatting are supported.
6. `QueryHost` output contains static keyframes and the per-key animation declaration.
7. README and project plan accurately mark delivered APIs and the retained-layout behavior.

---

# Sprint 5 third increment research — FadeToggle

## Bounded scope

Implement `QuerySelection.FadeToggle(TimeSpan)` as a toggle over Query-managed visibility only. It must not query computed browser style or infer visibility from external CSS. Preserve the existing distinction between `Hide()` (layout removal) and `FadeOut()` (visibility/opacity animation retaining layout).

## Relevant contracts and acceptance checklist

1. Query state remembers that `FadeOut` has made an element visually hidden after the CSS animation, without setting `Hide()` layout-removal state.
2. `FadeToggle` selects FadeIn for Query-managed hidden elements (`Hide` or prior `FadeOut`) and FadeOut otherwise.
3. The operation applies per-key rules in one batch, supports zero duration, rejects negative duration before mutation, and is a no-op for empty selections.
4. `FadeIn`, `Show`, and `Hide` synchronize/clear the managed fade visibility marker so repeated toggles remain deterministic.
5. Generated metadata and QueryHost render output cover the new operation.
6. Update README and project plan without rewriting historical `.testagent` entries or pre-existing user edits.

---

# Sprint 5 fourth increment research — Animate

## Bounded scope

Implement a synchronous `Animate(IReadOnlyDictionary<string, string>, TimeSpan)` that writes target CSS values and a per-property transition declaration into generated-key render state. It is not a timer-driven API and will not expose completion/cancellation semantics; those require a separate queue/lifecycle design. CSS values are caller-supplied, consistent with `Css`.

## Acceptance checklist

1. Applies all validated target properties and the corresponding transition to every resolved key.
2. Coalesces multi-property/multi-element updates into one state notification.
3. Rejects null maps, invalid property names/values, and negative durations before mutation; empty maps/selections do not mutate.
4. Uses deterministic property ordering and invariant duration formatting.
5. Advertises `Animate` in generated metadata and renders target CSS through QueryHost.
6. Documents that browser transitions are not awaited and CSS values are caller supplied.

---

# Sprint 5 fifth increment research — Queue, Dequeue, ClearQueue

## Bounded scope and queue contract

Add per-element, named asynchronous action queues to `QueryRenderState`. `Queue` enqueues without starting; `Dequeue` explicitly starts draining the queue sequentially and awaits completion; `ClearQueue` clears pending entries but does not cancel an active callback. This makes execution and exceptions observable rather than launching unobserved async work. Token-aware callbacks receive a cancellation token so state teardown can signal cancellation; plain `Func<Task>` callbacks are not preemptible. `Stop` remains a future public API.

Queue state is non-rendering orchestration state: enqueue/dequeue must not emit render notifications. `Clear()` and element removal cancel active work and release pending work. Default queue name is `fx`. `Delay`, stop/finish, promises, and automatic jQuery `fx` start behavior are out of scope here.

## Acceptance checklist

1. Enqueue accepts async actions and preserves per-element order; elements have independent queues.
2. Queueing does not run actions; explicit Dequeue drains sequentially and awaits completion.
3. Concurrent Dequeue requests for the same queue do not run callbacks concurrently.
4. ClearQueue removes pending work only and leaves an active action running.
5. Render-state Clear and selected element removal cancel active action tokens and discard queued work.
6. Queue name and callback validation occur before queue mutation; missing/empty queues are safe no-ops.
7. Callback faults propagate from Dequeue, leave later queued entries available for a subsequent Dequeue, and do not strand the queue in a running state.
8. Generated operation metadata and README/project plan reflect only this delivered slice.

Static pairing: Roslyn pairing reports `QueryRenderState.cs` paired to `QueryEffectsTests.cs` among other tests. This heuristic is not line/branch coverage evidence.

---

# Sprint 5 sixth increment research — Delay

Implement `Delay(TimeSpan, string queueName = "fx")` as a queue entry backed by `Task.Delay(duration, cancellationToken)`. It is a deterministic queue primitive, not an independently scheduled timer: it starts only when `Dequeue` drains the queue, observes teardown cancellation through the existing token, and leaves completion/error reporting on the existing awaited drain. Reject negative duration and blank queue name before mutation; allow zero duration; empty selection remains a no-op. `Stop`, `Finish`, promises, slide effects, and automatic queue start are out of scope.

---

# Sprint 5 seventh increment research — Stop

## Scope and existing constraints

`QueryRenderState` owns per-element named queues. `Dequeue` awaits a sequential drain; `Queue(Func<CancellationToken, Task>)` can cooperate with cancellation while `Queue(Func<Task>)` cannot be preempted. `ClearQueue` removes pending entries but deliberately leaves current work alone. Queue state does not emit render changes.

## Stop contract

Add `Stop(bool clearQueue = false, string queueName = "fx")` as an awaitable operation. It signals cancellation to the current drain and awaits its termination, so subsequent dequeue cannot race the prior callback. By default, pending actions are preserved and become dequeueable after Stop completes. With `clearQueue: true`, pending actions are discarded. A tokenless active callback is not preemptible, so Stop waits for it to complete naturally; a token-aware callback can terminate cooperatively. No jump-to-end behavior is promised for arbitrary callbacks. State/element teardown always discards work and must not preserve pending actions.

## Acceptance checklist

1. Stop cancels an active token-aware callback, awaits drain termination, and preserves pending work by default.
2. Stop with `clearQueue: true` cancels active work and discards pending entries.
3. Stop awaits tokenless work instead of allowing concurrent action execution.
4. Stopping an inactive queue is safe; optional clearing applies to pending-only queues.
5. Queue name validation and generated metadata are verified; Stop does not emit render notifications.

---

# Sprint 5 eighth increment research — Finish

## Existing structure and contract

`QueryRenderState` currently stores queue delegates, and `Delay` is implemented as a cancellable queue item. `Stop` cancels the active drain and optionally preserves or clears pending work. `Finish` must remain distinct: it should not cancel the whole queue or alias `Dequeue`.

## Finish-aware work item model

Represent queue entries as work items with an execution delegate and an optional finish delegate. Preserve existing `Queue` overloads as non-finishable work. Add an overload that accepts both run and finish delegates. `Delay` is finishable with a no-op completion, so pending delays are skipped. `Finish` marks the queue as finishing, cooperatively cancels an active finishable item, awaits its run delegate to stop, invokes its finish delegate exactly once, then processes pending items in finish mode: finish delegates are used when supplied, otherwise ordinary callbacks run normally. Tokenless active callbacks cannot be preempted; Finish awaits them and then continues. Queue teardown still cancels and discards work. At the selection level, Finish also settles library-owned CSS effects: remove owned transition/animation declarations and write fade terminal opacity/visibility while preserving Animate/FadeTo target CSS values. Explicit caller-owned animation/transition declarations remain untouched.

## Acceptance checklist

1. Pending finishable work invokes its finish action rather than its regular action.
2. Active token-aware finishable work is canceled, awaited, then its finish action runs exactly once.
3. Delay entries are skipped in finish mode, while pending ordinary callbacks still run.
4. Tokenless active work is awaited, never run concurrently with its finish delegate.
5. Empty/inactive queues and invalid callback/name inputs behave safely; Finish awaits all selected queues and remains non-rendering.
6. Generated operation metadata and README/project plan describe this library-specific contract, not arbitrary forced completion.

---

# Sprint 5 ninth increment research — Promise

## Target inventory and conventions

The Promise slice is limited to `QueryRenderState` queue lifecycle, `QuerySelection`, the source-generator operation inventory, queue unit tests, and Sprint 5 docs. Existing queue tests use xUnit, explicit `TaskCompletionSource` gates, and assert both positive and negative states. Queue operations are intentionally non-rendering; `Dequeue` is the only explicit start operation.

## Contract and acceptance checklist

`Promise(string queueName = "fx")` asynchronously observes the selected keys' current named queue completion. It never starts work. No queue or an empty selection completes immediately. If work is queued, it resolves when the queue becomes empty; callback exceptions remain reported by `Dequeue` and do not turn this into a success-result API. If a failed drain leaves pending actions, the promise remains pending until a retry empties the queue or an explicit clear discards remaining work. `ClearQueue`, `Stop(clearQueue: true)`, and `Finish` resolve when they leave no active or pending work; `Stop` preserving pending work carries the same promise across its resumed queue. Render-state/element teardown cancels outstanding promises so they cannot dangle. Validate queue name before observation and advertise Promise in generated metadata.

Targets: `src/Reflections.Blazor.Query/Rendering/QueryRenderState.cs`, `src/Reflections.Blazor.Query/Selection/QuerySelection.cs`, `src/Reflections.Blazor.Query.Generators/QueryRenderStateGenerator.cs`, `tests/Reflections.Blazor.Query.Tests/QueryEffectQueueTests.cs`, `README.md`, and `docs/PROJECT-PLAN.md`.

---

# Sprint 5 tenth increment research — measured slide effects

## Target inventory and conventions

`QuerySelection` has the measurement coordinator and queue state already injected. `QueryMeasurementCoordinator` batches requests through `QueryHost.OnAfterRenderAsync`; `queryMeasurement.js` performs read-only DOM queries and currently supports element dimensions/positions. Queue work is explicit-start and supports cancellation/finish callbacks. `QueryElement` routes only registered Blazor event descriptors, including typed event arguments. Render-state CSS is emitted through `QueryHost` from selector/property/value rules.

The repository pairing scan maps `QuerySelection` and `QueryRenderState` to unit/component suites, while event descriptor and measurement contract details are not independently paired. Tests use xUnit and bUnit with fake measurement bridges.

## Contract and acceptance checklist

`SlideDown`, `SlideUp`, and `SlideToggle` synchronously enqueue default-`fx` work and return the same selection; none starts until `Dequeue`. Slide work invalidates stale metric cache, reveals Query-hidden content in a clipped/hidden measurement state, reads `scrollHeight` via the read-only batched bridge, then animates via library-owned CSS. A namespaced Blazor custom event registered by the RCL JS initializer maps native `animationend` to the renderer event pipeline, with propagation stopped at the owned element. Natural slide-down completion removes temporary height constraints; slide-up hides only at completion. `Finish` jumps to the direction's terminal state, `Stop` cancels and restores the pre-slide visibility, and owner teardown cancels outstanding work. Empty selections enqueue nothing; invalid duration/name is rejected before mutation. CSS overlays must preserve caller-owned style declarations, and animation completion must not leak from nested elements.

Targets: measurement contracts and JS read bridge, event descriptors, render-state style overlays, selection queue API, QueryHost keyframes, unit/component tests, generated metadata, README, and project plan. No direct DOM mutation or timer-based completion is permitted.

---

# Sprint 5 eleventh increment research — browser smoke evidence

The repository has Server, WebAssembly, and Auto demos but no dedicated browser automation project. The installed Playwright CLI can drive a real isolated browser without adding a test dependency. A durable Server demo affordance is therefore appropriate as the first smoke target; it does not replace automated E2E coverage for all render modes.

The JS initializer and measurement module are emitted as fingerprinted RCL static web assets. Runtime evidence must check those URLs load successfully, exercise the slide's native completion event, and confirm the state returns to natural layout/terminal visibility. Do not use a pre-existing user browser profile or infer Auto's runtime from its URL alone.

---

# Sprint 5 reconnect and async-slide increment research

The accepted slide API contract is awaitable direct operations (`ValueTask<QuerySelection> SlideDown/SlideUp/SlideToggle(TimeSpan)`); explicit queue composition remains available as `QueueSlideDown/QueueSlideUp/QueueSlideToggle`. The Server demo's standard reconnect dialog emits `components-reconnect-state-changed`. A reconnect alone is not proof that a CSS animation has ended; the RCL must inspect the relevant `CSSAnimation.playState` and replay completion only for finished Query slide animations. Browser evidence uses a controlled SignalR WebSocket interruption plus browser-native `animationend` observations. Rejected/expired circuits remain teardown, not recovery.

Acceptance checklist: (1) an animation still active through reconnect continues and completes naturally; (2) a native slide animation ending while disconnected remains pending until reconnect and is completed exactly once; (3) no JS style or DOM-tree mutation; (4) API docs and generator metadata distinguish direct async calls from explicit-queue methods. The `tests/BrowserE2E` package uses Playwright Test and the isolated Chromium installation, starts the Server demo itself, and stores traces under ignored `output/playwright/`.

---

# Sprint 2 measurement-scope and browser coverage — research

## Scope

Close Sprint 2's measurement acceptance gap without changing the read-only JS boundary: expose an explicit application-lifetime render/query scope, prove page/layout/application measurement-cache isolation and invalidation, and automate browser measurements across Server, WebAssembly, and Auto.

## Target inventory

- Production: `Query.cs`, `QueryRenderState` registrations, `QueryMeasurementCoordinator`, `QueryHost` lifetime behavior, existing read-only `queryMeasurement.js`.
- Tests: `QueryMeasurementCoordinatorTests`, QueryHost component tests, and `tests/BrowserE2E` Playwright suites.
- Hosts: Server `5184`, WebAssembly `5185`, Auto `5186`; existing test routing excludes host-specific specs.

## Existing conventions and findings

- xUnit coordinator tests use deterministic fake/delayed bridges and `QueryRenderState` identity as the measurement cache key.
- QueryHost flushes after render and invalidates its active state on navigation/disposal. App scope must therefore be opt-in and navigation-persistent, while retaining disposal invalidation.
- Measurement code has no application-specific public state/query type; register an application state/query scoped to the Blazor circuit/client lifetime.
- JS uses generated keys and enum measurement kinds, with no writes. Existing browser tests do not exercise the measurement metrics; current proof pages cover only selected metrics manually.
- `find-untested-sources` static pairing reports 32 source files / 22 test files, with `QueryRenderState`, `Query`, and `QuerySelection` paired to the measurement/scope suites. This is a static symbol-pairing heuristic, not coverage data.

## Acceptance checklist

1. `ApplicationQueryRenderState` and `ApplicationQuery` are available through `AddBlazorQuery`, isolated from page/layout state, and retain measurements across page/layout invalidation until explicit application invalidation/disposal.
2. Browser coverage exercises Height, Width, Inner/Outer dimensions, Offset, Position, and owned OffsetParent in Server, WebAssembly, and Auto with deterministic owned elements and read-only assertions.
3. Existing page/layout cancellation, invalidation, disposal, and late-result tests remain valid; add application-scope lifecycle assertions.
4. Update Sprint 2 status, measurement boundary, and README/plan so no stale unchecked item contradicts evidence.

---

# Sprint 5 host performance — research

- Existing demos and `RBQ_E2E_HOST` provide separate Server, WebAssembly, and Auto launch paths; keep the harness in the existing Playwright package and artifacts under `output/playwright/<host>`.
- `QueryMeasurementCoordinator` has no timing diagnostics. Do not add benchmark-only public API or perturb production JS with permanent instrumentation; time the awaited query calls in sample-only routes.
- Each run must use a distinct generated element key to force a cache miss. Queue the dimensions/position requests concurrently so one `QueryHost.OnAfterRenderAsync` measurement batch serves each sample.
- Report (a) .NET handler elapsed around the awaited read, (b) browser capture-click to MutationObserver render elapsed, and (c) direct browser-only `getBoundingClientRect()` loop separately. Auto must produce independent Server and cached-WASM groups after reload and prove the runtime by whether a new SignalR circuit appears.
- Attribution limits: Server handler elapsed includes coordinator work and JS-interop request/response over the SignalR circuit; it is not pure server CPU or isolated transport latency. Browser click-to-render is end-to-end and includes event delivery, rendering, and browser DOM application. WebAssembly handler elapsed includes client .NET and JS interop. The browser-only loop isolates local layout-read overhead, not the full library operation.
- Acceptance checklist: deterministic unique-key miss per trial; 25 samples per runtime path; verify result correctness; calculate median and p95; attach raw samples and summary; execute for Server, WebAssembly, Auto Server, and Auto cached WebAssembly; label all metrics and inference limits.
- Static source/test pairing was run with the tree-sitter skill analyzer (31 source and 27 test files); the new TypeScript E2E spec is expected to be classified as an orphan because it drives Razor-hosted UI rather than same-language declarations. This pairing result is not coverage evidence.
