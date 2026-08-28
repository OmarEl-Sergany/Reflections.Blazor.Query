using Microsoft.Extensions.DependencyInjection;

namespace Reflections.Blazor.Query;

public static class QueryServiceCollectionExtensions
{
    public static IServiceCollection AddBlazorQuery(this IServiceCollection services)
    {
        services.AddScoped<QueryRenderState>();
        services.AddScoped<QueryElementRegistry>();
        services.AddScoped<Query>();
        services.AddScoped<LayoutQueryRenderState>();
        services.AddScoped<LayoutQuery>();
        return services;
    }
}
