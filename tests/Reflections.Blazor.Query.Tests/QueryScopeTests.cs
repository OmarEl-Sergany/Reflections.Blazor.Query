using Reflections.Blazor.Query;

namespace Reflections.Blazor.Query.Tests;

public sealed class QueryScopeTests
{
    [Fact]
    public void Layout_scope_is_independent_from_page_scope()
    {
        var page = new QueryRenderState();
        var layout = new LayoutQueryRenderState();

        new Query(page).Select(".page").Css("color", "red");
        new LayoutQuery(layout).Select(".sidebar").Css("background", "black");

        Assert.Equal(1, page.RuleCountForTest());
        Assert.Equal(1, layout.RuleCountForTest());

        page.ClearForTest();

        Assert.Equal(0, page.RuleCountForTest());
        Assert.Equal(1, layout.RuleCountForTest());
    }
}
