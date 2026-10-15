# Migration Guide

Reflections.Blazor.Query is still pre-release and has no published NuGet package. This guide describes the architectural mapping for teams moving jQuery-oriented UI code into Blazor; it is not a promise of drop-in jQuery compatibility.

## Breaking API change

The injectable service type was renamed from `Query` to `BlazorQuery` to avoid the confusing `Query.Query` naming. Update registrations only if you manually registered the service; `AddBlazorQuery()` now registers `BlazorQuery` automatically. In Razor, change `@inject Query Query` to `@inject BlazorQuery Query`. Existing fluent calls such as `Query.Select(...)` do not change.

## Change the rendering model

The central migration is from “select any live DOM node and mutate it” to “declare a queryable `QueryElement`, update its library-owned state, and let Blazor render it.”

| Existing jQuery pattern | Reflections.Blazor.Query pattern |
| --- | --- |
| `$(selector)` | Inject `BlazorQuery` (commonly as `Query`) and use `Query.Select(selector)` |
| Select arbitrary document elements | Register queryable nodes with `QueryElement` inside `QueryHost` |
| `.css()`, `.attr()`, `.addClass()` | Fluent state operations such as `.Css()`, `.Attr()`, `.AddClass()` |
| `$(document).ready(...)` | Use Blazor component lifecycle methods and event callbacks |
| `$.ajax(...)` | Use `HttpClient`, Blazor forms, and normal async component state |
| Global selector shared by reusable controls | Put the child control below `QueryScopeHost` and use its cascaded `IQueryScope` |

## First integration steps

1. Add the library project reference while the package remains unpublished.
2. Register `AddBlazorQuery()` in every host that can render the interactive component tree. Interactive Auto requires server and client registration.
3. Wrap the interactive content in `QueryHost`.
4. Replace each target whose state should be orchestrated with a `QueryElement` and give it a stable, unique `ElementKey`.
5. Replace imperative DOM writes with C# query operations. Use `BeginBatch()` around related updates.
6. Choose state lifetime intentionally: default page state clears on navigation; a layout host can preserve layout state; application state requires a long-lived host.

The complete setup and examples are in [Getting Started](GETTING-STARTED.md) and [Usage Examples](EXAMPLES.md).

## Compatibility differences to account for

- **Selectors:** selection resolves library-registered metadata. It does not run arbitrary CSS selectors against the browser document. The managed selector subset is intentionally constrained; selectors unsupported by the registry do not become browser queries.
- **Scopes:** `IQueryScope` constrains resolution and traversal for reusable controls. It is logical API isolation, not a security boundary. Unscoped `Query` is still available for deliberate broader orchestration.
- **Metrics:** use asynchronous `WidthAsync`, `HeightAsync`, `OffsetAsync`, `PositionAsync`, and related methods. Browser layout values exist only after rendering and may be `null` when an element is not currently measurable.
- **Effects:** measured slide effects are awaitable. Queued effects and callbacks do not run until `Dequeue()`; `Promise()` observes queue completion but does not start work.
- **HTML:** `Text`, `Append`, and `Prepend` render encoded text. `Html`, `AppendHtml`, `PrependHtml`, and element-creation methods with `isHtml: true` render raw markup without sanitizing it. Sanitize CMS/user-controlled HTML under the application's allow-list policy first; use text APIs when markup is unnecessary.
- **Events:** handlers are library-owned state rendered as Blazor event bindings. `DelegateJS` is a constrained bubbling-event bridge and requires the host initializer before Blazor starts.
- **Lifecycle:** page render state clears on navigation. Layout state persists while its owning layout host remains mounted. Disposal removes registrations and cancels token-aware asynchronous work.

## Intentionally unsupported or different

Ajax callbacks and document readiness are not mirrored. `AndSelf`, `Die`, and `Live` are legacy jQuery APIs; use the current collection and event APIs instead. `Context` and `Size` are not part of the target surface. `Clone()` duplicates library-created virtual elements, not a live Razor component instance.

See the [implementation plan and API matrix](PROJECT-PLAN.md) for the complete implemented/deferred status. Revisit this guide before the first stable package release, when version-specific migration notes and compatibility guarantees can be defined.
