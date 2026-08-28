using Bunit;
using System;
using Microsoft.Extensions.DependencyInjection;
using Reflections.Blazor.Query;

namespace Reflections.Blazor.Query.ComponentTests;

public sealed class QueryClassesAndStructureComponentTests
{
    [Fact]
    public void QueryElement_renders_owned_class_additions_and_removals_without_external_dom_mutation()
    {
        using var context = new BunitContext();
        var registry = new QueryElementRegistry();
        var renderState = new QueryRenderState();
        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(renderState);
        context.Services.AddSingleton(new QueryMeasurementCoordinator(new TestMeasurementBridge()));

        var cut = context.Render<QueryElement>(parameters => parameters
            .Add(component => component.ElementKey, "card")
            .Add(component => component.TagName, "div")
            .Add(component => component.Class, "card base"));
        var selection = new Query(renderState, registry).Select(".card");

        selection.AddClass("active");
        cut.WaitForAssertion(() => Assert.Contains("class=\"card base active\"", cut.Markup, StringComparison.Ordinal));

        selection.RemoveClass("base");
        cut.WaitForAssertion(() => Assert.Contains("class=\"card active\"", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void QueryElement_hides_via_owned_display_style_and_restores_on_show()
    {
        using var context = new BunitContext();
        var registry = new QueryElementRegistry();
        var renderState = new QueryRenderState();
        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(renderState);
        context.Services.AddSingleton(new QueryMeasurementCoordinator(new TestMeasurementBridge()));

        var cut = context.Render<QueryElement>(parameters => parameters
            .Add(component => component.ElementKey, "banner")
            .Add(component => component.TagName, "div")
            .Add(component => component.Class, "banner"));
        var selection = new Query(renderState, registry).Select(".banner");

        selection.Hide();
        cut.WaitForAssertion(() => Assert.Contains("display: none;", cut.Markup, StringComparison.Ordinal));

        selection.Show();
        cut.WaitForAssertion(() => Assert.DoesNotContain("display: none;", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void QueryElement_renders_prepended_content_before_and_appended_content_after_existing_content()
    {
        using var context = new BunitContext();
        var registry = new QueryElementRegistry();
        var renderState = new QueryRenderState();
        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(renderState);
        context.Services.AddSingleton(new QueryMeasurementCoordinator(new TestMeasurementBridge()));

        var cut = context.Render<QueryElement>(parameters => parameters
            .Add(component => component.ElementKey, "list")
            .Add(component => component.TagName, "div")
            .Add(component => component.Class, "list")
            .AddChildContent("middle"));
        var selection = new Query(renderState, registry).Select(".list");

        selection.Append("after").AppendHtml("<b>trusted-after</b>");
        selection.Prepend("before");

        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            var beforeIndex = markup.IndexOf("before", StringComparison.Ordinal);
            var middleIndex = markup.IndexOf("middle", StringComparison.Ordinal);
            var afterIndex = markup.IndexOf("after", StringComparison.Ordinal);
            var trustedIndex = markup.IndexOf("<b>trusted-after</b>", StringComparison.Ordinal);
            Assert.True(beforeIndex >= 0 && beforeIndex < middleIndex);
            Assert.True(middleIndex < afterIndex);
            Assert.True(afterIndex < trustedIndex);
        });
    }

    [Fact]
    public void QueryElement_renders_nothing_once_removed()
    {
        using var context = new BunitContext();
        var registry = new QueryElementRegistry();
        var renderState = new QueryRenderState();
        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(renderState);
        context.Services.AddSingleton(new QueryMeasurementCoordinator(new TestMeasurementBridge()));

        var cut = context.Render<QueryElement>(parameters => parameters
            .Add(component => component.ElementKey, "toast")
            .Add(component => component.TagName, "div")
            .Add(component => component.Class, "toast"));

        new Query(renderState, registry).Select(".toast").Remove();

        cut.WaitForAssertion(() => Assert.DoesNotContain("data-rbq-key=\"toast\"", cut.Markup, StringComparison.Ordinal));
    }
}
