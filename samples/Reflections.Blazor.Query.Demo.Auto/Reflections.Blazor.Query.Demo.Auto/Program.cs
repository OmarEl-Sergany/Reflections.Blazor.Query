using Reflections.Blazor.Query.Demo.Auto.Client.Pages;
using Reflections.Blazor.Query.Demo.Auto.Components;
using Reflections.Blazor.Query;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddSingleton<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider,
    Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider>();

builder.Services.AddReflectionsBlazorQuery();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
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
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Reflections.Blazor.Query.Demo.Auto.Client._Imports).Assembly);

app.Run();
