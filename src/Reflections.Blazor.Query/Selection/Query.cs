using System;
using System.Collections.Generic;
using System.Text;

namespace Reflections.Blazor.Query
{
    public class Query
    {
        private readonly QueryRenderState _state;
        private readonly QueryElementRegistry? _registry;
        private readonly QueryMeasurementCoordinator? _measurements;

        public Query(
            QueryRenderState state,
            QueryElementRegistry? registry = null,
            QueryMeasurementCoordinator? measurements = null)
        {
            _state = state;
            _registry = registry;
            _measurements = measurements;
        }

        public IDisposable BeginBatch() => _state.BeginBatch();

        public QuerySelection Select(string selector)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(selector);
            return new QuerySelection(selector, _state, _registry, _measurements, resolvedKeys: _registry?.Resolve(selector));
        }
    }
}

namespace Reflections.Blazor.Query
{
    public sealed class LayoutQuery : Query
    {
        public LayoutQuery(
            LayoutQueryRenderState state,
            QueryElementRegistry? registry = null,
            QueryMeasurementCoordinator? measurements = null) : base(state, registry, measurements) { }
    }

    public sealed class LayoutQueryRenderState : QueryRenderState { }
}
