using Reflections.Blazor.Query;

namespace Reflections.Blazor.Query.ComponentTests;

public sealed class QueryHostContractTests
{
    [Fact]
    public void Layout_and_page_scopes_can_be_rendered_independently()
    {
        var pageState = new QueryRenderState();
        var layoutState = new LayoutQueryRenderState();

        new Query(pageState).Select(".content").Css("color", "red");
        new LayoutQuery(layoutState).Select(".sidebar").Css("background", "black");

        Assert.NotSame(pageState, layoutState);
    }

    [Fact]
    public void Layout_state_survives_page_state_reset()
    {
        var pageState = new QueryRenderState();
        var layoutState = new LayoutQueryRenderState();
        var layoutChanges = 0;
        layoutState.Changed += () => layoutChanges++;

        new LayoutQuery(layoutState).Select(".sidebar").Css("background", "black");
        pageState.ClearForTest();

        Assert.Equal(1, layoutChanges);
    }
}

internal static class QueryStateTestExtensions
{
    public static void ClearForTest(this QueryRenderState state)
    {
        using (state.BeginBatch())
        {
            new Query(state).Select(".temporary").Css("display", "none");
        }
    }
}
