# Reflections.Blazor.Query

> A Blazor-native, jQuery 4-aligned fluent query library for .NET 10.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
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

## Current status (as of 2026-08-28, `master`)

`master` contains the completed foundation only (Sprint 0). Everything else below is in progress on stacked feature branches and has not been merged yet.

### Done (on `master`)

- [x] Generated per-element render-state metadata and stable generated element keys.
- [x] Page, layout, and application render scopes with correct navigation/disposal semantics.
- [x] Batched updates (`BeginBatch`) producing a single coalesced render.
- [x] Shared DI registration for the library.
- [x] Server, WebAssembly, and Auto demo hosts with working proof-of-concept pages.
- [x] Foundation test coverage: full solution build with 0 errors, unit/component tests passing; live browser validation on all three interactive hosting models.

### In progress (not yet merged to `master`)

- [ ] **Selection and traversal** (`feature/4465-foundation-render-state`, `feature/4466-selection-traversal`) — `Select`, `Length`, `Eq`, `First`, `Last`, `Slice`, `ToArray`, `Each`, `Map`, `End`, `PushStack`, `Find`, `Children`, `Closest`, `Parent(s)`, `Next*`/`Prev*`, `Siblings`, `Filter`, `Not`, `Has`, `Is`, `Index`.
- [ ] **Attributes, properties, data, text, and dimensions** (`feature/4467-attributes-data`) — `Attr`, `Prop`, `Data`, `RemoveAttr`/`RemoveProp`/`RemoveData`, `Text`, `Html`, `Val`, `Height`/`Width` and their inner/outer variants, `Offset`/`Position`/`OffsetParent`.

### Remaining (not started)

- [ ] **Classes and DOM structure** (Sprint 3 / Feature 4468) — `AddClass`/`RemoveClass`/`ToggleClass`/`HasClass`, `Show`/`Hide`/`Toggle`, `Append`/`Prepend`/`Before`/`After`/`Replace*`/`Wrap*`/`Remove`/`Empty`/`Detach`/`Clone`, and component scoping (`ComponentScopeKey`, `IQueryScope`).
- [ ] **Events and forms** (Sprint 4 / Feature 4469) — `On`/`Off`/`One`/`Trigger`/`TriggerHandler`, keyboard/pointer/focus/form/resize/scroll event coverage, `Serialize`/`SerializeArray`.
- [ ] **Effects and queues** (Sprint 5 / Feature 4470) — `Animate`, fade/slide effects, `Queue`/`Dequeue`/`ClearQueue`, `Delay`, `Stop`, `Finish`, `Promise`.
- [ ] **Compatibility, packaging, and release** (Sprint 6 / Feature 4471) — Tier 4 API decisions (`DelegateJS`, `SelectJS`, `Context`, etc.), independent NuGet packaging for the library and generator, migration guide.
- [ ] **Documentation and developer experience** (Feature 4472) — public docs beyond the internal project plan.
- Ajax-family APIs (`AjaxComplete`, `AjaxError`, `AjaxSend`, `AjaxStart`, `AjaxStop`, `AjaxSuccess`, Ajax-form `Load`) and legacy-removed APIs (`AndSelf`, `Die`, `Live`, `Delegate`, `Undelegate`) are intentionally excluded — see the mission statement above.

See [docs/PROJECT-PLAN.md](docs/PROJECT-PLAN.md) for the full API tiering, sprint breakdown, and acceptance criteria behind each item.

## Building and testing

```powershell
dotnet build Reflections.Blazor.Query.slnx
dotnet test Reflections.Blazor.Query.slnx
```

## Contributing

1. `master` is the only long-lived branch. Work on a short-lived `feature/`/`chore/` branch and submit a Pull Request.
2. Use [Conventional Commits](https://www.conventionalcommits.org/) for every commit.
3. Preserve the core contract: writes remain library-owned render state; JavaScript is observation-only unless an explicitly reviewed design changes that boundary.
4. Add tests and update `docs/PROJECT-PLAN.md` when public behavior changes.

## License

Licensed under the MIT License.
