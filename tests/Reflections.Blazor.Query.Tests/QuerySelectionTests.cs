using System;
using Reflections.Blazor.Query;

namespace Reflections.Blazor.Query.Tests;

public sealed class QuerySelectionTests
{
    [Fact]
    public void Add_combines_selectors_and_End_restores_previous_selection()
    {
        var selection = new BlazorQuery(new QueryRenderState()).Select(".primary");

        var combined = selection.Add(".secondary");

        Assert.Equal(".primary, .secondary", combined.Selector);
        Assert.Same(selection, combined.End());
    }

    [Fact]
    public void JQuery_alias_uses_the_same_library_owned_selector_resolution_as_Select()
    {
        var registry = new QueryElementRegistry();
        registry.Register("first", ".item");
        registry.Register("second", ".item");
        var query = new BlazorQuery(new QueryRenderState(), registry);

        var selection = query.JQuery(".item");

        Assert.Equal(".item", selection.Selector);
        Assert.Equal(["first", "second"], selection.ToArray());
        Assert.Equal(query.Select(".item").ToArray(), selection.ToArray());
        Assert.Contains("JQuery", QueryGeneratedMetadata.SupportedOperations);
    }

    [Fact]
    public void AddBack_combines_current_and_previous_selections()
    {
        var root = new BlazorQuery(new QueryRenderState()).Select(".panel");
        var children = root.Children("p");

        Assert.Equal(":is(.panel) > p, .panel", children.AddBack().Selector);
        Assert.Equal(":is(.panel) > p, :is(.panel):is(.active)", children.AddBack(".active").Selector);
    }

    [Fact]
    public void Traversal_builds_composable_css_selector_expressions()
    {
        var selection = new BlazorQuery(new QueryRenderState())
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

        new BlazorQuery(state).Select(".panel").Children("p").Css("color", "red");

        Assert.Equal(1, state.RuleCountForTest());
        Assert.Equal(":is(.panel) > p", state.Rules[0].Selector);
    }

    [Fact]
    public void Parent_and_ancestor_traversal_select_owners_of_the_current_selection()
    {
        var selection = new BlazorQuery(new QueryRenderState()).Select(".item");

        Assert.Equal("*:has(> :is(.item))", selection.Parent().Selector);
        Assert.Equal("section:has(> :is(.item))", selection.Parent("section").Selector);
        Assert.Equal("*:has(:is(.item))", selection.Parents().Selector);
        Assert.Equal(".container:has(:is(.item))", selection.Parents(".container").Selector);
    }

    [Fact]
    public void Forward_sibling_traversal_builds_adjacent_and_general_sibling_selectors()
    {
        var selection = new BlazorQuery(new QueryRenderState()).Select(".item");

        Assert.Equal(":is(.item) + *", selection.Next().Selector);
        Assert.Equal(":is(.item) + .enabled", selection.Next(".enabled").Selector);
        Assert.Equal(":is(.item) ~ *", selection.NextAll().Selector);
        Assert.Equal(":is(.item) ~ .enabled", selection.NextAll(".enabled").Selector);
    }

    [Fact]
    public void Reverse_and_all_sibling_traversal_use_relational_selectors()
    {
        var selection = new BlazorQuery(new QueryRenderState()).Select(".item");

        Assert.Equal("*:has(+ :is(.item))", selection.Prev().Selector);
        Assert.Equal(".enabled:has(+ :is(.item))", selection.Prev(".enabled").Selector);
        Assert.Equal("*:has(~ :is(.item))", selection.PrevAll().Selector);
        Assert.Equal(".enabled:has(~ :is(.item))", selection.PrevAll(".enabled").Selector);
        Assert.Equal("*:has(~ :is(.item)), :is(.item) ~ *", selection.Siblings().Selector);
    }

    [Fact]
    public void Traversal_history_can_return_to_each_previous_selection()
    {
        var root = new BlazorQuery(new QueryRenderState()).Select(".list");
        var children = root.Children("li");
        var siblings = children.NextAll();

        Assert.Same(children, siblings.End());
        Assert.Same(root, siblings.End().End());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Selector_operations_reject_empty_selectors(string selector)
    {
        var selection = new BlazorQuery(new QueryRenderState()).Select(".panel");

        Assert.Throws<ArgumentException>(() => selection.Add(selector));
        Assert.Throws<ArgumentException>(() => selection.Find(selector));
        Assert.Throws<ArgumentException>(() => selection.Filter(selector));
        Assert.Throws<ArgumentException>(() => selection.Not(selector));
        Assert.Throws<ArgumentException>(() => selection.Has(selector));
        Assert.Throws<ArgumentException>(() => selection.Parent(selector));
        Assert.Throws<ArgumentException>(() => selection.Parents(selector));
        Assert.Throws<ArgumentException>(() => selection.Next(selector));
        Assert.Throws<ArgumentException>(() => selection.NextAll(selector));
        Assert.Throws<ArgumentException>(() => selection.Prev(selector));
        Assert.Throws<ArgumentException>(() => selection.PrevAll(selector));
        Assert.Throws<ArgumentException>(() => selection.Siblings(selector));
    }
}
