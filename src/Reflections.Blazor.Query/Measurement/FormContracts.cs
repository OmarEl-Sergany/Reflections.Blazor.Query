using System.Collections.Generic;

namespace Reflections.Blazor.Query;

public sealed record SerializedField(string Name, string Value);

public sealed record SerializationRequest(string ElementKey);

public sealed record SerializationResult(string ElementKey, IReadOnlyList<SerializedField> Fields);

public interface IQueryFormBridge
{
    ValueTask<IReadOnlyList<SerializationResult>> SerializeAsync(
        IReadOnlyList<SerializationRequest> requests,
        CancellationToken cancellationToken);
}
