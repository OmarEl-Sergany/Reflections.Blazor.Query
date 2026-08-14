using Reflections.Blazor.Query;

namespace Reflections.Blazor.Query.Tests;

internal static class QueryRenderStateTestExtensions
{
    public static void SetCssForTest(this QueryRenderState state, string selector, string property, string value) =>
        state.SetCss(selector, property, value);

    public static void ClearForTest(this QueryRenderState state) => state.Clear();

    public static int RuleCountForTest(this QueryRenderState state) => state.Rules.Count;
}
