using Microsoft.JSInterop;

namespace Reflections.Blazor.Query;

public sealed class JsQueryFormBridge(IJSRuntime jsRuntime) : IQueryFormBridge, IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _module = new(() => jsRuntime
        .InvokeAsync<IJSObjectReference>("import", "./_content/Reflections.Blazor.Query/queryForm.js")
        .AsTask());

    public async ValueTask<IReadOnlyList<SerializationResult>> SerializeAsync(
        IReadOnlyList<SerializationRequest> requests,
        CancellationToken cancellationToken)
    {
        var module = await _module.Value;
        return await module.InvokeAsync<SerializationResult[]>("serialize", cancellationToken, requests);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module.IsValueCreated)
            await (await _module.Value).DisposeAsync();
    }
}
