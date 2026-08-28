using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Reflections.Blazor.Query;

namespace Reflections.Blazor.Query.ComponentTests;

internal sealed class TestMeasurementBridge(IReadOnlyList<MeasurementReading>? readings = null) : IQueryMeasurementBridge
{
    public List<IReadOnlyList<MeasurementRequest>> Batches { get; } = [];

    public ValueTask<IReadOnlyList<MeasurementReading>> MeasureAsync(
        IReadOnlyList<MeasurementRequest> requests,
        CancellationToken cancellationToken)
    {
        Batches.Add([.. requests]);
        return ValueTask.FromResult(readings ?? (IReadOnlyList<MeasurementReading>)[]);
    }
}
