using Bunit;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Reflections.Blazor.Query;
using Reflections.Blazor.Query.Components;

namespace Reflections.Blazor.Query.ComponentTests;

public sealed class QueryHostNavigationComponentTests
{
    [Fact]
    public void Navigation_clears_page_host_state_and_preserves_layout_host_state()
    {
        using var context = new BunitContext();
        var navigation = new TestNavigationManager();
        var pageState = new QueryRenderState();
        var layoutState = new LayoutQueryRenderState();
        context.Services.AddSingleton<NavigationManager>(navigation);
        context.Services.AddSingleton<QueryRenderState>(pageState);
        context.Services.AddSingleton(new QueryMeasurementCoordinator(new TestMeasurementBridge()));
        context.Services.AddSingleton(new QueryElementRegistry());

        new Query(pageState).Select(".page-target").Css("color", "red");
        new LayoutQuery(layoutState).Select(".layout-target").Css("color", "blue");

        var pageHost = context.Render<QueryHost>(parameters => parameters
            .Add(component => component.RenderState, pageState)
            .Add(component => component.ClearOnNavigation, true)
            .AddChildContent("<main>Page content</main>"));
        var layoutHost = context.Render<QueryHost>(parameters => parameters
            .Add(component => component.RenderState, layoutState)
            .Add(component => component.ClearOnNavigation, false)
            .AddChildContent("<aside>Layout content</aside>"));

        Assert.Contains("color: red", pageHost.Markup, StringComparison.Ordinal);
        Assert.Contains("color: blue", layoutHost.Markup, StringComparison.Ordinal);

        navigation.Navigate("next");

        pageHost.WaitForAssertion(() => Assert.DoesNotContain("color: red", pageHost.Markup, StringComparison.Ordinal));
        layoutHost.WaitForAssertion(() => Assert.Contains("color: blue", layoutHost.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task QueryHost_flushes_queued_measurements_after_a_render()
    {
        using var context = new BunitContext();
        var navigation = new TestNavigationManager();
        var pageState = new QueryRenderState();
        var bridge = new TestMeasurementBridge([new MeasurementReading("panel", MeasurementKind.Height, 42)]);
        var measurements = new QueryMeasurementCoordinator(bridge);
        context.Services.AddSingleton<NavigationManager>(navigation);
        context.Services.AddSingleton<QueryRenderState>(pageState);
        context.Services.AddSingleton(measurements);
        context.Services.AddSingleton(new QueryElementRegistry());

        context.Render<QueryHost>(parameters => parameters
            .Add(component => component.RenderState, pageState)
            .AddChildContent("<main>Page content</main>"));

        var height = measurements.RequestAsync(
            pageState,
            "panel",
            MeasurementKind.Height,
            Xunit.TestContext.Current.CancellationToken).AsTask();

        await Task.WhenAny(height, Task.Delay(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken));

        Assert.True(height.IsCompletedSuccessfully);
        Assert.Equal(42, await height);
        Assert.Single(bridge.Batches);
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://query.test/", "https://query.test/");

        public void Navigate(string relativeUri) => NavigateTo(relativeUri);

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            Uri = ToAbsoluteUri(uri).AbsoluteUri;
            NotifyLocationChanged(isInterceptedLink: false);
        }
    }
}
