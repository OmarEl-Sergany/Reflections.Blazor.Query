namespace Reflections.Blazor.Query;

internal static class SelectorParser
{
    public static bool TryParse(string selector, out Selector parsed)
    {
        parsed = default!;
        if (string.IsNullOrWhiteSpace(selector))
            return false;

        var index = 0;
        string? tagName = null;
        string? id = null;
        var classes = new HashSet<string>(StringComparer.Ordinal);

        var isWildcard = selector[index] == '*';
        if (isWildcard)
        {
            index++;
        }
        else if (IsIdentifierStart(selector[index]))
        {
            tagName = ReadIdentifier(selector, ref index);
        }

        while (index < selector.Length)
        {
            var prefix = selector[index++];
            if (prefix is not '.' and not '#')
                return false;

            if (index >= selector.Length || !IsIdentifierStart(selector[index]))
                return false;

            var value = ReadIdentifier(selector, ref index);
            if (prefix == '.')
            {
                classes.Add(value);
                continue;
            }

            if (id is not null)
                return false;
            id = value;
        }

        if (!isWildcard && tagName is null && id is null && classes.Count == 0)
            return false;

        parsed = new Selector(tagName, id, classes);
        return true;
    }

    private static string ReadIdentifier(string value, ref int index)
    {
        var start = index++;
        while (index < value.Length && IsIdentifierPart(value[index]))
            index++;
        return value[start..index];
    }

    private static bool IsIdentifierStart(char value) =>
        char.IsLetter(value) || value is '_' or '-';

    private static bool IsIdentifierPart(char value) =>
        char.IsLetterOrDigit(value) || value is '_' or '-';
}
