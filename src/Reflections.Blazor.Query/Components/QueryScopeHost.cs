using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Reflections.Blazor.Query;

public sealed class QueryScopeHost : ComponentBase
{
    [Inject] private BlazorQuery Query { get; set; } = default!;
    [Parameter, EditorRequired] public RenderFragment? ChildContent { get; set; }
    private IQueryScope? _scope;
    protected override void OnInitialized() => _scope = Query.CreateScope();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<IQueryScope>>(0);
        builder.AddComponentParameter(1, nameof(CascadingValue<IQueryScope>.Value), _scope);
        builder.AddComponentParameter(2, nameof(CascadingValue<IQueryScope>.IsFixed), true);
        builder.AddComponentParameter(3, nameof(CascadingValue<IQueryScope>.ChildContent), ChildContent);
        builder.CloseComponent();
    }
}
