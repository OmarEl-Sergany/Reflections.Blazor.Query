using Microsoft.AspNetCore.Components;

namespace Reflections.Blazor.Query;
public class QueryRenderState
{
    private readonly Dictionary<(string, string), string> _styles = new();
    private readonly Dictionary<(string Key, string Name), string> _attributes = new();
    private readonly Dictionary<(string Key, string Name), object> _properties = new();
    private readonly Dictionary<(string Key, string Name), System.Text.Json.JsonElement> _data = new();
    private readonly Dictionary<string, QueryContent> _content = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _addedClasses = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _removedClasses = new(StringComparer.Ordinal);
    private readonly HashSet<string> _hiddenKeys = new(StringComparer.Ordinal);
    private readonly HashSet<string> _removedKeys = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<QueryContent>> _prepended = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<QueryContent>> _appended = new(StringComparer.Ordinal);
    private readonly Dictionary<string, QueryVirtualNode> _virtualNodes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> _virtualChildKeys = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RenderFragment> _realElementFragments = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RenderFragment> _innerContentByOwner = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _innerWrapperByOwner = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _contentWrapperByOwner = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RenderFragment> _innerContentByWrapper = new(StringComparer.Ordinal);
    private readonly Dictionary<(string Key, string EventName), List<QueryEventHandler>> _eventHandlers = new();
    private readonly Dictionary<(string Key, string EventName), List<QueryTypedEventHandler>> _typedEventHandlers = new();
    private readonly Dictionary<(string RootKey, string EventName), List<QueryDelegatedEventHandler>> _delegatedEventHandlers = new();
    private readonly Dictionary<string, string> _relocatedParent = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _relocatedChildrenByHost = new(StringComparer.Ordinal);
    private readonly List<Func<Action, Task>> _dispatchers = [];
    private int _batchDepth;
    private bool _dirty;
    public event Action? Changed;
    internal IReadOnlyList<QueryStyleRule> Rules => _styles.Select(x => new QueryStyleRule(x.Key.Item1, x.Key.Item2, x.Value)).ToArray();
    internal IReadOnlyDictionary<string, string> AttributesFor(string elementKey) => _attributes
        .Where(attribute => string.Equals(attribute.Key.Key, elementKey, StringComparison.Ordinal))
        .OrderBy(attribute => attribute.Key.Name, StringComparer.Ordinal)
        .ToDictionary(attribute => attribute.Key.Name, attribute => attribute.Value, StringComparer.Ordinal);
    internal IReadOnlyDictionary<string, object> PropertiesFor(string elementKey) => _properties
        .Where(property => string.Equals(property.Key.Key, elementKey, StringComparison.Ordinal))
        .OrderBy(property => property.Key.Name, StringComparer.Ordinal)
        .ToDictionary(property => property.Key.Name, property => property.Value, StringComparer.Ordinal);
    internal IReadOnlyDictionary<string, System.Text.Json.JsonElement> DataFor(string elementKey) => _data
        .Where(data => string.Equals(data.Key.Key, elementKey, StringComparison.Ordinal))
        .OrderBy(data => data.Key.Name, StringComparer.Ordinal)
        .ToDictionary(data => data.Key.Name, data => data.Value, StringComparer.Ordinal);
    internal QueryContent? ContentFor(string elementKey) => _content.TryGetValue(elementKey, out var content) ? content : null;
    internal IReadOnlySet<string> AddedClassesFor(string elementKey) =>
        _addedClasses.TryGetValue(elementKey, out var classes) ? classes : (IReadOnlySet<string>)new HashSet<string>();
    internal IReadOnlySet<string> RemovedClassesFor(string elementKey) =>
        _removedClasses.TryGetValue(elementKey, out var classes) ? classes : (IReadOnlySet<string>)new HashSet<string>();
    internal bool IsHidden(string elementKey) => _hiddenKeys.Contains(elementKey);
    internal bool IsRemoved(string elementKey) => _removedKeys.Contains(elementKey);
    internal IReadOnlyList<QueryContent> PrependedFor(string elementKey) =>
        _prepended.TryGetValue(elementKey, out var fragments) ? fragments : [];
    internal IReadOnlyList<QueryContent> AppendedFor(string elementKey) =>
        _appended.TryGetValue(elementKey, out var fragments) ? fragments : [];
    internal bool IsVirtual(string elementKey) => _virtualNodes.ContainsKey(elementKey);
    internal QueryVirtualNode? VirtualNodeFor(string elementKey) =>
        _virtualNodes.TryGetValue(elementKey, out var node) ? node : null;
    internal IReadOnlyList<string> VirtualChildKeysFor(string parentKey) =>
        _virtualChildKeys.TryGetValue(parentKey, out var keys) ? keys : [];
    internal IReadOnlyList<string> AllVirtualKeys => _virtualNodes.Keys.ToArray();
    internal bool IsRealElement(string elementKey) => _realElementFragments.ContainsKey(elementKey);
    internal bool IsRelocatedElsewhere(string elementKey) => _relocatedParent.ContainsKey(elementKey);
    internal RenderFragment? RealElementFragmentFor(string elementKey) =>
        _realElementFragments.TryGetValue(elementKey, out var fragment) ? fragment : null;
    internal string? InnerWrapperFor(string elementKey) =>
        _innerWrapperByOwner.TryGetValue(elementKey, out var wrapperKey) ? wrapperKey : null;
    internal RenderFragment? InnerContentForWrapper(string wrapperKey) =>
        _innerContentByWrapper.TryGetValue(wrapperKey, out var fragment) ? fragment : null;
    internal IReadOnlyList<string> EventNamesFor(string elementKey) => _eventHandlers.Keys
        .Where(key => string.Equals(key.Key, elementKey, StringComparison.Ordinal))
        .Select(key => key.EventName)
        .Concat(_typedEventHandlers.Keys.Where(key => string.Equals(key.Key, elementKey, StringComparison.Ordinal)).Select(key => key.EventName))
        .Distinct(StringComparer.Ordinal).ToArray();
    internal IReadOnlyList<string> DelegatedEventNamesFor(string rootKey) => _delegatedEventHandlers.Keys
        .Where(key => string.Equals(key.RootKey, rootKey, StringComparison.Ordinal))
        .Select(key => key.EventName)
        .Distinct(StringComparer.Ordinal)
        .ToArray();
    internal void On(string elementKey, string eventName, Action handler, bool once)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ArgumentNullException.ThrowIfNull(handler);
        eventName = NormalizeEventName(eventName);
        (_eventHandlers.TryGetValue((elementKey, eventName), out var handlers) ? handlers : _eventHandlers[(elementKey, eventName)] = []).Add(new(handler, once));
        NotifyChanged();
    }
    internal void Off(string elementKey, string eventName, Action? handler = null)
    {
        eventName = NormalizeEventName(eventName);
        if (!_eventHandlers.TryGetValue((elementKey, eventName), out var handlers)) return;
        if (handler is null) _eventHandlers.Remove((elementKey, eventName));
        else { handlers.RemoveAll(entry => entry.Handler == handler); if (handlers.Count == 0) _eventHandlers.Remove((elementKey, eventName)); }
        NotifyChanged();
    }
    internal void On<TEventArgs>(string elementKey, string eventName, Action<TEventArgs> handler, bool once) where TEventArgs : EventArgs
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ArgumentNullException.ThrowIfNull(handler);
        eventName = NormalizeEventName(eventName);
        QueryEventDescriptors.Validate<TEventArgs>(eventName);
        (_typedEventHandlers.TryGetValue((elementKey, eventName), out var handlers) ? handlers : _typedEventHandlers[(elementKey, eventName)] = [])
            .Add(new(args => handler((TEventArgs)args), handler, typeof(TEventArgs), once));
        NotifyChanged();
    }
    internal void Off<TEventArgs>(string elementKey, string eventName, Action<TEventArgs>? handler = null) where TEventArgs : EventArgs
    {
        eventName = NormalizeEventName(eventName);
        if (!_typedEventHandlers.TryGetValue((elementKey, eventName), out var handlers))
            return;
        handlers.RemoveAll(entry => entry.ArgumentType == typeof(TEventArgs)
            && (handler is null || entry.OriginalHandler.Equals(handler)));
        if (handlers.Count == 0)
            _typedEventHandlers.Remove((elementKey, eventName));
        NotifyChanged();
    }
    internal void Trigger(string elementKey, string eventName, EventArgs? eventArgs = null)
    {
        eventName = NormalizeEventName(eventName);
        if (_eventHandlers.TryGetValue((elementKey, eventName), out var handlers))
        {
            foreach (var entry in handlers.ToArray())
            {
                entry.Handler();
                if (entry.Once) handlers.Remove(entry);
            }
            if (handlers.Count == 0) _eventHandlers.Remove((elementKey, eventName));
        }
        if (eventArgs is not null && _typedEventHandlers.TryGetValue((elementKey, eventName), out var typedHandlers))
        {
            foreach (var entry in typedHandlers.ToArray())
            {
                if (entry.ArgumentType.IsInstanceOfType(eventArgs))
                {
                    entry.Handler(eventArgs);
                    if (entry.Once) typedHandlers.Remove(entry);
                }
            }
            if (typedHandlers.Count == 0) _typedEventHandlers.Remove((elementKey, eventName));
        }
        NotifyChanged();
    }
    internal void Delegate(string rootKey, string selector, string eventName, Action<QueryEventTarget> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);
        ArgumentNullException.ThrowIfNull(handler);
        eventName = NormalizeDelegatedEventName(eventName);
        (_delegatedEventHandlers.TryGetValue((rootKey, eventName), out var handlers)
            ? handlers
            : _delegatedEventHandlers[(rootKey, eventName)] = [])
            .Add(new(selector, handler));
        NotifyChanged();
    }
    internal void Undelegate(string rootKey, string selector, string eventName, Action<QueryEventTarget>? handler = null)
    {
        eventName = NormalizeDelegatedEventName(eventName);
        if (!_delegatedEventHandlers.TryGetValue((rootKey, eventName), out var handlers))
            return;

        handlers.RemoveAll(entry => string.Equals(entry.Selector, selector, StringComparison.Ordinal)
            && (handler is null || entry.Handler == handler));
        if (handlers.Count == 0)
            _delegatedEventHandlers.Remove((rootKey, eventName));
        NotifyChanged();
    }
    internal void TriggerDelegated(string rootKey, string eventName, QueryEventTarget target, QueryElementRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(registry);
        eventName = NormalizeDelegatedEventName(eventName);
        if (target.ElementKey is not { Length: > 0 } targetKey
            || !_delegatedEventHandlers.TryGetValue((rootKey, eventName), out var handlers)
            || !registry.IsDescendantInSameScope(targetKey, rootKey))
            return;

        foreach (var entry in handlers.ToArray())
            if (registry.MatchesKey(targetKey, entry.Selector))
                entry.Handler(target);
        NotifyChanged();
    }
    public IDisposable BeginBatch() { _batchDepth++; return new Batch(this); }
    internal void RegisterDispatch(Func<Action, Task> dispatch)
    {
        if (!_dispatchers.Contains(dispatch))
            _dispatchers.Add(dispatch);
    }
    internal void UnregisterDispatch(Func<Action, Task> dispatch) => _dispatchers.Remove(dispatch);
    internal void RegisterRealElement(string key, RenderFragment fragment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(fragment);
        _realElementFragments[key] = fragment;
        // A host may already be waiting to render this key (attached before this element finished
        // mounting/registering). Notify so that host gets a follow-up render picking up the fragment;
        // otherwise skip notifying on ordinary mount to avoid a render on every element registration.
        if (_relocatedParent.ContainsKey(key))
            NotifyChanged();
    }
    internal void RegisterInnerContent(string key, RenderFragment fragment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(fragment);
        _innerContentByOwner[key] = fragment;
        if (_contentWrapperByOwner.TryGetValue(key, out var wrapperKey))
            _innerContentByWrapper[wrapperKey] = fragment;
    }
    internal void UnregisterInnerContent(string key)
    {
        _innerContentByOwner.Remove(key);
        _innerWrapperByOwner.Remove(key);
        if (_contentWrapperByOwner.Remove(key, out var wrapperKey))
            _innerContentByWrapper.Remove(wrapperKey);
    }
    internal void SetInnerWrapper(string ownerKey, string wrapperKey)
    {
        if (!_innerContentByOwner.TryGetValue(ownerKey, out var fragment))
            throw new InvalidOperationException($"'{ownerKey}' does not have component-owned inner content.");

        _innerWrapperByOwner[ownerKey] = wrapperKey;
        if (!_contentWrapperByOwner.ContainsKey(ownerKey))
        {
            _contentWrapperByOwner[ownerKey] = wrapperKey;
            _innerContentByWrapper[wrapperKey] = fragment;
        }
        NotifyChanged();
    }
    internal void UnregisterRealElement(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!_realElementFragments.Remove(key))
            return;
        DetachRealChildQuiet(key);
        HandleHostRemoval(key);
        NotifyChanged();
    }
    internal void AttachRealChild(string key, string parentKey, bool atStart)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parentKey);
        RequireRealElement(key);
        DetachRealChildQuiet(key);
        _relocatedParent[key] = parentKey;
        (_relocatedChildrenByHost.TryGetValue(parentKey, out var hostSet) ? hostSet : _relocatedChildrenByHost[parentKey] = new(StringComparer.Ordinal)).Add(key);
        var siblings = _virtualChildKeys.TryGetValue(parentKey, out var existing) ? existing : _virtualChildKeys[parentKey] = [];
        if (atStart)
            siblings.Insert(0, key);
        else
            siblings.Add(key);
        NotifyChanged();
    }
    internal void AttachRealSibling(string key, string anchorKey, string parentKey, bool before)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(anchorKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(parentKey);
        RequireRealElement(key);
        DetachRealChildQuiet(key);
        _relocatedParent[key] = parentKey;
        (_relocatedChildrenByHost.TryGetValue(parentKey, out var hostSet) ? hostSet : _relocatedChildrenByHost[parentKey] = new(StringComparer.Ordinal)).Add(key);
        var siblings = _virtualChildKeys.TryGetValue(parentKey, out var existing) ? existing : _virtualChildKeys[parentKey] = [];
        var anchorIndex = siblings.IndexOf(anchorKey);
        var insertIndex = anchorIndex < 0 ? siblings.Count : before ? anchorIndex : anchorIndex + 1;
        siblings.Insert(insertIndex, key);
        NotifyChanged();
    }
    internal void DetachRealChild(string key)
    {
        RequireRealElement(key);
        var wasRelocated = _relocatedParent.ContainsKey(key);
        DetachRealChildQuiet(key);
        if (wasRelocated)
            NotifyChanged();
    }
    internal void CreateVirtualChild(
        string key,
        string parentKey,
        string tagName,
        string? className,
        string? text,
        bool isHtml,
        bool atStart)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(parentKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
        if (_virtualNodes.ContainsKey(key))
            throw new InvalidOperationException($"The virtual element key '{key}' is already registered.");

        _virtualNodes[key] = new QueryVirtualNode(key, parentKey, tagName, className, text, isHtml);
        var siblings = _virtualChildKeys.TryGetValue(parentKey, out var existing) ? existing : _virtualChildKeys[parentKey] = [];
        if (atStart)
            siblings.Insert(0, key);
        else
            siblings.Add(key);
        NotifyChanged();
    }
    internal void CreateVirtualSibling(
        string key,
        string anchorKey,
        string parentKey,
        string tagName,
        string? className,
        string? text,
        bool isHtml,
        bool before)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(anchorKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(parentKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
        if (_virtualNodes.ContainsKey(key))
            throw new InvalidOperationException($"The virtual element key '{key}' is already registered.");

        _virtualNodes[key] = new QueryVirtualNode(key, parentKey, tagName, className, text, isHtml);
        var siblings = _virtualChildKeys.TryGetValue(parentKey, out var existing) ? existing : _virtualChildKeys[parentKey] = [];
        var anchorIndex = siblings.IndexOf(anchorKey);
        var insertIndex = anchorIndex < 0 ? siblings.Count : before ? anchorIndex : anchorIndex + 1;
        siblings.Insert(insertIndex, key);
        NotifyChanged();
    }
    internal void RemoveVirtualNode(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!_virtualNodes.Remove(key, out var node))
            return;
        HandleHostRemoval(key);
        if (node.ParentKey is not null && _virtualChildKeys.TryGetValue(node.ParentKey, out var siblings))
            siblings.Remove(key);
        NotifyChanged();
    }
    internal string CreateDetachedVirtualNode(string key, string tagName, string? className, string? text, bool isHtml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
        if (_virtualNodes.ContainsKey(key))
            throw new InvalidOperationException($"The virtual element key '{key}' is already registered.");

        _virtualNodes[key] = new QueryVirtualNode(key, null, tagName, className, text, isHtml);
        NotifyChanged();
        return key;
    }
    internal void DetachVirtualChild(string key)
    {
        var node = RequireVirtualNode(key);
        if (node.ParentKey is not null && _virtualChildKeys.TryGetValue(node.ParentKey, out var siblings))
            siblings.Remove(key);
        _virtualNodes[key] = node with { ParentKey = null };
        NotifyChanged();
    }
    internal void AttachVirtualChild(string key, string parentKey, bool atStart)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parentKey);
        RequireVirtualNode(key);
        DetachVirtualChild(key);
        _virtualNodes[key] = _virtualNodes[key] with { ParentKey = parentKey };
        var siblings = _virtualChildKeys.TryGetValue(parentKey, out var existing) ? existing : _virtualChildKeys[parentKey] = [];
        if (atStart)
            siblings.Insert(0, key);
        else
            siblings.Add(key);
        NotifyChanged();
    }
    internal void AttachVirtualSibling(string key, string anchorKey, string parentKey, bool before)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(anchorKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(parentKey);
        RequireVirtualNode(key);
        DetachVirtualChild(key);
        _virtualNodes[key] = _virtualNodes[key] with { ParentKey = parentKey };
        var siblings = _virtualChildKeys.TryGetValue(parentKey, out var existing) ? existing : _virtualChildKeys[parentKey] = [];
        var anchorIndex = siblings.IndexOf(anchorKey);
        var insertIndex = anchorIndex < 0 ? siblings.Count : before ? anchorIndex : anchorIndex + 1;
        siblings.Insert(insertIndex, key);
        NotifyChanged();
    }
    private QueryVirtualNode RequireVirtualNode(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _virtualNodes.TryGetValue(key, out var node)
            ? node
            : throw new InvalidOperationException($"'{key}' is not a library-created (virtual) element.");
    }
    private void RequireRealElement(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!_realElementFragments.ContainsKey(key))
            throw new InvalidOperationException($"'{key}' is not a currently-mounted real element.");
    }
    private void DetachRealChildQuiet(string key)
    {
        if (!_relocatedParent.Remove(key, out var oldHost))
            return;
        if (_virtualChildKeys.TryGetValue(oldHost, out var siblings))
            siblings.Remove(key);
        if (_relocatedChildrenByHost.TryGetValue(oldHost, out var hostSet))
            hostSet.Remove(key);
    }
    /// <summary>
    /// Cleans up a host key that has just stopped existing (disposed real element, or a removed virtual
    /// container): every child currently attached under it either cascades away (a pure virtual child, which
    /// has no other home) or springs back to its own declared Razor position (a relocated real child).
    /// </summary>
    private void HandleHostRemoval(string hostKey)
    {
        if (_virtualChildKeys.Remove(hostKey, out var children))
        {
            foreach (var childKey in children.ToArray())
            {
                if (_virtualNodes.ContainsKey(childKey))
                    RemoveVirtualNode(childKey);
                else if (_relocatedParent.ContainsKey(childKey))
                    SpringBackRealChild(childKey);
            }
        }
        _relocatedChildrenByHost.Remove(hostKey);
    }
    private void SpringBackRealChild(string childKey) => _relocatedParent.Remove(childKey);
    internal void AddClass(string elementKey, string className)
    {
        ValidateClassName(elementKey, className);
        (_removedClasses.TryGetValue(elementKey, out var removed) ? removed : null)?.Remove(className);
        (_addedClasses.TryGetValue(elementKey, out var added) ? added : _addedClasses[elementKey] = new(StringComparer.Ordinal)).Add(className);
        NotifyChanged();
    }
    internal void RemoveClass(string elementKey, string className)
    {
        ValidateClassName(elementKey, className);
        (_addedClasses.TryGetValue(elementKey, out var added) ? added : null)?.Remove(className);
        (_removedClasses.TryGetValue(elementKey, out var removed) ? removed : _removedClasses[elementKey] = new(StringComparer.Ordinal)).Add(className);
        NotifyChanged();
    }
    internal void Hide(string elementKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        if (_hiddenKeys.Add(elementKey)) NotifyChanged();
    }
    internal void Show(string elementKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        if (_hiddenKeys.Remove(elementKey)) NotifyChanged();
    }
    internal void Remove(string elementKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        if (_removedKeys.Add(elementKey)) NotifyChanged();
    }
    internal void Empty(string elementKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        _prepended.Remove(elementKey);
        _appended.Remove(elementKey);
        SetContent(elementKey, new QueryContent(string.Empty, false));
    }
    internal void SetCss(string selector, string property, string value) { _styles[(selector, property)] = value; NotifyChanged(); }
    internal void SetAttribute(string elementKey, string name, string value)
    {
        ValidateAttribute(elementKey, name, value);
        _attributes[(elementKey, name)] = value;
        NotifyChanged();
    }
    internal void RemoveAttribute(string elementKey, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ValidateBindableName(name);
        if (_attributes.Remove((elementKey, name))) NotifyChanged();
    }
    internal void SetProperty(string elementKey, string name, object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ValidateBindableName(name);
        _properties[(elementKey, name)] = value;
        NotifyChanged();
    }
    internal void RemoveProperty(string elementKey, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ValidateBindableName(name);
        if (_properties.Remove((elementKey, name))) NotifyChanged();
    }
    internal void SetData<T>(string elementKey, string name, T value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _data[(elementKey, name)] = System.Text.Json.JsonSerializer.SerializeToElement(value);
        NotifyChanged();
    }
    internal T? GetData<T>(string elementKey, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _data.TryGetValue((elementKey, name), out var value)
            ? System.Text.Json.JsonSerializer.Deserialize<T>(value.GetRawText())
            : default;
    }
    internal void RemoveData(string elementKey, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (_data.Remove((elementKey, name))) NotifyChanged();
    }
    internal void SetText(string elementKey, string value) => SetContent(elementKey, new QueryContent(value, false));
    internal void SetHtml(string elementKey, string value) => SetContent(elementKey, new QueryContent(value, true));
    internal void AppendContent(string elementKey, string value, bool isHtml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ArgumentNullException.ThrowIfNull(value);
        (_appended.TryGetValue(elementKey, out var fragments) ? fragments : _appended[elementKey] = [])
            .Add(new QueryContent(value, isHtml));
        NotifyChanged();
    }
    internal void PrependContent(string elementKey, string value, bool isHtml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ArgumentNullException.ThrowIfNull(value);
        (_prepended.TryGetValue(elementKey, out var fragments) ? fragments : _prepended[elementKey] = [])
            .Insert(0, new QueryContent(value, isHtml));
        NotifyChanged();
    }
    public void Clear()
    {
        if (_styles.Count == 0 && _attributes.Count == 0 && _properties.Count == 0 && _data.Count == 0 && _content.Count == 0
            && _addedClasses.Count == 0 && _removedClasses.Count == 0 && _hiddenKeys.Count == 0 && _removedKeys.Count == 0
            && _prepended.Count == 0 && _appended.Count == 0 && _virtualNodes.Count == 0 && _virtualChildKeys.Count == 0
            && _realElementFragments.Count == 0 && _innerContentByOwner.Count == 0 && _innerWrapperByOwner.Count == 0
            && _contentWrapperByOwner.Count == 0 && _innerContentByWrapper.Count == 0 && _eventHandlers.Count == 0 && _typedEventHandlers.Count == 0 && _delegatedEventHandlers.Count == 0 && _relocatedParent.Count == 0
            && _relocatedChildrenByHost.Count == 0)
            return;
        _styles.Clear();
        _attributes.Clear();
        _properties.Clear();
        _data.Clear();
        _content.Clear();
        _addedClasses.Clear();
        _removedClasses.Clear();
        _hiddenKeys.Clear();
        _removedKeys.Clear();
        _prepended.Clear();
        _appended.Clear();
        _virtualNodes.Clear();
        _virtualChildKeys.Clear();
        _realElementFragments.Clear();
        _innerContentByOwner.Clear();
        _innerWrapperByOwner.Clear();
        _contentWrapperByOwner.Clear();
        _innerContentByWrapper.Clear();
        _eventHandlers.Clear();
        _typedEventHandlers.Clear();
        _delegatedEventHandlers.Clear();
        _relocatedParent.Clear();
        _relocatedChildrenByHost.Clear();
        NotifyChanged();
    }
    private static void ValidateAttribute(string elementKey, string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ValidateBindableName(name);
        ArgumentNullException.ThrowIfNull(value);
    }
    private static void ValidateBindableName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (string.Equals(name, "data-rbq-key", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The library-owned data-rbq-key attribute cannot be changed.", nameof(name));
        if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Event-like names are reserved for the library event API.", nameof(name));
    }
    private static string NormalizeEventName(string eventName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        var normalized = eventName.Trim().ToLowerInvariant();
        return normalized.StartsWith("on", StringComparison.Ordinal) ? normalized : "on" + normalized;
    }
    private static string NormalizeDelegatedEventName(string eventName)
    {
        var normalized = NormalizeEventName(eventName)[2..];
        if (!QueryDelegatedEvents.SupportedNames.Contains(normalized, StringComparer.Ordinal))
            throw new NotSupportedException($"Delegated event '{eventName}' is not supported because it does not bubble to the query root.");
        return normalized;
    }
    private static void ValidateClassName(string elementKey, string className)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(className);
        if (className.AsSpan().IndexOfAny(' ', '\t', '\n') >= 0)
            throw new ArgumentException("A class name cannot contain whitespace.", nameof(className));
    }
    private void NotifyChanged() { _dirty = true; if (_batchDepth == 0) Flush(); }
    private void SetContent(string elementKey, QueryContent content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        _content[elementKey] = content;
        _prepended.Remove(elementKey);
        _appended.Remove(elementKey);
        NotifyChanged();
    }
    private void Flush()
    {
        if (!_dirty) return;
        _dirty = false;
        // Route the whole subscriber fan-out through one dispatch when at least one host component has
        // registered one (QueryHost.OnInitialized). This guarantees that a mutation affecting two different
        // components in lockstep (e.g. relocating a real element: source stops rendering, host starts)
        // always lands in the same Blazor render batch, even when the mutating call originates off the
        // renderer's thread (a background service, timer, or SignalR callback). When already on that
        // thread, Dispatcher.InvokeAsync runs its callback synchronously/inline, so this has no effect on
        // the existing synchronous fast path. Falls back to the direct, undispatched call used since before
        // this existed (e.g. plain unit tests that construct QueryRenderState without any QueryHost).
        //
        // Any registered dispatcher works interchangeably here: multiple QueryHost instances sharing one
        // QueryRenderState always share the same underlying Blazor renderer/dispatcher within one app or
        // circuit, so if the dispatcher this picks is later unregistered (its QueryHost disposed), a
        // remaining one takes over on the next flush rather than silently reverting to the unsafe
        // undispatched path.
        if (_dispatchers.Count > 0)
        {
            var task = _dispatchers[0](() => Changed?.Invoke());
            // If the callback ran synchronously (the common case: already on the dispatcher thread),
            // observe and rethrow any exception now, matching the pre-dispatch behavior where a Changed
            // subscriber's exception propagated synchronously out of the mutating call. A still-pending
            // task means this was a genuine cross-thread marshal; that path remains fire-and-forget, same
            // as the per-subscriber InvokeAsync(StateHasChanged) calls used before this existed.
            if (task.IsCompleted)
                task.GetAwaiter().GetResult();
        }
        else
        {
            Changed?.Invoke();
        }
    }
    private sealed class Batch(QueryRenderState owner) : IDisposable
    {
        private bool _disposed;
        public void Dispose() { if (_disposed) return; _disposed = true; if (--owner._batchDepth == 0) owner.Flush(); }
    }
}
public sealed record QueryStyleRule(string Selector, string Property, string Value);
internal sealed record QueryContent(string Value, bool IsHtml);
internal sealed record QueryEventHandler(Action Handler, bool Once);
internal sealed record QueryTypedEventHandler(Action<EventArgs> Handler, Delegate OriginalHandler, Type ArgumentType, bool Once);
internal sealed record QueryDelegatedEventHandler(string Selector, Action<QueryEventTarget> Handler);
internal sealed record QueryVirtualNode(string Key, string? ParentKey, string TagName, string? ClassName, string? Text, bool IsHtml);
