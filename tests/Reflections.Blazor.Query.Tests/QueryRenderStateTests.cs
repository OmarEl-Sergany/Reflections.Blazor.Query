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
}
