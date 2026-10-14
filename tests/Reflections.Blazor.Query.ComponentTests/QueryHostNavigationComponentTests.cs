using Bunit;
using System;
using System.Threading;
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
        context.Services.AddSingleton(new QueryFormCoordinator(new TestFormBridge()));
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
    public async Task Navigation_cancels_active_slide_and_promise_and_clears_temporary_state()
    {
        using var context = new BunitContext();
        var navigation = new TestNavigationManager();
        var pageState = new QueryRenderState();
        var registry = new QueryElementRegistry();
        var measurements = new QueryMeasurementCoordinator(new TestMeasurementBridge(
            [new MeasurementReading("navigation-slide", MeasurementKind.ScrollHeight, 64)]));
        context.Services.AddSingleton<NavigationManager>(navigation);
        context.Services.AddSingleton(pageState);
        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(measurements);
        context.Services.AddSingleton(new QueryFormCoordinator(new TestFormBridge()));

        var host = context.Render<QueryHost>(parameters => parameters
            .Add(component => component.RenderState, pageState)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<QueryElement>(0);
                builder.AddAttribute(1, nameof(QueryElement.ElementKey), "navigation-slide");
                builder.AddAttribute(2, nameof(QueryElement.Class), "slide-target");
                builder.CloseComponent();
            }));
        var selection = new Query(pageState, registry, measurements).Select(".slide-target");
        var slide = selection.SlideUp(TimeSpan.FromSeconds(5)).AsTask();
        var promise = selection.Promise().AsTask();

        host.WaitForAssertion(() => Assert.Contains("[data-rbq-key=\"navigation-slide\"] { animation: rbq-slide-up 5000ms ease both; }", host.Markup, StringComparison.Ordinal));
        Assert.False(promise.IsCompleted);
        Assert.False(slide.IsCompleted);

        navigation.Navigate("next");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            slide.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            promise.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken));
        Assert.Empty(pageState.Rules);
        Assert.False(pageState.IsHidden("navigation-slide"));
    }

    [Fact]
    public async Task Disposing_QueryElement_cancels_active_slide_and_promise_and_removes_temporary_state()
    {
        using var context = new BunitContext();
        var pageState = new QueryRenderState();
        var registry = new QueryElementRegistry();
        var measurements = new QueryMeasurementCoordinator(new TestMeasurementBridge(
            [new MeasurementReading("disposed-slide", MeasurementKind.ScrollHeight, 64)]));
        context.Services.AddSingleton<NavigationManager>(new TestNavigationManager());
        context.Services.AddSingleton(pageState);
        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(measurements);
        context.Services.AddSingleton(new QueryFormCoordinator(new TestFormBridge()));

        var host = context.Render<QueryHost>(parameters => parameters
            .Add(component => component.RenderState, pageState)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<QueryElement>(0);
                builder.AddAttribute(1, nameof(QueryElement.ElementKey), "disposed-slide");
                builder.AddAttribute(2, nameof(QueryElement.Class), "slide-target");
                builder.CloseComponent();
            }));
        var selection = new Query(pageState, registry, measurements).Select(".slide-target");
        var slide = selection.SlideUp(TimeSpan.FromSeconds(5)).AsTask();
        var promise = selection.Promise().AsTask();

        host.WaitForAssertion(() => Assert.Contains("[data-rbq-key=\"disposed-slide\"] { animation: rbq-slide-up 5000ms ease both; }", host.Markup, StringComparison.Ordinal));
        Assert.False(promise.IsCompleted);
        Assert.False(slide.IsCompleted);

        host.FindComponent<QueryElement>().Instance.Dispose();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            slide.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            promise.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken));
        Assert.DoesNotContain(pageState.Rules, rule => rule.Property is "--rbq-slide-height" or "height" or "overflow" or "animation");
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
        context.Services.AddSingleton(new QueryFormCoordinator(new TestFormBridge()));
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

    [Fact]
    public async Task Disposing_one_of_two_QueryHosts_sharing_a_render_state_does_not_break_dispatch_for_the_other()
    {
        using var context = new BunitContext();
        var sharedState = new QueryRenderState();
        var registry = new QueryElementRegistry();
        context.Services.AddSingleton<NavigationManager>(new TestNavigationManager());
        context.Services.AddSingleton(sharedState);
        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(new QueryMeasurementCoordinator(new TestMeasurementBridge()));
        context.Services.AddSingleton(new QueryFormCoordinator(new TestFormBridge()));

        var hostA = context.Render<QueryHost>(parameters => parameters
            .Add(component => component.RenderState, sharedState)
            .AddChildContent("<main>A</main>"));
        var hostB = context.Render<QueryHost>(parameters => parameters
            .Add(component => component.RenderState, sharedState)
            .AddChildContent("<aside>B</aside>"));

        // Host B registered its dispatch after host A. Disposing B must not clear A's ability to
        // receive dispatched notifications for mutations made off the renderer's thread — the bug: a
        // single dispatch slot got clobbered/cleared regardless of which host was still alive.
        hostB.Instance.Dispose();

        await Task.Run(() => new Query(sharedState, registry).Select(".target").Css("color", "green"));

        hostA.WaitForAssertion(() => Assert.Contains("color: green", hostA.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Removing_nested_QueryHost_cancels_its_queue_and_preserves_outer_layout_state()
    {
        using var context = new BunitContext();
        var outerState = new LayoutQueryRenderState();
        var innerState = new QueryRenderState();
        var registry = new QueryElementRegistry();
        context.Services.AddSingleton<NavigationManager>(new TestNavigationManager());
        context.Services.AddSingleton<QueryRenderState>(innerState);
        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(new QueryMeasurementCoordinator(new TestMeasurementBridge()));
        context.Services.AddSingleton(new QueryFormCoordinator(new TestFormBridge()));
        new LayoutQuery(outerState).Select(".outer-layout").Css("color", "blue");

        var host = context.Render<NestedHostFixture>(parameters => parameters
            .Add(component => component.OuterState, outerState)
            .Add(component => component.InnerState, innerState)
            .Add(component => component.ShowInner, true));
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var innerSelection = new Query(innerState, registry).Select(".inner-target");
        innerSelection.Queue(async cancellationToken =>
        {
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        });
        var drain = innerSelection.Dequeue().AsTask();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);

        await host.Instance.HideInnerAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            drain.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken));
        Assert.Equal(0, new Query(innerState, registry).Select(".inner-target").Length);
        Assert.Contains("color: blue", host.Markup, StringComparison.Ordinal);
        Assert.Contains("outer layout", host.Markup, StringComparison.Ordinal);
    }

    private sealed class NestedHostFixture : ComponentBase
    {
        [Parameter] public QueryRenderState OuterState { get; set; } = default!;
        [Parameter] public QueryRenderState InnerState { get; set; } = default!;
        [Parameter] public bool ShowInner { get; set; }

        public Task HideInnerAsync()
        {
            ShowInner = false;
            return InvokeAsync(StateHasChanged);
        }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<QueryHost>(0);
            builder.AddAttribute(1, nameof(QueryHost.RenderState), OuterState);
            builder.AddAttribute(2, nameof(QueryHost.ClearOnNavigation), false);
            builder.AddAttribute(3, nameof(QueryHost.ChildContent), (RenderFragment)(outerBuilder =>
            {
                outerBuilder.OpenElement(0, "main");
                outerBuilder.AddContent(1, "outer layout");
                if (ShowInner)
                {
                    outerBuilder.OpenComponent<QueryHost>(2);
                    outerBuilder.AddAttribute(3, nameof(QueryHost.RenderState), InnerState);
                    outerBuilder.AddAttribute(4, nameof(QueryHost.ChildContent), (RenderFragment)(innerBuilder =>
                    {
                        innerBuilder.OpenComponent<QueryElement>(0);
                        innerBuilder.AddAttribute(1, nameof(QueryElement.ElementKey), "nested-inner-target");
                        innerBuilder.AddAttribute(2, nameof(QueryElement.Class), "inner-target");
                        innerBuilder.CloseComponent();
                    }));
                    outerBuilder.CloseComponent();
                }
                outerBuilder.CloseElement();
            }));
            builder.CloseComponent();
        }
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
