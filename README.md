# Reflections.Blazor.Query

> A Blazor-native, jQuery 4-aligned fluent query library for .NET 10.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![Build passing](https://img.shields.io/badge/build-passing-success)
![Tests 257/257 passed](https://img.shields.io/badge/tests-257%2F257%20passed-success)
![License Apache-2.0](https://img.shields.io/badge/license-Apache--2.0-blue)

Copyright 2026 Reflections Digital Agency. Licensed under [Apache-2.0](LICENSE); see [NOTICE](NOTICE).

Reflections.Blazor.Query brings familiar fluent query patterns to Blazor without treating the browser DOM as an escape hatch. It is an orchestration layer for library-owned elements: concise C# selection and state operations, rendered by Blazor itself.

## Mission

jQuery made element orchestration approachable. Blazor made UI rendering stateful, component-based, and transport-agnostic. Combining the two naively is dangerous: a direct `document.querySelector(...).style...` or traditional jQuery write changes the live DOM behind Blazor's renderer. The next diff can overwrite the change, remove an unexpected node, or leave the UI visually and logically divergent.

This library preserves the fluent developer experience while keeping Blazor authoritative. Source-generated metadata identifies eligible elements, operations update library-owned render state, and Blazor's own rendering lifecycle produces the resulting output.

Supported hosting targets:

- Interactive Server
- Interactive WebAssembly
- Interactive Auto
- Static SSR for declarative output where no interaction is required

Ajax is explicitly out of scope. Blazor `HttpClient`, forms, routing, and event callbacks are the supported equivalents.

## Repository layout

- `src/Reflections.Blazor.Query` — public fluent API, render-state model, scope management, and Blazor components.
- `src/Reflections.Blazor.Query.Generators` — Roslyn incremental generator and generated API metadata.
- `tests/` — unit, generator, render-state, and component tests.
- `docs/PROJECT-PLAN.md` — full scope, API compatibility matrix, and sprint-by-sprint delivery plan.
- `docs/MEASUREMENT-BOUNDARY.md` — the read-only, queued, coalesced JS measurement contract.
- `docs/PERFORMANCE-BASELINE.md` — reproducible in-process performance baselines and their measurement boundaries.
- `docs/COMPONENT-SCOPING.md` — component-level query scoping design.
- `docs/GETTING-STARTED.md` — host registration, first query, scope lifetime, and delegated-event setup.
- `docs/EXAMPLES.md` — focused examples for state updates, scoped controls, layout persistence, measurement, effects, and queues.
- `docs/index.md` — documentation site landing page and architecture summary.
- `docs/docfx.json` — pinned DocFX site and generated .NET API reference configuration.
- `docs/DOCS-SITE.md` — pinned DocFX build, preview, CI artifact, and deployment boundary.
- `docs/MIGRATION.md` — migration guidance from jQuery-oriented UI code.
- `docs/NUGET-PACKAGING.md` — package composition, local validation, and the publication gate.

## Current status (validated 2026-10-14, `feature/4468-lifecycle-hardening`)

- **License:** Apache-2.0; see [LICENSE](LICENSE).

- **Completed:** Sprint 3 — Classes and DOM structure (Feature 4468), Sprint 4 — Events and Forms (Feature 4469), and Sprint 5 — Effects and Queues (Feature 4470) implementation/local validation. Formal release closure still requires an Azure Pipeline run and immutable artifact evidence.
- **Clone contract:** `Clone()` supports library-created virtual elements only. It does not duplicate a live Razor component instance.
- **Automated validation:** 257/257 .NET unit/component tests and all 20 configured local Playwright cases pass across Server, WebAssembly, and Auto, including real-browser measurements, host-path performance, measured slide-toggle down/up, explicit queue execution, and delegated custom-event routing in WebAssembly and Auto. Auto queue and delegated-event behavior are exercised on the initial Server render and again after warm reload on cached WebAssembly. Manual slide smoke checks also passed in all three hosts.
- **Sprint 5 validation:** The latest Release solution build has 0 warnings/errors. NuGet audit reports no vulnerable packages, and the runtime package includes its README and RCL static assets. ASP.NET Core component packages were updated to 10.0.13 to address the current advisories.
- **Sprint 5 closure:** implementation and local validation are complete. Formal release closure still requires an Azure Pipeline run on this source revision with published artifact evidence. Host-specific performance baselines are recorded for Server, WebAssembly, and Auto. Automated measured slide-toggle, explicit queue-interaction, and delegated custom-event coverage run in WebAssembly and Auto.
- **Hosting models:** Blazor Interactive Server, Interactive WebAssembly, and Interactive Auto.
- **Documentation:** searchable DocFX site and generated API reference build locally and are emitted as a CI artifact; public hosting/deployment remains pending approval.
- **NuGet release:** both packages are locally packable and consumer-tested under Apache-2.0. CI validates package metadata before publishing build artifacts; no NuGet push is configured.

### Implemented capabilities

- [x] Generated per-element render-state metadata and stable generated element keys.
- [x] Page, layout, and application render scopes with correct navigation/disposal semantics.
- [x] Explicit application-scope orchestration via scoped `ApplicationQueryRenderState` / `ApplicationQuery`; mount an application-lifetime `QueryHost` with `ClearOnNavigation="false"` to preserve its state across route changes.
- [x] Batched updates (`BeginBatch`) producing a single coalesced render.
- [x] Shared DI registration for the library.
- [x] Server, WebAssembly, and Auto demo hosts with working proof-of-concept pages.
- [x] `Select`
- [x] `JQuery(selector)` — familiar naming alias for `Select(selector)`; it resolves only library-registered elements and does not parse HTML strings.
- [x] `Length`
- [x] `Eq`
- [x] `First`
- [x] `Last`
- [x] `Slice`
- [x] `ToArray`
- [x] `Each`
- [x] `Map`
- [x] `End`
- [x] `PushStack`
- [x] `Add`
- [x] `AddBack`
- [x] `Find`
- [x] `Children`
- [x] `Closest`
- [x] `Parent`
- [x] `Parents`
- [x] `ParentsUntil`
- [x] `Next`
- [x] `NextAll`
- [x] `NextUntil`
- [x] `Prev`
- [x] `PrevAll`
- [x] `PrevUntil`
- [x] `Siblings`
- [x] `Filter`
- [x] `Not`
- [x] `Has`
- [x] `Is`
- [x] `Index`
- [x] `Get`
- [x] `Css`
- [x] `Attr`
- [x] `RemoveAttr`
- [x] `Prop`
- [x] `RemoveProp`
- [x] `Data`
- [x] `RemoveData`
- [x] `Text` (get and set)
- [x] `Html`
- [x] `Val`
- [x] `Clear` — atomically clears style, attribute, property, data, and content state.
- [x] `Height` (get and set)
- [x] `Width` (get and set)
- [x] `InnerHeight`
- [x] `InnerWidth`
- [x] `OuterHeight`
- [x] `OuterWidth`
- [x] `Offset`
- [x] `Position`
- [x] `OffsetParent`
- [x] `AddClass`
- [x] `RemoveClass`
- [x] `ToggleClass`
- [x] `HasClass`
- [x] `Show`
- [x] `Hide`
- [x] `Toggle`
- [x] `Remove`
- [x] `Empty`
- [x] `Serialize` / `SerializeArray` — asynchronous (`SerializeAsync` / `SerializeArrayAsync`), read-only form observation. Reads successful form controls from the browser through the same queued, coalesced, read-only JS boundary as layout measurement and returns name/value pairs to C# without mutating the DOM. Coordinator disposal closes admission and waits for an active bridge read before disposing the JS module.
- [x] `Append` — appends an owned, encoded text fragment after an element's existing content.
- [x] `AppendHtml` — trusted-markup variant of `Append`, under the same explicit trust boundary as `Html`.
- [x] `Prepend` — prepends an owned, encoded text fragment before an element's existing content.
- [x] `PrependHtml` — trusted-markup variant of `Prepend`.
- [x] `AppendElement(tagName, className?, text?, isHtml?)` — creates a brand-new, library-owned element (a "virtual node") as the last child of the selected element, registered in `QueryElementRegistry` so it is selectable, styleable, and composable with every other operation (`Attr`, `AddClass`, `Hide`, nested `AppendElement`, etc.). Returns a `QuerySelection` for the new element.
- [x] `PrependElement(...)` — same, as the first child.
- [x] `InsertElementBefore(...)` / `InsertElementAfter(...)` — creates a new virtual sibling immediately before/after the selected element, inside that element's registered parent.
- [x] `AppendTo(target)` / `PrependTo(target)` — moves an element (real, Razor-declared **or** library-created) into another element's child list. For a real `QueryElement`, this works via a live delegate to that component's own render method, captured on every render and invoked by the new host instead of the element's own declared position — the original renders nothing once relocated, so nothing is ever emitted twice. Cross-thread mutations (e.g. from a background service) are safe: a single dispatcher, registered by `QueryHost`, ensures the "source stops / host starts" pair always lands in one Blazor render batch.
- [x] `Before(anchorContent)` / `After(anchorContent)` — move an existing element to sit immediately before/after a single anchor element (real or virtual). Throws if the receiving selection resolves to anything other than exactly one anchor, since moving one element relative to several anchors is ambiguous.
- [x] `Wrap(tagName, className?)` — inserts a new library-created wrapper element at an element's current position and reparents the element inside it. Works for both real and virtual elements.
- [x] `WrapAll(tagName, className?)` — inserts one library-created wrapper before the first selected sibling, then reparents every selected real or virtual element into it in selector/render order. All selected elements must share one registered parent; cross-parent wrapping is rejected before any state changes.
- [x] `WrapInner(tagName, className?)` — moves a mounted `QueryElement`'s entire component-owned `ChildContent` render fragment and its existing library-owned direct children into a generated wrapper. This is render-tree relocation, not DOM mutation. Virtual targets are intentionally rejected because they have no component-owned Razor fragment.
- [x] `Detach()` — orphans an element from wherever it's currently attached, keeping its state intact so it can be re-attached later via `AppendTo`/`PrependTo`. For a relocated real element, this "springs it back" to render at its own declared Razor position again. For a real element that was never relocated, or a virtual element, this matches existing `Remove()`/detach-in-place semantics.
- [x] `Clone()` — duplicates a **virtual** element's tag, class, attributes, properties, added classes, content, appended/prepended fragments, and hidden state under a new key, unattached until explicitly `AppendTo`/`PrependTo`'d somewhere. Real Razor component instances are intentionally not cloneable: duplicating rendered output would not duplicate Blazor component parameters, callbacks, or lifecycle semantics.
- [x] `InsertBefore(target)` / `InsertAfter(target)` — jQuery-compatible receiver-order aliases over `target.Before(this)` / `target.After(this)`, preserving the existing single-anchor safety contract.
- [x] `ReplaceWith(replacement)` / `ReplaceAll(target)` / `Replace(replacement)` — moves one existing real or virtual replacement into one target slot, then removes the target. Multiple-target replacement and cloning are deliberately rejected.

### Explicit boundary and other implemented capabilities

- [ ] `Clone` of a real `QueryElement` — not a generator key-minting task. Reconsider only if a safe contract is designed for cloning static render descriptions without pretending to duplicate a live component instance.
- [x] Component scoping — `QueryScopeHost` creates and cascades an `IQueryScope` without a nested DI container. Scoped queries filter registry resolution, traversal, and cleanup to their own component identity; unscoped `Query` remains the explicit page/layout orchestration API.
- [x] `On` / `Off` / `One` / `Trigger` — library-owned C# handlers rendered as Blazor event bindings. Generic overloads preserve native Blazor arguments for supported event names; `Trigger<TEventArgs>` dispatches typed handlers only. `Func<Task>` and `Func<TEventArgs, Task>` registrations are awaited by Blazor; use `TriggerAsync` for synthetic async dispatch and `OffAsync` / `UnbindAsync` to remove an async handler by delegate. Untyped handlers remain source-compatible, and `One` consumes its callback before its first dispatch.
- [x] Typed event helpers — `Click`, `Change`, `Submit`, focus helpers, keyboard helpers, mouse helpers, `ContextMenu`, `DblClick`, `Error`, `Load`, and `Unload` accept native Blazor event argument callbacks. Their existing parameterless forms remain synthetic C# triggers. `Resize` and `Scroll` remain untyped because Blazor does not provide their measurements in event args.
- [x] Event registration lifecycle — removing a virtual node or disposing a real `QueryElement` releases its synchronous, asynchronous, typed, and delegated-root callbacks. Event state cannot outlive its owning element identity.
- [x] `TriggerHandler` / `Bind` / `Unbind` — typed and untyped aliases over the library-owned event pipeline. `Bind` / `Unbind` remain legacy compatibility names; `TriggerHandler<TEventArgs>` dispatches typed handlers for the selected elements.
- [x] `Blur`
- [x] `Change`
- [x] `Click`
- [x] `ContextMenu`
- [x] `DblClick`
- [x] `Focus`
- [x] `FocusIn`
- [x] `FocusOut`
- [x] `Hover`
- [x] `KeyDown`
- [x] `KeyPress`
- [x] `KeyUp`
- [x] `MouseDown`
- [x] `MouseEnter`
- [x] `MouseLeave`
- [x] `MouseMove`
- [x] `MouseOut`
- [x] `MouseOver`
- [x] `MouseUp`
- [x] `Resize`
- [x] `Scroll`
- [x] `Submit`
- [x] `Animate(IReadOnlyDictionary<string, string>, TimeSpan)` — applies caller-supplied CSS targets and per-property transitions through Blazor-owned state; does not await completion.
- [x] `Queue(Func<Task>)` / `Queue(Func<CancellationToken, Task>)` — enqueue per-element asynchronous callbacks; named queues default to `fx` and start only when dequeued.
- [x] `Dequeue` — starts and awaits sequential execution for each selected element; concurrent calls share the active drain.
- [x] `ClearQueue` — clears pending callbacks without canceling the active callback. Render-state clear or element removal signals cancellation to token-aware callbacks; plain `Func<Task>` callbacks can only finish naturally.
- [x] `Delay(TimeSpan, string)` — enqueues a cancellable delay on the selected element queues; starts only when `Dequeue` is called.
- [x] `Stop(bool clearQueue = false, string queueName = "fx")` — signals cancellation and awaits the active drain; pending work is preserved by default or discarded when requested. Tokenless callbacks finish naturally.
- [x] `Finish(string queueName = "fx")` — finishable queue items run their completion action, queued delays are skipped, and ordinary callbacks run normally. It also settles library-owned fade/transition CSS at target state; `Show`/`Hide` clear finish-owned fade styles. Tokenless active callbacks finish naturally.
- [x] `Promise(string queueName = "fx")` — awaits current selected queue emptiness without starting work. Callback faults remain observable through `Dequeue`; clearing discarded pending work resolves the promise, while element/render-state teardown cancels it.
- [x] `ClearQueue`
- [x] `Delay`
- [x] `Dequeue`
- [x] `FadeIn(TimeSpan duration)` — Blazor-rendered keyframe fade; clears library hidden state before animating.
- [x] `FadeOut(TimeSpan duration)` — Blazor-rendered keyframe fade; preserves the layout slot, with `Hide()` available for layout removal.
- [x] `FadeTo(TimeSpan duration, double opacity)` — applies a batched, generated-key CSS opacity transition through Blazor-owned render state.
- [x] `FadeToggle(TimeSpan duration)` — alternates Query-managed visibility; does not inspect arbitrary CSS or external DOM state.
- [x] `Finish`
- [x] `Promise`
- [x] `Queue`
- [x] `SlideDown(TimeSpan)`, `SlideToggle(TimeSpan)`, and `SlideUp(TimeSpan)` — await measured-height slides directly; completion restores natural height, and slide-up hides only after animation end.
- [x] `QueueSlideDown`, `QueueSlideToggle`, and `QueueSlideUp` — enqueue the same measured effects for callers who need explicit queue control.
- [x] `Stop`
- [x] Independent NuGet packaging for the library and generator with a shared version; the runtime package also carries the analyzer so a single package reference activates generation (Feature 4471). Packages are locally validated and not published.
- [x] Migration guide for jQuery-oriented Blazor adoption (Feature 4471)
- [x] Public documentation site and generated API reference; CI publishes a review artifact (Feature 4472). Hosting deployment remains pending approval of a target.

### Explicitly deferred, legacy, or excluded

- [ ] `AjaxComplete` — excluded; use Blazor `HttpClient`/networking.
- [ ] `AjaxError` — excluded; use Blazor `HttpClient`/networking.
- [ ] `AjaxSend` — excluded; use Blazor `HttpClient`/networking.
- [ ] `AjaxStart` — excluded; use Blazor `HttpClient`/networking.
- [ ] `AjaxStop` — excluded; use Blazor `HttpClient`/networking.
- [ ] `AjaxSuccess` — excluded; use Blazor `HttpClient`/networking.
- [ ] Ajax-form `Load` — excluded; use Blazor `HttpClient`/networking.
- [ ] `AndSelf` — legacy removed; use `AddBack`.
- [ ] `Die` — legacy removed API.
- [ ] `Live` — legacy removed API.
- [ ] `Undelegate` — legacy removed API.
- [ ] `Context` — excluded; jQuery removed this collection property in 3.0. Query scope is exposed explicitly through `IQueryScope` instead.
- [x] `DelegateJS` / `Undelegate` — delegated, bubbling browser events. The host must load the supplied pre-start custom-event initializer; it returns only the closest library-owned `data-rbq-key`, then C# validates the root, scope, and selector before dispatch. No JS handler registry or DOM mutation is used.
- [x] `Error` — event helper over the library-owned Blazor event pipeline; this does not imply Ajax error handling.
- [x] `Load` — event helper over the library-owned Blazor event pipeline. Ajax-form `Load` remains excluded.
- [ ] `Ready` — no jQuery-style document-ready callback; Blazor component lifecycle (`OnInitialized` / `OnAfterRender`) owns readiness.
- [x] `SelectJS` — Blazor-owned text-selection event helper with typed `EventArgs` and parameterless synthetic trigger overloads.
- [ ] `Size` — excluded; removed from jQuery 3.0. Use `Length`.
- [x] `Unload` — event helper over the library-owned Blazor event pipeline; this does not imply page-unload interception or Ajax behavior.

See [docs/PROJECT-PLAN.md](docs/PROJECT-PLAN.md) for the full API tiering, sprint breakdown, and acceptance criteria behind each item.

The searchable documentation site and generated API reference are built from [docs/docfx.json](docs/docfx.json). See [Building the docs site](docs/DOCS-SITE.md) for local preview instructions.

## Quick start

This is a compact API preview. For complete setup, hosting-model notes, and executable patterns, see [Getting Started](docs/GETTING-STARTED.md) and the [Examples](docs/EXAMPLES.md).

Register the library once in the host application:

```csharp
// Program.cs
builder.Services.AddBlazorQuery();
```

Render elements through the library infrastructure, then inject `Query` into an interactive component. This example styles and annotates a selected card without a browser DOM write:

```razor
@page "/release-console"
@inject Reflections.Blazor.Query.Query Query

<QueryHost>
    <QueryElement TagName="article" ElementKey="release-card" Class="release-card">
        <h2>Release candidate</h2>
        <QueryElement TagName="p" ElementKey="release-card-status" Class="release-status">
            Awaiting verification.
        </QueryElement>
    </QueryElement>

    <button class="btn btn-primary" @onclick="MarkVerified">Mark verified</button>
</QueryHost>

@code {
    private void MarkVerified()
    {
        using (Query.BeginBatch())
        {
            Query.Select(".release-card")
                .Attr("data-release-state", "verified")
                .Css("border-color", "seagreen");

            Query.Select(".release-status")
                .Text("Verification complete.")
                .Css("color", "seagreen");
        }
    }
}
```

For layout facts, use the asynchronous read boundary. It observes the browser after Blazor renders and returns a nullable typed snapshot:

```csharp
var position = await Query.Select(".release-card").PositionAsync();
```

## Building and testing

```powershell
dotnet build Reflections.Blazor.Query.slnx
dotnet test Reflections.Blazor.Query.slnx

# Run local performance baselines (results go under ignored output/benchmarks/)
dotnet run -c Release --project benchmarks/Reflections.Blazor.Query.Benchmarks -- --filter "*" --artifacts output/benchmarks/latest --noOverwrite
```

Run the real-browser Interactive Server navigation and reconnect scenarios with Playwright (Chromium is required):

```powershell
cd tests/BrowserE2E
npm ci
npx playwright install chromium
npm test
```

## Contributing

This project needs experienced Blazor engineers who care about renderer integrity, lifecycle correctness, and a clean developer experience. Help us turn the unchecked list into a rigorously tested, documented, Blazor-native alternative to unsafe DOM manipulation.

Before contributing:

1. `master` is the only long-lived branch. Work on a short-lived branch and submit a Pull Request.
2. Use [Conventional Commits](https://www.conventionalcommits.org/) for every commit.
3. Open a **Feedback & Support** issue before proposing a major API or architectural change.
4. Preserve the core contract: writes remain library-owned render state; JavaScript is observation-only unless an explicitly reviewed design changes that boundary.
5. Add tests and update the function list, project plan, and measurement documentation when public behavior changes.

Questions, design critiques, and well-scoped feature proposals are welcome. The hard problems here — selection semantics, component isolation, ownership boundaries, and cross-host lifecycle behavior — benefit from serious community review.

## License

Licensed under the [Apache License, Version 2.0](LICENSE).
