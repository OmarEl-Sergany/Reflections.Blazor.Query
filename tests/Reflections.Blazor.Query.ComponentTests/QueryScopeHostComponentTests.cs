using Bunit;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Reflections.Blazor.Query;

namespace Reflections.Blazor.Query.ComponentTests;

file sealed class QueryScopeProbe : ComponentBase
{
    [CascadingParameter] private IQueryScope? Scope { get; set; }
    [Parameter, EditorRequired] public string ElementKey { get; set; } = string.Empty;
    [Parameter] public Action<IQueryScope>? CaptureScope { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override void OnParametersSet()
    {
        if (Scope is not null)
            CaptureScope?.Invoke(Scope);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<QueryElement>(0);
        builder.AddComponentParameter(1, nameof(QueryElement.ElementKey), ElementKey);
        builder.AddComponentParameter(2, nameof(QueryElement.TagName), "section");
        builder.AddComponentParameter(3, nameof(QueryElement.Class), "scope-probe");
        builder.AddComponentParameter(4, nameof(QueryElement.ChildContent), ChildContent);
        builder.CloseComponent();
    }
}

file sealed class NestedScopeIntegrationHarness : ComponentBase
{
    [Parameter] public Action<IQueryScope>? CaptureOuter { get; set; }
    [Parameter] public Action<IQueryScope>? CaptureNested { get; set; }
    [Parameter] public Action<IQueryScope>? CaptureSibling { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<QueryScopeHost>(0);
        builder.AddComponentParameter(1, nameof(QueryScopeHost.ChildContent), (RenderFragment)(outer =>
        {
            outer.OpenComponent<QueryScopeProbe>(0);
            outer.AddComponentParameter(1, nameof(QueryScopeProbe.ElementKey), "outer-element");
            outer.AddComponentParameter(2, nameof(QueryScopeProbe.CaptureScope), CaptureOuter);
            outer.CloseComponent();

            outer.OpenComponent<QueryScopeHost>(3);
            outer.AddComponentParameter(4, nameof(QueryScopeHost.ChildContent), (RenderFragment)(nested =>
            {
                nested.OpenComponent<QueryScopeProbe>(0);
                nested.AddComponentParameter(1, nameof(QueryScopeProbe.ElementKey), "nested-element");
                nested.AddComponentParameter(2, nameof(QueryScopeProbe.CaptureScope), CaptureNested);
                nested.CloseComponent();
            }));
            outer.CloseComponent();
        }));
        builder.CloseComponent();

        builder.OpenComponent<QueryScopeHost>(5);
        builder.AddComponentParameter(6, nameof(QueryScopeHost.ChildContent), (RenderFragment)(sibling =>
        {
            sibling.OpenComponent<QueryScopeProbe>(0);
            sibling.AddComponentParameter(1, nameof(QueryScopeProbe.ElementKey), "sibling-element");
            sibling.AddComponentParameter(2, nameof(QueryScopeProbe.CaptureScope), CaptureSibling);
            sibling.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

file sealed class ScopeRerenderHarness : ComponentBase
{
    [Parameter] public bool ShowElement { get; set; }
    [Parameter] public Action<IQueryScope>? CaptureScope { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<QueryScopeHost>(0);
        builder.AddComponentParameter(1, nameof(QueryScopeHost.ChildContent), (RenderFragment)(child =>
        {
            if (!ShowElement)
                return;

            child.OpenComponent<QueryScopeProbe>(0);
            child.AddComponentParameter(1, nameof(QueryScopeProbe.ElementKey), "rerendered-element");
            child.AddComponentParameter(2, nameof(QueryScopeProbe.CaptureScope), CaptureScope);
            child.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

public sealed class QueryScopeHostComponentTests
{
    [Fact]
    public async Task QueryScopeHost_cascades_isolated_scopes_across_repeated_and_nested_hosts()
    {
        await using var context = CreateContext();
        IQueryScope? outerScope = null;
        IQueryScope? nestedScope = null;
        IQueryScope? siblingScope = null;

        var cut = context.Render<NestedScopeIntegrationHarness>(parameters => parameters
            .Add(component => component.CaptureOuter, scope => outerScope = scope)
            .Add(component => component.CaptureNested, scope => nestedScope = scope)
            .Add(component => component.CaptureSibling, scope => siblingScope = scope));

        Assert.NotNull(outerScope);
        Assert.NotNull(nestedScope);
        Assert.NotNull(siblingScope);
        Assert.NotEqual(outerScope.Key, nestedScope.Key);
        Assert.NotEqual(outerScope.Key, siblingScope.Key);
        Assert.Equal(["outer-element"], outerScope.Select(".scope-probe").ToArray());
        Assert.Equal(["nested-element"], nestedScope.Select(".scope-probe").ToArray());
        Assert.Equal(["sibling-element"], siblingScope.Select(".scope-probe").ToArray());

        outerScope.Select(".scope-probe").AddClass("outer-owned");
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("outer-owned", cut.Find("[data-rbq-key='outer-element']").ClassList);
            Assert.DoesNotContain("outer-owned", cut.Find("[data-rbq-key='nested-element']").ClassList);
            Assert.DoesNotContain("outer-owned", cut.Find("[data-rbq-key='sibling-element']").ClassList);
        });
    }

    [Fact]
    public async Task QueryScopeHost_rerender_preserves_scope_and_removing_descendant_unregisters_it()
    {
        await using var context = CreateContext();
        IQueryScope? capturedScope = null;
        var cut = context.Render<ScopeRerenderHarness>(parameters => parameters
            .Add(component => component.ShowElement, true)
            .Add(component => component.CaptureScope, scope => capturedScope = scope));

        Assert.NotNull(capturedScope);
        Assert.Equal(["rerendered-element"], capturedScope.Select(".scope-probe").ToArray());

        cut.Render(parameters => parameters
            .Add(component => component.ShowElement, false)
            .Add(component => component.CaptureScope, scope => capturedScope = scope));

        Assert.Equal([], capturedScope.Select(".scope-probe").ToArray());
        Assert.DoesNotContain("data-rbq-key=\"rerendered-element\"", cut.Markup, StringComparison.Ordinal);

        cut.Render(parameters => parameters
            .Add(component => component.ShowElement, true)
            .Add(component => component.CaptureScope, scope => capturedScope = scope));

        Assert.Equal(["rerendered-element"], capturedScope.Select(".scope-probe").ToArray());
    }

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.Services.AddBlazorQuery();
        context.Services.AddSingleton<IQueryMeasurementBridge, TestMeasurementBridge>();
        context.Services.AddSingleton<IQueryFormBridge, TestFormBridge>();
        return context;
    }
}
