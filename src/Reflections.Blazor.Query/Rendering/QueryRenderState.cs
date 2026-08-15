namespace Reflections.Blazor.Query;
public class QueryRenderState
{
    private readonly Dictionary<(string, string), string> _styles = new();
    private readonly Dictionary<(string Key, string Name), string> _attributes = new();
    private readonly Dictionary<(string Key, string Name), object> _properties = new();
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
    public IDisposable BeginBatch() { _batchDepth++; return new Batch(this); }
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
    public void Clear() { if (_styles.Count == 0 && _attributes.Count == 0 && _properties.Count == 0) return; _styles.Clear(); _attributes.Clear(); _properties.Clear(); NotifyChanged(); }
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
    private void NotifyChanged() { _dirty = true; if (_batchDepth == 0) Flush(); }
    private void Flush() { if (!_dirty) return; _dirty = false; Changed?.Invoke(); }
    private sealed class Batch(QueryRenderState owner) : IDisposable
    {
        private bool _disposed;
        public void Dispose() { if (_disposed) return; _disposed = true; if (--owner._batchDepth == 0) owner.Flush(); }
    }
}
public sealed record QueryStyleRule(string Selector, string Property, string Value);
