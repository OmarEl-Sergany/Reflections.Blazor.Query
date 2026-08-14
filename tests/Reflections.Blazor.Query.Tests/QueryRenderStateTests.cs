using Reflections.Blazor.Query;

namespace Reflections.Blazor.Query.Tests;

public sealed class QueryRenderStateTests
{
    [Fact]
    public void Batch_emits_one_change_notification()
    {
        var state = new QueryRenderState();
        var changes = 0;
        state.Changed += () => changes++;

        using (state.BeginBatch())
        {
            state.SetCssForTest(".one", "color", "red");
            state.SetCssForTest(".two", "color", "blue");
        }

        Assert.Equal(1, changes);
        Assert.Equal(2, state.RuleCountForTest());
    }

    [Fact]
    public void Nested_batches_flush_when_outer_batch_completes()
    {
        var state = new QueryRenderState();
        var changes = 0;
        state.Changed += () => changes++;

        using (state.BeginBatch())
        {
            using (state.BeginBatch())
                state.SetCssForTest(".one", "color", "red");

            Assert.Equal(0, changes);
        }

        Assert.Equal(1, changes);
    }

    [Fact]
    public void Clear_emits_only_when_state_has_rules()
    {
        var state = new QueryRenderState();
        var changes = 0;
        state.Changed += () => changes++;

        state.ClearForTest();
        Assert.Equal(0, changes);

        state.SetCssForTest(".one", "color", "red");
        state.ClearForTest();
        state.ClearForTest();

        Assert.Equal(2, changes);
    }

    [Fact]
    public void Navigation_clear_removes_page_rules()
    {
        var page = new QueryRenderState();
        new Query(page).Select(".page").Css("color", "red");

        page.ClearForTest();

        Assert.Equal(0, page.RuleCountForTest());
    }

    [Fact]
    public void Clearing_page_state_preserves_layout_state()
    {
        var page = new QueryRenderState();
        var layout = new LayoutQueryRenderState();
        new Query(page).Select(".page").Css("color", "red");
        new LayoutQuery(layout).Select(".sidebar").Css("background", "black");

        page.ClearForTest();

        Assert.Equal(0, page.RuleCountForTest());
        Assert.Equal(1, layout.RuleCountForTest());
    }

    [Fact]
    public void Application_layout_and_page_scopes_are_distinct()
    {
        var application = new QueryRenderState();
        var layout = new LayoutQueryRenderState();
        var page = new QueryRenderState();

        new Query(application).Select("html").Css("font-size", "16px");
        new LayoutQuery(layout).Select(".sidebar").Css("background", "black");
        new Query(page).Select(".content").Css("color", "red");

        Assert.Equal(1, application.RuleCountForTest());
        Assert.Equal(1, layout.RuleCountForTest());
        Assert.Equal(1, page.RuleCountForTest());
        Assert.NotSame(application, layout);
        Assert.NotSame(application, page);
        Assert.NotSame(layout, page);
    }
}
