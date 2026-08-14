using System;
using Reflections.Blazor.Query;

namespace Reflections.Blazor.Query.Tests;

public sealed class QuerySelectionTests
{
    [Fact]
    public void Add_combines_selectors_and_End_restores_previous_selection()
    {
        var selection = new Query(new QueryRenderState()).Select(".primary");

        var combined = selection.Add(".secondary");

        Assert.Equal(".primary, .secondary", combined.Selector);
        Assert.Same(selection, combined.End());
    }

    [Fact]
    public void AddBack_combines_current_and_previous_selections()
    {
        var root = new Query(new QueryRenderState()).Select(".panel");
        var children = root.Children("p");

        Assert.Equal(":is(.panel) > p, .panel", children.AddBack().Selector);
        Assert.Equal(":is(.panel) > p, :is(.panel):is(.active)", children.AddBack(".active").Selector);
    }

    [Fact]
    public void Traversal_builds_composable_css_selector_expressions()
    {
        var selection = new Query(new QueryRenderState())
            .Select(".panel")
            .Find("li")
            .Filter(".active")
            .Not(".disabled")
            .Has("button");

        Assert.Equal(":is(:is(:is(:is(.panel) li):is(.active)):not(.disabled)):has(button)", selection.Selector);
    }

    [Fact]
    public void Derived_selection_writes_css_through_render_state()
    {
        var state = new QueryRenderState();

        new Query(state).Select(".panel").Children("p").Css("color", "red");

        Assert.Equal(1, state.RuleCountForTest());
        Assert.Equal(":is(.panel) > p", state.Rules[0].Selector);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Selector_operations_reject_empty_selectors(string selector)
    {
        var selection = new Query(new QueryRenderState()).Select(".panel");

        Assert.Throws<ArgumentException>(() => selection.Add(selector));
        Assert.Throws<ArgumentException>(() => selection.Find(selector));
        Assert.Throws<ArgumentException>(() => selection.Filter(selector));
        Assert.Throws<ArgumentException>(() => selection.Not(selector));
        Assert.Throws<ArgumentException>(() => selection.Has(selector));
    }
}
