using System;

namespace Reflections.Blazor.Query
{
    public sealed class QuerySelection
    {
        private readonly QueryRenderState _state;
        private readonly QueryElementRegistry? _registry;
        private readonly QuerySelection? _previous;
        private readonly IReadOnlyList<string>? _resolvedKeys;

        internal QuerySelection(
            string selector,
            QueryRenderState state,
            QueryElementRegistry? registry = null,
            QuerySelection? previous = null,
            IReadOnlyList<string>? resolvedKeys = null)
        {
            Selector = selector;
            _state = state;
            _registry = registry;
            _previous = previous;
            _resolvedKeys = resolvedKeys;
        }

        public string Selector { get; }

        public int Length => RequireResolvedKeys().Count;

        public QuerySelection Css(
            string property,
            string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(property);
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _state.SetCss(Selector, property, value);
            return this;
        }

        public QuerySelection Attr(string name, string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(value);
            foreach (var key in RequireResolvedKeys())
                _state.SetAttribute(key, name, value);
            return this;
        }

        public QuerySelection RemoveAttr(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            foreach (var key in RequireResolvedKeys())
                _state.RemoveAttribute(key, name);
            return this;
        }

        public QuerySelection Prop(string name, object value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(value);
            foreach (var key in RequireResolvedKeys())
                _state.SetProperty(key, name, value);
            return this;
        }

        public QuerySelection RemoveProp(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            foreach (var key in RequireResolvedKeys())
                _state.RemoveProperty(key, name);
            return this;
        }

        public QuerySelection Data<T>(string name, T value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            foreach (var key in RequireResolvedKeys())
                _state.SetData(key, name, value);
            return this;
        }

        public T? Data<T>(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            var key = RequireResolvedKeys().FirstOrDefault();
            return key is null ? default : _state.GetData<T>(key, name);
        }

        public QuerySelection RemoveData(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            foreach (var key in RequireResolvedKeys())
                _state.RemoveData(key, name);
            return this;
        }

        public QuerySelection Add(string selector)
        {
            selector = ValidateSelector(selector);
            if (_resolvedKeys is null)
                return Push($"{Selector}, {selector}");

            return PushResolved(OrderKeys(_resolvedKeys.Concat(ResolveKeys(selector))));
        }

        public QuerySelection AddBack(string? selector = null)
        {
            if (_previous is null)
                return this;

            if (_resolvedKeys is not null && _previous._resolvedKeys is not null)
            {
                var previousKeys = _previous._resolvedKeys;
                if (selector is not null)
                {
                    var matchingKeys = ResolveKeys(ValidateSelector(selector));
                    previousKeys = previousKeys.Where(matchingKeys.Contains).ToArray();
                }

                return PushResolved(OrderKeys(_resolvedKeys.Concat(previousKeys)));
            }

            var previousSelector = selector is null
                ? _previous.Selector
                : $":is({_previous.Selector}):is({ValidateSelector(selector)})";
            return Push($"{Selector}, {previousSelector}");
        }

        public QuerySelection End() => _previous ?? this;

        public QuerySelection Find(string selector)
        {
            selector = ValidateSelector(selector);
            if (_resolvedKeys is not null)
                return PushResolved(RequireRegistry().Descendants(_resolvedKeys, selector));

            return Push($":is({Selector}) {selector}");
        }

        public QuerySelection Children(string? selector = null)
        {
            var childSelector = selector is null ? "*" : ValidateSelector(selector);
            if (_resolvedKeys is not null)
                return PushResolved(RequireRegistry().Children(_resolvedKeys, selector));

            return Push($":is({Selector}) > {childSelector}");
        }

        public QuerySelection Filter(string selector)
        {
            selector = ValidateSelector(selector);
            if (_resolvedKeys is null)
                return Push($":is({Selector}):is({selector})");

            var keys = RequireResolvedKeys();
            var matchingKeys = ResolveKeys(selector);
            return PushResolved(keys.Where(matchingKeys.Contains).ToArray());
        }

        public QuerySelection Not(string selector)
        {
            selector = ValidateSelector(selector);
            if (_resolvedKeys is null)
                return Push($":is({Selector}):not({selector})");

            var keys = RequireResolvedKeys();
            var matchingKeys = ResolveKeys(selector);
            return PushResolved(keys.Where(key => !matchingKeys.Contains(key)).ToArray());
        }

        public bool Is(string selector)
        {
            selector = ValidateSelector(selector);
            var keys = RequireResolvedKeys();
            var matchingKeys = ResolveKeys(selector);
            return keys.Any(matchingKeys.Contains);
        }

        public QuerySelection Has(string selector)
        {
            selector = ValidateSelector(selector);
            if (_resolvedKeys is not null)
                return PushResolved(RequireRegistry().WithDescendant(_resolvedKeys, selector));

            return Push($":is({Selector}):has({selector})");
        }

        public QuerySelection Parent(string? selector = null)
        {
            var parentSelector = selector is null ? "*" : ValidateSelector(selector);
            if (_resolvedKeys is not null)
                return PushResolved(RequireRegistry().Parent(_resolvedKeys, selector));

            return Push($"{parentSelector}:has(> :is({Selector}))");
        }

        public QuerySelection Parents(string? selector = null)
        {
            var ancestorSelector = selector is null ? "*" : ValidateSelector(selector);
            if (_resolvedKeys is not null)
                return PushResolved(RequireRegistry().Parents(_resolvedKeys, selector));

            return Push($"{ancestorSelector}:has(:is({Selector}))");
        }

        public QuerySelection Closest(string selector) =>
            PushResolved(RequireRegistry().Closest(RequireResolvedKeys(), ValidateSelector(selector)));

        public QuerySelection ParentsUntil(string untilSelector, string? filterSelector = null) =>
            PushResolved(RequireRegistry().ParentsUntil(
                RequireResolvedKeys(),
                ValidateSelector(untilSelector),
                filterSelector is null ? null : ValidateSelector(filterSelector)));

        public QuerySelection Next(string? selector = null)
        {
            var siblingSelector = selector is null ? "*" : ValidateSelector(selector);
            if (_resolvedKeys is not null)
                return PushResolved(RequireRegistry().Next(_resolvedKeys, selector));

            return Push($":is({Selector}) + {siblingSelector}");
        }

        public QuerySelection NextAll(string? selector = null)
        {
            var siblingSelector = selector is null ? "*" : ValidateSelector(selector);
            if (_resolvedKeys is not null)
                return PushResolved(RequireRegistry().NextAll(_resolvedKeys, selector));

            return Push($":is({Selector}) ~ {siblingSelector}");
        }

        public QuerySelection NextUntil(string untilSelector, string? filterSelector = null) =>
            PushResolved(RequireRegistry().NextUntil(
                RequireResolvedKeys(),
                ValidateSelector(untilSelector),
                filterSelector is null ? null : ValidateSelector(filterSelector)));

        public QuerySelection Prev(string? selector = null)
        {
            var siblingSelector = selector is null ? "*" : ValidateSelector(selector);
            if (_resolvedKeys is not null)
                return PushResolved(RequireRegistry().Prev(_resolvedKeys, selector));

            return Push($"{siblingSelector}:has(+ :is({Selector}))");
        }

        public QuerySelection PrevAll(string? selector = null)
        {
            var siblingSelector = selector is null ? "*" : ValidateSelector(selector);
            if (_resolvedKeys is not null)
                return PushResolved(RequireRegistry().PrevAll(_resolvedKeys, selector));

            return Push($"{siblingSelector}:has(~ :is({Selector}))");
        }

        public QuerySelection PrevUntil(string untilSelector, string? filterSelector = null) =>
            PushResolved(RequireRegistry().PrevUntil(
                RequireResolvedKeys(),
                ValidateSelector(untilSelector),
                filterSelector is null ? null : ValidateSelector(filterSelector)));

        public QuerySelection Siblings(string? selector = null)
        {
            var siblingSelector = selector is null ? "*" : ValidateSelector(selector);
            if (_resolvedKeys is not null)
                return PushResolved(RequireRegistry().Siblings(_resolvedKeys, selector));

            return Push($"{siblingSelector}:has(~ :is({Selector})), :is({Selector}) ~ {siblingSelector}");
        }

        public QuerySelection First() => Eq(0);

        public QuerySelection Last() => Eq(-1);

        public QuerySelection Eq(int index)
        {
            var keys = RequireResolvedKeys();
            var resolvedIndex = index < 0 ? keys.Count + index : index;
            return resolvedIndex < 0 || resolvedIndex >= keys.Count
                ? PushResolved([])
                : PushResolved([keys[resolvedIndex]]);
        }

        public QuerySelection Slice(int start, int? end = null)
        {
            var keys = RequireResolvedKeys();
            var resolvedStart = ResolveSliceIndex(start, keys.Count);
            var resolvedEnd = ResolveSliceIndex(end ?? keys.Count, keys.Count);
            if (resolvedEnd < resolvedStart)
                resolvedEnd = resolvedStart;
            return PushResolved(keys.Skip(resolvedStart).Take(resolvedEnd - resolvedStart).ToArray());
        }

        public string[] ToArray() => RequireResolvedKeys().ToArray();

        public string[] Get() => ToArray();

        public string? Get(int index)
        {
            var keys = RequireResolvedKeys();
            var resolvedIndex = index < 0 ? keys.Count + index : index;
            return resolvedIndex < 0 || resolvedIndex >= keys.Count ? null : keys[resolvedIndex];
        }

        public int Index(string elementKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
            var keys = RequireResolvedKeys();
            for (var index = 0; index < keys.Count; index++)
            {
                if (string.Equals(keys[index], elementKey, StringComparison.Ordinal))
                    return index;
            }
            return -1;
        }

        public QuerySelection Each(Action<int, string> callback)
        {
            ArgumentNullException.ThrowIfNull(callback);
            var keys = RequireResolvedKeys();
            for (var index = 0; index < keys.Count; index++)
                callback(index, keys[index]);
            return this;
        }

        public QuerySelection Each(Func<int, string, bool> callback)
        {
            ArgumentNullException.ThrowIfNull(callback);
            var keys = RequireResolvedKeys();
            for (var index = 0; index < keys.Count; index++)
            {
                if (!callback(index, keys[index]))
                    break;
            }
            return this;
        }

        public IReadOnlyList<TResult> Map<TResult>(Func<int, string, TResult> callback)
        {
            ArgumentNullException.ThrowIfNull(callback);
            var keys = RequireResolvedKeys();
            var results = new TResult[keys.Count];
            for (var index = 0; index < keys.Count; index++)
                results[index] = callback(index, keys[index]);
            return results;
        }

        public QuerySelection PushStack(IEnumerable<string> elementKeys)
        {
            ArgumentNullException.ThrowIfNull(elementKeys);
            var keys = elementKeys.ToArray();
            foreach (var key in keys)
                ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return PushResolved(keys);
        }

        private QuerySelection Push(string selector) => new(selector, _state, _registry, this);

        private QuerySelection PushResolved(IReadOnlyList<string> keys) =>
            new(ToKeySelector(keys), _state, _registry, this, keys);

        private HashSet<string> ResolveKeys(string selector) => _registry is null
            ? throw new InvalidOperationException(
                "This operation requires library-owned element metadata. Register generated element keys before evaluating selection predicates.")
            : _registry.Resolve(selector).ToHashSet(StringComparer.Ordinal);

        private QueryElementRegistry RequireRegistry() => _registry ??
            throw new InvalidOperationException(
                "This operation requires library-owned element metadata. Register generated element keys before traversing the element hierarchy.");

        private IReadOnlyList<string> OrderKeys(IEnumerable<string> keys) => _registry is null
            ? keys.Distinct(StringComparer.Ordinal).ToArray()
            : _registry.Order(keys);

        private IReadOnlyList<string> RequireResolvedKeys() => _resolvedKeys ??
            throw new InvalidOperationException(
                "This operation requires library-owned element metadata. Register generated element keys before using collection-position operations.");

        private static int ResolveSliceIndex(int index, int count) =>
            Math.Clamp(index < 0 ? count + index : index, 0, count);

        private static string ToKeySelector(IReadOnlyList<string> keys) => keys.Count == 0
            ? ":not(*)"
            : string.Join(", ", keys.Select(key => $"[data-rbq-key=\"{EscapeKey(key)}\"]"));

        private static string EscapeKey(string key) => key.Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static string ValidateSelector(string selector)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(selector);
            return selector;
        }
    }
}
