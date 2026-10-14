using Microsoft.CodeAnalysis;

namespace Reflections.Blazor.Query.Generators;

[Generator]
public sealed class QueryRenderStateGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static output => output.AddSource(
            "QueryGeneratedMetadata.g.cs",
            """
            namespace Reflections.Blazor.Query;

            internal static class QueryGeneratedMetadata
            {
                public const string Architecture = "LibraryOwnedRenderState";
                public const string GeneratorVersion = "0.2.0";
                public static readonly string[] SupportedOperations =
                [
                    "Select", "Css", "Attr", "RemoveAttr", "Prop", "RemoveProp", "Data", "RemoveData", "Text", "Html", "Val", "Height", "Width", "HeightAsync", "WidthAsync", "InnerHeightAsync", "InnerWidthAsync", "OuterHeightAsync", "OuterWidthAsync", "OffsetAsync", "PositionAsync", "OffsetParentAsync", "Serialize", "SerializeArray", "BeginBatch", "Clear", "QueryHost",
                    "PageScope", "LayoutScope", "ApplicationScope", "ComponentScope",
                    "NavigationLifecycle", "DisposalLifecycle", "GeneratedMetadata", "On", "TypedOn", "Off", "TypedOff", "One", "Trigger", "TypedTrigger", "TriggerHandler", "TypedTriggerHandler", "Bind", "TypedBind", "Unbind", "TypedUnbind", "DelegateJS", "Undelegate", "Click", "TypedClick", "Change", "TypedChange", "Submit", "TypedSubmit", "Focus", "TypedFocus", "Blur", "TypedBlur", "FocusIn", "TypedFocusIn", "FocusOut", "TypedFocusOut", "SelectJS", "KeyDown", "TypedKeyDown", "KeyUp", "TypedKeyUp", "KeyPress", "TypedKeyPress", "MouseDown", "TypedMouseDown", "MouseUp", "TypedMouseUp", "MouseMove", "TypedMouseMove", "MouseOver", "TypedMouseOver", "MouseOut", "TypedMouseOut", "MouseEnter", "TypedMouseEnter", "MouseLeave", "TypedMouseLeave", "ContextMenu", "TypedContextMenu", "DblClick", "TypedDblClick", "Error", "TypedError", "Load", "TypedLoad", "Unload", "TypedUnload", "NativeEventDescriptors", "EventArgsTypeValidation",
                    "Add", "AddBack", "End", "Find", "Children", "Filter", "Not", "Has", "Is",
                    "Parent", "Parents", "ParentsUntil", "Closest", "Next", "NextAll", "NextUntil",
                    "Prev", "PrevAll", "PrevUntil", "Siblings",
                    "Length", "First", "Last", "Eq", "Slice", "ToArray", "ElementRegistry",
                    "QueryElement", "GeneratedElementKey", "ElementDisposal",
                    "Each", "Map", "Get", "Index", "PushStack", "SelectorParser", "SelectorMatcher",
                    "CompoundTagIdClassSelectors",
                    "AddClass", "RemoveClass", "ToggleClass", "HasClass", "Show", "Hide", "Toggle", "Remove", "Empty", "Detach",
                    "Append", "AppendHtml", "Prepend", "PrependHtml", "AppendElement", "PrependElement", "InsertElementBefore", "InsertElementAfter",
                    "AppendTo", "PrependTo", "Before", "After", "InsertBefore", "InsertAfter", "Wrap", "WrapAll", "WrapInner", "Clone",
                    "ReplaceWith", "ReplaceAll", "Replace"
                ];
            }
            """));
    }
}
