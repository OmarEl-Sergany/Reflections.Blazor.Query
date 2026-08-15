using System;
using Reflections.Blazor.Query;

namespace Reflections.Blazor.Query.Tests;

public sealed class QueryElementRegistryTests
{
    [Fact]
    public void Registration_resolves_aliases_in_render_order()
    {
        var registry = new QueryElementRegistry();
        registry.Register("one", "p", ".item");
        registry.Register("two", "p", ".item", ".active");

        Assert.Equal(["one", "two"], registry.Resolve(".item"));
        Assert.Equal(["two"], registry.Resolve(".active"));
    }

    [Fact]
    public void Registration_resolves_supported_compound_tag_id_and_class_selectors()
    {
        var registry = new QueryElementRegistry();
        registry.Register("one", "p", "#first", ".item", ".active");
        registry.Register("two", "p", ".item");

        Assert.Equal(["one"], registry.Resolve("p.item.active"));
        Assert.Equal(["one"], registry.Resolve("#first.item"));
        Assert.Equal(["one", "two"], registry.Resolve("p.item"));
        Assert.Equal(["one", "two"], registry.Resolve("*"));
        Assert.Empty(registry.Resolve("div.item"));
        Assert.Empty(registry.Resolve("p > .item"));
    }

    [Fact]
    public void Unregister_removes_disposed_element_metadata()
    {
        var registry = new QueryElementRegistry();
        registry.Register("one", ".item");

        Assert.True(registry.Unregister("one"));
        Assert.False(registry.Unregister("one"));
        Assert.Empty(registry.Resolve(".item"));
    }

    [Fact]
    public void Duplicate_keys_are_rejected()
    {
        var registry = new QueryElementRegistry();
        registry.Register("one", ".item");

        Assert.Throws<InvalidOperationException>(() => registry.Register("one", ".other"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Registration_rejects_empty_keys_and_selectors(string value)
    {
        var registry = new QueryElementRegistry();

        Assert.Throws<ArgumentException>(() => registry.Register(value, ".item"));
        Assert.Throws<ArgumentException>(() => registry.Register("one", value));
    }
}
