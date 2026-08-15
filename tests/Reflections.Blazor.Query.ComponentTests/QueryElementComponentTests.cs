using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Reflections.Blazor.Query;

namespace Reflections.Blazor.Query.ComponentTests;

public sealed class QueryElementComponentTests
{
    [Fact]
    public void QueryElement_registers_reregisters_and_unregisters_owned_metadata_through_the_component_lifecycle()
    {
        using var context = new BunitContext();
        var registry = new QueryElementRegistry();
        context.Services.AddSingleton(registry);
        registry.Register("parent-one", ".parent-one");
        registry.Register("parent-two", ".parent-two");

        var cut = context.Render<QueryElement>(parameters => parameters
            .Add(component => component.ElementKey, "child")
            .Add(component => component.ParentElementKey, "parent-one")
            .Add(component => component.TagName, "p")
            .Add(component => component.Class, "child"));

        Assert.Equal(["child"], new Query(new QueryRenderState(), registry).Select("p.child").ToArray());
        Assert.Equal(["parent-one"], new Query(new QueryRenderState(), registry).Select(".child").Parent().ToArray());

        cut.Render(parameters => parameters
            .Add(component => component.ElementKey, "renamed-child")
            .Add(component => component.ParentElementKey, "parent-two")
            .Add(component => component.TagName, "p")
            .Add(component => component.Class, "updated"));

        Assert.Empty(new Query(new QueryRenderState(), registry).Select(".child").ToArray());
        Assert.Equal(["renamed-child"], new Query(new QueryRenderState(), registry).Select("p.updated").ToArray());
        Assert.Equal(["parent-two"], new Query(new QueryRenderState(), registry).Select(".updated").Parent().ToArray());

        context.Dispose();

        Assert.Empty(new Query(new QueryRenderState(), registry).Select(".updated").ToArray());
    }
}
