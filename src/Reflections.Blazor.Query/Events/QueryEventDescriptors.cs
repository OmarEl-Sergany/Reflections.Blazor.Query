using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Reflections.Blazor.Query;

internal static class QueryEventDescriptors
{
    public static void Validate<TEventArgs>(string eventName) where TEventArgs : EventArgs
    {
        if (!TryGetArgumentType(eventName, out var expected) || expected != typeof(TEventArgs))
            throw new ArgumentException($"Event '{eventName}' requires '{expected?.Name ?? "a supported Blazor event-argument type"}', not '{typeof(TEventArgs).Name}'.", nameof(eventName));
    }

    public static bool AddBinding(RenderTreeBuilder builder, int sequence, object receiver, QueryRenderState state, string elementKey, string eventName)
    {
        switch (eventName)
        {
            case "onclick": case "ondblclick": case "onmousedown": case "onmouseup": case "onmousemove": case "onmouseover": case "onmouseout": case "onmouseenter": case "onmouseleave": case "oncontextmenu":
                builder.AddAttribute(sequence, eventName, EventCallback.Factory.Create<MouseEventArgs>(receiver, args => state.Trigger(elementKey, eventName, args))); return true;
            case "onkeydown": case "onkeyup": case "onkeypress":
                builder.AddAttribute(sequence, eventName, EventCallback.Factory.Create<KeyboardEventArgs>(receiver, args => state.Trigger(elementKey, eventName, args))); return true;
            case "onchange": case "onselect":
                builder.AddAttribute(sequence, eventName, EventCallback.Factory.Create<ChangeEventArgs>(receiver, args => state.Trigger(elementKey, eventName, args))); return true;
            case "onfocus": case "onblur": case "onfocusin": case "onfocusout":
                builder.AddAttribute(sequence, eventName, EventCallback.Factory.Create<FocusEventArgs>(receiver, args => state.Trigger(elementKey, eventName, args))); return true;
            case "onsubmit": case "onresize": case "onscroll": case "onerror": case "onload": case "onunload":
                builder.AddAttribute(sequence, eventName, EventCallback.Factory.Create<EventArgs>(receiver, args => state.Trigger(elementKey, eventName, args))); return true;
            default:
                return false;
        }
    }

    private static bool TryGetArgumentType(string eventName, out Type? argumentType)
    {
        argumentType = eventName switch
        {
            "onclick" or "ondblclick" or "onmousedown" or "onmouseup" or "onmousemove" or "onmouseover" or "onmouseout" or "onmouseenter" or "onmouseleave" or "oncontextmenu" => typeof(MouseEventArgs),
            "onkeydown" or "onkeyup" or "onkeypress" => typeof(KeyboardEventArgs),
            "onchange" or "onselect" => typeof(ChangeEventArgs),
            "onfocus" or "onblur" or "onfocusin" or "onfocusout" => typeof(FocusEventArgs),
            "onsubmit" or "onresize" or "onscroll" or "onerror" or "onload" or "onunload" => typeof(EventArgs),
            _ => null
        };
        return argumentType is not null;
    }
}
