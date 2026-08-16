namespace Reflections.Blazor.Query;

public sealed class QueryMeasurementCoordinator(IQueryMeasurementBridge bridge) : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<MeasurementCacheKey, double?> _cache = new();
    private readonly Dictionary<MeasurementCacheKey, PendingMeasurement> _pending = new();
    private bool _disposed;

    public event Action? Requested;

    public ValueTask<double?> RequestAsync(
        QueryRenderState renderState,
        string elementKey,
        MeasurementKind kind,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(renderState);
        ArgumentException.ThrowIfNullOrWhiteSpace(elementKey);

        var key = new MeasurementCacheKey(renderState, elementKey, kind);
        Task<double?> task;
        var notify = false;
        lock (_gate)
        {
            if (_cache.TryGetValue(key, out var cached))
                return ValueTask.FromResult(cached);

            if (!_pending.TryGetValue(key, out var pending))
            {
                pending = new PendingMeasurement(key);
                _pending.Add(key, pending);
                notify = true;
            }

            task = pending.Completion.Task;
        }

        if (notify)
            Requested?.Invoke();

        return cancellationToken.CanBeCanceled
            ? new ValueTask<double?>(task.WaitAsync(cancellationToken))
            : new ValueTask<double?>(task);
    }

    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        PendingMeasurement[] batch;
        lock (_gate)
        {
            batch = _pending.Values.Where(item => !item.IsFlushing).ToArray();
            foreach (var item in batch)
                item.IsFlushing = true;
        }

        if (batch.Length == 0)
            return;

        IReadOnlyList<MeasurementReading> readings;
        try
        {
            readings = await bridge.MeasureAsync(
                batch.Select(pending => new MeasurementRequest(pending.Key.ElementKey, pending.Key.Kind)).ToArray(),
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

        var values = readings.ToDictionary(
            reading => (reading.ElementKey, reading.Kind),
            reading => reading.Value);
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
        PendingMeasurement[] pending;
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
        IEnumerable<PendingMeasurement> batch,
        IReadOnlyDictionary<(string ElementKey, MeasurementKind Kind), double?> values)
    {
        lock (_gate)
        {
            foreach (var item in batch)
            {
                if (!_pending.Remove(item.Key))
                    continue;

                var value = values.GetValueOrDefault((item.Key.ElementKey, item.Key.Kind));
                _cache[item.Key] = value;
                item.Completion.TrySetResult(value);
            }
        }
    }

    private void Fault(IEnumerable<PendingMeasurement> batch, Exception exception)
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

    private void Cancel(IEnumerable<PendingMeasurement> batch)
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

    private void RemoveWhere(Func<MeasurementCacheKey, bool> predicate)
    {
        PendingMeasurement[] pending;
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

    private sealed record MeasurementCacheKey(QueryRenderState RenderState, string ElementKey, MeasurementKind Kind);

    private sealed class PendingMeasurement(MeasurementCacheKey key)
    {
        public MeasurementCacheKey Key { get; } = key;
        public TaskCompletionSource<double?> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool IsFlushing { get; set; }
    }
}
