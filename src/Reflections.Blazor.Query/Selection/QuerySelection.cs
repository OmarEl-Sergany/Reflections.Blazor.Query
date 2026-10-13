using System;

using System.Globalization;
using System.Text;

namespace Reflections.Blazor.Query
{
    public sealed class QuerySelection
    {
        private readonly QueryRenderState _state;
        private readonly QueryElementRegistry? _registry;
        private readonly QueryMeasurementCoordinator? _measurements;
        private readonly QueryFormCoordinator? _forms;
        private readonly QuerySelection? _previous;
        private readonly IReadOnlyList<string>? _resolvedKeys;
        private readonly string? _scopeKey;

        internal QuerySelection(
            string selector,
            QueryRenderState state,
            QueryElementRegistry? registry = null,
            QueryMeasurementCoordinator? measurements = null,
            QueryFormCoordinator? forms = null,
            string? scopeKey = null,
            QuerySelection? previous = null,
            IReadOnlyList<string>? resolvedKeys = null)
        {
            Selector = selector;
            _state = state;
            _registry = registry;
            _measurements = measurements;
            _forms = forms;
            _scopeKey = scopeKey;
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

        public QuerySelection Text(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            foreach (var key in RequireResolvedKeys())
                _state.SetText(key, value);
            return this;
        }

        public string? Text()
        {
            var key = RequireResolvedKeys().FirstOrDefault();
            return key is null ? null : _state.ContentFor(key)?.Value;
        }

        public QuerySelection Html(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            foreach (var key in RequireResolvedKeys())
                _state.SetHtml(key, value);
            return this;
        }

        public string? Html()
        {
            var key = RequireResolvedKeys().FirstOrDefault();
            var content = key is null ? null : _state.ContentFor(key);
            return content is { IsHtml: true } ? content.Value : null;
        }

        public QuerySelection Val(string value) => Prop("value", value);

        public string? Val()
        {
            var key = RequireResolvedKeys().FirstOrDefault();
            return key is not null && _state.PropertiesFor(key).TryGetValue("value", out var value)
                ? value as string
                : null;
        }

        public ValueTask<double?> HeightAsync(CancellationToken cancellationToken = default) =>
            MeasureFirstAsync(MeasurementKind.Height, cancellationToken);

        public ValueTask<double?> WidthAsync(CancellationToken cancellationToken = default) =>
            MeasureFirstAsync(MeasurementKind.Width, cancellationToken);

        public ValueTask<double?> InnerHeightAsync(CancellationToken cancellationToken = default) =>
            MeasureFirstAsync(MeasurementKind.InnerHeight, cancellationToken);

        public ValueTask<double?> InnerWidthAsync(CancellationToken cancellationToken = default) =>
            MeasureFirstAsync(MeasurementKind.InnerWidth, cancellationToken);

        public ValueTask<double?> OuterHeightAsync(CancellationToken cancellationToken = default) =>
            MeasureFirstAsync(MeasurementKind.OuterHeight, cancellationToken);

        public ValueTask<double?> OuterWidthAsync(CancellationToken cancellationToken = default) =>
            MeasureFirstAsync(MeasurementKind.OuterWidth, cancellationToken);

        public ValueTask<QueryPoint?> OffsetAsync(CancellationToken cancellationToken = default) =>
            MeasurePointFirstAsync(MeasurementKind.Offset, cancellationToken);

        public ValueTask<QueryPoint?> PositionAsync(CancellationToken cancellationToken = default) =>
            MeasurePointFirstAsync(MeasurementKind.Position, cancellationToken);

        public ValueTask<string?> OffsetParentAsync(CancellationToken cancellationToken = default) =>
            MeasureRelatedKeyFirstAsync(MeasurementKind.OffsetParent, cancellationToken);

        public ValueTask<IReadOnlyList<SerializedField>> SerializeArrayAsync(CancellationToken cancellationToken = default) =>
            SerializeFieldsAsync(cancellationToken);

        public async ValueTask<string> SerializeAsync(CancellationToken cancellationToken = default)
        {
            var fields = await SerializeFieldsAsync(cancellationToken);
            var builder = new StringBuilder();
            foreach (var field in fields)
            {
                if (builder.Length > 0)
                    builder.Append('&');
                builder.Append(EscapeDataString(field.Name));
                builder.Append('=');
                builder.Append(EscapeDataString(field.Value));
            }
            return builder.ToString();
        }

        public QuerySelection Height(double pixels) => SetOwnedLength("height", pixels);

        public QuerySelection Width(double pixels) => SetOwnedLength("width", pixels);

        public QuerySelection Append(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            foreach (var key in RequireResolvedKeys())
                _state.AppendContent(key, text, isHtml: false);
            return this;
        }

        public QuerySelection AppendHtml(string html)
        {
            ArgumentNullException.ThrowIfNull(html);
            foreach (var key in RequireResolvedKeys())
                _state.AppendContent(key, html, isHtml: true);
            return this;
        }

        public QuerySelection Prepend(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            foreach (var key in RequireResolvedKeys())
                _state.PrependContent(key, text, isHtml: false);
            return this;
        }

        public QuerySelection PrependHtml(string html)
        {
            ArgumentNullException.ThrowIfNull(html);
            foreach (var key in RequireResolvedKeys())
                _state.PrependContent(key, html, isHtml: true);
            return this;
        }

        public QuerySelection AddClass(string className)
        {
            foreach (var name in SplitClassNames(className))
                foreach (var key in RequireResolvedKeys())
                    _state.AddClass(key, name);
            return this;
        }

        public QuerySelection RemoveClass(string className)
        {
            foreach (var name in SplitClassNames(className))
                foreach (var key in RequireResolvedKeys())
                    _state.RemoveClass(key, name);
            return this;
        }

        public QuerySelection ToggleClass(string className, bool? force = null)
        {
            foreach (var name in SplitClassNames(className))
            {
                foreach (var key in RequireResolvedKeys())
                {
                    var shouldAdd = force ?? !HasClassOn(key, name);
                    if (shouldAdd)
                        _state.AddClass(key, name);
                    else
                        _state.RemoveClass(key, name);
                }
            }
            return this;
        }

        public bool HasClass(string className)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(className);
            return RequireResolvedKeys().Any(key => HasClassOn(key, className));
        }

        public QuerySelection Show()
        {
            foreach (var key in RequireResolvedKeys())
                _state.Show(key);
            return this;
        }

        public QuerySelection Hide()
        {
            foreach (var key in RequireResolvedKeys())
                _state.Hide(key);
            return this;
        }

        public QuerySelection Toggle(bool? show = null)
        {
            foreach (var key in RequireResolvedKeys())
            {
                var shouldShow = show ?? _state.IsHidden(key);
                if (shouldShow)
                    _state.Show(key);
                else
                    _state.Hide(key);
            }
            return this;
        }

        public QuerySelection Remove()
        {
            foreach (var key in RequireResolvedKeys())
            {
                if (_state.IsVirtual(key))
                {
                    _state.RemoveVirtualNode(key);
                    _registry?.Unregister(key);
                }
                else
                {
                    _state.Remove(key);
                }
            }
            return this;
        }

        public QuerySelection Detach()
        {
            foreach (var key in RequireResolvedKeys())
            {
                if (_state.IsVirtual(key))
                {
                    _state.DetachVirtualChild(key);
                    _registry?.SetParent(key, null);
                }
                else if (_state.IsRealElement(key) && _state.IsRelocatedElsewhere(key))
                {
                    // Springs back to its own declared Razor position: clearing the relocation makes
                    // QueryElement.BuildRenderTree render it in place again, and its own next
                    // OnParametersSet resyncs the registry parent to ParentElementKey.
                    _state.DetachRealChild(key);
                }
                else
                {
                    // A real element that was never relocated has nowhere else to go; this only
                    // preserves its state while hiding it from render, matching Remove().
                    _state.Remove(key);
                }
            }
            return this;
        }

        public QuerySelection Before(QuerySelection element)
        {
            ArgumentNullException.ThrowIfNull(element);
            var movingKey = element.RequireResolvedKeys().FirstOrDefault()
                ?? throw new InvalidOperationException("Before requires the moving selection to resolve to at least one element.");
            AttachSibling(movingKey, RequireSingleAnchor("Before"), before: true);
            return this;
        }

        public QuerySelection After(QuerySelection element)
        {
            ArgumentNullException.ThrowIfNull(element);
            var movingKey = element.RequireResolvedKeys().FirstOrDefault()
                ?? throw new InvalidOperationException("After requires the moving selection to resolve to at least one element.");
            AttachSibling(movingKey, RequireSingleAnchor("After"), before: false);
            return this;
        }

        /// <summary>
        /// Moves this selection immediately before the single target anchor.
        /// This is the fluent, jQuery-compatible receiver-order alias for <c>target.Before(this)</c>.
        /// </summary>
        public QuerySelection InsertBefore(QuerySelection target)
        {
            ArgumentNullException.ThrowIfNull(target);
            target.Before(this);
            return this;
        }

        /// <summary>
        /// Moves this selection immediately after the single target anchor.
        /// This is the fluent, jQuery-compatible receiver-order alias for <c>target.After(this)</c>.
        /// </summary>
        public QuerySelection InsertAfter(QuerySelection target)
        {
            ArgumentNullException.ThrowIfNull(target);
            target.After(this);
            return this;
        }

        /// <summary>
        /// Replaces this single target anchor with a single existing library-owned element.
        /// The replacement is relocated through Blazor-owned render state before the target is removed.
        /// </summary>
        public QuerySelection ReplaceWith(QuerySelection replacement)
        {
            ArgumentNullException.ThrowIfNull(replacement);
            var targetKey = RequireSingleAnchor("ReplaceWith");
            var replacementKey = replacement.RequireSingleElement("ReplaceWith");
            if (string.Equals(targetKey, replacementKey, StringComparison.Ordinal))
                throw new InvalidOperationException("ReplaceWith cannot replace an element with itself.");

            AttachSibling(replacementKey, targetKey, before: true);
            RemoveSingle(targetKey);
            return this;
        }

        /// <summary>
        /// Replaces a single target anchor with this single receiver element.
        /// This is the receiver-order counterpart of <c>target.ReplaceWith(this)</c>.
        /// </summary>
        public QuerySelection ReplaceAll(QuerySelection target)
        {
            ArgumentNullException.ThrowIfNull(target);
            target.ReplaceWith(this);
            return this;
        }

        /// <summary>
        /// Compatibility alias for <see cref="ReplaceWith(QuerySelection)"/>.
        /// </summary>
        public QuerySelection Replace(QuerySelection replacement) => ReplaceWith(replacement);

        public QuerySelection Wrap(string wrapperTagName, string? wrapperClassName = null)
        {
            var results = new List<string>();
            foreach (var key in RequireResolvedKeys())
                results.Add(WrapSingle(key, wrapperTagName, wrapperClassName));
            return PushResolved(results);
        }

        /// <summary>
        /// Creates one library-owned wrapper around all selected siblings in their current render order.
        /// Every selected element must share a registered parent; cross-parent wrapping would require
        /// implicit ordering and cloning semantics that this render-state model deliberately does not infer.
        /// </summary>
        public QuerySelection WrapAll(string wrapperTagName, string? wrapperClassName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(wrapperTagName);
            var keys = RequireResolvedKeys();
            if (keys.Count == 0)
                return PushResolved([]);

            EnsureCommonParent(keys, "WrapAll");
            var wrapperKey = CreateSibling(keys[0], wrapperTagName, wrapperClassName, text: null, isHtml: false, before: true);
            foreach (var key in keys)
                AttachTo(key, wrapperKey, atStart: false);

            return PushResolved([wrapperKey]);
        }

        /// <summary>
        /// Moves a mounted <see cref="QueryElement"/>'s component-owned inner render fragment into a
        /// library-owned wrapper. This does not inspect or mutate browser DOM nodes.
        /// </summary>
        public QuerySelection WrapInner(string wrapperTagName, string? wrapperClassName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(wrapperTagName);
            var results = new List<string>();
            foreach (var key in RequireResolvedKeys())
            {
                if (!_state.IsRealElement(key))
                    throw new InvalidOperationException(
                        "WrapInner requires a mounted QueryElement with component-owned ChildContent; virtual elements are not supported.");

                var existingChildren = _state.VirtualChildKeysFor(key).ToArray();
                var wrapperKey = CreateChild(key, wrapperTagName, wrapperClassName, text: null, isHtml: false, atStart: true);
                foreach (var childKey in existingChildren)
                    AttachTo(childKey, wrapperKey, atStart: false);
                _state.SetInnerWrapper(key, wrapperKey);
                results.Add(wrapperKey);
            }
            return PushResolved(results);
        }

        public QuerySelection Clone()
        {
            var results = new List<string>();
            foreach (var key in RequireResolvedKeys())
                results.Add(CloneVirtual(key));
            return PushResolved(results);
        }

        public QuerySelection AppendTo(QuerySelection target)
        {
            ArgumentNullException.ThrowIfNull(target);
            var targetKey = target.RequireResolvedKeys().FirstOrDefault()
                ?? throw new InvalidOperationException("AppendTo requires the target selection to resolve to at least one element.");
            foreach (var key in RequireResolvedKeys())
                AttachTo(key, targetKey, atStart: false);
            return this;
        }

        public QuerySelection PrependTo(QuerySelection target)
        {
            ArgumentNullException.ThrowIfNull(target);
            var targetKey = target.RequireResolvedKeys().FirstOrDefault()
                ?? throw new InvalidOperationException("PrependTo requires the target selection to resolve to at least one element.");
            foreach (var key in RequireResolvedKeys())
                AttachTo(key, targetKey, atStart: true);
            return this;
        }

        public QuerySelection AppendElement(string tagName, string? className = null, string? text = null, bool isHtml = false)
        {
            var results = new List<string>();
            foreach (var parentKey in RequireResolvedKeys())
                results.Add(CreateChild(parentKey, tagName, className, text, isHtml, atStart: false));
            return PushResolved(results);
        }

        public QuerySelection PrependElement(string tagName, string? className = null, string? text = null, bool isHtml = false)
        {
            var results = new List<string>();
            foreach (var parentKey in RequireResolvedKeys())
                results.Add(CreateChild(parentKey, tagName, className, text, isHtml, atStart: true));
            return PushResolved(results);
        }

        public QuerySelection InsertElementBefore(string tagName, string? className = null, string? text = null, bool isHtml = false)
        {
            var results = new List<string>();
            foreach (var anchorKey in RequireResolvedKeys())
                results.Add(CreateSibling(anchorKey, tagName, className, text, isHtml, before: true));
            return PushResolved(results);
        }

        public QuerySelection InsertElementAfter(string tagName, string? className = null, string? text = null, bool isHtml = false)
        {
            var results = new List<string>();
            foreach (var anchorKey in RequireResolvedKeys())
                results.Add(CreateSibling(anchorKey, tagName, className, text, isHtml, before: false));
            return PushResolved(results);
        }

        public QuerySelection Empty()
        {
            foreach (var key in RequireResolvedKeys())
            {
                _state.Empty(key);
                foreach (var descendantKey in FilterToScope(RequireRegistry().AllDescendants([key])))
                    _state.Remove(descendantKey);
            }
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

        private QuerySelection Push(string selector) => new(selector, _state, _registry, _measurements, _forms, _scopeKey, this);

        private QuerySelection PushResolved(IReadOnlyList<string> keys) =>
            new(ToKeySelector(keys), _state, _registry, _measurements, _forms, _scopeKey, this, FilterToScope(keys));

        private ValueTask<double?> MeasureFirstAsync(MeasurementKind kind, CancellationToken cancellationToken)
        {
            var key = RequireResolvedKeys().FirstOrDefault();
            return key is null
                ? ValueTask.FromResult<double?>(null)
                : (_measurements ?? throw new InvalidOperationException(
                    "Measurement APIs require QueryMeasurementCoordinator registration through AddBlazorQuery()."))
                    .RequestAsync(_state, key, kind, cancellationToken);
        }

        private ValueTask<QueryPoint?> MeasurePointFirstAsync(MeasurementKind kind, CancellationToken cancellationToken)
        {
            var key = RequireResolvedKeys().FirstOrDefault();
            return key is null
                ? ValueTask.FromResult<QueryPoint?>(null)
                : (_measurements ?? throw new InvalidOperationException(
                    "Measurement APIs require QueryMeasurementCoordinator registration through AddBlazorQuery()."))
                    .RequestPointAsync(_state, key, kind, cancellationToken);
        }

        private ValueTask<string?> MeasureRelatedKeyFirstAsync(MeasurementKind kind, CancellationToken cancellationToken)
        {
            var key = RequireResolvedKeys().FirstOrDefault();
            return key is null
                ? ValueTask.FromResult<string?>(null)
                : (_measurements ?? throw new InvalidOperationException(
                    "Measurement APIs require QueryMeasurementCoordinator registration through AddBlazorQuery()."))
                    .RequestRelatedKeyAsync(_state, key, kind, cancellationToken);
        }

        private ValueTask<IReadOnlyList<SerializedField>> SerializeFieldsAsync(CancellationToken cancellationToken)
        {
            var keys = RequireResolvedKeys();
            return keys.Count == 0
                ? ValueTask.FromResult<IReadOnlyList<SerializedField>>([])
                : (_forms ?? throw new InvalidOperationException(
                    "Serialization APIs require QueryFormCoordinator registration through AddBlazorQuery()."))
                    .RequestArrayAsync(_state, keys, cancellationToken);
        }

        private static string EscapeDataString(string value)
        {
            var escaped = System.Uri.EscapeDataString(value);
            return escaped.Replace("%20", "+");
        }

        private QuerySelection SetOwnedLength(string property, double pixels)
        {
            if (!double.IsFinite(pixels) || pixels < 0)
                throw new ArgumentOutOfRangeException(nameof(pixels), "CSS pixel lengths must be finite and non-negative.");

            var value = pixels.ToString("0.################", CultureInfo.InvariantCulture) + "px";
            foreach (var key in RequireResolvedKeys())
                _state.SetCss($"[data-rbq-key=\"{EscapeKey(key)}\"]", property, value);
            return this;
        }

        private string CreateChild(string parentKey, string tagName, string? className, string? text, bool isHtml, bool atStart)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
            var key = NewVirtualKey();
            _state.CreateVirtualChild(key, parentKey, tagName, className, text, isHtml, atStart);
            RequireRegistry().RegisterWithParent(key, parentKey, BuildVirtualSelectors(tagName, className));
            return key;
        }

        private string CreateSibling(string anchorKey, string tagName, string? className, string? text, bool isHtml, bool before)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
            var parentKey = RequireRegistry().Parent([anchorKey]).FirstOrDefault()
                ?? throw new InvalidOperationException(
                    "Before/After insertion requires the anchor element to have a registered parent element.");
            var key = NewVirtualKey();
            _state.CreateVirtualSibling(key, anchorKey, parentKey, tagName, className, text, isHtml, before);
            RequireRegistry().RegisterWithParent(key, parentKey, BuildVirtualSelectors(tagName, className));
            return key;
        }

        private static string[] BuildVirtualSelectors(string tagName, string? className)
        {
            var selectors = new HashSet<string>(StringComparer.Ordinal) { tagName };
            if (!string.IsNullOrWhiteSpace(className))
            {
                foreach (var name in className.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    selectors.Add($".{name}");
            }
            return [.. selectors];
        }

        private static string NewVirtualKey() => $"rbq-vnode-{Guid.NewGuid():N}";

        private void AttachTo(string key, string targetKey, bool atStart)
        {
            ThrowIfWouldCreateCycle(key, targetKey);
            if (_state.IsVirtual(key))
                _state.AttachVirtualChild(key, targetKey, atStart);
            else if (_state.IsRealElement(key))
                _state.AttachRealChild(key, targetKey, atStart);
            else
                throw new InvalidOperationException($"'{key}' is not a mounted element known to this render state.");

            RequireRegistry().SetParent(key, targetKey);
        }

        private string RequireSingleAnchor(string operationName)
        {
            var anchors = RequireResolvedKeys();
            return anchors.Count switch
            {
                1 => anchors[0],
                0 => throw new InvalidOperationException($"{operationName} requires the receiver selection to resolve to an anchor element."),
                _ => throw new InvalidOperationException(
                    $"{operationName} requires the receiver selection to resolve to exactly one anchor element; " +
                    $"it resolved to {anchors.Count}. Moving a single element {operationName.ToLowerInvariant()} multiple anchors is ambiguous."),
            };
        }

        private string RequireSingleElement(string operationName)
        {
            var elements = RequireResolvedKeys();
            return elements.Count switch
            {
                1 => elements[0],
                0 => throw new InvalidOperationException($"{operationName} requires the replacement selection to resolve to an element."),
                _ => throw new InvalidOperationException(
                    $"{operationName} requires the replacement selection to resolve to exactly one element; " +
                    $"it resolved to {elements.Count}. Cloning replacements for multiple elements is not supported."),
            };
        }

        private void EnsureCommonParent(IReadOnlyList<string> keys, string operationName)
        {
            var registry = RequireRegistry();
            var parentKey = registry.Parent([keys[0]]).FirstOrDefault()
                ?? throw new InvalidOperationException(
                    $"{operationName} requires selected elements to have a registered parent element.");

            foreach (var key in keys.Skip(1))
            {
                var candidateParent = registry.Parent([key]).FirstOrDefault();
                if (!string.Equals(parentKey, candidateParent, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"{operationName} requires every selected element to share the same registered parent.");
            }
        }

        private void RemoveSingle(string elementKey)
        {
            if (_state.IsVirtual(elementKey))
            {
                _state.RemoveVirtualNode(elementKey);
                _registry?.Unregister(elementKey);
            }
            else
            {
                _state.Remove(elementKey);
            }
        }

        private void AttachSibling(string movingKey, string anchorKey, bool before)
        {
            var parentKey = RequireRegistry().Parent([anchorKey]).FirstOrDefault()
                ?? throw new InvalidOperationException("Before/After requires the anchor element to have a registered parent element.");
            ThrowIfWouldCreateCycle(movingKey, parentKey);

            if (_state.IsVirtual(movingKey))
                _state.AttachVirtualSibling(movingKey, anchorKey, parentKey, before);
            else if (_state.IsRealElement(movingKey))
                _state.AttachRealSibling(movingKey, anchorKey, parentKey, before);
            else
                throw new InvalidOperationException($"'{movingKey}' is not a mounted element known to this render state.");

            RequireRegistry().SetParent(movingKey, parentKey);
        }

        private string WrapSingle(string key, string wrapperTagName, string? wrapperClassName)
        {
            // CreateSibling resolves key's registered parent internally and throws if it has none.
            var wrapperKey = CreateSibling(key, wrapperTagName, wrapperClassName, text: null, isHtml: false, before: true);

            if (_state.IsVirtual(key))
                _state.AttachVirtualChild(key, wrapperKey, atStart: true);
            else if (_state.IsRealElement(key))
                _state.AttachRealChild(key, wrapperKey, atStart: true);
            else
                throw new InvalidOperationException($"'{key}' is not a mounted element known to this render state.");
            RequireRegistry().SetParent(key, wrapperKey);

            return wrapperKey;
        }

        private void ThrowIfWouldCreateCycle(string movingKey, string proposedParentKey)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var current = proposedParentKey;
            while (current is not null && visited.Add(current))
            {
                if (string.Equals(current, movingKey, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"Cannot move '{movingKey}' into '{proposedParentKey}' — '{proposedParentKey}' is a descendant of '{movingKey}', and attaching it there would create a cycle.");
                current = RequireRegistry().Parent([current]).FirstOrDefault();
            }
        }

        private string CloneVirtual(string sourceKey)
        {
            if (!_state.IsVirtual(sourceKey))
                throw new InvalidOperationException(
                    "Clone() is only supported for library-created elements (created via AppendElement, PrependElement, InsertElementBefore, or InsertElementAfter). " +
                    "Cloning a Razor-declared QueryElement would require two elements to share a data-rbq-key, which is not supported.");

            var node = _state.VirtualNodeFor(sourceKey)!;
            var newKey = NewVirtualKey();
            _state.CreateDetachedVirtualNode(newKey, node.TagName, node.ClassName, node.Text, node.IsHtml);

            foreach (var attribute in _state.AttributesFor(sourceKey))
                _state.SetAttribute(newKey, attribute.Key, attribute.Value);
            foreach (var property in _state.PropertiesFor(sourceKey))
                _state.SetProperty(newKey, property.Key, property.Value);
            foreach (var className in _state.AddedClassesFor(sourceKey))
                _state.AddClass(newKey, className);
            var content = _state.ContentFor(sourceKey);
            if (content is { IsHtml: true })
                _state.SetHtml(newKey, content.Value);
            else if (content is not null)
                _state.SetText(newKey, content.Value);
            foreach (var fragment in _state.PrependedFor(sourceKey).Reverse())
                _state.PrependContent(newKey, fragment.Value, fragment.IsHtml);
            foreach (var fragment in _state.AppendedFor(sourceKey))
                _state.AppendContent(newKey, fragment.Value, fragment.IsHtml);
            if (_state.IsHidden(sourceKey))
                _state.Hide(newKey);

            RequireRegistry().Register(newKey, BuildVirtualSelectors(node.TagName, node.ClassName));
            return newKey;
        }

        private bool HasClassOn(string key, string className)
        {
            if (_state.RemovedClassesFor(key).Contains(className))
                return false;
            if (_state.AddedClassesFor(key).Contains(className))
                return true;
            return _registry?.BaseClassesFor(key).Contains(className) ?? false;
        }

        private static string[] SplitClassNames(string className)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(className);
            var names = className.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (names.Length == 0)
                throw new ArgumentException("At least one class name is required.", nameof(className));
            return names;
        }

        private HashSet<string> ResolveKeys(string selector) => _registry is null
            ? throw new InvalidOperationException(
                "This operation requires library-owned element metadata. Register generated element keys before evaluating selection predicates.")
            : FilterToScope(_registry.Resolve(selector)).ToHashSet(StringComparer.Ordinal);

        private QueryElementRegistry RequireRegistry() => _registry ??
            throw new InvalidOperationException(
                "This operation requires library-owned element metadata. Register generated element keys before traversing the element hierarchy.");

        private IReadOnlyList<string> OrderKeys(IEnumerable<string> keys) => _registry is null
            ? keys.Distinct(StringComparer.Ordinal).ToArray()
            : FilterToScope(_registry.Order(keys));

        private IReadOnlyList<string> FilterToScope(IEnumerable<string> keys) => _registry is null || _scopeKey is null
            ? keys.Distinct(StringComparer.Ordinal).ToArray()
            : _registry.FilterScope(keys, _scopeKey);

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
