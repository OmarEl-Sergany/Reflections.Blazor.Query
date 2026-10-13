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
        private readonly QueryFormCoordinator? _forms;

        public Query(
            QueryRenderState state,
            QueryElementRegistry? registry = null,
            QueryMeasurementCoordinator? measurements = null,
            QueryFormCoordinator? forms = null)
        {
            _state = state;
            _registry = registry;
            _measurements = measurements;
            _forms = forms;
        }

        public IDisposable BeginBatch() => _state.BeginBatch();

        public QuerySelection Select(string selector)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(selector);
            return new QuerySelection(selector, _state, _registry, _measurements, _forms, resolvedKeys: _registry?.Resolve(selector));
        }

        public IQueryScope CreateScope() => new QueryScope(this, Guid.NewGuid().ToString("N"));

        internal QuerySelection SelectInScope(string selector, string scopeKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(selector);
            ArgumentException.ThrowIfNullOrWhiteSpace(scopeKey);
            return new QuerySelection(selector, _state, _registry, _measurements, _forms, scopeKey, resolvedKeys: _registry?.FilterScope(_registry.Resolve(selector), scopeKey));
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
