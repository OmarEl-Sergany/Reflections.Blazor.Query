using System;
using System.Collections.Generic;
using System.Text;

namespace Reflections.Blazor.Query
{
    /// <summary>Creates fluent selections over elements registered with a Blazor Query render host.</summary>
    /// <remarks>
    /// Query operations update library-owned render state. They do not mutate the browser DOM directly;
    /// selected elements must be represented by registered <see cref="QueryElement"/> instances.
    /// </remarks>
    public class Query
    {
        private readonly QueryRenderState _state;
        private readonly QueryElementRegistry? _registry;
        private readonly QueryMeasurementCoordinator? _measurements;
        private readonly QueryFormCoordinator? _forms;

        /// <summary>Creates a query facade over the supplied render state and optional element services.</summary>
        /// <param name="state">The render state that receives selection changes.</param>
        /// <param name="registry">Resolves selectors to registered element keys when available.</param>
        /// <param name="measurements">Provides read-only browser measurements for geometry-dependent operations.</param>
        /// <param name="forms">Provides read-only browser values for form operations.</param>
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

        /// <summary>Groups subsequent render-state changes so they notify the owning host as one update.</summary>
        /// <returns>A token that ends the batch when disposed.</returns>
        public IDisposable BeginBatch() => _state.BeginBatch();

        /// <summary>Creates a selector-backed fluent selection.</summary>
        /// <param name="selector">A supported selector evaluated against registered Query elements.</param>
        /// <returns>A selection that can apply operations to matching registered elements.</returns>
        /// <remarks>Arbitrary browser DOM elements are outside the library-owned render boundary.</remarks>
        public QuerySelection Select(string selector)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(selector);
            return new QuerySelection(selector, _state, _registry, _measurements, _forms, resolvedKeys: _registry?.Resolve(selector));
        }

        /// <summary>Creates a selection using the familiar jQuery constructor name.</summary>
        /// <remarks>This is an alias for <see cref="Select(string)"/> and retains the library-owned element boundary.</remarks>
        public QuerySelection JQuery(string selector) => Select(selector);

        /// <summary>Creates a disposable context that limits subsequent selections to its registered subtree.</summary>
        /// <returns>A scope-bound query context.</returns>
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
    /// <summary>Query facade for state owned by a persistent application layout.</summary>
    public sealed class LayoutQuery : Query
    {
        public LayoutQuery(
            LayoutQueryRenderState state,
            QueryElementRegistry? registry = null,
            QueryMeasurementCoordinator? measurements = null) : base(state, registry, measurements) { }
    }

    /// <summary>Render state intended to persist across page navigation while its layout host remains mounted.</summary>
    public sealed class LayoutQueryRenderState : QueryRenderState { }

    /// <summary>
    /// Render state whose lifetime is explicitly owned by the Blazor application/circuit rather than a page.
    /// </summary>
    public sealed class ApplicationQueryRenderState : QueryRenderState { }

    /// <summary>
    /// Query facade for application-lifetime state. Mount a QueryHost with this state and
    /// <c>ClearOnNavigation="false"</c> around the application content it owns.
    /// </summary>
    public sealed class ApplicationQuery : Query
    {
        public ApplicationQuery(
            ApplicationQueryRenderState state,
            QueryElementRegistry? registry = null,
            QueryMeasurementCoordinator? measurements = null,
            QueryFormCoordinator? forms = null) : base(state, registry, measurements, forms) { }
    }
}
