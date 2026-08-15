using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Reflections.Blazor.Query;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddBlazorQuery();

await builder.Build().RunAsync();
