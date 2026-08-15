using System;
using System.Collections.Generic;
using System.Text;

namespace Reflections.Blazor.Query
{
    public class Query
    {
        private readonly QueryRenderState _state;
        private readonly QueryElementRegistry? _registry;

        public Query(QueryRenderState state, QueryElementRegistry? registry = null)
        {
            _state = state;
            _registry = registry;
        }

        public IDisposable BeginBatch() => _state.BeginBatch();

        public QuerySelection Select(string selector)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(selector);
            return new QuerySelection(selector, _state, _registry, resolvedKeys: _registry?.Resolve(selector));
        }
    }
}

namespace Reflections.Blazor.Query
{
    public sealed class LayoutQuery : Query
    {
        public LayoutQuery(LayoutQueryRenderState state, QueryElementRegistry? registry = null) : base(state, registry) { }
    }

    public sealed class LayoutQueryRenderState : QueryRenderState { }
}
