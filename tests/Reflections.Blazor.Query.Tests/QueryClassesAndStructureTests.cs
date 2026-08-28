using Reflections.Blazor.Query;

namespace Reflections.Blazor.Query.Tests;

public sealed class QueryClassesAndStructureTests
{
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
