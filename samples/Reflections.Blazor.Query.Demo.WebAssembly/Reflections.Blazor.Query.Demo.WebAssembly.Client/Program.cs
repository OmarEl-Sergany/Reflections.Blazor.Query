using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Reflections.Blazor.Query;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped<QueryRenderState>();
builder.Services.AddScoped<Query>();
builder.Services.AddScoped<LayoutQueryRenderState>();
builder.Services.AddScoped<LayoutQuery>();

await builder.Build().RunAsync();
