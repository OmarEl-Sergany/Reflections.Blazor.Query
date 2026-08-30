using Bunit;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Reflections.Blazor.Query;
using Reflections.Blazor.Query.Components;

namespace Reflections.Blazor.Query.ComponentTests;

public sealed class QueryFormSerializationComponentTests
{
    [Fact]
    public async Task QueryHost_flushes_queued_form_serialization_after_a_render()
    {
        using var context = new BunitContext();
        var navigation = new TestNavigationManager();
        var pageState = new QueryRenderState();
        var bridge = new TestFormBridge(
        [
            new SerializationResult("form", [new SerializedField("q", "hello")])
        ]);
        var forms = new QueryFormCoordinator(bridge);
        context.Services.AddSingleton<NavigationManager>(navigation);
        context.Services.AddSingleton<QueryRenderState>(pageState);
        context.Services.AddSingleton(new QueryMeasurementCoordinator(new TestMeasurementBridge()));
        context.Services.AddSingleton(forms);
        context.Services.AddSingleton(new QueryElementRegistry());

        context.Render<QueryHost>(parameters => parameters
            .Add(component => component.RenderState, pageState)
            .AddChildContent("<main>Page content</main>"));

        var fields = forms.RequestArrayAsync(
            pageState,
            ["form"],
            Xunit.TestContext.Current.CancellationToken).AsTask();

        await Task.WhenAny(fields, Task.Delay(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken));

        Assert.True(fields.IsCompletedSuccessfully);
        Assert.Equal([new SerializedField("q", "hello")], await fields);
        Assert.Single(bridge.Batches);
    }

    [Fact]
    public async Task QueryElement_renders_and_its_form_state_can_be_serialized_and_invalidated_on_dispose()
    {
        using var context = new BunitContext();
        var registry = new QueryElementRegistry();
        var renderState = new QueryRenderState();
        var bridge = new TestFormBridge(
        [
            new SerializationResult("input", [new SerializedField("q", "v")])
        ]);
        var forms = new QueryFormCoordinator(bridge);
        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(renderState);
        context.Services.AddSingleton(new QueryMeasurementCoordinator(new TestMeasurementBridge()));
        context.Services.AddSingleton(forms);

        var cut = context.Render<QueryElement>(parameters => parameters
            .Add(component => component.ElementKey, "input")
            .Add(component => component.TagName, "input")
            .Add(component => component.AdditionalAttributes, new Dictionary<string, object> { ["name"] = "q" }));

        var first = forms.RequestArrayAsync(
            renderState,
            ["input"],
            Xunit.TestContext.Current.CancellationToken).AsTask();
        await forms.FlushAsync(Xunit.TestContext.Current.CancellationToken);
        Assert.Equal([new SerializedField("q", "v")], await first);
        Assert.Single(bridge.Batches);

        cut.Instance.Dispose();
        forms.InvalidateElement(renderState, "input");

        var second = forms.RequestArrayAsync(
            renderState,
            ["input"],
            Xunit.TestContext.Current.CancellationToken).AsTask();
        await forms.FlushAsync(Xunit.TestContext.Current.CancellationToken);
        Assert.Equal([new SerializedField("q", "v")], await second);
        Assert.Equal(2, bridge.Batches.Count);
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://query.test/", "https://query.test/");

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            Uri = ToAbsoluteUri(uri).AbsoluteUri;
            NotifyLocationChanged(isInterceptedLink: false);
        }
    }
}
