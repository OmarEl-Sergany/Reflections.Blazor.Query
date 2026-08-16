using Microsoft.JSInterop;

namespace Reflections.Blazor.Query;

public sealed class JsQueryMeasurementBridge(IJSRuntime jsRuntime) : IQueryMeasurementBridge, IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _module = new(() => jsRuntime
        .InvokeAsync<IJSObjectReference>("import", "./_content/Reflections.Blazor.Query/queryMeasurement.js")
        .AsTask());

    public async ValueTask<IReadOnlyList<MeasurementReading>> MeasureAsync(
        IReadOnlyList<MeasurementRequest> requests,
        CancellationToken cancellationToken)
    {
        var module = await _module.Value;
        return await module.InvokeAsync<MeasurementReading[]>("measure", cancellationToken, requests);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module.IsValueCreated)
            await (await _module.Value).DisposeAsync();
    }
}
