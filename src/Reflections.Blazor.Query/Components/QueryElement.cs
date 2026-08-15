using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Reflections.Blazor.Query;

public sealed class QueryElement : ComponentBase, IDisposable
{
    [Inject] private QueryElementRegistry Registry { get; set; } = default!;

    [Parameter, EditorRequired] public string ElementKey { get; set; } = string.Empty;
    [Parameter] public string TagName { get; set; } = "div";
    [Parameter] public string? Id { get; set; }
    [Parameter] public string? Class { get; set; }
    [Parameter] public IReadOnlyList<string>? Selectors { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string? _registrationSignature;
    private string? _registeredKey;

    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ElementKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(TagName);

        var selectors = BuildSelectors();
        var signature = $"{ElementKey}\u001f{string.Join("\u001f", selectors)}";
        if (signature == _registrationSignature)
            return;

        if (_registeredKey is not null)
            Registry.Unregister(_registeredKey);

        Registry.Register(ElementKey, selectors);
        _registeredKey = ElementKey;
        _registrationSignature = signature;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, TagName);
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "data-rbq-key", ElementKey);
        if (!string.IsNullOrWhiteSpace(Id))
            builder.AddAttribute(3, "id", Id);
        if (!string.IsNullOrWhiteSpace(Class))
            builder.AddAttribute(4, "class", Class);
        builder.AddContent(5, ChildContent);
        builder.CloseElement();
    }

    public void Dispose()
    {
        if (_registeredKey is not null)
            Registry.Unregister(_registeredKey);
    }

    private string[] BuildSelectors()
    {
        var selectors = new HashSet<string>(StringComparer.Ordinal) { TagName };
        if (!string.IsNullOrWhiteSpace(Id))
            selectors.Add($"#{Id}");
        if (!string.IsNullOrWhiteSpace(Class))
        {
            foreach (var className in Class.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                selectors.Add($".{className}");
        }
        if (Selectors is not null)
        {
            foreach (var selector in Selectors)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(selector);
                selectors.Add(selector);
            }
        }
        return selectors.ToArray();
    }
}
