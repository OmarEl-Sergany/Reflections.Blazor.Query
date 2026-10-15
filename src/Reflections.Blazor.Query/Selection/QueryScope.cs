namespace Reflections.Blazor.Query;

internal sealed class QueryScope(BlazorQuery query, string key) : IQueryScope
{
    public string Key { get; } = key;
    public QuerySelection Select(string selector) => query.SelectInScope(selector, Key);
}
