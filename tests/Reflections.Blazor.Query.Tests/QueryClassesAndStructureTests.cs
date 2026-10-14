using System;
using System.Linq;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Reflections.Blazor.Query;

namespace Reflections.Blazor.Query.Tests;

public sealed class QueryClassesAndStructureTests
{
    [Fact]
    public void On_and_Trigger_invoke_all_library_owned_handlers()
    {
        var (query, registry, _) = CreateQuery();
        registry.Register("button", ".button");
        var count = 0;

        query.Select(".button").On("click", () => count++).On("onclick", () => count += 10).Trigger("click");

        Assert.Equal(11, count);
    }

    [Fact]
    public void One_removes_its_handler_after_the_first_trigger_and_Off_removes_remaining_handlers()
    {
        var (query, registry, _) = CreateQuery();
        registry.Register("button", ".button");
        var count = 0;
        var selection = query.Select(".button").One("click", () => count++).On("click", () => count += 10);

        selection.Trigger("click").Trigger("click").Off("click").Trigger("click");

        Assert.Equal(21, count);
    }

    [Fact]
    public void TriggerHandler_and_legacy_bind_aliases_share_the_same_handler_pipeline()
    {
        var (query, registry, _) = CreateQuery(); registry.Register("button", ".button"); var first = 0; var second = 0;
        Action firstHandler = () => first++; Action secondHandler = () => second++;
        query.Select(".button").Bind("click", firstHandler).Bind("click", secondHandler).Unbind("click", firstHandler).TriggerHandler("click");
        Assert.Equal(0, first); Assert.Equal(1, second);
    }

    [Fact]
    public void Typed_Bind_Unbind_and_TriggerHandler_preserve_payload_and_remove_only_the_requested_handler()
    {
        var (query, registry, _) = CreateQuery();
        registry.Register("button", ".button");
        var firstCount = 0;
        MouseEventArgs? observed = null;
        Action<MouseEventArgs> first = _ => firstCount++;
        Action<MouseEventArgs> second = args => observed = args;
        var payload = new MouseEventArgs { ClientX = 91 };

        query.Select(".button").Bind("click", first).Bind("click", second).Unbind("click", first).TriggerHandler("click", payload);

        Assert.Equal(0, firstCount);
        Assert.Same(payload, Assert.IsType<MouseEventArgs>(observed));
    }

    [Fact]
    public void TriggerHandler_without_event_arguments_remains_an_untyped_source_compatible_alias()
    {
        var (query, registry, _) = CreateQuery();
        registry.Register("button", ".button");
        var count = 0;

        query.Select(".button").Bind("click", () => count++).TriggerHandler("click");

        Assert.Equal(1, count);
    }

    [Fact]
    public void Click_change_and_submit_helpers_bind_and_trigger_their_normalized_events()
    {
        var (query, registry, _) = CreateQuery(); registry.Register("form", ".form"); var count = 0;
        query.Select(".form").Click(() => count++).Change(() => count += 10).Submit(() => count += 100).Click().Change().Submit();
        Assert.Equal(111, count);
    }

    [Fact]
    public void Focus_keyboard_mouse_and_context_helpers_bind_and_trigger_their_events()
    {
        var (query, registry, _) = CreateQuery(); registry.Register("input", ".input"); var count = 0;
        query.Select(".input").Focus(() => count++).Blur(() => count += 10).KeyDown(() => count += 100).KeyUp(() => count += 1000)
            .MouseEnter(() => count += 10000).MouseLeave(() => count += 100000).ContextMenu(() => count += 1000000)
            .Focus().Blur().KeyDown().KeyUp().MouseEnter().MouseLeave().ContextMenu();
        Assert.Equal(1111111, count);
    }

    [Fact]
    public void Remaining_keyboard_mouse_resize_and_scroll_helpers_bind_and_trigger_their_events()
    {
        var (query, registry, _) = CreateQuery(); registry.Register("surface", ".surface"); var count = 0;
        query.Select(".surface").KeyPress(() => count++).MouseDown(() => count += 10).MouseUp(() => count += 100)
            .MouseMove(() => count += 1000).MouseOver(() => count += 10000).MouseOut(() => count += 100000)
            .Resize(() => count += 1000000).Scroll(() => count += 10000000)
            .KeyPress().MouseDown().MouseUp().MouseMove().MouseOver().MouseOut().Resize().Scroll();
        Assert.Equal(11111111, count);
    }

    [Fact]
    public void Double_click_hover_load_error_and_unload_helpers_compose_the_event_pipeline()
    {
        var (query, registry, _) = CreateQuery(); registry.Register("surface", ".surface"); var count = 0;
        query.Select(".surface").DblClick(() => count++).Hover(() => count += 10, () => count += 100)
            .Load(() => count += 1000).Error(() => count += 10000).Unload(() => count += 100000)
            .DblClick().MouseEnter().MouseLeave().Load().Error().Unload();
        Assert.Equal(111111, count);
    }

    [Fact]
    public void Focusin_focusout_and_selectjs_helpers_do_not_collide_with_query_selection()
    {
        var (query, registry, _) = CreateQuery(); registry.Register("input", ".input"); var count = 0;
        query.Select(".input").FocusIn(() => count++).FocusOut(() => count += 10).SelectJS(() => count += 100)
            .FocusIn().FocusOut().SelectJS();
        Assert.Equal(111, count);
    }

    [Fact]
    public void DelegateJS_dispatches_only_matching_descendants_in_the_root_scope()
    {
        var (query, registry, state) = CreateQuery();
        registry.RegisterWithParentAndScope("root", null, "scope-a", ".root");
        registry.RegisterWithParentAndScope("item", "root", "scope-a", ".item");
        registry.RegisterWithParentAndScope("other", "root", "scope-b", ".item");
        state.RegisterRealElement("root", _ => { });
        QueryEventTarget? observed = null;

        query.Select(".root").DelegateJS(".item", "click", target => observed = target);
        state.TriggerDelegated("root", "click", new("other"), registry);
        Assert.Null(observed);

        state.TriggerDelegated("root", "click", new("item"), registry);
        Assert.Equal("item", observed?.ElementKey);
    }

    [Fact]
    public void Undelegate_removes_only_the_matching_delegated_handler()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("root", ".root");
        registry.RegisterWithParent("item", "root", ".item");
        state.RegisterRealElement("root", _ => { });
        var first = 0;
        var second = 0;
        Action<QueryEventTarget> firstHandler = _ => first++;
        Action<QueryEventTarget> secondHandler = _ => second++;

        query.Select(".root").DelegateJS(".item", "click", firstHandler).DelegateJS(".item", "click", secondHandler)
            .Undelegate(".item", "click", firstHandler);
        state.TriggerDelegated("root", "click", new("item"), registry);

        Assert.Equal(0, first);
        Assert.Equal(1, second);
    }

    [Fact]
    public void DelegateJS_rejects_non_bubbling_events_and_virtual_roots()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("root", ".root");
        state.RegisterRealElement("root", _ => { });

        Assert.Throws<NotSupportedException>(() => query.Select(".root").DelegateJS(".item", "focus", _ => { }));

        var virtualRoot = query.Select(".root").AppendElement("div", "virtual-root");
        Assert.Throws<InvalidOperationException>(() => virtualRoot.DelegateJS(".item", "click", _ => { }));
    }

    [Fact]
    public void Typed_On_and_Trigger_preserve_the_native_event_payload_without_invoking_untyped_handlers()
    {
        var (query, registry, _) = CreateQuery();
        registry.Register("button", ".button");
        MouseEventArgs? observed = null;
        var untypedCount = 0;
        var payload = new MouseEventArgs { ClientX = 37, ClientY = 19 };

        query.Select(".button").On("click", () => untypedCount++).On<MouseEventArgs>("click", args => observed = args).Trigger("click", payload);

        Assert.Equal(0, untypedCount);
        var captured = Assert.IsType<MouseEventArgs>(observed);
        Assert.Same(payload, captured);
        Assert.Equal(37, captured.ClientX);
    }

    [Fact]
    public void Typed_On_rejects_an_event_argument_type_that_does_not_match_the_browser_event()
    {
        var (query, registry, _) = CreateQuery();
        registry.Register("button", ".button");

        Assert.Throws<ArgumentException>(() => query.Select(".button").On<KeyboardEventArgs>("click", _ => { }));
    }

    [Fact]
    public void Typed_On_rejects_event_names_without_a_typed_Blazor_descriptor()
    {
        var (query, registry, _) = CreateQuery();
        registry.Register("button", ".button");

        Assert.Throws<ArgumentException>(() => query.Select(".button").On<MouseEventArgs>("unknown", _ => { }));
    }

    [Fact]
    public void Typed_Off_removes_only_the_matching_typed_handler()
    {
        var (query, registry, _) = CreateQuery();
        registry.Register("button", ".button");
        var first = 0;
        var second = 0;
        Action<MouseEventArgs> firstHandler = _ => first++;
        Action<MouseEventArgs> secondHandler = _ => second++;

        query.Select(".button").On("click", firstHandler).On("click", secondHandler).Off("click", firstHandler).Trigger("click", new MouseEventArgs());

        Assert.Equal(0, first);
        Assert.Equal(1, second);
    }

    [Fact]
    public void Typed_Trigger_does_not_dispatch_untyped_handlers_or_silently_skip_an_untyped_only_registration()
    {
        var (query, registry, _) = CreateQuery();
        registry.Register("button", ".button");
        var untyped = 0;
        var typed = 0;
        var selection = query.Select(".button").On("click", () => untyped++);

        selection.Trigger("click", new MouseEventArgs());
        Assert.Equal(0, untyped);

        selection.On<MouseEventArgs>("click", _ => typed++).Trigger("click", new MouseEventArgs());
        Assert.Equal(0, untyped);
        Assert.Equal(1, typed);
    }

    [Fact]
    public void Typed_event_helpers_bind_to_their_native_argument_types_and_keep_synthetic_trigger_overloads()
    {
        var (query, registry, _) = CreateQuery();
        registry.Register("button", ".button");
        var mouse = 0;
        var keyboard = 0;
        var change = 0;

        query.Select(".button")
            .Click((MouseEventArgs _) => mouse++)
            .KeyDown((KeyboardEventArgs _) => keyboard++)
            .Change((ChangeEventArgs _) => change++)
            .Click()
            .KeyDown()
            .Change();

        Assert.Equal(0, mouse);
        Assert.Equal(0, keyboard);
        Assert.Equal(0, change);
    }

    [Fact]
    public void Typed_Hover_composes_mouse_enter_and_leave_callbacks()
    {
        var (query, registry, _) = CreateQuery();
        registry.Register("surface", ".surface");
        var entered = 0;
        var left = 0;
        var selection = query.Select(".surface").Hover(_ => entered++, _ => left++);

        selection.Trigger<MouseEventArgs>("mouseenter", new MouseEventArgs());
        selection.Trigger<MouseEventArgs>("mouseleave", new MouseEventArgs());

        Assert.Equal(1, entered);
        Assert.Equal(1, left);
    }

    [Fact]
    public void Removing_a_virtual_parent_removes_its_event_registrations_and_those_of_virtual_descendants_only()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("host", ".host");
        registry.Register("live", ".live");
        var parent = query.Select(".host").AppendElement("section", "parent");
        var child = parent.AppendElement("button", "child");
        var parentKey = parent.ToArray().Single();
        var childKey = child.ToArray().Single();
        var removedCount = 0;
        var liveCount = 0;
        parent.On("click", () => removedCount++);
        child.On<MouseEventArgs>("click", _ => removedCount++);
        query.Select(".live").On("click", () => liveCount++);

        state.RemoveVirtualNode(parentKey);
        state.Trigger(parentKey, "click");
        state.Trigger(childKey, "click", new MouseEventArgs());
        query.Select(".live").Trigger("click");

        Assert.Empty(state.EventNamesFor(parentKey));
        Assert.Empty(state.EventNamesFor(childKey));
        Assert.Equal(0, removedCount);
        Assert.Equal(1, liveCount);
    }

    private static (Query Query, QueryElementRegistry Registry, QueryRenderState State) CreateQuery()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        return (new Query(state, registry), registry, state);
    }

    [Fact]
    public void AddClass_adds_new_class_and_HasClass_reports_it()
    {
        var (query, registry, _) = CreateQuery();
        registry.Register("card", ".card");

        var selection = query.Select(".card").AddClass("active");

        Assert.True(selection.HasClass("active"));
        Assert.True(selection.HasClass("card"));
    }

    [Fact]
    public void RemoveClass_removes_a_base_class()
    {
        var (query, registry, _) = CreateQuery();
        registry.Register("card", ".card", ".highlighted");

        var selection = query.Select(".card").RemoveClass("highlighted");

        Assert.False(selection.HasClass("highlighted"));
        Assert.True(selection.HasClass("card"));
    }

    [Fact]
    public void ToggleClass_flips_class_membership()
    {
        var (query, registry, _) = CreateQuery();
        registry.Register("card", ".card");

        var selection = query.Select(".card").ToggleClass("active");
        Assert.True(selection.HasClass("active"));

        selection.ToggleClass("active");
        Assert.False(selection.HasClass("active"));
    }

    [Fact]
    public void ToggleClass_with_explicit_force_ignores_current_state()
    {
        var (query, registry, _) = CreateQuery();
        registry.Register("card", ".card");

        var selection = query.Select(".card").ToggleClass("active", force: true);
        Assert.True(selection.HasClass("active"));

        selection.ToggleClass("active", force: true);
        Assert.True(selection.HasClass("active"));

        selection.ToggleClass("active", force: false);
        Assert.False(selection.HasClass("active"));
    }

    [Fact]
    public void AddClass_supports_space_separated_class_names()
    {
        var (query, registry, _) = CreateQuery();
        registry.Register("card", ".card");

        var selection = query.Select(".card").AddClass("one two");

        Assert.True(selection.HasClass("one"));
        Assert.True(selection.HasClass("two"));
    }

    [Fact]
    public void Hide_and_Show_toggle_visibility_state()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("card", ".card");

        query.Select(".card").Hide();
        Assert.True(state.IsHidden("card"));

        query.Select(".card").Show();
        Assert.False(state.IsHidden("card"));
    }

    [Fact]
    public void Toggle_flips_hidden_state_without_argument()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("card", ".card");

        query.Select(".card").Toggle();
        Assert.True(state.IsHidden("card"));

        query.Select(".card").Toggle();
        Assert.False(state.IsHidden("card"));
    }

    [Fact]
    public void Remove_marks_element_removed()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("card", ".card");

        query.Select(".card").Remove();

        Assert.True(state.IsRemoved("card"));
    }

    [Fact]
    public void Empty_clears_content_and_removes_registered_descendants()
    {
        var (query, registry, state) = CreateQuery();
        registry.RegisterWithParent("panel", null, ".panel");
        registry.RegisterWithParent("child", "panel", ".child");
        query.Select(".panel").Text("some text");

        query.Select(".panel").Empty();

        Assert.Equal(string.Empty, state.ContentFor("panel")?.Value);
        Assert.True(state.IsRemoved("child"));
    }

    [Fact]
    public void Append_and_Prepend_accumulate_ordered_encoded_fragments()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");

        query.Select(".list").Append("first").Append("second");
        query.Select(".list").Prepend("zero-b").Prepend("zero-a");

        Assert.Equal(["zero-a", "zero-b"], state.PrependedFor("list").Select(f => f.Value));
        Assert.Equal(["first", "second"], state.AppendedFor("list").Select(f => f.Value));
        Assert.All(state.PrependedFor("list").Concat(state.AppendedFor("list")), f => Assert.False(f.IsHtml));
    }

    [Fact]
    public void AppendHtml_and_PrependHtml_mark_fragments_as_trusted_markup()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");

        query.Select(".list").AppendHtml("<b>end</b>").PrependHtml("<b>start</b>");

        Assert.True(state.AppendedFor("list").Single().IsHtml);
        Assert.True(state.PrependedFor("list").Single().IsHtml);
    }

    [Fact]
    public void Setting_Text_or_Html_clears_previously_appended_and_prepended_fragments()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");
        query.Select(".list").Append("stale");

        query.Select(".list").Text("fresh");

        Assert.Empty(state.AppendedFor("list"));
    }

    [Fact]
    public void Empty_clears_appended_and_prepended_fragments()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");
        query.Select(".list").Append("stale").Prepend("also-stale");

        query.Select(".list").Empty();

        Assert.Empty(state.AppendedFor("list"));
        Assert.Empty(state.PrependedFor("list"));
    }

    [Fact]
    public void AppendElement_creates_a_registered_child_selectable_by_tag_and_class()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");

        var created = query.Select(".list").AppendElement("li", "item", "one");
        var newKey = created.ToArray().Single();

        Assert.True(state.IsVirtual(newKey));
        Assert.Equal(["one"], query.Select(".list").Children().Map((_, key) => state.ContentFor(key)?.Value ?? state.VirtualNodeFor(key)!.Text));
        Assert.Contains(newKey, registry.Resolve("li.item"));
        Assert.Equal(["list"], registry.Parent([newKey]));
    }

    [Fact]
    public void AppendElement_and_PrependElement_control_ordering_among_virtual_children()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");

        query.Select(".list").AppendElement("li", text: "b");
        query.Select(".list").AppendElement("li", text: "c");
        query.Select(".list").PrependElement("li", text: "a");

        var order = state.VirtualChildKeysFor("list").Select(key => state.VirtualNodeFor(key)!.Text);
        Assert.Equal(["a", "b", "c"], order);
    }

    [Fact]
    public void InsertElementBefore_and_InsertElementAfter_position_relative_to_an_existing_virtual_sibling()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");
        var middleSelection = query.Select(".list").AppendElement("li", text: "middle");

        middleSelection.InsertElementBefore("li", text: "before");
        middleSelection.InsertElementAfter("li", text: "after");

        var order = state.VirtualChildKeysFor("list").Select(key => state.VirtualNodeFor(key)!.Text);
        Assert.Equal(["before", "middle", "after"], order);
    }

    [Fact]
    public void InsertBefore_moves_the_receiver_before_its_single_target_anchor_and_preserves_the_receiver_selection()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");
        var anchor = query.Select(".list").AppendElement("li", text: "anchor");
        var moving = query.Select(".list").AppendElement("li", text: "moving");

        var result = moving.InsertBefore(anchor);

        Assert.Same(moving, result);
        Assert.Equal(["moving", "anchor"], state.VirtualChildKeysFor("list").Select(key => state.VirtualNodeFor(key)!.Text));
    }

    [Fact]
    public void InsertAfter_moves_the_receiver_after_its_single_target_anchor_and_preserves_the_receiver_selection()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");
        var moving = query.Select(".list").AppendElement("li", text: "moving");
        var anchor = query.Select(".list").AppendElement("li", text: "anchor");

        var result = moving.InsertAfter(anchor);

        Assert.Same(moving, result);
        Assert.Equal(["anchor", "moving"], state.VirtualChildKeysFor("list").Select(key => state.VirtualNodeFor(key)!.Text));
    }

    [Fact]
    public void ReplaceWith_moves_a_virtual_replacement_to_the_target_position_and_unregisters_the_target()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");
        query.Select(".list").AppendElement("li", text: "before");
        var target = query.Select(".list").AppendElement("li", "target", "target");
        var replacement = query.Select(".list").AppendElement("li", "replacement", "replacement");
        replacement.Detach();

        var result = target.ReplaceWith(replacement);

        Assert.Same(target, result);
        Assert.Equal(["before", "replacement"], state.VirtualChildKeysFor("list").Select(key => state.VirtualNodeFor(key)!.Text));
        Assert.Empty(query.Select(".target").ToArray());
        Assert.Single(query.Select(".replacement").ToArray());
    }

    [Fact]
    public void ReplaceAll_moves_the_receiver_to_the_target_position()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");
        var target = query.Select(".list").AppendElement("li", "target", "target");
        var replacement = query.Select(".list").AppendElement("li", "replacement", "replacement");
        replacement.Detach();

        var result = replacement.ReplaceAll(target);

        Assert.Same(replacement, result);
        Assert.Equal(["replacement"], state.VirtualChildKeysFor("list").Select(key => state.VirtualNodeFor(key)!.Text));
        Assert.Empty(query.Select(".target").ToArray());
    }

    [Fact]
    public void Replace_is_an_alias_for_ReplaceWith()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");
        var target = query.Select(".list").AppendElement("li", "target", "target");
        var replacement = query.Select(".list").AppendElement("li", "replacement", "replacement");
        replacement.Detach();

        target.Replace(replacement);

        Assert.Equal(["replacement"], state.VirtualChildKeysFor("list").Select(key => state.VirtualNodeFor(key)!.Text));
        Assert.Empty(query.Select(".target").ToArray());
    }

    [Fact]
    public void ReplaceWith_rejects_self_replacement()
    {
        var (query, registry, _) = CreateQuery();
        registry.Register("card", ".card");
        var card = query.Select(".card");

        Assert.Throws<InvalidOperationException>(() => card.ReplaceWith(card));
    }

    [Fact]
    public void WrapAll_creates_one_wrapper_and_preserves_selected_virtual_sibling_order()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");
        var first = query.Select(".list").AppendElement("li", "item", "first");
        var second = query.Select(".list").AppendElement("li", "item", "second");

        var wrapper = query.Select(".item").WrapAll("section", "wrapper");
        var wrapperKey = wrapper.ToArray().Single();

        Assert.Equal([wrapperKey], state.VirtualChildKeysFor("list"));
        Assert.Equal([first.ToArray().Single(), second.ToArray().Single()], state.VirtualChildKeysFor(wrapperKey));
        Assert.Equal([wrapperKey], query.Select(".item").Parent().ToArray());
    }

    [Fact]
    public void WrapAll_rejects_a_cross_parent_selection_before_creating_a_wrapper()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("listA", ".listA");
        registry.Register("listB", ".listB");
        query.Select(".listA").AppendElement("li", "a", "a");
        query.Select(".listB").AppendElement("li", "b", "b");
        var virtualCount = state.AllVirtualKeys.Count;

        Assert.Throws<InvalidOperationException>(() => query.Select(".a").Add(".b").WrapAll("section"));

        Assert.Equal(virtualCount, state.AllVirtualKeys.Count);
        Assert.Single(state.VirtualChildKeysFor("listA"));
        Assert.Single(state.VirtualChildKeysFor("listB"));
    }

    [Fact]
    public void WrapInner_moves_existing_owned_children_under_a_component_owned_wrapper()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("panel", ".panel");
        state.RegisterRealElement("panel", _ => { });
        state.RegisterInnerContent("panel", _ => { });
        var first = query.Select(".panel").AppendElement("span", "item", "first");
        var second = query.Select(".panel").AppendElement("span", "item", "second");

        var wrapper = query.Select(".panel").WrapInner("section", "inner");
        var wrapperKey = wrapper.ToArray().Single();

        Assert.Equal([wrapperKey], state.VirtualChildKeysFor("panel"));
        Assert.Equal([first.ToArray().Single(), second.ToArray().Single()], state.VirtualChildKeysFor(wrapperKey));
        Assert.Equal([wrapperKey], query.Select(".item").Parent().ToArray().Distinct().ToArray());
    }

    [Fact]
    public void WrapInner_rejects_a_virtual_target_before_creating_a_wrapper()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");
        var item = query.Select(".list").AppendElement("li", "item");
        var virtualCount = state.AllVirtualKeys.Count;

        Assert.Throws<InvalidOperationException>(() => item.WrapInner("section"));

        Assert.Equal(virtualCount, state.AllVirtualKeys.Count);
        Assert.Equal([item.ToArray().Single()], state.VirtualChildKeysFor("list"));
    }

    [Fact]
    public void Clear_removes_component_owned_inner_wrapper_mappings()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("panel", ".panel");
        state.RegisterRealElement("panel", _ => { });
        state.RegisterInnerContent("panel", _ => { });
        query.Select(".panel").WrapInner("section");

        state.Clear();

        Assert.Null(state.InnerWrapperFor("panel"));
    }

    [Fact]
    public void Generated_metadata_advertises_all_supported_structural_operations()
    {
        Assert.Contains("ReplaceWith", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("ReplaceAll", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("Replace", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("InsertBefore", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("InsertAfter", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("WrapAll", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("WrapInner", QueryGeneratedMetadata.SupportedOperations);
    }

    [Fact]
    public void Generated_metadata_advertises_typed_event_compatibility_aliases()
    {
        Assert.Contains("TriggerHandler", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("Bind", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("Unbind", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("TypedTriggerHandler", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("TypedBind", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("TypedUnbind", QueryGeneratedMetadata.SupportedOperations);
    }

    [Fact]
    public void InsertElementBefore_throws_when_anchor_has_no_registered_parent()
    {
        var (query, registry, _) = CreateQuery();
        registry.Register("orphan", ".orphan");

        Assert.Throws<InvalidOperationException>(() => query.Select(".orphan").InsertElementBefore("div"));
    }

    [Fact]
    public void Remove_on_a_virtual_element_fully_detaches_it_from_state_and_registry()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");
        var childSelection = query.Select(".list").AppendElement("li", "item");
        var child = childSelection.ToArray().Single();

        childSelection.Remove();

        Assert.False(state.IsVirtual(child));
        Assert.Empty(state.VirtualChildKeysFor("list"));
        Assert.Empty(registry.Resolve("li.item"));
    }

    [Fact]
    public void Removing_a_virtual_parent_cascades_to_its_virtual_children()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");
        var groupSelection = query.Select(".list").AppendElement("ul", "group");
        var group = groupSelection.ToArray().Single();
        groupSelection.AppendElement("li", text: "nested");
        var nestedKey = state.VirtualChildKeysFor(group).Single();

        state.RemoveVirtualNode(group);

        Assert.False(state.IsVirtual(group));
        Assert.False(state.IsVirtual(nestedKey));
    }

    [Fact]
    public void AppendTo_moves_a_virtual_element_between_parents()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("listA", ".listA");
        registry.Register("listB", ".listB");
        var item = query.Select(".listA").AppendElement("li", text: "roaming");

        item.AppendTo(query.Select(".listB"));

        Assert.Empty(state.VirtualChildKeysFor("listA"));
        Assert.Equal([item.ToArray().Single()], state.VirtualChildKeysFor("listB"));
        Assert.Equal(["listB"], registry.Parent(item.ToArray()));
    }

    [Fact]
    public void PrependTo_inserts_a_virtual_element_at_the_start_of_the_target()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");
        query.Select(".list").AppendElement("li", text: "existing");
        var moved = query.Select(".list").AppendElement("li", text: "moved");
        moved.Detach();

        moved.PrependTo(query.Select(".list"));

        var order = state.VirtualChildKeysFor("list").Select(key => state.VirtualNodeFor(key)!.Text);
        Assert.Equal(["moved", "existing"], order);
    }

    [Fact]
    public void AppendTo_throws_when_source_is_a_real_Razor_declared_element()
    {
        var (query, registry, _) = CreateQuery();
        registry.Register("real", ".real");
        registry.Register("target", ".target");

        Assert.Throws<InvalidOperationException>(() => query.Select(".real").AppendTo(query.Select(".target")));
    }

    [Fact]
    public void Detach_orphans_a_virtual_element_without_deleting_it()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");
        var item = query.Select(".list").AppendElement("li", "item", "text");
        var key = item.ToArray().Single();

        item.Detach();

        Assert.True(state.IsVirtual(key));
        Assert.Empty(state.VirtualChildKeysFor("list"));
        Assert.Contains(key, registry.Resolve("li.item"));
    }

    [Fact]
    public void Detached_element_can_be_reattached_via_AppendTo()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");
        var item = query.Select(".list").AppendElement("li", text: "text");
        item.Detach();

        item.AppendTo(query.Select(".list"));

        Assert.Equal([item.ToArray().Single()], state.VirtualChildKeysFor("list"));
    }

    [Fact]
    public void Clone_duplicates_a_virtual_elements_state_under_a_new_key()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");
        var original = query.Select(".list").AppendElement("li", "item", "hello");
        original.AddClass("active").Attr("data-role", "row").Hide();

        var clone = original.Clone();
        var cloneKey = clone.ToArray().Single();
        var originalKey = original.ToArray().Single();

        Assert.NotEqual(originalKey, cloneKey);
        Assert.True(state.IsVirtual(cloneKey));
        Assert.Null(state.VirtualNodeFor(cloneKey)!.ParentKey);
        Assert.Equal("hello", state.VirtualNodeFor(cloneKey)!.Text);
        Assert.True(clone.HasClass("active"));
        Assert.True(clone.HasClass("item"));
        Assert.Equal("row", state.AttributesFor(cloneKey)["data-role"]);
        Assert.True(state.IsHidden(cloneKey));
        Assert.Contains(cloneKey, registry.Resolve("li.item"));
    }

    [Fact]
    public void Clone_is_unattached_until_explicitly_attached()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");
        var original = query.Select(".list").AppendElement("li", text: "hello");

        var clone = original.Clone();

        Assert.Single(state.VirtualChildKeysFor("list"));
        clone.AppendTo(query.Select(".list"));
        Assert.Equal(2, state.VirtualChildKeysFor("list").Count);
    }

    [Fact]
    public void Clone_throws_for_a_real_Razor_declared_element()
    {
        var (query, registry, _) = CreateQuery();
        registry.Register("real", ".real");

        Assert.Throws<InvalidOperationException>(() => query.Select(".real").Clone());
    }

    [Fact]
    public void Clear_removes_virtual_elements_from_state()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("list", ".list");
        query.Select(".list").AppendElement("li", text: "one");

        state.Clear();

        Assert.Empty(state.AllVirtualKeys);
        Assert.Empty(state.VirtualChildKeysFor("list"));
    }

    [Fact]
    public void Clear_resets_class_visibility_and_removal_state()
    {
        var (query, registry, state) = CreateQuery();
        registry.Register("card", ".card");
        query.Select(".card").AddClass("active").Hide().Remove();

        state.Clear();

        Assert.False(state.IsHidden("card"));
        Assert.False(state.IsRemoved("card"));
        Assert.False(query.Select(".card").HasClass("active"));
    }
}
