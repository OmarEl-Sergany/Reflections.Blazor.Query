# Usage Examples

These examples show the intended consumer-facing patterns. They rely on `AddBlazorQuery()` being registered in the interactive host and queryable elements being declared with `QueryElement` inside a `QueryHost`. See [Getting Started](GETTING-STARTED.md) for setup and host-specific details.

## Batched state updates

Group related changes so the render state emits one coalesced notification:

```csharp
private void Approve()
{
    using (Query.BeginBatch())
    {
        Query.Select("#approval-card")
            .AddClass("approved")
            .Attr("aria-live", "polite")
            .Css("border-color", "seagreen");

        Query.Select(".approval-status").Text("Approved");
    }
}
```

```razor
<QueryHost>
    <QueryElement TagName="article" ElementKey="approval-card" Id="approval-card" Class="approval-card">
        <QueryElement TagName="p" ElementKey="approval-status" Class="approval-status">
            Awaiting review
        </QueryElement>
    </QueryElement>
    <button @onclick="Approve">Approve</button>
</QueryHost>
```

`QueryElement` keys identify owned render elements. Selectors resolve against registered metadata, not arbitrary DOM nodes.

## Isolated reusable control

Place a reusable control under `QueryScopeHost`; the host cascades `IQueryScope` to child components, not back to the component that declares the host. The caller:

```razor
<QueryScopeHost>
    <ScopedAccordion InstanceKey="billing-42" />
</QueryScopeHost>
```

`ScopedAccordion.razor` receives the scope and uses instance-specific element keys:

```razor
<QueryElement TagName="section" ElementKey="@($"{InstanceKey}-root")" Class="accordion">
    <QueryElement TagName="div" ElementKey="@($"{InstanceKey}-details")"
                  ParentElementKey="@($"{InstanceKey}-root")" Class="accordion-details">
        Billing details
    </QueryElement>
</QueryElement>
<button @onclick="ToggleDetails">Toggle details</button>

@code {
    [Parameter, EditorRequired] public string InstanceKey { get; set; } = string.Empty;
    [CascadingParameter] private IQueryScope Scope { get; set; } = default!;

    private void ToggleDetails() => Scope.Select(".accordion-details").Toggle();
}
```

In a parameterized reusable component, derive element keys from a required unique instance key rather than hard-coding `billing-42-*`. Scope isolation is logical API isolation, not a security boundary; keep the unscoped `Query` service for deliberate cross-component orchestration only. Structural operations require correct `ParentElementKey` metadata.

## Preserve layout state across navigation

Keep the layout-owned host mounted around the routed content and nest a default page host inside it:

```razor
@inherits LayoutComponentBase
@inject LayoutQueryRenderState LayoutState

<QueryHost RenderState="LayoutState" ClearOnNavigation="false">
    <QueryElement TagName="aside" ElementKey="global-sidebar" Class="sidebar">
        Shared navigation
    </QueryElement>

    <QueryHost>
        @Body
    </QueryHost>
</QueryHost>
```

Navigation clears the inner page state while retaining the layout state. A host that is disposed still releases its subscriptions; persistence does not mean surviving disposal of the owning layout.

## Read browser dimensions and position

Metrics are read-only browser observations and therefore asynchronous. The element must be mounted in an interactive `QueryHost` so the measurement can flush after render:

```csharp
private async Task ReadPanelAsync()
{
    var panel = Query.Select(".metrics-panel");
    double? width = await panel.WidthAsync();
    QueryPoint? position = await panel.PositionAsync();

    // Both results are nullable: the element may not currently be registered or measurable.
    _lastWidth = width;
    _lastPosition = position;
}
```

Available reads include `WidthAsync`, `HeightAsync`, `InnerWidthAsync`, `InnerHeightAsync`, `OuterWidthAsync`, `OuterHeightAsync`, `OffsetAsync`, `PositionAsync`, and `OffsetParentAsync`. Requests are queued/coalesced through the read-only JS bridge; they do not write to the DOM. See [Measurement and Position Boundary](MEASUREMENT-BOUNDARY.md).

## Direct effects and explicit queues

Slide operations are asynchronous because their height is measured in the browser. Call them directly when the interaction should run immediately:

```csharp
private async Task TogglePanelAsync()
{
    await Query.Select(".details-panel").SlideToggle(TimeSpan.FromMilliseconds(220));
}
```

For ordered work, enqueue callbacks and explicitly start the queue. Queue APIs do not auto-start:

```csharp
private async Task RunSequenceAsync()
{
    var panel = Query.Select(".details-panel");

    panel.Queue(token => Task.Delay(TimeSpan.FromMilliseconds(100), token));
    panel.Queue(token =>
    {
        token.ThrowIfCancellationRequested();
        panel.AddClass("sequence-complete");
        return Task.CompletedTask;
    });

    await panel.Dequeue();
}
```

Token-aware callbacks can respond to cancellation when the owning element or render state is removed. A tokenless `Func<Task>` callback cannot be forcibly preempted. `Promise()` observes queue emptiness; it does not start queued work.

## Delegated events

Register delegation on a queryable root and specify the descendant selector. The event bridge reports the closest library-owned element key; the library validates root, scope, and selector before invoking the C# callback:

```csharp
private void RegisterDelegation()
{
    Query.Select(".delegation-root")
        .DelegateJS(".delegation-child", "click", target =>
        {
            _lastClickedKey = target.ElementKey;
        });
}
```

The root and delegated descendants must be registered `QueryElement` instances; set `ParentElementKey` to describe their ownership relationship. Load and register the supplied initializer before `Blazor.start()` as described in [Getting Started](GETTING-STARTED.md). This is constrained bubbling-event support, not arbitrary JavaScript execution.
