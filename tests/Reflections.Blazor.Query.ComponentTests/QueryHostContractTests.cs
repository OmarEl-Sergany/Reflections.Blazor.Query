using Reflections.Blazor.Query;
using Bunit;
using System;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Reflections.Blazor.Query.Components;

namespace Reflections.Blazor.Query.ComponentTests;

public sealed class QueryHostContractTests
{
    [Fact]
    public void Layout_and_page_scopes_can_be_rendered_independently()
    {
        var pageState = new QueryRenderState();
        var layoutState = new LayoutQueryRenderState();

        new Query(pageState).Select(".content").Css("color", "red");
        new LayoutQuery(layoutState).Select(".sidebar").Css("background", "black");

        Assert.NotSame(pageState, layoutState);
    }

    [Fact]
    public void Registered_application_query_state_survives_navigation_until_application_clear()
    {
        using var context = new BunitContext();
        var navigation = new TestNavigationManager();
        context.Services.AddSingleton<NavigationManager>(navigation);
        context.Services.AddSingleton<QueryRenderState>(new QueryRenderState());
        context.Services.AddSingleton<QueryElementRegistry>(new QueryElementRegistry());
        context.Services.AddSingleton(new QueryMeasurementCoordinator(new TestMeasurementBridge()));
        context.Services.AddSingleton(new QueryFormCoordinator(new TestFormBridge()));

        var registrations = new ServiceCollection();
        registrations.AddBlazorQuery();
        Assert.Contains(registrations, descriptor => descriptor.ServiceType == typeof(ApplicationQueryRenderState)
            && descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(registrations, descriptor => descriptor.ServiceType == typeof(ApplicationQuery)
            && descriptor.Lifetime == ServiceLifetime.Scoped);

        var state = new ApplicationQueryRenderState();
        var query = new ApplicationQuery(state);
        query.Select(".application-scope").Css("color", "purple");

        var host = context.Render<QueryHost>(parameters => parameters
            .Add(component => component.RenderState, state)
            .Add(component => component.ClearOnNavigation, false)
            .AddChildContent("<div class='application-scope'>App content</div>"));
        Assert.Contains(".application-scope { color: purple; }", host.Markup, StringComparison.Ordinal);

        navigation.Navigate("next");
        host.WaitForAssertion(() => Assert.Contains(".application-scope { color: purple; }", host.Markup, StringComparison.Ordinal));

        state.Clear();
        host.WaitForAssertion(() => Assert.DoesNotContain(".application-scope { color: purple; }", host.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void Application_query_state_is_scoped_to_each_Blazor_application_service_scope()
    {
        var services = new ServiceCollection();
        services.AddBlazorQuery();
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ApplicationQueryRenderState)
            && descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ApplicationQuery)
            && descriptor.Lifetime == ServiceLifetime.Scoped);

        using var provider = services.BuildServiceProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var firstState = firstScope.ServiceProvider.GetRequiredService<ApplicationQueryRenderState>();

        Assert.Same(firstState, firstScope.ServiceProvider.GetRequiredService<ApplicationQueryRenderState>());
        Assert.NotSame(firstState, secondScope.ServiceProvider.GetRequiredService<ApplicationQueryRenderState>());
    }

    [Fact]
    public void Layout_state_survives_page_state_reset()
    {
        var pageState = new QueryRenderState();
        var layoutState = new LayoutQueryRenderState();
        var layoutChanges = 0;
        layoutState.Changed += () => layoutChanges++;

        new LayoutQuery(layoutState).Select(".sidebar").Css("background", "black");
        pageState.ClearForTest();

        Assert.Equal(1, layoutChanges);
    }

    [Fact]
    public void Page_host_clear_does_not_clear_layout_host()
    {
        var pageState = new QueryRenderState();
        var layoutState = new LayoutQueryRenderState();
        var pageStateChanges = 0;
        var layoutChanges = 0;
        pageState.Changed += () => pageStateChanges++;
        layoutState.Changed += () => layoutChanges++;
        new Query(pageState).Select(".page").Css("display", "none");
        new LayoutQuery(layoutState).Select(".sidebar").Css("background", "black");

        pageState.ClearForTest();

        Assert.Equal(2, pageStateChanges);
        Assert.Equal(1, layoutChanges);
    }

    [Fact]
    public void Disposed_host_subscription_can_be_removed_without_changing_state()
    {
        var state = new QueryRenderState();
        var changes = 0;
        void OnChanged() => changes++;
        state.Changed += OnChanged;
        new Query(state).Select(".page").Css("color", "red");
        state.Changed -= OnChanged;

        new Query(state).Select(".page").Css("color", "blue");

        Assert.Equal(1, changes);
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

internal static class QueryStateTestExtensions
{
    public static void ClearForTest(this QueryRenderState state)
    {
        state.Clear();
    }
}
