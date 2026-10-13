namespace Reflections.Blazor.Query;

/// <summary>
/// Read-only event target identity supplied by the Blazor custom-event argument factory.
/// The browser provides no selector result or handler state; those remain library-owned.
/// </summary>
public sealed record QueryEventTarget(string? ElementKey);
