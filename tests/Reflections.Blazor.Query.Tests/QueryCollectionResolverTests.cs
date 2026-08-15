using System;
using System.Collections.Generic;
using Reflections.Blazor.Query;

namespace Reflections.Blazor.Query.Tests;

public sealed class QueryCollectionResolverTests
{
    [Fact]
    public void Registry_preserves_generated_element_order()
    {
        var registry = CreateRegistry();
        var selection = new Query(new QueryRenderState(), registry).Select(".item");

        Assert.Equal(3, selection.Length);
        Assert.Equal(["first", "second", "third"], selection.ToArray());
    }

    [Fact]
    public void First_Last_and_Eq_select_stable_generated_keys()
    {
        var selection = new Query(new QueryRenderState(), CreateRegistry()).Select(".item");

        Assert.Equal("[data-rbq-key=\"first\"]", selection.First().Selector);
        Assert.Equal("[data-rbq-key=\"third\"]", selection.Last().Selector);
        Assert.Equal("[data-rbq-key=\"second\"]", selection.Eq(1).Selector);
        Assert.Equal("[data-rbq-key=\"second\"]", selection.Eq(-2).Selector);
        Assert.Equal(0, selection.Eq(20).Length);
    }

    [Fact]
    public void Slice_supports_positive_and_negative_collection_indexes()
    {
        var selection = new Query(new QueryRenderState(), CreateRegistry()).Select(".item");

        Assert.Equal(["second", "third"], selection.Slice(1).ToArray());
        Assert.Equal(["first", "second"], selection.Slice(0, -1).ToArray());
        Assert.Equal(["second"], selection.Slice(-2, -1).ToArray());
    }

    [Fact]
    public void Positional_selection_still_writes_through_render_state()
    {
        var state = new QueryRenderState();
        var selection = new Query(state, CreateRegistry()).Select(".item");

        selection.Eq(1).Css("color", "red");

        Assert.Equal("[data-rbq-key=\"second\"]", state.Rules[0].Selector);
    }

    [Fact]
    public void Unresolved_css_selection_rejects_collection_position_operations()
    {
        var selection = new Query(new QueryRenderState()).Select(".item");

        Assert.Throws<InvalidOperationException>(() => _ = selection.Length);
        Assert.Throws<InvalidOperationException>(() => selection.First());
        Assert.Throws<InvalidOperationException>(() => selection.ToArray());
    }

    [Fact]
    public void Get_supports_all_positive_negative_and_missing_indexes()
    {
        var selection = new Query(new QueryRenderState(), CreateRegistry()).Select(".item");

        Assert.Equal(["first", "second", "third"], selection.Get());
        Assert.Equal("second", selection.Get(1));
        Assert.Equal("third", selection.Get(-1));
        Assert.Null(selection.Get(20));
        Assert.Null(selection.Get(-20));
    }

    [Fact]
    public void Index_returns_collection_position_for_a_stable_key()
    {
        var selection = new Query(new QueryRenderState(), CreateRegistry()).Select(".item");

        Assert.Equal(0, selection.Index("first"));
        Assert.Equal(2, selection.Index("third"));
        Assert.Equal(-1, selection.Index("missing"));
    }

    [Fact]
    public void Each_visits_keys_in_order_and_can_stop_early()
    {
        var selection = new Query(new QueryRenderState(), CreateRegistry()).Select(".item");
        var visited = new List<string>();

        var returned = selection.Each((index, key) =>
        {
            visited.Add($"{index}:{key}");
            return index < 1;
        });

        Assert.Same(selection, returned);
        Assert.Equal(["0:first", "1:second"], visited);
    }

    [Fact]
    public void Map_projects_resolved_keys_without_changing_the_selection()
    {
        var selection = new Query(new QueryRenderState(), CreateRegistry()).Select(".item");

        var mapped = selection.Map((index, key) => $"{index}-{key}");

        Assert.Equal(["0-first", "1-second", "2-third"], mapped);
        Assert.Equal(3, selection.Length);
    }

    [Fact]
    public void PushStack_creates_a_resolved_selection_and_End_restores_its_source()
    {
        var selection = new Query(new QueryRenderState(), CreateRegistry()).Select(".item");

        var pushed = selection.PushStack(["third", "first"]);

        Assert.Equal(["third", "first"], pushed.ToArray());
        Assert.Equal("[data-rbq-key=\"third\"], [data-rbq-key=\"first\"]", pushed.Selector);
        Assert.Same(selection, pushed.End());
    }

    [Fact]
    public void Filter_Not_and_Is_use_library_owned_element_metadata()
    {
        var selection = new Query(new QueryRenderState(), CreateRegistry()).Select(".item");

        var filtered = selection.Filter(".active");
        var excluded = selection.Not(".active");

        Assert.Equal(["second"], filtered.ToArray());
        Assert.Equal("[data-rbq-key=\"second\"]", filtered.Selector);
        Assert.Equal(["first", "third"], excluded.ToArray());
        Assert.True(selection.Is(".active"));
        Assert.False(selection.Is(".missing"));
    }

    [Fact]
    public void Add_merges_resolved_keys_in_registry_order()
    {
        var selection = new Query(new QueryRenderState(), CreateRegistry()).Select(".active");

        var combined = selection.Add(".item");

        Assert.Equal(["first", "second", "third"], combined.ToArray());
        Assert.Equal(
            "[data-rbq-key=\"first\"], [data-rbq-key=\"second\"], [data-rbq-key=\"third\"]",
            combined.Selector);
        Assert.Same(selection, combined.End());
    }

    [Fact]
    public void AddBack_merges_resolved_previous_selection_in_registry_order()
    {
        var root = new Query(new QueryRenderState(), CreateRegistry()).Select(".item");
        var current = root.Eq(-1);

        Assert.Equal(["first", "second", "third"], current.AddBack().ToArray());
        Assert.Equal(["second", "third"], current.AddBack(".active").ToArray());
        Assert.Same(current, current.AddBack().End());
    }

    [Fact]
    public void Closest_and_ParentsUntil_follow_owned_parent_metadata()
    {
        var query = new Query(new QueryRenderState(), CreateHierarchyRegistry());

        Assert.Equal(["group"], query.Select(".active").Closest(".group").ToArray());
        Assert.Equal(["group"], query.Select(".active").ParentsUntil(".panel").ToArray());
        Assert.Empty(query.Select(".active").ParentsUntil(".panel", ".panel").ToArray());
    }

    [Fact]
    public void NextUntil_and_PrevUntil_stop_at_owned_sibling_boundaries()
    {
        var query = new Query(new QueryRenderState(), CreateHierarchyRegistry());

        Assert.Equal(["second"], query.Select(".first").NextUntil(".stop").ToArray());
        Assert.Equal(["second"], query.Select(".first").NextUntil(".stop", ".active").ToArray());
        Assert.Equal(["third"], query.Select(".fourth").PrevUntil(".stop").ToArray());
    }

    [Fact]
    public void Hierarchy_operations_reject_unresolved_selections()
    {
        var selection = new Query(new QueryRenderState()).Select(".item");

        Assert.Throws<InvalidOperationException>(() => selection.Closest(".panel"));
        Assert.Throws<InvalidOperationException>(() => selection.ParentsUntil(".panel"));
        Assert.Throws<InvalidOperationException>(() => selection.NextUntil(".stop"));
        Assert.Throws<InvalidOperationException>(() => selection.PrevUntil(".stop"));
    }

    [Fact]
    public void Is_rejects_unresolved_selections_while_css_filters_remain_composable()
    {
        var selection = new Query(new QueryRenderState()).Select(".item");

        Assert.Throws<InvalidOperationException>(() => selection.Is(".active"));
        Assert.Equal(":is(.item):is(.active)", selection.Filter(".active").Selector);
        Assert.Equal(":is(.item):not(.active)", selection.Not(".active").Selector);
    }

    private static QueryElementRegistry CreateRegistry()
    {
        var registry = new QueryElementRegistry();
        registry.Register("first", ".item", "li");
        registry.Register("second", ".item", ".active");
        registry.Register("third", ".item", "li");
        return registry;
    }

    private static QueryElementRegistry CreateHierarchyRegistry()
    {
        var registry = new QueryElementRegistry();
        registry.Register("panel", ".panel");
        registry.RegisterWithParent("group", "panel", ".group");
        registry.RegisterWithParent("first", "group", ".item", ".first");
        registry.RegisterWithParent("second", "group", ".item", ".active");
        registry.RegisterWithParent("stop", "group", ".stop");
        registry.RegisterWithParent("third", "group", ".item");
        registry.RegisterWithParent("fourth", "group", ".item", ".fourth");
        return registry;
    }
}
