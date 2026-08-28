namespace Reflections.Blazor.Query;

internal sealed record Selector(string? TagName, string? Id, IReadOnlySet<string> Classes)
{
    public static Selector FromAliases(IEnumerable<string> aliases)
    {
        ArgumentNullException.ThrowIfNull(aliases);

        string? tagName = null;
        string? id = null;
        var classes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var alias in aliases)
        {
            if (!SelectorParser.TryParse(alias, out var selector))
                continue;

            tagName ??= selector.TagName;
            id ??= selector.Id;
            classes.UnionWith(selector.Classes);
        }

        return new Selector(tagName, id, classes);
    }
}
