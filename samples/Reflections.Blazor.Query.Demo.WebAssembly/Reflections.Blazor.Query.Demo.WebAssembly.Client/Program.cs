using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Reflections.Blazor.Query;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddReflectionsBlazorQuery();

await builder.Build().RunAsync();
