namespace Reflections.Blazor.Query;

internal static class QueryDelegatedEvents
{
    public static readonly string[] SupportedNames =
    [
        "click", "change", "submit", "focusin", "focusout", "select", "keydown", "keyup", "keypress",
        "mousedown", "mouseup", "mousemove", "mouseover", "mouseout", "contextmenu", "dblclick", "scroll"
    ];

    public static string AttributeName(string eventName) => "onrbq" + eventName;
}
