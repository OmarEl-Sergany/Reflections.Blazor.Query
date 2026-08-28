namespace Reflections.Blazor.Query;
public class QueryRenderState
{
    private readonly Dictionary<(string, string), string> _styles = new();
    private readonly Dictionary<(string Key, string Name), string> _attributes = new();
    private readonly Dictionary<(string Key, string Name), object> _properties = new();
    private readonly Dictionary<(string Key, string Name), System.Text.Json.JsonElement> _data = new();
    private readonly Dictionary<string, QueryContent> _content = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _addedClasses = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _removedClasses = new(StringComparer.Ordinal);
    private readonly HashSet<string> _hiddenKeys = new(StringComparer.Ordinal);
    private readonly HashSet<string> _removedKeys = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<QueryContent>> _prepended = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<QueryContent>> _appended = new(StringComparer.Ordinal);
    private readonly Dictionary<string, QueryVirtualNode> _virtualNodes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> _virtualChildKeys = new(StringComparer.Ordinal);
    private int _batchDepth;
    private bool _dirty;
    public event Action? Changed;
    internal IReadOnlyList<QueryStyleRule> Rules => _styles.Select(x => new QueryStyleRule(x.Key.Item1, x.Key.Item2, x.Value)).ToArray();
    internal IReadOnlyDictionary<string, string> AttributesFor(string elementKey) => _attributes
        .Where(attribute => string.Equals(attribute.Key.Key, elementKey, StringComparison.Ordinal))
        .OrderBy(attribute => attribute.Key.Name, StringComparer.Ordinal)
        .ToDictionary(attribute => attribute.Key.Name, attribute => attribute.Value, StringComparer.Ordinal);
    internal IReadOnlyDictionary<string, object> PropertiesFor(string elementKey) => _properties
        .Where(property => string.Equals(property.Key.Key, elementKey, StringComparison.Ordinal))
        .OrderBy(property => property.Key.Name, StringComparer.Ordinal)
        .ToDictionary(property => property.Key.Name, property => property.Value, StringComparer.Ordinal);
    internal IReadOnlyDictionary<string, System.Text.Json.JsonElement> DataFor(string elementKey) => _data
        .Where(data => string.Equals(data.Key.Key, elementKey, StringComparison.Ordinal))
        .OrderBy(data => data.Key.Name, StringComparer.Ordinal)
        .ToDictionary(data => data.Key.Name, data => data.Value, StringComparer.Ordinal);
    internal QueryContent? ContentFor(string elementKey) => _content.TryGetValue(elementKey, out var content) ? content : null;
    internal IReadOnlySet<string> AddedClassesFor(string elementKey) =>
        _addedClasses.TryGetValue(elementKey, out var classes) ? classes : (IReadOnlySet<string>)new HashSet<string>();
    internal IReadOnlySet<string> RemovedClassesFor(string elementKey) =>
        _removedClasses.TryGetValue(elementKey, out var classes) ? classes : (IReadOnlySet<string>)new HashSet<string>();
    internal bool IsHidden(string elementKey) => _hiddenKeys.Contains(elementKey);
    internal bool IsRemoved(string elementKey) => _removedKeys.Contains(elementKey);
    internal IReadOnlyList<QueryContent> PrependedFor(string elementKey) =>
        _prepended.TryGetValue(elementKey, out var fragments) ? fragments : [];
    internal IReadOnlyList<QueryContent> AppendedFor(string elementKey) =>
        _appended.TryGetValue(elementKey, out var fragments) ? fragments : [];
    internal bool IsVirtual(string elementKey) => _virtualNodes.ContainsKey(elementKey);
    internal QueryVirtualNode? VirtualNodeFor(string elementKey) =>
        _virtualNodes.TryGetValue(elementKey, out var node) ? node : null;
    internal IReadOnlyList<string> VirtualChildKeysFor(string parentKey) =>
        _virtualChildKeys.TryGetValue(parentKey, out var keys) ? keys : [];
    internal IReadOnlyList<string> AllVirtualKeys => _virtualNodes.Keys.ToArray();
    public IDisposable BeginBatch() { _batchDepth++; return new Batch(this); }
    internal void CreateVirtualChild(
        string key,
        string parentKey,
        string tagName,
        string? className,
        string? text,
        bool isHtml,
        bool atStart)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(parentKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
        if (_virtualNodes.ContainsKey(key))
            throw new InvalidOperationException($"The virtual element key '{key}' is already registered.");

        _virtualNodes[key] = new QueryVirtualNode(key, parentKey, tagName, className, text, isHtml);
        var siblings = _virtualChildKeys.TryGetValue(parentKey, out var existing) ? existing : _virtualChildKeys[parentKey] = [];
        if (atStart)
            siblings.Insert(0, key);
        else
            siblings.Add(key);
        NotifyChanged();
    }
    internal void CreateVirtualSibling(
        string key,
        string anchorKey,
        string parentKey,
        string tagName,
        string? className,
        string? text,
        bool isHtml,
        bool before)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(anchorKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(parentKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
        if (_virtualNodes.ContainsKey(key))
            throw new InvalidOperationException($"The virtual element key '{key}' is already registered.");

        _virtualNodes[key] = new QueryVirtualNode(key, parentKey, tagName, className, text, isHtml);
        var siblings = _virtualChildKeys.TryGetValue(parentKey, out var existing) ? existing : _virtualChildKeys[parentKey] = [];
        var anchorIndex = siblings.IndexOf(anchorKey);
        var insertIndex = anchorIndex < 0 ? siblings.Count : before ? anchorIndex : anchorIndex + 1;
        siblings.Insert(insertIndex, key);
        NotifyChanged();
    }
    internal void RemoveVirtualNode(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!_virtualNodes.Remove(key, out var node))
            return;
        foreach (var descendantKey in VirtualChildKeysFor(key).ToArray())
            RemoveVirtualNode(descendantKey);
        if (node.ParentKey is not null && _virtualChildKeys.TryGetValue(node.ParentKey, out var siblings))
            siblings.Remove(key);
        _virtualChildKeys.Remove(key);
        NotifyChanged();
    }
    internal string CreateDetachedVirtualNode(string key, string tagName, string? className, string? text, bool isHtml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
        if (_virtualNodes.ContainsKey(key))
            throw new InvalidOperationException($"The virtual element key '{key}' is already registered.");

        _virtualNodes[key] = new QueryVirtualNode(key, null, tagName, className, text, isHtml);
        NotifyChanged();
        return key;
    }
    internal void DetachVirtualChild(string key)
    {
        var node = RequireVirtualNode(key);
        if (node.ParentKey is not null && _virtualChildKeys.TryGetValue(node.ParentKey, out var siblings))
            siblings.Remove(key);
        _virtualNodes[key] = node with { ParentKey = null };
        NotifyChanged();
    }
    internal void AttachVirtualChild(string key, string parentKey, bool atStart)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parentKey);
        RequireVirtualNode(key);
        DetachVirtualChild(key);
        _virtualNodes[key] = _virtualNodes[key] with { ParentKey = parentKey };
        var siblings = _virtualChildKeys.TryGetValue(parentKey, out var existing) ? existing : _virtualChildKeys[parentKey] = [];
        if (atStart)
            siblings.Insert(0, key);
        else
            siblings.Add(key);
        NotifyChanged();
    }
    internal void AttachVirtualSibling(string key, string anchorKey, string parentKey, bool before)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(anchorKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(parentKey);
        RequireVirtualNode(key);
        DetachVirtualChild(key);
        _virtualNodes[key] = _virtualNodes[key] with { ParentKey = parentKey };
        var siblings = _virtualChildKeys.TryGetValue(parentKey, out var existing) ? existing : _virtualChildKeys[parentKey] = [];
        var anchorIndex = siblings.IndexOf(anchorKey);
        var insertIndex = anchorIndex < 0 ? siblings.Count : before ? anchorIndex : anchorIndex + 1;
        siblings.Insert(insertIndex, key);
        NotifyChanged();
    }
    private QueryVirtualNode RequireVirtualNode(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _virtualNodes.TryGetValue(key, out var node)
            ? node
            : throw new InvalidOperationException($"'{key}' is not a library-created (virtual) element.");
    }
    internal void AddClass(string elementKey, string className)
    {
        ValidateClassName(elementKey, className);
        (_removedClasses.TryGetValue(elementKey, out var removed) ? removed : null)?.Remove(className);
        (_addedClasses.TryGetValue(elementKey, out var added) ? added : _addedClasses[elementKey] = new(StringComparer.Ordinal)).Add(className);
        NotifyChanged();
    }
    internal void RemoveClass(string elementKey, string className)
    {
        ValidateClassName(elementKey, className);
        (_addedClasses.TryGetValue(elementKey, out var added) ? added : null)?.Remove(className);
        (_removedClasses.TryGetValue(elementKey, out var removed) ? removed : _removedClasses[elementKey] = new(StringComparer.Ordinal)).Add(className);
        NotifyChanged();
    }
    internal void Hide(string elementKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        if (_hiddenKeys.Add(elementKey)) NotifyChanged();
    }
    internal void Show(string elementKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        if (_hiddenKeys.Remove(elementKey)) NotifyChanged();
    }
    internal void Remove(string elementKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        if (_removedKeys.Add(elementKey)) NotifyChanged();
    }
    internal void Empty(string elementKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        _prepended.Remove(elementKey);
        _appended.Remove(elementKey);
        SetContent(elementKey, new QueryContent(string.Empty, false));
    }
    internal void SetCss(string selector, string property, string value) { _styles[(selector, property)] = value; NotifyChanged(); }
    internal void SetAttribute(string elementKey, string name, string value)
    {
        ValidateAttribute(elementKey, name, value);
        _attributes[(elementKey, name)] = value;
        NotifyChanged();
    }
    internal void RemoveAttribute(string elementKey, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ValidateBindableName(name);
        if (_attributes.Remove((elementKey, name))) NotifyChanged();
    }
    internal void SetProperty(string elementKey, string name, object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ValidateBindableName(name);
        _properties[(elementKey, name)] = value;
        NotifyChanged();
    }
    internal void RemoveProperty(string elementKey, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ValidateBindableName(name);
        if (_properties.Remove((elementKey, name))) NotifyChanged();
    }
    internal void SetData<T>(string elementKey, string name, T value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _data[(elementKey, name)] = System.Text.Json.JsonSerializer.SerializeToElement(value);
        NotifyChanged();
    }
    internal T? GetData<T>(string elementKey, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _data.TryGetValue((elementKey, name), out var value)
            ? System.Text.Json.JsonSerializer.Deserialize<T>(value.GetRawText())
            : default;
    }
    internal void RemoveData(string elementKey, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (_data.Remove((elementKey, name))) NotifyChanged();
    }
    internal void SetText(string elementKey, string value) => SetContent(elementKey, new QueryContent(value, false));
    internal void SetHtml(string elementKey, string value) => SetContent(elementKey, new QueryContent(value, true));
    internal void AppendContent(string elementKey, string value, bool isHtml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ArgumentNullException.ThrowIfNull(value);
        (_appended.TryGetValue(elementKey, out var fragments) ? fragments : _appended[elementKey] = [])
            .Add(new QueryContent(value, isHtml));
        NotifyChanged();
    }
    internal void PrependContent(string elementKey, string value, bool isHtml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ArgumentNullException.ThrowIfNull(value);
        (_prepended.TryGetValue(elementKey, out var fragments) ? fragments : _prepended[elementKey] = [])
            .Insert(0, new QueryContent(value, isHtml));
        NotifyChanged();
    }
    public void Clear()
    {
        if (_styles.Count == 0 && _attributes.Count == 0 && _properties.Count == 0 && _data.Count == 0 && _content.Count == 0
            && _addedClasses.Count == 0 && _removedClasses.Count == 0 && _hiddenKeys.Count == 0 && _removedKeys.Count == 0
            && _prepended.Count == 0 && _appended.Count == 0 && _virtualNodes.Count == 0 && _virtualChildKeys.Count == 0)
            return;
        _styles.Clear();
        _attributes.Clear();
        _properties.Clear();
        _data.Clear();
        _content.Clear();
        _addedClasses.Clear();
        _removedClasses.Clear();
        _hiddenKeys.Clear();
        _removedKeys.Clear();
        _prepended.Clear();
        _appended.Clear();
        _virtualNodes.Clear();
        _virtualChildKeys.Clear();
        NotifyChanged();
    }
    private static void ValidateAttribute(string elementKey, string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ValidateBindableName(name);
        ArgumentNullException.ThrowIfNull(value);
    }
    private static void ValidateBindableName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (string.Equals(name, "data-rbq-key", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The library-owned data-rbq-key attribute cannot be changed.", nameof(name));
        if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Event-like names are reserved for the library event API.", nameof(name));
    }
    private static void ValidateClassName(string elementKey, string className)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(className);
        if (className.AsSpan().IndexOfAny(' ', '\t', '\n') >= 0)
            throw new ArgumentException("A class name cannot contain whitespace.", nameof(className));
    }
    private void NotifyChanged() { _dirty = true; if (_batchDepth == 0) Flush(); }
    private void SetContent(string elementKey, QueryContent content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        _content[elementKey] = content;
        _prepended.Remove(elementKey);
        _appended.Remove(elementKey);
        NotifyChanged();
    }
    private void Flush() { if (!_dirty) return; _dirty = false; Changed?.Invoke(); }
    private sealed class Batch(QueryRenderState owner) : IDisposable
    {
        private bool _disposed;
        public void Dispose() { if (_disposed) return; _disposed = true; if (--owner._batchDepth == 0) owner.Flush(); }
    }
}
public sealed record QueryStyleRule(string Selector, string Property, string Value);
internal sealed record QueryContent(string Value, bool IsHtml);
internal sealed record QueryVirtualNode(string Key, string? ParentKey, string TagName, string? ClassName, string? Text, bool IsHtml);
