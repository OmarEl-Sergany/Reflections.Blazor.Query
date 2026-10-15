# Integration fit and rendering constraints

Reflections.Blazor.Query is an orchestration API for elements that the library can track and Blazor can render. It is not a document-wide selector and does not take ownership of arbitrary markup.

## What must be true

| Requirement | Contract |
| --- | --- |
| Element ownership | A target must be represented by a registered `QueryElement` inside a `QueryHost`. Ordinary HTML, third-party component output, and CMS-rendered markup are not automatically queryable. |
| Interactive behavior | Event-driven operations need an interactive Blazor render tree: Interactive Server, WebAssembly, or Auto. Static SSR can emit the initial declarative output, but cannot run query operations in response to events. |
| Rendering authority | Operations update library-owned state and Blazor renders the result. Direct DOM changes made by JavaScript or other libraries are outside this contract. |
| Reusable controls | Use `QueryScopeHost` and `IQueryScope` when a control should be restricted to its registered subtree. This is logical API isolation, not a security boundary. |

## CMS and existing component trees

There is no adapter that makes an existing CMS block renderer, ordinary Razor markup, or arbitrary component output queryable. For a CMS-rendered block to participate, its interactive surface must be authored or adapted so that the target elements are represented by `QueryElement`s with stable unique keys and the required parent metadata, under a `QueryHost`.

That conversion can be intrusive when a block is already a component with its own parameters, callbacks, or lifecycle. Do not replace a component's state model merely to use this library. Keep ordinary Blazor component state for local toggles, tabs, FAQ disclosure, active navigation, and similar interactions when it is the simpler fit.

A reasonable adoption path is to select one bounded interactive island, confirm that its elements can be represented as `QueryElement`s, enable an interactive render mode for that island, and validate the behavior in the intended host. If the required changes spread across the CMS renderer or force unrelated static content to become interactive, use standard Blazor patterns instead.

## Static SSR

Static SSR is supported only for declarative initial output. It does not make `BlazorQuery` interactive: event-driven updates, browser measurements, and render-state changes require an interactive component tree. An application may keep the surrounding page statically rendered and introduce a Blazor interactive island for a selected region, but the island still needs `QueryHost` and registered `QueryElement` targets.

See [Getting Started](GETTING-STARTED.md) for host setup and [Usage Examples](EXAMPLES.md) for registered elements, scopes, and state lifetimes.
