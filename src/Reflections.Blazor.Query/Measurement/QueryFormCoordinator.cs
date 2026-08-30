namespace Reflections.Blazor.Query;

public sealed class QueryFormCoordinator(IQueryFormBridge bridge) : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<SerializationCacheKey, SerializationResult> _cache = new();
    private readonly Dictionary<SerializationCacheKey, PendingSerialization> _pending = new();
    private bool _disposed;

    public event Action? Requested;

    /// <summary>
    /// Requests serialization for a set of resolved element keys and returns the concatenated
    /// name/value fields for all of them, in element-key order. All keys are enqueued up front so a
    /// single post-render flush forms one JS batch covering every key; duplicate requests made for the
    /// same (render state, element key) pair before that flush are coalesced and share a single pending
    /// result. Completed results are snapshots cached against the owning render state.
    /// </summary>
    public async ValueTask<IReadOnlyList<SerializedField>> RequestArrayAsync(
        QueryRenderState renderState,
        IReadOnlyList<string> elementKeys,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(renderState);
        ArgumentNullException.ThrowIfNull(elementKeys);
        foreach (var elementKey in elementKeys)
            ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);

        var tasks = new Task<SerializationResult>[elementKeys.Count];
        for (var index = 0; index < elementKeys.Count; index++)
            tasks[index] = EnqueueResult(renderState, elementKeys[index]);

        var results = await Task.WhenAll(tasks).WaitAsync(cancellationToken);

        var fields = new List<SerializedField>();
        foreach (var result in results)
        {
            if (result.Fields is not null)
                fields.AddRange(result.Fields);
        }
        return fields;
    }

    private Task<SerializationResult> EnqueueResult(QueryRenderState renderState, string elementKey)
    {
        var key = new SerializationCacheKey(renderState, elementKey);
        Task<SerializationResult> task;
        var notify = false;
        lock (_gate)
        {
            if (_cache.TryGetValue(key, out var cached))
                return Task.FromResult(cached);

            if (!_pending.TryGetValue(key, out var pending))
            {
                pending = new PendingSerialization(key);
                _pending.Add(key, pending);
                notify = true;
            }

            task = pending.Completion.Task;
        }

        if (notify)
            Requested?.Invoke();

        return task;
    }

    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        PendingSerialization[] batch;
        lock (_gate)
        {
            batch = _pending.Values.Where(item => !item.IsFlushing).ToArray();
            foreach (var item in batch)
                item.IsFlushing = true;
        }

        if (batch.Length == 0)
            return;

        IReadOnlyList<SerializationResult> results;
        try
        {
            results = await bridge.SerializeAsync(
                batch.Select(pending => new SerializationRequest(pending.Key.ElementKey)).ToArray(),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Cancel(batch);
            throw;
        }
        catch (Exception exception)
        {
            Fault(batch, exception);
            return;
        }

        var values = results.ToDictionary(result => result.ElementKey, result => result);
        Complete(batch, values);
    }

    public void Invalidate(QueryRenderState renderState)
    {
        ArgumentNullException.ThrowIfNull(renderState);
        RemoveWhere(key => ReferenceEquals(key.RenderState, renderState));
    }

    public void InvalidateElement(QueryRenderState renderState, string elementKey)
    {
        ArgumentNullException.ThrowIfNull(renderState);
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);
        RemoveWhere(key => ReferenceEquals(key.RenderState, renderState)
            && string.Equals(key.ElementKey, elementKey, StringComparison.Ordinal));
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
            return ValueTask.CompletedTask;

        _disposed = true;
        PendingSerialization[] pending;
        lock (_gate)
        {
            pending = _pending.Values.ToArray();
            _pending.Clear();
            _cache.Clear();
        }

        foreach (var item in pending)
            item.Completion.TrySetCanceled();

        return bridge is IAsyncDisposable asyncDisposable
            ? asyncDisposable.DisposeAsync()
            : ValueTask.CompletedTask;
    }

    private void Complete(
        IEnumerable<PendingSerialization> batch,
        IReadOnlyDictionary<string, SerializationResult> values)
    {
        lock (_gate)
        {
            foreach (var item in batch)
            {
                if (!_pending.Remove(item.Key))
                    continue;

                var result = values.GetValueOrDefault(item.Key.ElementKey)
                    ?? new SerializationResult(item.Key.ElementKey, []);
                _cache[item.Key] = result;
                item.Completion.TrySetResult(result);
            }
        }
    }

    private void Fault(IEnumerable<PendingSerialization> batch, Exception exception)
    {
        lock (_gate)
        {
            foreach (var item in batch)
            {
                if (_pending.Remove(item.Key))
                    item.Completion.TrySetException(exception);
            }
        }
    }

    private void Cancel(IEnumerable<PendingSerialization> batch)
    {
        lock (_gate)
        {
            foreach (var item in batch)
            {
                if (_pending.Remove(item.Key))
                    item.Completion.TrySetCanceled();
            }
        }
    }

    private void RemoveWhere(Func<SerializationCacheKey, bool> predicate)
    {
        PendingSerialization[] pending;
        lock (_gate)
        {
            foreach (var key in _cache.Keys.Where(predicate).ToArray())
                _cache.Remove(key);

            pending = _pending
                .Where(pair => predicate(pair.Key))
                .Select(pair => pair.Value)
                .ToArray();
            foreach (var item in pending)
                _pending.Remove(item.Key);
        }

        foreach (var item in pending)
            item.Completion.TrySetCanceled();
    }

    private sealed record SerializationCacheKey(QueryRenderState RenderState, string ElementKey);

    private sealed class PendingSerialization(SerializationCacheKey key)
    {
        public SerializationCacheKey Key { get; } = key;
        public TaskCompletionSource<SerializationResult> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool IsFlushing { get; set; }
    }
}
