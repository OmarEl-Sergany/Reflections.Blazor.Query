using Reflections.Blazor.Query.Demo.WebAssembly.Client.Pages;
using Reflections.Blazor.Query.Demo.WebAssembly.Components;
using Reflections.Blazor.Query;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddSingleton<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider,
    Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider>();

builder.Services.AddBlazorQuery();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Reflections.Blazor.Query.Demo.WebAssembly.Client._Imports).Assembly);

app.Run();
