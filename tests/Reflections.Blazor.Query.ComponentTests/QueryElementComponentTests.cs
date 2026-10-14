using Bunit;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Reflections.Blazor.Query;

namespace Reflections.Blazor.Query.ComponentTests;

public sealed class QueryElementComponentTests
{
    [Fact]
    public async Task QueryElement_registers_reregisters_and_unregisters_owned_metadata_through_the_component_lifecycle()
    {
        using var context = new BunitContext();
        var registry = new QueryElementRegistry();
        var renderState = new QueryRenderState();
        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(renderState);
        context.Services.AddSingleton(new QueryMeasurementCoordinator(new TestMeasurementBridge()));
        context.Services.AddSingleton(new QueryFormCoordinator(new TestFormBridge()));
        registry.Register("parent-one", ".parent-one");
        registry.Register("parent-two", ".parent-two");

        var cut = context.Render<QueryElement>(parameters => parameters
            .Add(component => component.ElementKey, "child")
            .Add(component => component.ParentElementKey, "parent-one")
            .Add(component => component.TagName, "p")
            .Add(component => component.Class, "child"));

        Assert.Equal(["child"], new Query(new QueryRenderState(), registry).Select("p.child").ToArray());
        Assert.Equal(["parent-one"], new Query(new QueryRenderState(), registry).Select(".child").Parent().ToArray());

        new Query(renderState, registry).Select(".child").Attr("title", "registered");
        cut.WaitForAssertion(() => Assert.Contains("title=\"registered\"", cut.Markup, StringComparison.Ordinal));

        new Query(renderState, registry).Select(".child").Prop("hidden", true);
        cut.WaitForAssertion(() => Assert.Contains("hidden", cut.Markup, StringComparison.Ordinal));

        cut.Render(parameters => parameters
            .Add(component => component.ElementKey, "renamed-child")
            .Add(component => component.ParentElementKey, "parent-two")
            .Add(component => component.TagName, "p")
            .Add(component => component.Class, "updated"));

        Assert.Empty(new Query(new QueryRenderState(), registry).Select(".child").ToArray());
        Assert.Equal(["renamed-child"], new Query(new QueryRenderState(), registry).Select("p.updated").ToArray());
        Assert.Equal(["parent-two"], new Query(new QueryRenderState(), registry).Select(".updated").Parent().ToArray());

        await context.DisposeAsync();

        Assert.Empty(new Query(new QueryRenderState(), registry).Select(".updated").ToArray());
    }

    [Fact]
    public void QueryElement_renders_owned_text_html_and_value_without_external_dom_mutation()
    {
        using var context = new BunitContext();
        var registry = new QueryElementRegistry();
        var renderState = new QueryRenderState();
        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(renderState);
        context.Services.AddSingleton(new QueryMeasurementCoordinator(new TestMeasurementBridge()));
        context.Services.AddSingleton(new QueryFormCoordinator(new TestFormBridge()));

        var cut = context.Render<QueryElement>(parameters => parameters
            .Add(component => component.ElementKey, "input")
            .Add(component => component.TagName, "input")
            .Add(component => component.Class, "field"));
        var selection = new Query(renderState, registry).Select(".field");

        selection.Val("chosen");
        cut.WaitForAssertion(() => Assert.Contains("value=\"chosen\"", cut.Markup, StringComparison.Ordinal));

        cut.Render(parameters => parameters
            .Add(component => component.ElementKey, "content")
            .Add(component => component.TagName, "div")
            .Add(component => component.Class, "content")
            .AddChildContent("Initial"));
        selection = new Query(renderState, registry).Select(".content");
        selection.Text("<strong>encoded</strong>");
        cut.WaitForAssertion(() => Assert.Contains("&lt;strong&gt;encoded&lt;/strong&gt;", cut.Markup, StringComparison.Ordinal));

        selection.Html("<strong>trusted</strong>");
        cut.WaitForAssertion(() => Assert.Contains("<strong>trusted</strong>", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void Disposing_QueryElement_releases_its_untyped_typed_and_delegated_event_registrations()
    {
        using var context = new BunitContext();
        var registry = new QueryElementRegistry();
        var renderState = new QueryRenderState();
        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(renderState);
        context.Services.AddSingleton(new QueryMeasurementCoordinator(new TestMeasurementBridge()));
        context.Services.AddSingleton(new QueryFormCoordinator(new TestFormBridge()));
        var cut = context.Render<QueryElement>(parameters => parameters
            .Add(component => component.ElementKey, "root")
            .Add(component => component.TagName, "div")
            .Add(component => component.Class, "root"));
        var selection = new Query(renderState, registry).Select(".root");
        selection.On("click", () => { }).On<Microsoft.AspNetCore.Components.Web.MouseEventArgs>("click", _ => { }).DelegateJS(".item", "click", _ => { });

        cut.Instance.Dispose();

        Assert.Empty(renderState.EventNamesFor("root"));
        Assert.Empty(renderState.DelegatedEventNamesFor("root"));
    }
}
