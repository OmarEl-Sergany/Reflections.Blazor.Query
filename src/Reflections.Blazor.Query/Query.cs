using System;
using System.Collections.Generic;
using System.Text;

namespace Reflections.Blazor.Query
{
    public class Query
    {
    private readonly QueryRenderState _state;

        public Query(QueryRenderState state) => _state = state;

        public IDisposable BeginBatch() => _state.BeginBatch();

        public QuerySelection Select(string selector)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(selector);
            return new QuerySelection(selector, _state);
        }
    }
}

namespace Reflections.Blazor.Query
{
    public sealed class LayoutQuery : Query
    {
        public LayoutQuery(LayoutQueryRenderState state) : base(state) { }
    }

    public sealed class LayoutQueryRenderState : QueryRenderState { }
}
