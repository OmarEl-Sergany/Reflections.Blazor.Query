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

        var aliases = selectors.ToHashSet(StringComparer.Ordinal);
        _entries.Add(new Entry(key, parentKey, aliases, Selector.FromAliases(aliases)));
    }

    public bool Unregister(string key) =>
        _entries.RemoveAll(entry => entry.Key == key) > 0;

    internal IReadOnlySet<string> BaseClassesFor(string key) =>
        Find(key)?.Selectors
            .Where(selector => selector.StartsWith('.'))
            .Select(selector => selector[1..])
            .ToHashSet(StringComparer.Ordinal)
        ?? new HashSet<string>();

    internal IReadOnlyList<string> Resolve(string selector) =>
        _entries.Where(entry => Matches(entry, selector)).Select(entry => entry.Key).ToArray();

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

    internal IReadOnlyList<string> Children(IEnumerable<string> keys, string? selector = null)
    {
        var parents = keys.ToHashSet(StringComparer.Ordinal);
        return Order(_entries
            .Where(entry => entry.ParentKey is not null && parents.Contains(entry.ParentKey) && Matches(entry, selector))
            .Select(entry => entry.Key));
    }

    internal IReadOnlyList<string> Descendants(IEnumerable<string> keys, string selector)
    {
        var ancestors = keys.ToHashSet(StringComparer.Ordinal);
        return Order(_entries
            .Where(entry => Matches(entry, selector) && IsDescendantOf(entry, ancestors))
            .Select(entry => entry.Key));
    }

    internal IReadOnlyList<string> AllDescendants(IEnumerable<string> keys)
    {
        var ancestors = keys.ToHashSet(StringComparer.Ordinal);
        return Order(_entries
            .Where(entry => IsDescendantOf(entry, ancestors))
            .Select(entry => entry.Key));
    }

    internal IReadOnlyList<string> WithDescendant(IEnumerable<string> keys, string selector) =>
        Order(keys.Where(key => Descendants([key], selector).Count > 0));

    internal IReadOnlyList<string> Parent(IEnumerable<string> keys, string? selector = null) =>
        Order(keys
            .Select(key => Find(key)?.ParentKey)
            .Where(key => key is not null)
            .Select(key => Find(key!))
            .Where(entry => entry is not null && Matches(entry, selector))
            .Select(entry => entry!.Key));

    internal IReadOnlyList<string> Parents(IEnumerable<string> keys, string? selector = null) =>
        Order(keys.SelectMany(key => EnumerateParentsUntil(key, untilSelector: null, selector)));

    internal IReadOnlyList<string> Next(IEnumerable<string> keys, string? selector = null) =>
        Order(keys.SelectMany(key => EnumerateSiblings(key, selector, forward: true).Take(1)));

    internal IReadOnlyList<string> NextAll(IEnumerable<string> keys, string? selector = null) =>
        Order(keys.SelectMany(key => EnumerateSiblings(key, selector, forward: true)));

    internal IReadOnlyList<string> Prev(IEnumerable<string> keys, string? selector = null) =>
        Order(keys.SelectMany(key => EnumerateSiblings(key, selector, forward: false).Take(1)));

    internal IReadOnlyList<string> PrevAll(IEnumerable<string> keys, string? selector = null) =>
        Order(keys.SelectMany(key => EnumerateSiblings(key, selector, forward: false)));

    internal IReadOnlyList<string> Siblings(IEnumerable<string> keys, string? selector = null)
    {
        var selected = keys.ToHashSet(StringComparer.Ordinal);
        return Order(_entries
            .Where(entry => !selected.Contains(entry.Key)
                && entry.ParentKey is not null
                && selected.Any(key => string.Equals(Find(key)?.ParentKey, entry.ParentKey, StringComparison.Ordinal))
                && Matches(entry, selector))
            .Select(entry => entry.Key));
    }

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
            if (Matches(current, selector))
                return current.Key;
        }

        return null;
    }

    private IEnumerable<string> EnumerateParentsUntil(string key, string? untilSelector, string? filterSelector)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        for (var current = Find(Find(key)?.ParentKey); current is not null && visited.Add(current.Key); current = Find(current.ParentKey))
        {
            if (untilSelector is not null && Matches(current, untilSelector))
                yield break;
            if (Matches(current, filterSelector))
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
            if (Matches(sibling, untilSelector))
                yield break;
            if (Matches(sibling, filterSelector))
                yield return sibling.Key;
        }
    }

    private IEnumerable<string> EnumerateSiblings(string key, string? selector, bool forward)
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
            if (Matches(sibling, selector))
                yield return sibling.Key;
        }
    }

    private bool IsDescendantOf(Entry entry, HashSet<string> ancestors)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        for (var current = Find(entry.ParentKey); current is not null && visited.Add(current.Key); current = Find(current.ParentKey))
        {
            if (ancestors.Contains(current.Key))
                return true;
        }

        return false;
    }

    private static bool Matches(Entry entry, string? selector) =>
        selector is null
        || entry.Selectors.Contains(selector)
        || SelectorParser.TryParse(selector, out var parsed) && SelectorMatcher.Matches(entry.Metadata, parsed);

    private Entry? Find(string? key) => key is null
        ? null
        : _entries.FirstOrDefault(entry => string.Equals(entry.Key, key, StringComparison.Ordinal));

    private sealed record Entry(string Key, string? ParentKey, HashSet<string> Selectors, Selector Metadata);
}
