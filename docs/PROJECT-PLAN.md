# Reflections.Blazor.Query — Implementation Plan

This plan follows the Azure DevOps hierarchy and repository delivery policy. Sprints 0–4 are complete in the repository; Sprint 4 is formally closed. Sprint 5 (Feature 4470: effects and queues) implementation and local validation are complete. Remote CI/artifact publication and deployment are release operations, not missing library APIs.

## ✅ Current Phase: Sprint 5 (Feature 4470)

**Current status:** Library implementation is complete. Remaining actions are remote CI/artifact evidence and release/deployment operations.

- **Status:** Local implementation and acceptance criteria are complete. `Animate`, fades, measured slide effects, and queue APIs through `Promise` are implemented vertical slices.

**Sprint 5 exit gates (ordered):**

1. [x] Manual real-browser slide smoke in Interactive Server, WebAssembly, and Auto. Verified RCL JS assets, `rbqanimationend`, measured height, and Blazor completion; Auto was observed first on Server then on WebAssembly after cached-runtime reload.
2. [x] Add and locally execute the browser suite: Server 8/8, WebAssembly 6/6, and Auto 6/6 scenarios passed locally. **Release gate (outside library implementation):** Run the Azure Pipeline against the release revision and retain its published JUnit/performance/diagnostic artifacts.
3. [x] Add repeatable in-process baselines for batched measurement and render-state effects at 1/10/100/1000 keys, plus host-specific 25-sample browser baselines for Server, WebAssembly, and Auto Server-to-cached-WebAssembly; record scope and timing attribution limits in [Performance Baseline](PERFORMANCE-BASELINE.md).
4. [x] Resolve the current NuGet advisories by updating ASP.NET Core component packages to 10.0.13; Release solution build and package archive validation passed locally. **Release gate (outside library implementation):** Capture and verify immutable artifact identity through the remote CI publication workflow.

Local validation on 2026-10-15: 257/257 .NET tests passed; browser E2E passed Server 8/8, WebAssembly 6/6, and Auto 6/6; Release solution build completed with zero warnings/errors; DocFX generated the site with zero warnings/errors; runtime and standalone generator packages passed archive/license validation at version 1.0.0. Browser performance sampling ran in all three host configurations. Server automates queue/reconnect behavior; WebAssembly and Auto automate measured slide toggles, explicit sequential queue execution, delegated custom-event routing, navigation, measurement, and performance. Remote CI/artifact publication remains a separate release gate.

Remote status last checked on 2026-10-14: the first pipeline request (build 833) correctly targeted the pushed commit but failed pipeline validation because this Azure DevOps Server has no hosted `Azure Pipelines` pool. The project has a `Default` self-hosted pool; its registered agent was offline at that check. The YAML now targets `Default`. Work item 4470 (“Effects and queues”) remains **New**, assigned to Omar El Sergany. Re-run CI after the configured agent is online and verify the published browser/performance and runtime package artifacts. This is external release evidence, not an unimplemented library feature.

### Implemented matrix

- **Attr / RemoveAttr:** Manages render-owned attribute state for each resolved element key.
- **Prop / RemoveProp:** Tracks typed property state and protects reserved keys and event-like names.
- **Data / RemoveData:** Stores JSON-normalized library state isolated from DOM `data-*` attributes.
- **Text:** State-driven text content rendered with Blazor HTML encoding.
- **Html:** Explicit caller-supplied markup rendered through the component tree; this is a documented trust boundary.
- **Val:** Owns and reads the rendered `value` property. Blazor two-way event binding remains a future forms concern.
- **Clear:** Atomically clears styles, attributes, properties, data, and content state.

### Immediate backlog

- [x] Implement `OffsetAsync`, `PositionAsync`, and `OffsetParentAsync` under the existing read-only batch boundary.
- [x] Implement the generated-key measurement queue, coalesced `QueryHost.OnAfterRenderAsync` flush, immutable snapshot DTOs, and no-public-`ElementReference` boundary.
- [x] Implement render-owned `Height(double)` and `Width(double)` writes using generated-key CSS rules.
- [x] Add page/layout/application measurement-scope coverage for navigation/disposal invalidation, cancellation, cache retention, and late-result rejection. Application scope uses explicit `ApplicationQueryRenderState` / `ApplicationQuery` services; application cache is retained until explicit invalidation or its owning host/service scope is disposed.
- [x] Manually verify the `HeightAsync` / `WidthAsync` queue and JS module in Interactive Server, WebAssembly, and Auto.
- [x] Manually verify typed `OffsetAsync`, `PositionAsync`, and external-parent `OffsetParentAsync` results in Interactive Server, WebAssembly, and Auto.
- [x] Add automated browser coverage for every public measurement metric in Server, WebAssembly, and Auto; unit/component tests cover navigation, disposal, cancellation, cache invalidation, and late-result rejection.

### 🗺️ Long-term roadmap (Epic 4464)

#### Sprint 3 — Classes & DOM Structure (Feature 4468) — Complete

- [x] Class manipulation: `AddClass`, `RemoveClass`, `ToggleClass`, `HasClass`, `Show`, `Hide`, and `Toggle`.
- [x] Structural state operations: append/prepend, sibling insertion and relocation, replacement, wrapping, removal, emptying, and detachment for supported real and virtual elements.
- [x] Stable element identity, ordered state reconciliation, and disposal behavior for relocation and nested virtual elements.
- [x] Logical component query isolation using `IQueryScope`/`QueryScopeHost`; no per-component DI lifetime.
- [x] `Clone()` for library-created virtual elements; clones receive unique library keys and start unattached.

**Explicit boundary:** `Clone()` does not clone a real Razor-declared `QueryElement`. A Razor element is a live Blazor component instance with its own parameters, callbacks, and lifecycle; synthesizing a second live instance from its rendered output would not preserve those semantics. This is intentionally unsupported, not a missing generated-key feature. Use a reusable Razor component or create a virtual element when a second instance is required.

#### Sprint 4 — Events & Forms (Feature 4469) — Complete

- [x] Fluent event binding: `On`, `Off`, `One`, `Trigger`, and awaitable `TriggerAsync`, with `OffAsync`/`UnbindAsync` for async delegate removal and native `TEventArgs` overloads for supported Blazor event descriptors.
- [x] Event helpers: `Blur`, `Change`, `Click`, `ContextMenu`, `DblClick`, `Focus`, `FocusIn`, `FocusOut`, `Hover`, keyboard and mouse helpers, `Resize`, `Scroll`, and `Submit`; both synchronous and `Task`-returning overloads are supported, with native Blazor event args where available.
- [x] Compatibility aliases: `TriggerHandler`, `Bind`, and `Unbind`, including typed and awaitable callback overloads; `UnbindAsync` removes awaitable delegates without introducing `null` overload ambiguity in the legacy API.
- [x] Delegated routing: `DelegateJS` / `Undelegate` with host pre-start registration and C# root/scope/selector checks.
- [x] Form-state handlers: `Serialize` and `SerializeArray` — read-only form observation (`SerializeAsync` / `SerializeArrayAsync`) via the queued, coalesced, read-only JS boundary defined in [Measurement and Position Boundary](MEASUREMENT-BOUNDARY.md). jQuery "successful control" semantics; no DOM mutation. Coordinator disposal is idempotent and serialized behind active bridge reads.

#### Sprint 5 — Effects & Queues (Feature 4470)

- [x] Add synchronous `FadeTo(TimeSpan, double)` as a batched, Blazor-rendered CSS opacity transition with validated inputs.
- [x] Add `FadeIn(TimeSpan)` and `FadeOut(TimeSpan)` as batched per-key CSS keyframe state rendered through `QueryHost`; `FadeOut` preserves layout, and `Show`/`Hide` cancel library-owned fade animations.
- [x] Add `FadeToggle(TimeSpan)` over Query-managed visibility state, without reading computed styles or external DOM.
- [x] Add `Animate(IReadOnlyDictionary<string, string>, TimeSpan)` as a batched per-key CSS transition and target-state update; transition completion is not awaited.
- [x] Add named per-element callback queues (`Queue`, `Dequeue`, `ClearQueue`, `Delay`) with explicit start, awaited sequential draining, pending-only clear, and cancellation signaling on state/element removal for token-aware callbacks.
- [x] Measured slide effects: awaitable `SlideDown`, `SlideToggle`, and `SlideUp` start and complete directly; `QueueSlideDown`, `QueueSlideToggle`, and `QueueSlideUp` provide explicit queue composition. Effects read `scrollHeight` through the read-only measurement batch and render Blazor-owned keyframes. Temporary styles are removed at completion.
- [x] Server circuit reconnect recovery: on successful reconnection, the RCL checks Query slide animations and re-dispatches completion only for browser-confirmed finished animations. An animation still running is not forced to completion; no DOM styles are written.
- [x] Queue tracking and cancellable delay: `Queue`, `Dequeue`, `ClearQueue`, and `Delay`.
- [x] Add `Stop(bool clearQueue = false, string queueName = "fx")`: signal cancellation, await the active drain, and preserve pending callbacks unless clearing is requested. Tokenless callbacks finish naturally.
- [x] Add finish-aware work items and `Finish(string queueName = "fx")`: execute supplied completion delegates, skip queued delays, run non-finishable callbacks normally, and settle library-owned fade/transition state at its target. `Show`/`Hide` clear finish-owned fade styles.
- [x] Add `Promise(string queueName = "fx")`: observe currently queued work without auto-start; resolve on queue emptiness/explicit clearing and cancel on owner teardown. Callback failures remain reported by `Dequeue`.
- [x] Manual browser smoke: verify slide up/down completion and JS initializer in Interactive Server, WebAssembly, and Auto; verify Auto's initial Server and cached WebAssembly executions.
- [x] CI integration for browser E2E: the Azure Pipeline runs the Playwright Server reconnect suite and publishes JUnit results/failure traces.
- [x] Automate Server slide/reconnect, navigation, element disposal, queue cancellation, and expired-circuit cases; automate navigation lifecycle on WebAssembly and Auto.
- [x] Automate measured `SlideToggle` down/up completion and temporary-style cleanup in WebAssembly and Auto (`client-effects.spec.ts`).
- [x] Automate explicit-dequeue and sequential queue execution in WebAssembly and Auto; Auto repeats the interaction after warm reload on cached WebAssembly (`client-queue.spec.ts`).
- [x] Automate the delegated custom-event initializer path in WebAssembly and Auto: pre-start registration, nested element-key routing, selector filtering, and Auto's warmed WebAssembly reload (`client-delegation.spec.ts`).
- [x] Server reconnect browser E2E: a controllable SignalR disconnect verifies an in-progress animation continues and a browser-confirmed completion while disconnected settles after reconnection.
- [x] Server navigation browser E2E: page-scoped CSS is cleared when navigating away, while layout-scoped CSS remains rendered.
- [x] Server component-disposal browser E2E: removing a `QueryElement` unregisters its selector metadata; a subsequent query resolves zero matches.
- [x] Server queue-disposal browser E2E: disposing the element that owns active token-aware queued work cancels the callback and removes the element.
- [x] Nested-host component lifecycle: renderer-driven removal of an inner `QueryHost` cancels its active token-aware queue and unregisters its element while preserving the outer layout's rendered state.
- [x] Server expired-circuit browser E2E: after the test-configured retention window, the active queue is not resumed and the page reports its fresh `idle` state.
- [x] WebAssembly browser E2E: applying page-scoped CSS, navigating away, and returning confirms the old page state is cleared and not replayed.
- [x] Auto browser E2E: verifies a fresh visit uses a Server circuit, a warm reload handles interactions without opening a new circuit (cached WebAssembly), and page state is cleared on navigation and not replayed on return.
- [x] Performance core baseline: BenchmarkDotNet coverage for measurement request coalescing and render-state `Animate` updates over 1/10/100/1000 keys, with allocation and latency snapshots recorded in [Performance Baseline](PERFORMANCE-BASELINE.md).
- [x] Performance host baselines: recorded browser/Blazor render and interop measurements separately for Interactive Server (including circuit transport), WebAssembly, and Auto's Server-to-cached-WebAssembly path; limits and attribution caveats are documented in [Performance Baseline](PERFORMANCE-BASELINE.md).
- [x] Dependency/release local gate: resolve NU1903 advisories, build Release with zero warnings/errors, and validate the runtime package contains its README and static web assets.
- [ ] **Release operations:** verify a successful Azure Pipeline run and immutable published artifact identity for the release revision. This is outside the library implementation completion measure.

## Product goal

Build a Blazor-native, jQuery 4-aligned query and manipulation library that exposes a fluent API while keeping mutations in library-owned render state. The implementation must not manipulate `RenderTreeFrame` or directly mutate DOM nodes owned by Blazor. The source generator will generate metadata and render-state integration code from declarative API definitions.

Supported hosting targets:

- Interactive Server
- Interactive WebAssembly
- Interactive Auto
- Static SSR for declarative output where no interaction is required

Ajax is explicitly out of scope. Blazor `HttpClient`, forms, routing, and event callbacks are the supported equivalents.

## Architecture boundaries

1. `Reflections.Blazor.Query` contains the public fluent API, render-state model, scope management, and Blazor components.
2. `Reflections.Blazor.Query.Generators` contains the Roslyn incremental generator and generated API metadata.
3. `samples/Reflections.Blazor.Query.Demo` is the interactive compatibility laboratory.
4. `tests/` will contain unit, generator, render-state, component, and browser-level tests.
5. `docs/` contains the API matrix, architecture decisions, compatibility notes, and release evidence.
6. `deploy/` contains Azure DevOps pipeline definitions and promotion scripts.

## State and rendering model

- Page scope: cleared on navigation.
- Layout scope: survives navigation while the layout remains active.
- Application scope: optional, explicit, and long-lived.
- Reusable component isolation is implemented as a logical query capability using cascaded `IQueryScope` keys registered by `QueryElement`; it is not a nested DI scope or a security boundary. See [Component Query Scoping](COMPONENT-SCOPING.md). Unit and bUnit coverage includes repeated/nested-host isolation, mutation confinement, and element unregister/re-register during rerender.
- Every operation updates state and produces Blazor-owned render output.
- No operation writes to a Blazor-owned DOM node through JavaScript.
- Browser layout metrics are an explicit read-only interop boundary. Measurement results are snapshots held in a dedicated library-owned measurement cache, not render-driving mutation state.
- Measurement interop identifies only generated `data-rbq-key` values through an isolated JS module; `ElementReference` and browser handles are not public query abstractions.
- Event subscriptions, queues, animation state, and async operations must have deterministic disposal and cancellation.
- Operations must define behavior for zero matches, one match, and multiple matches.

## API compatibility matrix

### Tier 1 - jQuery 4 core target

Implement and test these first:

`Add`, `AddBack`, `AddClass`, `After`, `Append`, `AppendTo`, `Attr`, `Before`, `Children`, `Clone`, `Closest`, `Css`, `Data`, `Detach`, `Each`, `Empty`, `End`, `Eq`, `Filter`, `Find`, `First`, `Get`, `Has`, `HasClass`, `Height_Get`, `Height_Set`, `Hide`, `Html`, `Index`, `InnerHeight`, `InnerWidth`, `InsertAfter`, `InsertBefore`, `Is`, `JQuery` (Blazor-safe alias for `Select`), `Last`, `Length`, `Map`, `Next`, `NextAll`, `NextUntil`, `Not`, `Offset`, `OffsetParent`, `On`, `OuterHeight`, `OuterWidth`, `Parent`, `Parents`, `ParentsUntil`, `Position`, `Prepend`, `PrependTo`, `Prev`, `PrevAll`, `PrevUntil`, `Prop`, `PushStack`, `Remove`, `RemoveAttr`, `RemoveClass`, `RemoveData`, `RemoveProp`, `ReplaceAll`, `ReplaceWith`, `Select`, `Serialize`, `SerializeArray`, `Show`, `Siblings`, `Slice`, `Text_Get`, `Text_Set`, `ToArray`, `Toggle`, `ToggleClass`, `Trigger`, `TriggerHandler`, `Val`, `Width_Get`, `Width_Set`, `Wrap`, `WrapAll`, `WrapInner`.

### Tier 2 - effects and queue modules

Implement after the core state model is stable:

`Animate`, `ClearQueue`, `Delay`, `Dequeue`, `FadeIn`, `FadeOut`, `FadeTo`, `FadeToggle`, `Finish`, `Promise`, `Queue`, `SlideDown`, `SlideToggle`, `SlideUp`, `Stop`.

Effects must use render-state transitions and CSS/Web Animations-compatible output. They must not rely on direct DOM mutation. Cancellation and navigation disposal are mandatory acceptance criteria.

### Tier 3 - event APIs

Implement with Blazor event descriptors and generated handler metadata:

`Bind`, `Blur`, `Change`, `Click`, `ContextMenu`, `DBLClick`, `Focus`, `FocusIn`, `FocusOut`, `Hover`, `KeyDown`, `KeyPress`, `KeyUp`, `MouseDown`, `MouseEnter`, `MouseLeave`, `MouseMove`, `MouseOut`, `MouseOver`, `MouseUp`, `Off`, `One`, `Resize`, `Scroll`, `Submit`, `Unbind`.

Compatibility note: `Bind` and `Unbind` are legacy jQuery names, implemented only as compatibility aliases over `On` and `Off`. `TriggerHandler` and its typed overload use local library-owned dispatch for the selected elements and do not bubble to a parent element.

Typed overloads (`On<TEventArgs>`, `Off<TEventArgs>`, and `Trigger<TEventArgs>`) are available for supported browser events. A single event descriptor maps the normalized event name to its Blazor callback type; mismatches and unsupported event names are rejected before state mutation. `Trigger<TEventArgs>` dispatches typed registrations only; parameterless `Trigger` retains the untyped synthetic event contract. Typed and untyped handlers are rendered through the same Blazor-owned event attribute.

Event-specific helpers also expose typed overloads for the same descriptor types. `Resize` and `Scroll` remain parameterless/untyped: Blazor supplies only generic event args for those events, while useful dimensions and offsets are served by the library's read-only measurement API.

Event registrations are lifecycle-owned state: removing a virtual element cascades registration cleanup to virtual descendants, and disposing a mounted `QueryElement` clears untyped, typed, and delegated-root registrations for that key. This avoids callback retention in long-lived layout or application render states.

### Tier 4 - compatibility boundary decisions

`Context` and `Ready` are documented as Blazor-specific alternatives below, not as jQuery-compatibility implementation work. `Get` is part of the implemented collection API, `JQuery` is a library-specific naming alias for `Select`, and `Error` / `Load` / `Unload` are supported as library-owned event helpers only—not as Ajax or document lifecycle interception. `SelectJS` is implemented as a normal Blazor-owned text-selection event with no JavaScript escape hatch.

`DelegateJS` / `Undelegate` are implemented as a constrained, safe delegation boundary: the host loads `_content/Reflections.Blazor.Query/queryEvents.js` after `blazor.web.js` with `autostart="false"`, registers the finite custom event set, then calls `Blazor.start()`. The initializer supplies only the nearest `data-rbq-key`; C# retains handler state and validates descendant, scope, and selector membership before invoking the handler. Only bubbling events are supported; it is not a general JavaScript escape hatch.

### Excluded or replaced APIs

Ajax is excluded: `AjaxComplete`, `AjaxError`, `AjaxSend`, `AjaxStart`, `AjaxStop`, `AjaxSuccess`, and the Ajax form of `Load`.

`Size` is excluded because [jQuery removed it in 3.0](https://api.jquery.com/size/); use `Length`. The `JQuery(selector)` alias is library-specific and accepts CSS selectors only; unlike [jQuery's constructor](https://api.jquery.com/jQuery/), it does not accept HTML strings, DOM objects, or document-ready callbacks.

### Blazor-specific alternatives (not jQuery API targets)

- `Context` — no separate jQuery-style context argument is planned; use `IQueryScope` for explicit component isolation.
- `Ready` — no jQuery-style DOM-ready API is planned; use Blazor component lifecycle methods such as `OnAfterRenderAsync`.

The following are not jQuery 4 core targets because they were removed long before jQuery 4 or are legacy compatibility APIs: `AndSelf`, `Die`, `Live`, `Delegate`, and `Undelegate`. Use `AddBack` and `On`/`Off` equivalents. `Bind`/`Unbind` remain compatibility aliases only.

The API matrix must be maintained as a versioned document and each item must have an implementation status: Proposed, Designed, Implemented, Tested, or Deferred.

## Delivery increments

### Sprint 0 - foundation and controls

- Approve this scope and API matrix. (Tracked; API implementation continues by tier.)
- Protect `master`; confirm PR-only workflow and required checks.
- Add repository structure, test projects, docs, CODEOWNERS, PR template, and pipeline skeleton.
- Add `.env` protection and secret scanning.
- Define generated API metadata format and operation identifiers. (Complete; metadata includes operations, scopes, and lifecycle capabilities.)
- Create deterministic render-state and scope tests. (Complete; batching, navigation clearing, layout persistence, application/page/layout isolation, and host subscription disposal are covered.)
- Provide shared DI registration through `AddBlazorQuery`. (Complete.)
- Add Server, WebAssembly, and Auto demo hosts and proof pages. (Complete; build verified.)

Foundation verification: full solution build succeeds with 0 errors; 11 unit/component tests pass. Live browser validation also passes for Interactive Server, WebAssembly, and Auto. Server preserves layout-owned styles across navigation while page-owned styles reset. WebAssembly and Auto apply state-owned CSS after hydration, rerender successfully, and reset page state after navigating away and back.

### Sprint 1 - selection and collection semantics

- Implement `Select`, `Length`, `Eq`, `First`, `Last`, `Slice`, `ToArray`, `Each`, `Map`, `End`, `PushStack`.
- Implement `Find`, `Children`, `Closest`, `Parent`, `Parents`, `ParentsUntil`, `Next*`, `Prev*`, `Siblings`, `Filter`, `Not`, `Has`, `Is`, `Index`.
- Add zero/one/many match tests and navigation-scope tests.

Managed selector subset: `*`, tag, id, class, and compound tag/id/class selectors resolve through generated element metadata. Combinators, pseudo-classes, attribute selectors, and arbitrary browser selector expressions remain CSS-only until an explicit managed matcher contract is designed.

### Sprint 2 - attributes, properties, data, text, and dimensions (Feature 4467)

- [x] `Attr`, `Prop`, `Data`, `RemoveAttr`, `RemoveProp`, `RemoveData`, `Text_Get`, `Text_Set`, `Html`, and `Val` are implemented and tested.
- [x] Implement dimensions and position reads, plus render-owned `Height_Set` and `Width_Set` writes.
- [x] Define safe value serialization and element ownership rules.
- [x] Add render output snapshots and accessibility checks.

**Status: Complete.** Final scope validation passed with 254/254 unit/component tests, 11/11 Playwright cases across all three interactive hosting modes, and a zero-warning Release solution build. Application-scoped state and read-only browser measurement are covered by unit, component, and real-browser tests.

Sprint 2 measurement validation: `ApplicationQueryRenderState` and `ApplicationQuery` are registered as scoped services through `AddBlazorQuery`. Their application-lifetime state is isolated from the page `BlazorQuery` and `LayoutQuery` states; mount a long-lived `QueryHost` with that state and `ClearOnNavigation="false"`. Measurement unit/component tests cover page, layout, and application cache lifetimes, including cancellation and stale-result rejection. Playwright exercises all public metrics against actual browser layout in Server, WebAssembly, and Auto.

Measurement architecture is specified in [Measurement and Position Boundary](MEASUREMENT-BOUNDARY.md). `HeightAsync`, `WidthAsync`, `InnerHeightAsync`, `InnerWidthAsync`, `OuterHeightAsync`, `OuterWidthAsync`, `OffsetAsync`, `PositionAsync`, and `OffsetParentAsync` are implemented as asynchronous, read-only browser observations. `Height(double)` and `Width(double)` are generated-key render-owned CSS writes. The read path is queued, coalesced, flushed once by `QueryHost.OnAfterRenderAsync`, and never mutates the DOM.

### Sprint 3 - classes and structural manipulation (Feature 4468)

- **Complete:** Class and visibility state operations, supported real/virtual structural moves, insertion, replacement, wrapping, removal, emptying, and detach semantics.
- **Complete:** Stable keys, render-state reconciliation, and lifecycle/disposal coverage for supported structures.
- **Complete:** Component-bound query scoping through generated/cascaded scope identity, constrained selection, traversal, and cleanup.
- **Contract:** `Clone()` duplicates library-created virtual elements only. A live Razor component instance is not cloneable through a DOM-style API; this is an explicit semantic boundary.

Sprint 3 exit evidence: `dotnet test Reflections.Blazor.Query.slnx --no-restore` passes 172 unit/component tests (134 unit, 38 component). This verifies automated behavior; it is not a substitute for browser validation of a specific hosting mode.

### Sprint 4 - events and form behavior (Feature 4469)

- [x] Implement event registration, delegation semantics, one-time handlers, trigger behavior, and disposal.
- [x] Integrate Blazor event callbacks and prevent duplicate subscriptions after rerender.
- [x] Cover keyboard, pointer, focus, form, resize, and scroll APIs, plus native typed event args where supported.
- [x] Implement read-only `Serialize` / `SerializeArray` via queued JS observation, with cancellation, stale-result protection, and deterministic coordinator disposal.

Sprint 4 exit evidence: `dotnet test Reflections.Blazor.Query.slnx --no-restore` passes 172 tests (134 unit, 38 component). This confirms automated coverage; it does not claim a fresh interactive browser run in Server, WebAssembly, and Auto for every event/form combination.

### Sprint 5 - effects, queues, and async behavior (Feature 4470)

- [x] Implement `FadeTo(TimeSpan, double)` using per-generated-key render-state CSS rules; no direct DOM writes, JS bridge, or timer is used.
- [x] Implement `FadeIn(TimeSpan)` and `FadeOut(TimeSpan)` using host-owned keyframes and per-key render-state animation declarations; fade-out retains the layout slot.
- [x] Implement `FadeToggle(TimeSpan)` based only on Query-managed visibility state.
- [x] Implement `Animate(IReadOnlyDictionary<string, string>, TimeSpan)` with a transition declaration per supplied property and a batched target update.
- [x] Implement awaitable direct `SlideDown`, `SlideToggle`, and `SlideUp` operations plus explicit-queue `QueueSlideDown`, `QueueSlideToggle`, and `QueueSlideUp`; retain measured `scrollHeight`, cancellation/finish cleanup, and natural-layout restoration.
- [x] Implement named per-element `Queue`, explicit `Dequeue`, and pending-only `ClearQueue`; state/element teardown signals cancellation to token-aware active callbacks (plain `Func<Task>` callbacks are not preemptible).
- [x] Implement `Delay(TimeSpan, string)` as cancellable queued work; it starts only when its queue is dequeued.
- [x] Implement awaitable `Stop` with cooperative cancellation and optional pending-queue clearing; tokenless work is awaited rather than preempted.
- [x] Implement finish-aware queue completion, skipped delays, and terminal render-state CSS for library-owned fades/transitions; `Show`/`Hide` remove finish-owned fade terminal styles. Active tokenless callbacks remain non-preemptible.
- [x] Implement awaitable completion promises.
- [x] Validate navigation and component disposal in component and browser tests, including page/layout persistence, element/host disposal, queue cancellation, circuit expiration, reconnect during animation, and completion while disconnected.
- [x] Add performance benchmarks and host-specific browser baselines; see [Performance Baseline](PERFORMANCE-BASELINE.md).
- [x] Cover long-lived circuit lifecycle transitions through reconnect and configured circuit-expiration browser scenarios. A separate soak test is not a release criterion without an agreed duration, load profile, and pass thresholds.

### Sprint 6 - compatibility, packaging, and release

- Resolve Tier 4 decisions and mark non-goals explicitly; do not expand the core API matrix merely to reproduce removed/legacy jQuery behavior.
- [x] Add consumer Getting Started and focused usage examples, including registration for each hosting model, page/layout/application lifetime, component scopes, read-only measurements, delegated events, effects, and queues. See [Getting Started](GETTING-STARTED.md) and [Usage Examples](EXAMPLES.md).
- [x] Add a migration guide for jQuery-oriented UI code and the library's intentional compatibility boundaries.
- [x] Generate a searchable static documentation site and managed .NET API reference with a repository-pinned DocFX tool; publish the generated site as a CI build artifact for review.
- [ ] Deploy the documentation site to a hosting provider after the release/deployment target is approved.
- [x] Package the library and generator independently with a shared version; bundle the analyzer into the runtime package and validate source generation from clean consumer projects. Local package validation only; no feed publication.
- [x] Adopt Apache-2.0, add the canonical license file and package SPDX metadata, and enforce the license in package validation.
- Build once, publish immutable artifacts, and promote the same artifact through environments.

## Azure DevOps structure to create after approval

Project: `Reflections.Blazor.Query`

Work item hierarchy:

- Epic: jQuery 4-compatible Blazor query library
  - Feature: Foundation and render-state architecture
  - Feature: Selection and traversal
  - Feature: Attributes, properties, data, text, and dimensions
  - Feature: Classes and DOM structure
  - Feature: Events and forms
  - Feature: Effects and queues
  - Feature: Compatibility, packaging, and release
  - Feature: Documentation and developer experience

Each feature will receive user stories and tasks linked to tests, documentation, and acceptance criteria. The API matrix will be the source for individual capability stories.

Sprint cadence: two weeks, with Sprint 0 as a one-week setup increment. Names and dates will be created only after confirming the project’s existing iteration path and timezone.

Boards columns: New, Approved, In Progress, Code Review, Validation, Ready for Release, Done.

Area paths:

- Core
- Generator
- Components
- Events
- Effects
- Demo
- Tests
- Documentation
- DevOps

## Pipeline and artifact plan

- PR validation: restore, format check, build, unit tests, generator tests, component tests, dependency scan, secret scan.
- Master CI: build once, run full tests, create immutable package and demo artifacts, record commit SHA and digest.
- Development promotion: deploy the exact artifact and run smoke tests.
- Staging promotion: integration tests, browser tests, package validation, and performance checks.
- Production promotion: approval, artifact identity verification, health checks, and observation evidence.
- Artifact feeds: separate versioned NuGet packages for runtime and generator, plus a demo deployment artifact.
- No secrets in source or pipeline YAML; use Azure DevOps variable groups or approved secret storage.

## Test plan

- Unit tests for selector parsing, state transitions, operation semantics, and scope disposal.
- Generator tests for generated metadata and diagnostics.
- Component tests for rendered output and rerender stability.
- Browser tests for interactive Server, WebAssembly, and Auto where available.
- Compatibility tests for zero, one, and many elements.
- Navigation tests for page reset and layout persistence.
- Measurement tests for request coalescing, post-render flush timing, zero/one/many selections, navigation/disposal invalidation, and Server/WebAssembly/Auto behavior.
- Performance tests for batched operations, large selections, and repeated rerenders.
- Accessibility tests for forms, focus, keyboard interactions, and generated markup.

## Branch and commit policy

Use the SOP prefixes and Azure work item IDs:

- `feature/RBQ-123-selection-core`
- `feature/RBQ-124-render-state-scopes`
- `chore/RBQ-125-pipeline-foundation`
- `test/RBQ-126-selection-contracts` is not an approved SOP prefix, so use `feature/` or `chore/` for test-only work.

Commit examples:

- `feat(core): add collection traversal operations`
- `feat(render-state): separate page and layout scopes`
- `test(generator): cover generated operation metadata`
- `ci: add pull request validation pipeline`
- `docs: add jquery 4 compatibility matrix`

Every branch is short-lived, starts from current `master`, targets `master`, and is deleted after merge.

## Decisions required before Azure DevOps creation

1. Approve the scope and tiering above.
2. Confirm whether legacy aliases (`Bind`, `Unbind`) are required or should be deferred.
3. Confirm whether `DelegateJS` and `SelectJS` are allowed in the first public release.
4. Confirm the Azure DevOps project’s existing team, iteration path, area path, and default branch settings.
5. Confirm package feed name and deployment target for the demo.
6. Confirm required reviewers/CODEOWNERS and sprint dates.
