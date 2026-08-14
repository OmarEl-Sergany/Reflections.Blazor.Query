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
