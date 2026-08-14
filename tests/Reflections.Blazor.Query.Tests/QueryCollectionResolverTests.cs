using System;
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

    private static QueryElementRegistry CreateRegistry()
    {
        var registry = new QueryElementRegistry();
        registry.Register("first", ".item", "li");
        registry.Register("second", ".item", ".active");
        registry.Register("third", ".item", "li");
        return registry;
    }
}
