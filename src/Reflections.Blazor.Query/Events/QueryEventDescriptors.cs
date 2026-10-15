using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Reflections.Blazor.Query;

[EventHandler("onrbqanimationend", typeof(EventArgs), enableStopPropagation: true, enablePreventDefault: false)]
[EventHandler("onrbqclick", typeof(QueryEventTarget))]
[EventHandler("onrbqchange", typeof(QueryEventTarget))]
[EventHandler("onrbqsubmit", typeof(QueryEventTarget))]
[EventHandler("onrbqfocusin", typeof(QueryEventTarget))]
[EventHandler("onrbqfocusout", typeof(QueryEventTarget))]
[EventHandler("onrbqselect", typeof(QueryEventTarget))]
[EventHandler("onrbqkeydown", typeof(QueryEventTarget))]
[EventHandler("onrbqkeyup", typeof(QueryEventTarget))]
[EventHandler("onrbqkeypress", typeof(QueryEventTarget))]
[EventHandler("onrbqmousedown", typeof(QueryEventTarget))]
[EventHandler("onrbqmouseup", typeof(QueryEventTarget))]
[EventHandler("onrbqmousemove", typeof(QueryEventTarget))]
[EventHandler("onrbqmouseover", typeof(QueryEventTarget))]
[EventHandler("onrbqmouseout", typeof(QueryEventTarget))]
[EventHandler("onrbqcontextmenu", typeof(QueryEventTarget))]
[EventHandler("onrbqdblclick", typeof(QueryEventTarget))]
[EventHandler("onrbqscroll", typeof(QueryEventTarget))]
public static class QueryCustomEventHandlers
{
}

internal static class QueryEventDescriptors
{
    private static readonly IReadOnlyDictionary<string, EventDescriptor> Descriptors = new Dictionary<string, EventDescriptor>(StringComparer.Ordinal)
    {
        ["onclick"] = new(typeof(MouseEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<MouseEventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["ondblclick"] = new(typeof(MouseEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<MouseEventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onmousedown"] = new(typeof(MouseEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<MouseEventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onmouseup"] = new(typeof(MouseEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<MouseEventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onmousemove"] = new(typeof(MouseEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<MouseEventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onmouseover"] = new(typeof(MouseEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<MouseEventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onmouseout"] = new(typeof(MouseEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<MouseEventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onmouseenter"] = new(typeof(MouseEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<MouseEventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onmouseleave"] = new(typeof(MouseEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<MouseEventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["oncontextmenu"] = new(typeof(MouseEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<MouseEventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onkeydown"] = new(typeof(KeyboardEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<KeyboardEventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onkeyup"] = new(typeof(KeyboardEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<KeyboardEventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onkeypress"] = new(typeof(KeyboardEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<KeyboardEventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onchange"] = new(typeof(ChangeEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<ChangeEventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onselect"] = new(typeof(EventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<EventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onfocus"] = new(typeof(FocusEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<FocusEventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onblur"] = new(typeof(FocusEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<FocusEventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onfocusin"] = new(typeof(FocusEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<FocusEventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onfocusout"] = new(typeof(FocusEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<FocusEventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onsubmit"] = new(typeof(EventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<EventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onresize"] = new(typeof(EventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<EventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onscroll"] = new(typeof(EventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<EventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onerror"] = new(typeof(Microsoft.AspNetCore.Components.Web.ErrorEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.ErrorEventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onload"] = new(typeof(ProgressEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<ProgressEventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args)))),
        ["onrbqanimationend"] = new(typeof(EventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<EventArgs>(receiver, args => state.TriggerAsync(key, name, args))), StopPropagation: true),
        ["onunload"] = new(typeof(EventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<EventArgs>(receiver, args => state.TriggerAsync(key, name, (EventArgs)args))))
    };

    public static void Validate<TEventArgs>(string eventName) where TEventArgs : EventArgs
    {
        if (!Descriptors.TryGetValue(eventName, out var descriptor) || descriptor.ArgumentType != typeof(TEventArgs))
            throw new ArgumentException($"Event '{eventName}' requires '{(Descriptors.TryGetValue(eventName, out descriptor) ? descriptor.ArgumentType.Name : "a supported Blazor event-argument type")}', not '{typeof(TEventArgs).Name}'.", nameof(eventName));
    }

    public static int AddBinding(RenderTreeBuilder builder, int sequence, object receiver, QueryRenderState state, string elementKey, string eventName)
    {
        if (!Descriptors.TryGetValue(eventName, out var descriptor))
            return 0;
        descriptor.AddBinding(builder, sequence, receiver, state, elementKey, eventName);
        if (descriptor.StopPropagation)
            builder.AddEventStopPropagationAttribute(sequence + 1, eventName, true);
        return descriptor.StopPropagation ? 2 : 1;
    }

    private sealed record EventDescriptor(
        Type ArgumentType,
        Action<RenderTreeBuilder, int, object, QueryRenderState, string, string> AddBinding,
        bool StopPropagation = false);
}
