# Getting Started

Reflections.Blazor.Query targets .NET 10 and supports Interactive Server, Interactive WebAssembly, and Interactive Auto. The package has not been published to NuGet yet; until release, consume it through a project reference or run the included demo applications.

## 1. Reference the library

From your application project, add a project reference to `src/Reflections.Blazor.Query/Reflections.Blazor.Query.csproj` in this repository. For example, adjust the relative path to match your app's location:

```powershell
dotnet add reference ..\..\src\Reflections.Blazor.Query\Reflections.Blazor.Query.csproj
```

After a package release, use the published package version documented in the release notes instead.

## 2. Register services in the interactive host

Register once in each process that hosts the interactive components:

```csharp
// Program.cs
using Reflections.Blazor.Query;

builder.Services.AddBlazorQuery();
```

For Interactive Server, this is the server app. For Interactive WebAssembly, register in the client app. For Interactive Auto, register in both the server and client startup projects, as each can host the component tree.

The registration uses scoped services for the current Blazor circuit/client. Components do not get separate DI containers; component isolation is provided through `QueryScopeHost` and `IQueryScope` when needed.

## 3. Add the component namespace

Add the namespace to `_Imports.razor` if it is not already present:

```razor
@using Reflections.Blazor.Query
```

## 4. Host queryable elements and update their state

`QueryHost` provides the render-state and after-render lifecycle. `QueryElement` declares a library-owned element that can be resolved by the injected `BlazorQuery` service; ordinary HTML outside a `QueryElement` is not part of the query registry.

```razor
@page "/query-demo"
@inject BlazorQuery Query

<QueryHost>
    <QueryElement TagName="article" ElementKey="release-card" Class="release-card">
        <h2>Release candidate</h2>
        <QueryElement TagName="p" ElementKey="release-status" Class="release-status">
            Awaiting verification.
        </QueryElement>
    </QueryElement>

    <button @onclick="MarkVerified">Mark verified</button>
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

The operation changes library-owned render state; Blazor renders the result. It does not write styles or attributes directly into the browser DOM. See [Examples](EXAMPLES.md) for component isolation, layout persistence, measurements, effects, and queues.

This does not make ordinary HTML or third-party/CMS component output queryable. Targets must be registered `QueryElement`s under a `QueryHost`. See [Integration fit](INTEGRATION-FIT.md) before adapting an existing component or CMS-rendered block tree.

## State lifetime

The default `QueryHost` uses page-scoped `QueryRenderState` and clears it on navigation. A layout can own persistent state while placing a nested default host around the routed page:

```razor
@inject LayoutQueryRenderState LayoutState

<QueryHost RenderState="LayoutState" ClearOnNavigation="false">
    <header>
        <QueryElement TagName="div" ElementKey="app-header" Class="app-header">
            Application header
        </QueryElement>
    </header>

    <QueryHost>
        @Body
    </QueryHost>
</QueryHost>
```

This is the same lifetime pattern used by the demo layout. For application-lifetime state, use `ApplicationQueryRenderState` with a long-lived host and `ClearOnNavigation="false"`; use it deliberately because the state outlives individual pages and layouts.

## Component-level isolation

Place the reusable control below `QueryScopeHost`, then select through the control's cascaded `IQueryScope`. The scope host cascades to child components; it does not cascade back into the component that declares the host. This constrains resolution, traversal, and cleanup while sharing the normal circuit/client services:

```razor
<QueryScopeHost>
    <AccountPanel InstanceKey="account-42" />
</QueryScopeHost>
```

Inside `AccountPanel.razor`, receive the cascaded scope and declare the owned elements:

```razor
<QueryElement TagName="section" ElementKey="@($"{InstanceKey}-root")" Class="account-panel">
    <QueryElement TagName="div" ElementKey="@($"{InstanceKey}-body")"
                  ParentElementKey="@($"{InstanceKey}-root")" Class="panel-body">
        Account details
    </QueryElement>
</QueryElement>

<button @onclick="ToggleBody">Toggle details</button>

@code {
    [Parameter, EditorRequired] public string InstanceKey { get; set; } = string.Empty;
    [CascadingParameter] private IQueryScope Scope { get; set; } = default!;

    private void ToggleBody() => Scope.Select(".panel-body").Toggle();
}
```

Supply unique element keys for repeated control instances. Set `ParentElementKey` for registered descendants when an operation depends on structural parent/child relationships (for example, delegated event roots or traversal). This is an API isolation boundary, not a security boundary: code with access to the unscoped `BlazorQuery` service can still perform page/layout orchestration. See [Component Query Scoping](COMPONENT-SCOPING.md).

## Raw HTML and untrusted content

`Html`, `AppendHtml`, `PrependHtml`, and virtual-element creation with `isHtml: true` emit raw markup through Blazor; the library does not sanitize it. If content originates from a CMS, a user, or another untrusted source, sanitize it with the application's approved HTML allow-list policy before using these APIs. Prefer `Text`, `Append`, `Prepend`, or the default `isHtml: false` when the value should be displayed as text. See [Text versus trusted markup](EXAMPLES.md#text-versus-trusted-markup).

## Delegated browser events (optional)

Only applications that use `DelegateJS` need the delegated-event initializer. It must be registered before Blazor starts. Follow the ordering used in the demo host document:

```html
<script src="_framework/blazor.web.js" autostart="false"></script>
<script src="_content/Reflections.Blazor.Query/queryEvents.js"></script>
<script>
    ReflectionsBlazorQuery.registerDelegatedEvents();
    Blazor.start();
</script>
```

Do not add a second `Blazor.start()` if the host already starts Blazor elsewhere. Delegation supports the library's finite bubbling-event set; C# retains and validates handler state. The initializer is not a general-purpose JavaScript interop escape hatch.

## Hosting and browser-read boundary

Interactive operations require an interactive component tree hosted by `QueryHost`. Static SSR can emit the declarative initial markup, but it cannot execute event-driven query operations. Browser layout reads such as `WidthAsync`, `HeightAsync`, and `PositionAsync` are asynchronous, nullable, read-only observations flushed after render by `QueryHost`; they do not mutate DOM state. See [Measurement and Position Boundary](MEASUREMENT-BOUNDARY.md).

## Run the demos

The repository contains Server, WebAssembly, and Auto demos under `samples/`. Build and test from the solution root:

```powershell
dotnet build Reflections.Blazor.Query.slnx
dotnet test Reflections.Blazor.Query.slnx
```

The demos are the executable compatibility reference for host registration, routing, delegated-event initialization, and real-browser behavior.
