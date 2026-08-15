namespace Reflections.Blazor.Query;

public sealed class QueryElementRegistry
{
    private readonly List<Entry> _entries = [];

    public void Register(string key, params string[] selectors)
        => RegisterCore(key, parentKey: null, selectors);

    public void RegisterWithParent(string key, string? parentKey, params string[] selectors)
        => RegisterCore(key, parentKey, selectors);

    private void RegisterCore(string key, string? parentKey, string[] selectors)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (string.IsNullOrWhiteSpace(parentKey))
            parentKey = null;
        if (string.Equals(key, parentKey, StringComparison.Ordinal))
            throw new ArgumentException("An element cannot be its own parent.", nameof(parentKey));
        ArgumentNullException.ThrowIfNull(selectors);
        if (selectors.Length == 0)
            throw new ArgumentException("At least one selector is required.", nameof(selectors));
        if (_entries.Any(entry => entry.Key == key))
            throw new InvalidOperationException($"The query element key '{key}' is already registered.");

        foreach (var selector in selectors)
            ArgumentException.ThrowIfNullOrWhiteSpace(selector);

        _entries.Add(new Entry(key, parentKey, selectors.ToHashSet(StringComparer.Ordinal)));
    }

    public bool Unregister(string key) =>
        _entries.RemoveAll(entry => entry.Key == key) > 0;

    internal IReadOnlyList<string> Resolve(string selector) =>
        _entries.Where(entry => entry.Selectors.Contains(selector)).Select(entry => entry.Key).ToArray();

    internal IReadOnlyList<string> Order(IEnumerable<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        var source = keys.ToArray();
        var distinctKeys = source.ToHashSet(StringComparer.Ordinal);
        var ordered = _entries
            .Where(entry => distinctKeys.Remove(entry.Key))
            .Select(entry => entry.Key)
            .ToList();

        foreach (var key in source)
        {
            if (distinctKeys.Remove(key))
                ordered.Add(key);
        }

        return ordered;
    }

    internal IReadOnlyList<string> Closest(IEnumerable<string> keys, string selector) =>
        Order(keys.Select(key => FindClosest(key, selector)).Where(key => key is not null)!);

    internal IReadOnlyList<string> ParentsUntil(
        IEnumerable<string> keys,
        string untilSelector,
        string? filterSelector = null) =>
        Order(keys.SelectMany(key => EnumerateParentsUntil(key, untilSelector, filterSelector)));

    internal IReadOnlyList<string> NextUntil(
        IEnumerable<string> keys,
        string untilSelector,
        string? filterSelector = null) =>
        Order(keys.SelectMany(key => EnumerateSiblingsUntil(key, untilSelector, filterSelector, forward: true)));

    internal IReadOnlyList<string> PrevUntil(
        IEnumerable<string> keys,
        string untilSelector,
        string? filterSelector = null) =>
        Order(keys.SelectMany(key => EnumerateSiblingsUntil(key, untilSelector, filterSelector, forward: false)));

    private string? FindClosest(string key, string selector)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        for (var current = Find(key); current is not null && visited.Add(current.Key); current = Find(current.ParentKey))
        {
            if (current.Selectors.Contains(selector))
                return current.Key;
        }

        return null;
    }

    private IEnumerable<string> EnumerateParentsUntil(string key, string untilSelector, string? filterSelector)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        for (var current = Find(Find(key)?.ParentKey); current is not null && visited.Add(current.Key); current = Find(current.ParentKey))
        {
            if (current.Selectors.Contains(untilSelector))
                yield break;
            if (filterSelector is null || current.Selectors.Contains(filterSelector))
                yield return current.Key;
        }
    }

    private IEnumerable<string> EnumerateSiblingsUntil(string key, string untilSelector, string? filterSelector, bool forward)
    {
        var current = Find(key);
        if (current is null)
            yield break;

        var siblings = _entries.Where(entry => string.Equals(entry.ParentKey, current.ParentKey, StringComparison.Ordinal)).ToArray();
        var index = Array.FindIndex(siblings, entry => string.Equals(entry.Key, key, StringComparison.Ordinal));
        if (index < 0)
            yield break;

        var step = forward ? 1 : -1;
        for (var siblingIndex = index + step; siblingIndex >= 0 && siblingIndex < siblings.Length; siblingIndex += step)
        {
            var sibling = siblings[siblingIndex];
            if (sibling.Selectors.Contains(untilSelector))
                yield break;
            if (filterSelector is null || sibling.Selectors.Contains(filterSelector))
                yield return sibling.Key;
        }
    }

    private Entry? Find(string? key) => key is null
        ? null
        : _entries.FirstOrDefault(entry => string.Equals(entry.Key, key, StringComparison.Ordinal));

    private sealed record Entry(string Key, string? ParentKey, HashSet<string> Selectors);
}
