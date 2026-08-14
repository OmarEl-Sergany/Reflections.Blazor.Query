namespace Reflections.Blazor.Query;
public class QueryRenderState
{
    private readonly Dictionary<(string, string), string> _styles = new();
    public event Action? Changed;
    internal IReadOnlyList<QueryStyleRule> Rules => _styles.Select(x => new QueryStyleRule(x.Key.Item1, x.Key.Item2, x.Value)).ToArray();
    internal void SetCss(string selector, string property, string value) { _styles[(selector, property)] = value; Changed?.Invoke(); }
    internal void Clear() { if (_styles.Count == 0) return; _styles.Clear(); Changed?.Invoke(); }
}
public sealed record QueryStyleRule(string Selector, string Property, string Value);
