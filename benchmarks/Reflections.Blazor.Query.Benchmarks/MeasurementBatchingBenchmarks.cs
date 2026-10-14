using BenchmarkDotNet.Attributes;
using Reflections.Blazor.Query;

namespace Reflections.Blazor.Query.Benchmarks;

[MemoryDiagnoser]
public class MeasurementBatchingBenchmarks
{
    private QueryRenderState _state = default!;
    private CountingMeasurementBridge _bridge = default!;
    private QueryMeasurementCoordinator _coordinator = default!;

    [Params(1, 10, 100, 1000)]
    public int SelectionSize { get; set; }

    [IterationSetup(Target = nameof(CoalesceRequestsIntoOneBridgeBatch))]
    public void SetupBatch()
    {
        _state = new QueryRenderState();
        _bridge = new CountingMeasurementBridge();
        _coordinator = new QueryMeasurementCoordinator(_bridge);
    }

    [IterationCleanup(Target = nameof(CoalesceRequestsIntoOneBridgeBatch))]
    public void VerifySingleBridgeBatch()
    {
        if (_bridge.BatchCount != 1)
            throw new InvalidOperationException($"Expected one bridge batch, observed {_bridge.BatchCount}.");
    }

    [Benchmark]
    public async Task<int> CoalesceRequestsIntoOneBridgeBatch()
    {
        // One invocation models one render's set of distinct element reads, not repeated batches.
        var pendingReads = new ValueTask<double?>[SelectionSize];
        for (var index = 0; index < SelectionSize; index++)
            pendingReads[index] = _coordinator.RequestAsync(
                _state,
                $"measurement-target-{index}",
                MeasurementKind.Width);

        await _coordinator.FlushAsync();

        var total = _bridge.BatchCount;
        foreach (var pendingRead in pendingReads)
            total += (int)(await pendingRead ?? 0);
        return total;
    }

    private sealed class CountingMeasurementBridge : IQueryMeasurementBridge
    {
        public int BatchCount { get; private set; }

        public ValueTask<IReadOnlyList<MeasurementReading>> MeasureAsync(
            IReadOnlyList<MeasurementRequest> requests,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BatchCount++;
            IReadOnlyList<MeasurementReading> readings = requests
                .Select((request, index) => new MeasurementReading(
                    request.ElementKey,
                    request.Kind,
                    Value: index + 1))
                .ToArray();
            return ValueTask.FromResult(readings);
        }
    }
}
