using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Reflections.Blazor.Query;

public sealed class QueryElement : ComponentBase, IDisposable
{
    [Inject] private QueryElementRegistry Registry { get; set; } = default!;
    [Inject] private QueryRenderState DefaultRenderState { get; set; } = default!;
    [Inject] private QueryMeasurementCoordinator Measurements { get; set; } = default!;
    [Inject] private QueryFormCoordinator Forms { get; set; } = default!;
    [CascadingParameter] private QueryRenderState? CascadedRenderState { get; set; }

    [Parameter, EditorRequired] public string ElementKey { get; set; } = string.Empty;
    [Parameter] public string? ParentElementKey { get; set; }
    [Parameter] public string TagName { get; set; } = "div";
    [Parameter] public string? Id { get; set; }
    [Parameter] public string? Class { get; set; }
    [Parameter] public IReadOnlyList<string>? Selectors { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string? _registrationSignature;
    private string? _registeredKey;
    private QueryRenderState? _registeredRenderState;
    private QueryRenderState? _subscribedRenderState;

    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ElementKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(TagName);

        var renderState = CascadedRenderState ?? DefaultRenderState;
        if (!ReferenceEquals(renderState, _subscribedRenderState))
        {
            _subscribedRenderState?.Changed -= OnRenderStateChanged;
            _subscribedRenderState = renderState;
            _subscribedRenderState.Changed += OnRenderStateChanged;
        }

        var keyChanged = _registeredKey is not null && !string.Equals(_registeredKey, ElementKey, StringComparison.Ordinal);
        var renderStateChanged = _registeredRenderState is not null && !ReferenceEquals(_registeredRenderState, renderState);

        // Only tear down the real-element registration (and, transitively, any relocation bookkeeping
        // it holds via UnregisterRealElement's HandleHostRemoval cascade) when this component's old
        // (key, render state) identity is genuinely retiring — never for an ordinary same-key,
        // same-render-state reparent or class/id change, which must not disturb children relocated onto
        // this element.
        if ((keyChanged || renderStateChanged) && _registeredKey is not null)
            _registeredRenderState?.UnregisterRealElement(_registeredKey);

        renderState.RegisterRealElement(ElementKey, RenderSelf);
        renderState.RegisterInnerContent(ElementKey, ChildContent ?? EmptyContent);

        var selectors = BuildSelectors();
        var signature = $"{ElementKey}\u001f{ParentElementKey}\u001f{string.Join("\u001f", selectors)}";
        if (!keyChanged && !renderStateChanged && signature == _registrationSignature)
        {
            if (!renderState.IsRelocatedElsewhere(ElementKey))
                Registry.SetParent(ElementKey, ParentElementKey);
            return;
        }

        if (_registeredKey is not null)
            Registry.Unregister(_registeredKey); // Registry has no in-place update; always re-add below.

        Registry.RegisterWithParent(ElementKey, ParentElementKey, selectors);
        _registeredKey = ElementKey;
        _registeredRenderState = renderState;
        _registrationSignature = signature;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var renderState = _subscribedRenderState ?? DefaultRenderState;
        if (renderState.IsRelocatedElsewhere(ElementKey))
            return; // rendered by its current host instead; emitting here too would duplicate it

        RenderSelf(builder);
    }

    private void RenderSelf(RenderTreeBuilder builder)
    {
        var renderState = _subscribedRenderState ?? DefaultRenderState;
        if (renderState.IsRemoved(ElementKey))
            return;

        builder.OpenElement(0, TagName);
        builder.SetKey(ElementKey);
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "data-rbq-key", ElementKey);
        if (!string.IsNullOrWhiteSpace(Id))
            builder.AddAttribute(3, "id", Id);
        var sequence = 4;
        var className = BuildClassName(renderState);
        if (!string.IsNullOrEmpty(className))
            builder.AddAttribute(sequence++, "class", className);
        if (renderState.IsHidden(ElementKey))
            builder.AddAttribute(sequence++, "style", BuildHiddenStyle());
        foreach (var attribute in renderState.AttributesFor(ElementKey))
            builder.AddAttribute(sequence++, attribute.Key, attribute.Value);
        foreach (var property in renderState.PropertiesFor(ElementKey))
            builder.AddAttribute(sequence++, property.Key, property.Value);
        foreach (var fragment in renderState.PrependedFor(ElementKey))
            AddFragment(builder, ref sequence, fragment);
        var content = renderState.ContentFor(ElementKey);
        if (content is { IsHtml: true })
            builder.AddMarkupContent(sequence++, content.Value);
        else if (content is not null)
            builder.AddContent(sequence++, content.Value);
        else if (renderState.InnerWrapperFor(ElementKey) is { } innerWrapperKey)
            RenderVirtualNode(builder, ref sequence, renderState, innerWrapperKey);
        else
            builder.AddContent(sequence++, ChildContent);
        foreach (var fragment in renderState.AppendedFor(ElementKey))
            AddFragment(builder, ref sequence, fragment);
        RenderChildren(builder, ref sequence, renderState, ElementKey, renderState.InnerWrapperFor(ElementKey));
        builder.CloseElement();
    }

    private static void AddFragment(RenderTreeBuilder builder, ref int sequence, QueryContent fragment)
    {
        if (fragment.IsHtml)
            builder.AddMarkupContent(sequence++, fragment.Value);
        else
            builder.AddContent(sequence++, fragment.Value);
    }

    private static void RenderChildren(RenderTreeBuilder builder, ref int sequence, QueryRenderState renderState, string hostKey, string? excludedChildKey = null)
    {
        foreach (var childKey in renderState.VirtualChildKeysFor(hostKey))
        {
            if (string.Equals(childKey, excludedChildKey, StringComparison.Ordinal))
                continue;
            var realFragment = renderState.RealElementFragmentFor(childKey);
            if (realFragment is not null)
                builder.AddContent(sequence++, realFragment);
            else if (renderState.IsVirtual(childKey))
                RenderVirtualNode(builder, ref sequence, renderState, childKey);
            // else: a real element was relocated here but hasn't registered its fragment yet this pass
            // (its own QueryElement hasn't mounted/re-rendered this render). Render nothing for it now —
            // RegisterRealElement notifies this host to re-render as soon as it does register, so the gap
            // is a transient, self-healing one, not a silent permanent drop.
        }
    }

    private static void RenderVirtualNode(RenderTreeBuilder builder, ref int sequence, QueryRenderState renderState, string elementKey)
    {
        var node = renderState.VirtualNodeFor(elementKey);
        if (node is null || renderState.IsRemoved(elementKey))
            return;

        builder.OpenElement(sequence++, node.TagName);
        builder.SetKey(elementKey);
        builder.AddAttribute(sequence++, "data-rbq-key", elementKey);
        var className = BuildClassName(renderState, elementKey, node.ClassName);
        if (!string.IsNullOrEmpty(className))
            builder.AddAttribute(sequence++, "class", className);
        if (renderState.IsHidden(elementKey))
            builder.AddAttribute(sequence++, "style", "display: none;");
        foreach (var attribute in renderState.AttributesFor(elementKey))
            builder.AddAttribute(sequence++, attribute.Key, attribute.Value);
        foreach (var property in renderState.PropertiesFor(elementKey))
            builder.AddAttribute(sequence++, property.Key, property.Value);
        foreach (var fragment in renderState.PrependedFor(elementKey))
            AddFragment(builder, ref sequence, fragment);
        var content = renderState.ContentFor(elementKey) ?? (node.Text is null ? null : new QueryContent(node.Text, node.IsHtml));
        if (content is { IsHtml: true })
            builder.AddMarkupContent(sequence++, content.Value);
        else if (content is not null)
            builder.AddContent(sequence++, content.Value);
        if (renderState.InnerContentForWrapper(elementKey) is { } innerContent)
            builder.AddContent(sequence++, innerContent);
        foreach (var fragment in renderState.AppendedFor(elementKey))
            AddFragment(builder, ref sequence, fragment);
        RenderChildren(builder, ref sequence, renderState, elementKey);
        builder.CloseElement();
    }

    public void Dispose()
    {
        if (_registeredKey is not null)
        {
            Registry.Unregister(_registeredKey);
            Measurements.InvalidateElement(_subscribedRenderState ?? DefaultRenderState, _registeredKey);
            Forms.InvalidateElement(_subscribedRenderState ?? DefaultRenderState, _registeredKey);
            _registeredRenderState?.UnregisterInnerContent(_registeredKey);
            _registeredRenderState?.UnregisterRealElement(_registeredKey);
        }
        if (_subscribedRenderState is not null)
            _subscribedRenderState.Changed -= OnRenderStateChanged;
    }

    private void OnRenderStateChanged() => _ = InvokeAsync(StateHasChanged);

    private static readonly RenderFragment EmptyContent = _ => { };

    private string BuildHiddenStyle()
    {
        if (AdditionalAttributes is not null
            && AdditionalAttributes.TryGetValue("style", out var existing)
            && existing is string existingStyle
            && !string.IsNullOrWhiteSpace(existingStyle))
        {
            var trimmed = existingStyle.TrimEnd();
            if (!trimmed.EndsWith(';'))
                trimmed += ";";
            return $"{trimmed} display: none;";
        }
        return "display: none;";
    }

    private string? BuildClassName(QueryRenderState renderState) => BuildClassName(renderState, ElementKey, Class);

    private static string? BuildClassName(QueryRenderState renderState, string elementKey, string? staticClass)
    {
        var removed = renderState.RemovedClassesFor(elementKey);
        var classes = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(staticClass))
        {
            foreach (var className in staticClass.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!removed.Contains(className) && seen.Add(className))
                    classes.Add(className);
            }
        }
        foreach (var className in renderState.AddedClassesFor(elementKey))
        {
            if (seen.Add(className))
                classes.Add(className);
        }
        return classes.Count == 0 ? null : string.Join(' ', classes);
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
