# Reflections.Blazor.Query

> A Blazor-native, jQuery 4-aligned fluent query library for .NET 10.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![Build passing](https://img.shields.io/badge/build-passing-success)
![Tests 61/61 passed](https://img.shields.io/badge/tests-61%2F61%20passed-success)
![License MIT](https://img.shields.io/badge/license-MIT-blue)

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
- `docs/COMPONENT-SCOPING.md` — component-level query scoping design.

## Current status (as of 2026-08-28, `master`)

- **Active stage:** Sprint 3 — Classes and DOM structure (Feature 4468) is next up.
- **Validation:** 61/61 unit and component tests passing.
- **Hosting models:** Blazor Interactive Server, Interactive WebAssembly, and Interactive Auto.

### Done (merged to `master`)

- [x] Generated per-element render-state metadata and stable generated element keys.
- [x] Page, layout, and application render scopes with correct navigation/disposal semantics.
- [x] Batched updates (`BeginBatch`) producing a single coalesced render.
- [x] Shared DI registration for the library.
- [x] Server, WebAssembly, and Auto demo hosts with working proof-of-concept pages.
- [x] `Select`
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

### In progress / remaining (not yet started)

- [ ] `AddClass`
- [ ] `RemoveClass`
- [ ] `ToggleClass`
- [ ] `HasClass`
- [ ] `Show`
- [ ] `Hide`
- [ ] `Toggle`
- [ ] `Append`
- [ ] `AppendTo`
- [ ] `Prepend`
- [ ] `PrependTo`
- [ ] `Before`
- [ ] `After`
- [ ] `InsertBefore`
- [ ] `InsertAfter`
- [ ] `Replace`
- [ ] `ReplaceAll`
- [ ] `ReplaceWith`
- [ ] `Remove`
- [ ] `Empty`
- [ ] `Detach`
- [ ] `Clone`
- [ ] `Wrap`
- [ ] `WrapAll`
- [ ] `WrapInner`
- [ ] Component scoping — `ComponentScopeKey` and `IQueryScope` (Sprint 3 / Feature 4468)
- [ ] `On`
- [ ] `Off`
- [ ] `One`
- [ ] `Trigger`
- [ ] `TriggerHandler`
- [ ] `Bind` (legacy alias)
- [ ] `Unbind` (legacy alias)
- [ ] `Blur`
- [ ] `Change`
- [ ] `Click`
- [ ] `ContextMenu`
- [ ] `DblClick`
- [ ] `Focus`
- [ ] `FocusIn`
- [ ] `FocusOut`
- [ ] `Hover`
- [ ] `KeyDown`
- [ ] `KeyPress`
- [ ] `KeyUp`
- [ ] `MouseDown`
- [ ] `MouseEnter`
- [ ] `MouseLeave`
- [ ] `MouseMove`
- [ ] `MouseOut`
- [ ] `MouseOver`
- [ ] `MouseUp`
- [ ] `Resize`
- [ ] `Scroll`
- [ ] `Submit`
- [ ] `Serialize`
- [ ] `SerializeArray`
- [ ] `Animate`
- [ ] `ClearQueue`
- [ ] `Delay`
- [ ] `Dequeue`
- [ ] `FadeIn`
- [ ] `FadeOut`
- [ ] `FadeTo`
- [ ] `FadeToggle`
- [ ] `Finish`
- [ ] `Promise`
- [ ] `Queue`
- [ ] `SlideDown`
- [ ] `SlideToggle`
- [ ] `SlideUp`
- [ ] `Stop`
- [ ] Independent NuGet packaging for the library and generator (Feature 4471)
- [ ] Migration guide (Feature 4471)
- [ ] Public documentation and developer experience site (Feature 4472)

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
- [ ] `Context` — review required or deferred.
- [ ] `DelegateJS` — review required or deferred; explicit escape hatch that can bypass Blazor ownership rules.
- [ ] `Error` — review required or deferred.
- [ ] `JQuery` — review required or deferred.
- [ ] `Load` (non-Ajax) — review required or deferred.
- [ ] `Ready` — review required or deferred.
- [ ] `SelectJS` — review required or deferred; explicit escape hatch that can bypass Blazor ownership rules.
- [ ] `Size` — review required or deferred.
- [ ] `Unload` — review required or deferred.

See [docs/PROJECT-PLAN.md](docs/PROJECT-PLAN.md) for the full API tiering, sprint breakdown, and acceptance criteria behind each item.

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

## Building and testing

```powershell
dotnet build Reflections.Blazor.Query.slnx
dotnet test Reflections.Blazor.Query.slnx
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

Licensed under the MIT License.
