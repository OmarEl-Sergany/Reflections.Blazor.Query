using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Reflections.Blazor.Query;

internal static class QueryEventDescriptors
{
    private static readonly IReadOnlyDictionary<string, EventDescriptor> Descriptors = new Dictionary<string, EventDescriptor>(StringComparer.Ordinal)
    {
        ["onclick"] = new(typeof(MouseEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<MouseEventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["ondblclick"] = new(typeof(MouseEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<MouseEventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onmousedown"] = new(typeof(MouseEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<MouseEventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onmouseup"] = new(typeof(MouseEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<MouseEventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onmousemove"] = new(typeof(MouseEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<MouseEventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onmouseover"] = new(typeof(MouseEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<MouseEventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onmouseout"] = new(typeof(MouseEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<MouseEventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onmouseenter"] = new(typeof(MouseEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<MouseEventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onmouseleave"] = new(typeof(MouseEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<MouseEventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["oncontextmenu"] = new(typeof(MouseEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<MouseEventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onkeydown"] = new(typeof(KeyboardEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<KeyboardEventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onkeyup"] = new(typeof(KeyboardEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<KeyboardEventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onkeypress"] = new(typeof(KeyboardEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<KeyboardEventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onchange"] = new(typeof(ChangeEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<ChangeEventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onselect"] = new(typeof(EventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<EventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onfocus"] = new(typeof(FocusEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<FocusEventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onblur"] = new(typeof(FocusEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<FocusEventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onfocusin"] = new(typeof(FocusEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<FocusEventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onfocusout"] = new(typeof(FocusEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<FocusEventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onsubmit"] = new(typeof(EventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<EventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onresize"] = new(typeof(EventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<EventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onscroll"] = new(typeof(EventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<EventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onerror"] = new(typeof(Microsoft.AspNetCore.Components.Web.ErrorEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.ErrorEventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onload"] = new(typeof(ProgressEventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<ProgressEventArgs>(receiver, args => state.Trigger(key, name, args)))),
        ["onunload"] = new(typeof(EventArgs), static (builder, sequence, receiver, state, key, name) => builder.AddAttribute(sequence, name, EventCallback.Factory.Create<EventArgs>(receiver, args => state.Trigger(key, name, args))))
    };

    public static void Validate<TEventArgs>(string eventName) where TEventArgs : EventArgs
    {
        if (!Descriptors.TryGetValue(eventName, out var descriptor) || descriptor.ArgumentType != typeof(TEventArgs))
            throw new ArgumentException($"Event '{eventName}' requires '{(Descriptors.TryGetValue(eventName, out descriptor) ? descriptor.ArgumentType.Name : "a supported Blazor event-argument type")}', not '{typeof(TEventArgs).Name}'.", nameof(eventName));
    }

    public static bool AddBinding(RenderTreeBuilder builder, int sequence, object receiver, QueryRenderState state, string elementKey, string eventName)
    {
        if (!Descriptors.TryGetValue(eventName, out var descriptor))
            return false;
        descriptor.AddBinding(builder, sequence, receiver, state, elementKey, eventName);
        return true;
    }

    private sealed record EventDescriptor(Type ArgumentType, Action<RenderTreeBuilder, int, object, QueryRenderState, string, string> AddBinding);
}
