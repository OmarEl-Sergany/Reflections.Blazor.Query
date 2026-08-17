# Reflections.Blazor.Query

> A Blazor-native, jQuery 4-aligned fluent query library for .NET 10.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![Build passing](https://img.shields.io/badge/build-passing-success)
![Tests 61/61 passed](https://img.shields.io/badge/tests-61%2F61%20passed-success)
![License MIT](https://img.shields.io/badge/license-MIT-blue)

Reflections.Blazor.Query brings familiar fluent query patterns to Blazor without treating the browser DOM as an escape hatch. It is an orchestration layer for library-owned elements: concise C# selection and state operations, rendered by Blazor itself.

## Mission

jQuery made element orchestration approachable. Blazor made UI rendering stateful, component-based, and transport-agnostic. Combining the two naively is dangerous: a direct `document.querySelector(...).style...` or traditional jQuery write changes the live DOM behind Blazor's renderer. The next diff can overwrite the change, remove an unexpected node, or leave the UI visually and logically divergent.

This library preserves the fluent developer experience while keeping Blazor authoritative. Source-generated metadata identifies eligible elements, operations update library-owned render state, and `QueryHost` emits the resulting output through Blazor's normal rendering lifecycle.

## Architectural intent

### Zero Blazor desync

Reflections.Blazor.Query works **with** Blazor's diffing engine, never around it.

- `QueryElement` registers a stable, generated key and structural metadata for each library-owned element.
- Selectors, collection operations, predicates, and parent-aware traversal resolve against that metadata—not arbitrary browser DOM nodes.
- Fluent writes update render state. Blazor produces the CSS, attributes, properties, text, HTML, and value output during its next render.
- Dimension and position APIs cross a deliberately read-only JS observation boundary. Requests queue during application code, coalesce after render, and return typed snapshots.
- Page navigation, host disposal, and element disposal invalidate the matching snapshots, preventing stale measurements from becoming future state.

The result is an imperative-looking API with a declarative rendering contract: no `RenderTreeFrame` manipulation, no DOM writes through JavaScript, and no external mutations for Blazor to reconcile.

### 🚫 Why no Ajax?

Ajax APIs are intentionally excluded. A Blazor-native application already has state-integrated `HttpClient`, native form handling, circuit-aware routing, and component `EventCallback`s. Reintroducing legacy jQuery networking would duplicate better platform primitives and blur ownership of application state.

Reflections.Blazor.Query focuses on fluent, state-driven element orchestration. Networking belongs to Blazor and the application architecture.

## What makes it different

Traditional jQuery resolves a selector against the live DOM and writes to that DOM. This library resolves library-owned, stable generated keys in render order, with metadata-backed predicates and parent-aware traversal. The browser remains a read-only source for layout facts; Blazor remains the only renderer.

| Concern | Traditional jQuery | Reflections.Blazor.Query |
| --- | --- | --- |
| Element identity | Live DOM lookup | Stable generated element keys |
| UI mutation | Immediate browser writes | Library-owned render state, then Blazor render |
| Layout metrics | Direct DOM reads/writes often intermixed | Post-render, coalesced, read-only JS snapshots |
| Lifecycle | Browser-centric | Component, navigation, and disposal-aware |
| Networking | `$.ajax()` family | Native Blazor `HttpClient`, forms, routing, callbacks |

## Current project state

- **Active stage:** Sprint 2 — Attributes, properties, data, text, and dimensions (**Feature 4467**).
- **Validation:** 61/61 unit and component tests passing.
- **Hosting models:** Blazor Interactive Server, Interactive WebAssembly, and Interactive Auto.
- **Measurement boundary:** read-only, queued, coalesced, and snapshot-invalidated. See [Measurement and Position Boundary](docs/MEASUREMENT-BOUNDARY.md).
- **Roadmap and delivery plan:** [Project Plan](docs/PROJECT-PLAN.md).

## Quick start

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

## Function matrix

This is the public API status dashboard. Checked items are implemented in the current codebase; unchecked entries are planned, deferred, legacy, or intentionally excluded.

### Selection, traversal, and collection

- [x] Add / AddBack / Children / Closest / CSS / Each / End / Eq / Filter / Find / First / Get / Has / Index / Is / Last / Length / Map / Next / NextAll / NextUntil / Not / Parent / Parents / ParentsUntil / Prev / PrevAll / PrevUntil / PushStack / Select / Siblings / Slice / ToArray

### Attributes, properties, data, text, forms, and measurement

- [x] Attr / Data / Height_Get / Height_Set / Html / InnerHeight / InnerWidth / Offset / OffsetParent / OuterHeight / OuterWidth / Position / Prop / RemoveAttr / RemoveData / RemoveProp / Text_Get / Text_Set / Val / Width_Get / Width_Set
- [ ] Serialize / SerializeArray

### Classes, visibility, and structure

- [ ] AddClass / After / Append / AppendTo / Before / Clone / Detach / Empty / HasClass / Hide / InsertAfter / InsertBefore / Prepend / PrependTo / Remove / RemoveClass / ReplaceAll / ReplaceWith / Show / Toggle / ToggleClass / Wrap / WrapAll / WrapInner

### Events and forms

- [ ] Bind (Legacy alias) / Blur / Change / Click / ContextMenu / DBLClick / Focus / FocusIn / FocusOut / Hover / KeyDown / KeyPress / KeyUp / MouseDown / MouseEnter / MouseLeave / MouseMove / MouseOut / MouseOver / MouseUp / Off / On / One / Resize / Scroll / Submit / Trigger / TriggerHandler / Unbind (Legacy alias)

### Effects and queues

- [ ] Animate / ClearQueue / Delay / Dequeue / FadeIn / FadeOut / FadeTo / FadeToggle / Finish / Promise / Queue / SlideDown / SlideToggle / SlideUp / Stop

### Explicitly deferred, legacy, or excluded compatibility APIs

- [ ] AjaxComplete (Excluded; use Blazor HttpClient/networking)
- [ ] AjaxError (Excluded; use Blazor HttpClient/networking)
- [ ] AjaxSend (Excluded; use Blazor HttpClient/networking)
- [ ] AjaxStart (Excluded; use Blazor HttpClient/networking)
- [ ] AjaxStop (Excluded; use Blazor HttpClient/networking)
- [ ] AjaxSuccess (Excluded; use Blazor HttpClient/networking)
- [ ] AndSelf (Legacy removed; use AddBack)
- [ ] Die / Live / Undelegate (Legacy removed APIs)
- [ ] Context / DelegateJS / Error / JQuery / Load / Ready / SelectJS / Size / Unload (Review required or deferred)

### Core library infrastructure

- [x] BeginBatch
- [x] QueryElement stable-key registration
- [x] Page, layout, and application render scopes
- [x] Navigation and disposal lifecycle handling

## Sprint roadmap

### Sprint 3 — Classes and DOM structure (Feature 4468)

Class manipulation and render-owned structural state trees: `AddClass`, `RemoveClass`, `ToggleClass`, `HasClass`, `Append`, `Prepend`, `Before`, `After`, and wrapping operations.

Component scoping is also planned here. `ComponentScopeKey` plus `IQueryScope` will provide lexical component isolation without creating a DI container scope per component. A component-scoped query will resolve only metadata registered beneath its own boundary, while explicit page and layout queries retain their broader orchestration role.

### Sprint 4 — Events and forms (Feature 4469)

Fluent event binding abstraction, lifecycle-safe registration, and form serialization state handlers: `On`, `Off`, `One`, `Trigger`, `Serialize`, and `SerializeArray`.

### Sprint 5 — Effects and queues (Feature 4470)

C#-driven, render-owned transitions and queue management: `Animate`, fade and slide operations, `Queue`, `Delay`, `Stop`, and `Finish`.

## Contributing

This project needs experienced Blazor engineers who care about renderer integrity, lifecycle correctness, and a clean developer experience. Help us turn the unchecked matrix into a rigorously tested, documented, Blazor-native alternative to unsafe DOM manipulation.

Before contributing:

1. `master` is the only long-lived branch. Work on a short-lived branch and submit a Pull Request.
2. Use [Conventional Commits](https://www.conventionalcommits.org/) for every commit.
3. Open a **Feedback & Support** issue before proposing a major API or architectural change.
4. Preserve the core contract: writes remain library-owned render state; JavaScript is observation-only unless an explicitly reviewed design changes that boundary.
5. Add tests and update the function matrix, project plan, and measurement documentation when public behavior changes.

Run the validation suite before opening a PR:

```powershell
dotnet test Reflections.Blazor.Query.slnx
dotnet build Reflections.Blazor.Query.slnx --no-restore
```

Questions, design critiques, and well-scoped feature proposals are welcome. The hard problems here—selection semantics, component isolation, ownership boundaries, and cross-host lifecycle behavior—benefit from serious community review.

## License

Licensed under the MIT License.
