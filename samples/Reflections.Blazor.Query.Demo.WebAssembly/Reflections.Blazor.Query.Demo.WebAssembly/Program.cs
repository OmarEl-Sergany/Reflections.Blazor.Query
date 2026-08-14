using Reflections.Blazor.Query.Demo.WebAssembly.Client.Pages;
using Reflections.Blazor.Query.Demo.WebAssembly.Components;
using Reflections.Blazor.Query;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<QueryRenderState>();
builder.Services.AddScoped<Query>();
builder.Services.AddScoped<LayoutQueryRenderState>();
builder.Services.AddScoped<LayoutQuery>();

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
