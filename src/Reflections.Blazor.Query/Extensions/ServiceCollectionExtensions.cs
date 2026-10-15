using Microsoft.Extensions.DependencyInjection;

namespace Reflections.Blazor.Query;

public static class QueryServiceCollectionExtensions
{
    /// <summary>Registers the Query render-state, element, measurement, form, and query services.</summary>
    /// <param name="services">The application service collection.</param>
    /// <returns>The same service collection for further registration.</returns>
    /// <remarks>
    /// Services use scoped lifetimes, matching a Blazor circuit in Interactive Server and an application
    /// instance in WebAssembly. Add this once to the host application's service configuration.
    /// </remarks>
    public static IServiceCollection AddBlazorQuery(this IServiceCollection services)
    {
        services.AddScoped<QueryRenderState>();
        services.AddScoped<QueryElementRegistry>();
        services.AddScoped<IQueryMeasurementBridge, JsQueryMeasurementBridge>();
        services.AddScoped<QueryMeasurementCoordinator>();
        services.AddScoped<IQueryFormBridge, JsQueryFormBridge>();
        services.AddScoped<QueryFormCoordinator>();
        services.AddScoped<Query>();
        services.AddScoped<LayoutQueryRenderState>();
        services.AddScoped<LayoutQuery>();
        services.AddScoped<ApplicationQueryRenderState>();
        services.AddScoped<ApplicationQuery>();
        return services;
    }
}
