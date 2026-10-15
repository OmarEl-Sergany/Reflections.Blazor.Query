using Reflections.Blazor.Query;

namespace Reflections.Blazor.Query.Tests;

public sealed class QueryScopeTests
{
    [Fact]
    public void Component_scope_resolves_and_traverses_only_its_own_registered_elements()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        var query = new BlazorQuery(state, registry);
        var firstScope = query.CreateScope();
        var secondScope = query.CreateScope();
        registry.RegisterWithParentAndScope("first-root", null, firstScope.Key, ".root");
        registry.RegisterWithParentAndScope("first-item", "first-root", firstScope.Key, ".item");
        registry.RegisterWithParentAndScope("second-root", null, secondScope.Key, ".root");
        registry.RegisterWithParentAndScope("second-item", "second-root", secondScope.Key, ".item");

        Assert.Equal(["first-item"], firstScope.Select(".item").ToArray());
        Assert.Equal(["first-root"], firstScope.Select(".item").Parent().ToArray());
        Assert.Equal(["second-item"], secondScope.Select(".item").ToArray());
    }

    [Fact]
    public void Component_scope_empty_does_not_remove_a_descendant_owned_by_another_scope()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        var query = new BlazorQuery(state, registry);
        var outerScope = query.CreateScope();
        var nestedScope = query.CreateScope();
        registry.RegisterWithParentAndScope("outer", null, outerScope.Key, ".outer");
        registry.RegisterWithParentAndScope("nested", "outer", nestedScope.Key, ".nested");

        outerScope.Select(".outer").Empty();

        Assert.False(state.IsRemoved("nested"));
        Assert.Equal(["nested"], nestedScope.Select(".nested").ToArray());
    }

    [Fact]
    public void Layout_scope_is_independent_from_page_scope()
    {
        var page = new QueryRenderState();
        var layout = new LayoutQueryRenderState();

        new BlazorQuery(page).Select(".page").Css("color", "red");
        new LayoutQuery(layout).Select(".sidebar").Css("background", "black");

        Assert.Equal(1, page.RuleCountForTest());
        Assert.Equal(1, layout.RuleCountForTest());

        page.ClearForTest();

        Assert.Equal(0, page.RuleCountForTest());
        Assert.Equal(1, layout.RuleCountForTest());
    }
}
