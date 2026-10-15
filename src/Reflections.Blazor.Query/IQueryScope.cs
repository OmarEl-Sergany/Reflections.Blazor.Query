namespace Reflections.Blazor.Query;

public interface IQueryScope
{
    string Key { get; }
    QuerySelection Select(string selector);
}
