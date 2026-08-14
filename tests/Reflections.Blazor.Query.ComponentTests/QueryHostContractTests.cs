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

    [Fact]
    public void Page_host_clear_does_not_clear_layout_host()
    {
        var pageState = new QueryRenderState();
        var layoutState = new LayoutQueryRenderState();
        var pageStateChanges = 0;
        var layoutChanges = 0;
        pageState.Changed += () => pageStateChanges++;
        layoutState.Changed += () => layoutChanges++;
        new Query(pageState).Select(".page").Css("display", "none");
        new LayoutQuery(layoutState).Select(".sidebar").Css("background", "black");

        pageState.ClearForTest();

        Assert.Equal(2, pageStateChanges);
        Assert.Equal(1, layoutChanges);
    }

    [Fact]
    public void Disposed_host_subscription_can_be_removed_without_changing_state()
    {
        var state = new QueryRenderState();
        var changes = 0;
        void OnChanged() => changes++;
        state.Changed += OnChanged;
        new Query(state).Select(".page").Css("color", "red");
        state.Changed -= OnChanged;

        new Query(state).Select(".page").Css("color", "blue");

        Assert.Equal(1, changes);
    }
}

internal static class QueryStateTestExtensions
{
    public static void ClearForTest(this QueryRenderState state)
    {
        state.Clear();
    }
}
