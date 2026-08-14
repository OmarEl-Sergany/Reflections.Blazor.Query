namespace Reflections.Blazor.Query;
public class QueryRenderState
{
    private readonly Dictionary<(string, string), string> _styles = new();
    private int _batchDepth;
    private bool _dirty;
    public event Action? Changed;
    internal IReadOnlyList<QueryStyleRule> Rules => _styles.Select(x => new QueryStyleRule(x.Key.Item1, x.Key.Item2, x.Value)).ToArray();
    public IDisposable BeginBatch() { _batchDepth++; return new Batch(this); }
    internal void SetCss(string selector, string property, string value) { _styles[(selector, property)] = value; NotifyChanged(); }
    public void Clear() { if (_styles.Count == 0) return; _styles.Clear(); NotifyChanged(); }
    private void NotifyChanged() { _dirty = true; if (_batchDepth == 0) Flush(); }
    private void Flush() { if (!_dirty) return; _dirty = false; Changed?.Invoke(); }
    private sealed class Batch(QueryRenderState owner) : IDisposable
    {
        private bool _disposed;
        public void Dispose() { if (_disposed) return; _disposed = true; if (--owner._batchDepth == 0) owner.Flush(); }
    }
}
public sealed record QueryStyleRule(string Selector, string Property, string Value);
