# Reflections.Blazor.Query — Implementation Plan

This plan follows the Azure DevOps hierarchy and repository delivery policy. Foundation and selection/traversal are implemented; the active work is Feature 4467.

## 🚀 Active Phase: Sprint 2 (Feature 4467)

**Current focus:** Attributes, properties, data, text, and dimensions.

- **Assigned to:** `DEVOPS22-SVR\Omar`
- **Branch:** `feature/4467-attributes-data`
- **Status:** 59/59 tests passing ✅ · 0 warnings · 0 errors

### Implemented matrix

- **Attr / RemoveAttr:** Manages render-owned attribute state for each resolved element key.
- **Prop / RemoveProp:** Tracks typed property state and protects reserved keys and event-like names.
- **Data / RemoveData:** Stores JSON-normalized library state isolated from DOM `data-*` attributes.
- **Text:** State-driven text content rendered with Blazor HTML encoding.
- **Html:** Explicit caller-supplied markup rendered through the component tree; this is a documented trust boundary.
- **Val:** Owns and reads the rendered `value` property. Blazor two-way event binding remains a future forms concern.
- **Clear:** Atomically clears styles, attributes, properties, data, and content state.

### Immediate backlog

- [ ] Design and implement `Offset`, `Position`, and `OffsetParent` under the existing read-only batch boundary.
- [x] Implement the generated-key measurement queue, coalesced `QueryHost.OnAfterRenderAsync` flush, immutable snapshot DTOs, and no-public-`ElementReference` boundary.
- [ ] Add page/layout/application navigation and disposal coverage for cancellation, cache invalidation, and late-result rejection.
- [ ] Verify the measurement contract in Interactive Server, WebAssembly, and Auto.

### 🗺️ Long-term roadmap (Epic 4464)

#### Sprint 3 — Classes & DOM Structure (Feature 4468)

- [ ] Class manipulation: `AddClass`, `RemoveClass`, `ToggleClass`, `HasClass`.
- [ ] Structural state trees: `Append`, `Prepend`, `Before`, `After`, `Wrap`, and their supported counterparts.
- [ ] Logical component query capabilities using generated `ComponentScopeKey` metadata; no per-component DI lifetime.

#### Sprint 4 — Events & Forms (Feature 4469)

- [ ] Fluent event binding: `On`, `Off`, `One`, and `Trigger`.
- [ ] Form-state handlers: `Serialize` and `SerializeArray`.

#### Sprint 5 — Effects & Queues (Feature 4470)

- [ ] C#-driven render-state transitions: `Animate`, `FadeIn`, and `SlideUp`.
- [ ] Queue tracking: `Queue`, `Delay`, `Stop`, and `Finish`.

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
- Reusable component isolation is a planned logical query capability constrained by generated `ComponentScopeKey` metadata, not a nested DI scope. See [Component Scoping Boundaries](COMPONENT-SCOPING.md).
- Every operation updates state and produces Blazor-owned render output.
- No operation writes to a Blazor-owned DOM node through JavaScript.
- Browser layout metrics are an explicit read-only interop boundary. Measurement results are snapshots held in a dedicated library-owned measurement cache, not render-driving mutation state.
- Measurement interop identifies only generated `data-rbq-key` values through an isolated JS module; `ElementReference` and browser handles are not public query abstractions.
- Event subscriptions, queues, animation state, and async operations must have deterministic disposal and cancellation.
- Operations must define behavior for zero matches, one match, and multiple matches.

## API compatibility matrix

### Tier 1 - jQuery 4 core target

Implement and test these first:

`Add`, `AddBack`, `AddClass`, `After`, `Append`, `AppendTo`, `Attr`, `Before`, `Children`, `Clone`, `Closest`, `Css`, `Data`, `Detach`, `Each`, `Empty`, `End`, `Eq`, `Filter`, `Find`, `First`, `Has`, `HasClass`, `Height_Get`, `Height_Set`, `Hide`, `Html`, `Index`, `InnerHeight`, `InnerWidth`, `InsertAfter`, `InsertBefore`, `Is`, `Last`, `Length`, `Map`, `Next`, `NextAll`, `NextUntil`, `Not`, `Offset`, `OffsetParent`, `On`, `OuterHeight`, `OuterWidth`, `Parent`, `Parents`, `ParentsUntil`, `Position`, `Prepend`, `PrependTo`, `Prev`, `PrevAll`, `PrevUntil`, `Prop`, `PushStack`, `Remove`, `RemoveAttr`, `RemoveClass`, `RemoveData`, `RemoveProp`, `ReplaceAll`, `ReplaceWith`, `Select`, `Serialize`, `SerializeArray`, `Show`, `Siblings`, `Size`, `Slice`, `Text_Get`, `Text_Set`, `ToArray`, `Toggle`, `ToggleClass`, `Trigger`, `TriggerHandler`, `Val`, `Width_Get`, `Width_Set`, `Wrap`, `WrapAll`, `WrapInner`.

### Tier 2 - effects and queue modules

Implement after the core state model is stable:

`Animate`, `ClearQueue`, `Delay`, `Dequeue`, `FadeIn`, `FadeOut`, `FadeTo`, `FadeToggle`, `Finish`, `Promise`, `Queue`, `SlideDown`, `SlideToggle`, `SlideUp`, `Stop`.

Effects must use render-state transitions and CSS/Web Animations-compatible output. They must not rely on direct DOM mutation. Cancellation and navigation disposal are mandatory acceptance criteria.

### Tier 3 - event APIs

Implement with Blazor event descriptors and generated handler metadata:

`Bind`, `Blur`, `Change`, `Click`, `ContextMenu`, `DBLClick`, `Focus`, `FocusIn`, `FocusOut`, `Hover`, `KeyDown`, `KeyPress`, `KeyUp`, `MouseDown`, `MouseEnter`, `MouseLeave`, `MouseMove`, `MouseOut`, `MouseOver`, `MouseUp`, `Off`, `One`, `Resize`, `Scroll`, `Submit`, `Unbind`.

Compatibility note: `Bind` and `Unbind` are legacy jQuery names. They may be provided only as compatibility aliases over the supported event model and must be clearly marked as legacy in documentation.

### Tier 4 - explicit compatibility or non-core features

`Context`, `DelegateJS`, `Error`, `Get`, `JQuery`, `Load`, `Ready`, `SelectJS`, `Unload` require separate design decisions because they depend on browser globals, arbitrary JavaScript, document lifecycle, or Ajax-era behavior. `DelegateJS` and `SelectJS` must be opt-in escape hatches with explicit documentation that they can bypass Blazor ownership rules.

### Excluded or replaced APIs

Ajax is excluded: `AjaxComplete`, `AjaxError`, `AjaxSend`, `AjaxStart`, `AjaxStop`, `AjaxSuccess`, and the Ajax form of `Load`.

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

- `Attr`, `Prop`, `Data`, `RemoveAttr`, `RemoveProp`, `RemoveData`, `Text_Get`, `Text_Set`, `Html`, and `Val` are implemented and tested.
- Implement dimensions and position reads, plus render-owned `Height_Set` and `Width_Set` writes.
- Define safe value serialization and element ownership rules.
- Add render output snapshots and accessibility checks.

Measurement architecture is specified in [Measurement and Position Boundary](MEASUREMENT-BOUNDARY.md). `HeightAsync`, `WidthAsync`, `InnerHeightAsync`, `InnerWidthAsync`, `OuterHeightAsync`, and `OuterWidthAsync` are implemented as asynchronous, read-only browser observations. `Offset`, `OffsetParent`, and `Position` remain pending. `Height_Set` and `Width_Set` remain planned render-state CSS writes. The read path is queued, coalesced, flushed once by `QueryHost.OnAfterRenderAsync`, and never mutates the DOM.

### Sprint 3 - classes and structural manipulation (Feature 4468)

- Implement class operations and `Show`/`Hide`/`Toggle`.
- Implement append/prepend/before/after/replace/wrap/remove/empty/detach/clone operations.
- Define stable keys and reconciliation behavior for repeated and reordered elements.
- Add `IQueryScope` / `IQueryScopeFactory`, generated/cascaded component scope keys, constrained subtree selection, and repeated-component disposal coverage.

### Sprint 4 - events and form behavior (Feature 4469)

- Implement event registration, delegation semantics, one-time handlers, trigger behavior, and disposal.
- Integrate Blazor event callbacks and prevent duplicate subscriptions after rerender.
- Add keyboard, pointer, focus, form, resize, and scroll coverage.

### Sprint 5 - effects, queues, and async behavior (Feature 4470)

- Implement queues, delays, effects, animation cancellation, and promises.
- Validate behavior during navigation, component disposal, and reconnect.
- Add performance benchmarks and long-running circuit tests.

### Sprint 6 - compatibility, packaging, and release

- Resolve Tier 4 decisions.
- Publish API documentation and migration guide.
- Package the library and generator independently with version alignment.
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
