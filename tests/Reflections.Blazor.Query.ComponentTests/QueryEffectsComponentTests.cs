using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Reflections.Blazor.Query;
using Reflections.Blazor.Query.Components;

namespace Reflections.Blazor.Query.ComponentTests;

public sealed class QueryEffectsComponentTests
{
    [Fact]
    public void FadeTo_renders_transition_and_opacity_through_the_QueryHost_style_tree()
    {
        using var context = new BunitContext();
        var renderState = new QueryRenderState();
        var registry = new QueryElementRegistry();
        context.Services.AddSingleton<NavigationManager>(new TestNavigationManager());
        context.Services.AddSingleton(renderState);
        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(new QueryMeasurementCoordinator(new TestMeasurementBridge()));
        context.Services.AddSingleton(new QueryFormCoordinator(new TestFormBridge()));

        var host = context.Render<QueryHost>(parameters => parameters
            .Add(component => component.RenderState, renderState)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<QueryElement>(0);
                builder.AddAttribute(1, nameof(QueryElement.ElementKey), "fade-target");
                builder.AddAttribute(2, nameof(QueryElement.Class), "fade");
                builder.CloseComponent();
            }));

        new Query(renderState, registry).Select(".fade").FadeTo(TimeSpan.FromMilliseconds(375), 0.25);

        host.WaitForAssertion(() =>
        {
            Assert.Contains("[data-rbq-key=\"fade-target\"] { transition: opacity 375ms ease; }", host.Markup, StringComparison.Ordinal);
            Assert.Contains("[data-rbq-key=\"fade-target\"] { opacity: 0.25; }", host.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void FadeIn_and_FadeOut_render_keyframes_and_animation_from_QueryHost_state()
    {
        using var context = new BunitContext();
        var renderState = new QueryRenderState();
        var registry = new QueryElementRegistry();
        context.Services.AddSingleton<NavigationManager>(new TestNavigationManager());
        context.Services.AddSingleton(renderState);
        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(new QueryMeasurementCoordinator(new TestMeasurementBridge()));
        context.Services.AddSingleton(new QueryFormCoordinator(new TestFormBridge()));

        var host = context.Render<QueryHost>(parameters => parameters
            .Add(component => component.RenderState, renderState)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<QueryElement>(0);
                builder.AddAttribute(1, nameof(QueryElement.ElementKey), "fade-target");
                builder.AddAttribute(2, nameof(QueryElement.Class), "fade");
                builder.CloseComponent();
            }));
        var selection = new Query(renderState, registry).Select(".fade");

        selection.FadeOut(TimeSpan.FromMilliseconds(300));
        host.WaitForAssertion(() =>
        {
            Assert.Contains("@keyframes rbq-fade-in", host.Markup, StringComparison.Ordinal);
            Assert.Contains("@keyframes rbq-fade-out", host.Markup, StringComparison.Ordinal);
            Assert.Contains("[data-rbq-key=\"fade-target\"] { animation: rbq-fade-out 300ms ease both; }", host.Markup, StringComparison.Ordinal);
        });

        selection.FadeIn(TimeSpan.FromMilliseconds(150));
        host.WaitForAssertion(() => Assert.Contains(
            "[data-rbq-key=\"fade-target\"] { animation: rbq-fade-in 150ms ease both; }",
            host.Markup,
            StringComparison.Ordinal));
    }

    [Fact]
    public void FadeToggle_updates_the_rendered_QueryHost_animation_rule()
    {
        using var context = new BunitContext();
        var renderState = new QueryRenderState();
        var registry = new QueryElementRegistry();
        context.Services.AddSingleton<NavigationManager>(new TestNavigationManager());
        context.Services.AddSingleton(renderState);
        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(new QueryMeasurementCoordinator(new TestMeasurementBridge()));
        context.Services.AddSingleton(new QueryFormCoordinator(new TestFormBridge()));

        var host = context.Render<QueryHost>(parameters => parameters
            .Add(component => component.RenderState, renderState)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<QueryElement>(0);
                builder.AddAttribute(1, nameof(QueryElement.ElementKey), "fade-toggle-target");
                builder.AddAttribute(2, nameof(QueryElement.Class), "fade-toggle");
                builder.CloseComponent();
            }));
        var selection = new Query(renderState, registry).Select(".fade-toggle");

        selection.FadeToggle(TimeSpan.FromMilliseconds(225));
        host.WaitForAssertion(() => Assert.Contains(
            "[data-rbq-key=\"fade-toggle-target\"] { animation: rbq-fade-out 225ms ease both; }",
            host.Markup,
            StringComparison.Ordinal));

        selection.FadeToggle(TimeSpan.FromMilliseconds(225));
        host.WaitForAssertion(() => Assert.Contains(
            "[data-rbq-key=\"fade-toggle-target\"] { animation: rbq-fade-in 225ms ease both; }",
            host.Markup,
            StringComparison.Ordinal));
    }

    [Fact]
    public void Animate_renders_transition_and_target_values_through_QueryHost()
    {
        using var context = new BunitContext();
        var renderState = new QueryRenderState();
        var registry = new QueryElementRegistry();
        context.Services.AddSingleton<NavigationManager>(new TestNavigationManager());
        context.Services.AddSingleton(renderState);
        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(new QueryMeasurementCoordinator(new TestMeasurementBridge()));
        context.Services.AddSingleton(new QueryFormCoordinator(new TestFormBridge()));

        var host = context.Render<QueryHost>(parameters => parameters
            .Add(component => component.RenderState, renderState)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<QueryElement>(0);
                builder.AddAttribute(1, nameof(QueryElement.ElementKey), "animated-target");
                builder.AddAttribute(2, nameof(QueryElement.Class), "animated");
                builder.CloseComponent();
            }));

        new Query(renderState, registry).Select(".animated").Animate(
            new Dictionary<string, string> { ["opacity"] = "0.4", ["transform"] = "scale(0.8)" },
            TimeSpan.FromMilliseconds(400));

        host.WaitForAssertion(() =>
        {
            Assert.Contains("[data-rbq-key=\"animated-target\"] { transition: opacity 400ms ease, transform 400ms ease; }", host.Markup, StringComparison.Ordinal);
            Assert.Contains("[data-rbq-key=\"animated-target\"] { opacity: 0.4; }", host.Markup, StringComparison.Ordinal);
            Assert.Contains("[data-rbq-key=\"animated-target\"] { transform: scale(0.8); }", host.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Slide_effects_render_measured_target_and_complete_from_Blazor_animation_event()
    {
        using var context = new BunitContext();
        var renderState = new QueryRenderState();
        var registry = new QueryElementRegistry();
        var measurements = new QueryMeasurementCoordinator(new TestMeasurementBridge(
        [
            new MeasurementReading("slide-parent", MeasurementKind.ScrollHeight, 70),
            new MeasurementReading("slide-child", MeasurementKind.ScrollHeight, 24)
        ]));
        context.Services.AddSingleton<NavigationManager>(new TestNavigationManager());
        context.Services.AddSingleton(renderState);
        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(measurements);
        context.Services.AddSingleton(new QueryFormCoordinator(new TestFormBridge()));

        var host = context.Render<QueryHost>(parameters => parameters
            .Add(component => component.RenderState, renderState)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<QueryElement>(0);
                builder.AddAttribute(1, nameof(QueryElement.ElementKey), "slide-parent");
                builder.AddAttribute(2, nameof(QueryElement.Class), "slide-parent");
                builder.CloseComponent();
                builder.OpenComponent<QueryElement>(4);
                builder.AddAttribute(5, nameof(QueryElement.ElementKey), "slide-child");
                builder.AddAttribute(6, nameof(QueryElement.Class), "slide-child");
                builder.CloseComponent();
            }));
        var query = new Query(renderState, registry, measurements);
        var parent = query.Select(".slide-parent");
        var child = query.Select(".slide-child");
        var parentDrain = parent.SlideUp(TimeSpan.FromMilliseconds(300)).AsTask();
        var childDrain = child.SlideUp(TimeSpan.FromMilliseconds(300)).AsTask();
        host.WaitForAssertion(() =>
        {
            Assert.Contains("@keyframes rbq-slide-up", host.Markup, StringComparison.Ordinal);
            Assert.Contains("[data-rbq-key=\"slide-parent\"] { --rbq-slide-height: 70px; }", host.Markup, StringComparison.Ordinal);
            Assert.Contains("[data-rbq-key=\"slide-child\"] { --rbq-slide-height: 24px; }", host.Markup, StringComparison.Ordinal);
        });

        await host.InvokeAsync(() => host.Find("[data-rbq-key=\"slide-child\"]").TriggerEventAsync("onrbqanimationend", EventArgs.Empty));
        await childDrain.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        Assert.False(parentDrain.IsCompleted);

        await host.InvokeAsync(() => host.Find("[data-rbq-key=\"slide-parent\"]").TriggerEventAsync("onrbqanimationend", EventArgs.Empty));
        await parentDrain.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        Assert.True(renderState.IsHidden("slide-parent"));
        Assert.True(renderState.IsHidden("slide-child"));
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://query.test/", "https://query.test/");
        protected override void NavigateToCore(string uri, bool forceLoad) => Uri = ToAbsoluteUri(uri).ToString();
    }
}
