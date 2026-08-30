using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Reflections.Blazor.Query;

namespace Reflections.Blazor.Query.ComponentTests;

internal sealed class TestFormBridge(IReadOnlyList<SerializationResult>? results = null) : IQueryFormBridge
{
    public List<IReadOnlyList<SerializationRequest>> Batches { get; } = [];

    public ValueTask<IReadOnlyList<SerializationResult>> SerializeAsync(
        IReadOnlyList<SerializationRequest> requests,
        CancellationToken cancellationToken)
    {
        Batches.Add([.. requests]);
        return ValueTask.FromResult(results ?? (IReadOnlyList<SerializationResult>)[]);
    }
}
