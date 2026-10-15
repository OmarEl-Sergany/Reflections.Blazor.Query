---
title: Reflections.Blazor.Query
description: Blazor-native fluent element orchestration, rendered through library-owned state.
---

# Reflections.Blazor.Query

**A fluent query API for Blazor that keeps Blazor in control of rendering.**

Reflections.Blazor.Query brings familiar jQuery-style orchestration to library-owned Blazor elements. Calls update library-owned render state; Blazor renders the result. The library does not directly mutate Blazor-owned DOM nodes or manipulate `RenderTreeFrame`.

> **Release status:** pre-release. The NuGet package is not published. Follow [Getting Started](GETTING-STARTED.md) to use a project reference or run the repository demos.

## Start building

- [Get started](GETTING-STARTED.md) — register services and run the first state-driven query.
- [Integration fit](INTEGRATION-FIT.md) — supported ownership and interactivity requirements, including CMS and static SSR constraints.
- [Explore examples](EXAMPLES.md) — batching, scopes, layout state, measurements, effects, queues, and delegation.
- [Browse the API reference](api/toc.yml) — generated from the current library assembly.

## Hosting targets

| Host | Support |
| --- | --- |
| Interactive Server | Supported and exercised by the demo and browser suite |
| Interactive WebAssembly | Supported and exercised by the demo and browser suite |
| Interactive Auto | Supported and exercised by the demo and browser suite |
| Static SSR | Declarative output only; interaction-dependent query operations require interactivity |

## Architectural contract

```text
C# query operation
        ↓
Library-owned render state
        ↓
Blazor render/diff lifecycle
        ↓
Browser DOM
```

Browser measurements and successful form values are read through a constrained, read-only JS interop bridge. Results return to C#; the bridge does not write to the DOM. Query operations resolve registered `QueryElement` nodes, not arbitrary page markup.

## Project status

The implementation plan tracks the jQuery 4-aligned function matrix, completed and deferred APIs, and remaining release work. See [Implementation plan and API matrix](PROJECT-PLAN.md). Ajax methods are intentionally excluded; use Blazor's `HttpClient`, forms, routing, and event callbacks.
