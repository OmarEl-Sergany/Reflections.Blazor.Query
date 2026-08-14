namespace Reflections.Blazor.Query;

public sealed class QueryElementRegistry
{
    private readonly List<Entry> _entries = [];

    public void Register(string key, params string[] selectors)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(selectors);
        if (selectors.Length == 0)
            throw new ArgumentException("At least one selector is required.", nameof(selectors));
        if (_entries.Any(entry => entry.Key == key))
            throw new InvalidOperationException($"The query element key '{key}' is already registered.");

        foreach (var selector in selectors)
            ArgumentException.ThrowIfNullOrWhiteSpace(selector);

        _entries.Add(new Entry(key, selectors.ToHashSet(StringComparer.Ordinal)));
    }

    public bool Unregister(string key) =>
        _entries.RemoveAll(entry => entry.Key == key) > 0;

    internal IReadOnlyList<string> Resolve(string selector) =>
        _entries.Where(entry => entry.Selectors.Contains(selector)).Select(entry => entry.Key).ToArray();

    private sealed record Entry(string Key, HashSet<string> Selectors);
}
