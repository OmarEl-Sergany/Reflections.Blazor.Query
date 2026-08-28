namespace Reflections.Blazor.Query;

internal static class SelectorMatcher
{
    public static bool Matches(Selector element, Selector selector)
    {
        if (selector.TagName is not null
            && !string.Equals(element.TagName, selector.TagName, StringComparison.OrdinalIgnoreCase))
            return false;

        if (selector.Id is not null
            && !string.Equals(element.Id, selector.Id, StringComparison.Ordinal))
            return false;

        return selector.Classes.IsSubsetOf(element.Classes);
    }
}
