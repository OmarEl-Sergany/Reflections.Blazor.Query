# Reflections.Blazor.Query

A Blazor-native, jQuery 4-aligned fluent query library for .NET 10. It uses library-owned render state and generated element metadata to orchestrate Blazor-rendered elements without directly mutating Blazor-owned DOM.

## Architectural Intent

### 🚫 Why No Ajax?

Reflections.Blazor.Query purposefully excludes jQuery Ajax methods. Blazor native applications already provide robust, state-integrated alternatives through `HttpClient`, native form handling, circuit-aware routing, and component `EventCallback`s. This library focuses purely on fluent state-driven element orchestration.

## Design principles

- Blazor remains the sole renderer for library-owned elements.
- Fluent operations update library-owned state; they do not mutate `RenderTreeFrame` or Blazor-owned DOM nodes.
- Browser layout metrics use a read-only, post-render JS interop boundary.
- Interactive Server, WebAssembly, and Auto are first-class targets.

## Project status

The active implementation phase is Sprint 2 / Feature 4467: attributes, properties, data, text, and dimensions. The current public delivery plan and API roadmap are in [docs/PROJECT-PLAN.md](docs/PROJECT-PLAN.md). The layout-measurement contract is in [docs/MEASUREMENT-BOUNDARY.md](docs/MEASUREMENT-BOUNDARY.md).

## Development

```powershell
dotnet test Reflections.Blazor.Query.slnx
dotnet build Reflections.Blazor.Query.slnx --no-restore
```
