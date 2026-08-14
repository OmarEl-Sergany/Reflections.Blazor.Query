using System;
using System.Collections.Generic;
using System.Text;

namespace Reflections.Blazor.Query
{
    public sealed class Query
    {
    private readonly QueryRenderState _state;

        public Query(QueryRenderState state) => _state = state;

        public QuerySelection Select(string selector)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(selector);
            return new QuerySelection(selector, _state);
        }
    }
}
