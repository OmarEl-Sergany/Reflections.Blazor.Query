using Bunit;
using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Reflections.Blazor.Query;

namespace Reflections.Blazor.Query.ComponentTests;

file sealed class HostReparentTestHost : ComponentBase
{
    [Parameter] public string HostParentKey { get; set; } = "p1";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<QueryElement>(0);
        builder.AddComponentParameter(1, nameof(QueryElement.ElementKey), "host");
        builder.AddComponentParameter(2, nameof(QueryElement.TagName), "div");
        builder.AddComponentParameter(3, nameof(QueryElement.Class), "host");
        builder.AddComponentParameter(4, nameof(QueryElement.ParentElementKey), HostParentKey);
        builder.CloseComponent();
        builder.OpenComponent<QueryElement>(5);
        builder.AddComponentParameter(6, nameof(QueryElement.ElementKey), "mover");
        builder.AddComponentParameter(7, nameof(QueryElement.TagName), "span");
        builder.AddComponentParameter(8, nameof(QueryElement.Class), "mover");
        builder.CloseComponent();
    }
}

public sealed class QueryRealElementRelocationComponentTests
{
    private static (BunitContext Context, QueryElementRegistry Registry, QueryRenderState State) CreateContext()
    {
        var context = new BunitContext();
        var registry = new QueryElementRegistry();
        var state = new QueryRenderState();
        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(state);
        context.Services.AddSingleton(new QueryMeasurementCoordinator(new TestMeasurementBridge()));
        context.Services.AddSingleton(new QueryFormCoordinator(new TestFormBridge()));
        return (context, registry, state);
    }

    [Fact]
    public void AppendTo_physically_moves_a_real_Razor_declared_elements_markup_between_hosts()
    {
        var (context, registry, state) = CreateContext();
        using var _ = context;

        var cut = context.Render(builder =>
        {
            builder.OpenComponent<QueryElement>(0);
            builder.AddComponentParameter(1, nameof(QueryElement.ElementKey), "listA");
            builder.AddComponentParameter(2, nameof(QueryElement.TagName), "ul");
            builder.AddComponentParameter(3, nameof(QueryElement.Class), "listA");
            builder.CloseComponent();
            builder.OpenComponent<QueryElement>(4);
            builder.AddComponentParameter(5, nameof(QueryElement.ElementKey), "listB");
            builder.AddComponentParameter(6, nameof(QueryElement.TagName), "ul");
            builder.AddComponentParameter(7, nameof(QueryElement.Class), "listB");
            builder.CloseComponent();
            builder.OpenComponent<QueryElement>(8);
            builder.AddComponentParameter(9, nameof(QueryElement.ElementKey), "mover");
            builder.AddComponentParameter(10, nameof(QueryElement.TagName), "li");
            builder.AddComponentParameter(11, nameof(QueryElement.Class), "mover");
            builder.AddComponentParameter(12, nameof(QueryElement.ChildContent), (RenderFragment)(b => b.AddContent(0, "moving-content")));
            builder.CloseComponent();
        });
        var query = new Query(state, registry);

        // Declaring ParentElementKey only affects registry traversal (Parent()/Children()), never actual
        // DOM nesting for a real, Razor-declared element — it still renders exactly where written above,
        // as a flat sibling of listA/listB, until explicitly relocated.
        cut.WaitForAssertion(() => Assert.Contains("moving-content", cut.Markup, StringComparison.Ordinal));

        query.Select(".mover").AppendTo(query.Select(".listB"));

        cut.WaitForAssertion(() =>
        {
            var listBStart = cut.Markup.IndexOf("class=\"listB\"", StringComparison.Ordinal);
            var listBEnd = cut.Markup.IndexOf("</ul>", listBStart, StringComparison.Ordinal);
            var movingContentIndex = cut.Markup.IndexOf("moving-content", StringComparison.Ordinal);
            Assert.True(movingContentIndex > listBStart && movingContentIndex < listBEnd,
                "Expected 'moving-content' to render inside listB's <ul>...</ul> after AppendTo.");
        });

        Assert.Equal(["listB"], query.Select(".mover").Parent().ToArray());
    }

    [Fact]
    public void Detach_returns_a_relocated_real_element_to_its_own_declared_position()
    {
        var (context, registry, state) = CreateContext();
        using var _ = context;

        var cut = context.Render(builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "home");
            builder.OpenComponent<QueryElement>(2);
            builder.AddComponentParameter(3, nameof(QueryElement.ElementKey), "mover");
            builder.AddComponentParameter(4, nameof(QueryElement.TagName), "span");
            builder.AddComponentParameter(5, nameof(QueryElement.Class), "mover");
            builder.CloseComponent();
            builder.CloseElement();
            builder.OpenComponent<QueryElement>(6);
            builder.AddComponentParameter(7, nameof(QueryElement.ElementKey), "host");
            builder.AddComponentParameter(8, nameof(QueryElement.TagName), "div");
            builder.AddComponentParameter(9, nameof(QueryElement.Class), "host");
            builder.CloseComponent();
        });
        var query = new Query(state, registry);

        query.Select(".mover").AppendTo(query.Select(".host"));
        cut.WaitForAssertion(() =>
        {
            var homeMarkup = cut.Markup[..cut.Markup.IndexOf("</div>", StringComparison.Ordinal)];
            Assert.DoesNotContain("class=\"mover\"", homeMarkup, StringComparison.Ordinal);
        });

        query.Select(".mover").Detach();

        cut.WaitForAssertion(() =>
        {
            var homeMarkup = cut.Markup[..cut.Markup.IndexOf("</div>", StringComparison.Ordinal)];
            Assert.Contains("class=\"mover\"", homeMarkup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Removing_a_virtual_container_host_springs_its_relocated_real_children_back()
    {
        var (context, registry, state) = CreateContext();
        using var _ = context;

        var cut = context.Render(builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "home");
            builder.OpenComponent<QueryElement>(2);
            builder.AddComponentParameter(3, nameof(QueryElement.ElementKey), "mover");
            builder.AddComponentParameter(4, nameof(QueryElement.TagName), "span");
            builder.AddComponentParameter(5, nameof(QueryElement.Class), "mover");
            builder.CloseComponent();
            builder.CloseElement();
            builder.OpenComponent<QueryElement>(6);
            builder.AddComponentParameter(7, nameof(QueryElement.ElementKey), "root");
            builder.AddComponentParameter(8, nameof(QueryElement.TagName), "div");
            builder.AddComponentParameter(9, nameof(QueryElement.Class), "root");
            builder.CloseComponent();
        });
        var query = new Query(state, registry);
        var host = query.Select(".root").AppendElement("div", "host");

        query.Select(".mover").AppendTo(host);
        cut.WaitForAssertion(() =>
        {
            var homeMarkup = cut.Markup[..cut.Markup.IndexOf("</div>", StringComparison.Ordinal)];
            Assert.DoesNotContain("class=\"mover\"", homeMarkup, StringComparison.Ordinal);
        });

        host.Remove();

        cut.WaitForAssertion(() =>
        {
            var homeMarkup = cut.Markup[..cut.Markup.IndexOf("</div>", StringComparison.Ordinal)];
            Assert.Contains("class=\"mover\"", homeMarkup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void AppendTo_throws_when_moving_an_element_into_its_own_descendant()
    {
        var (context, registry, state) = CreateContext();
        using var _ = context;

        context.Render(builder =>
        {
            builder.OpenComponent<QueryElement>(0);
            builder.AddComponentParameter(1, nameof(QueryElement.ElementKey), "parent");
            builder.AddComponentParameter(2, nameof(QueryElement.TagName), "div");
            builder.AddComponentParameter(3, nameof(QueryElement.Class), "parent");
            builder.CloseComponent();
        });
        var query = new Query(state, registry);
        var child = query.Select(".parent").AppendElement("div", "child");

        Assert.Throws<InvalidOperationException>(() => query.Select(".parent").AppendTo(child));
    }

    [Fact]
    public void Wrap_inserts_a_new_wrapper_around_a_real_element_and_reparents_it()
    {
        var (context, registry, state) = CreateContext();
        using var _ = context;

        var cut = context.Render(builder =>
        {
            builder.OpenComponent<QueryElement>(0);
            builder.AddComponentParameter(1, nameof(QueryElement.ElementKey), "root");
            builder.AddComponentParameter(2, nameof(QueryElement.TagName), "div");
            builder.AddComponentParameter(3, nameof(QueryElement.Class), "root");
            builder.CloseComponent();
            builder.OpenComponent<QueryElement>(4);
            builder.AddComponentParameter(5, nameof(QueryElement.ElementKey), "item");
            builder.AddComponentParameter(6, nameof(QueryElement.TagName), "span");
            builder.AddComponentParameter(7, nameof(QueryElement.Class), "item");
            builder.AddComponentParameter(8, nameof(QueryElement.ParentElementKey), "root");
            builder.CloseComponent();
        });
        var query = new Query(state, registry);

        var wrapper = query.Select(".item").Wrap("figure", "wrapper");

        cut.WaitForAssertion(() => Assert.Contains("<figure", cut.Markup, StringComparison.Ordinal));
        Assert.Equal(wrapper.ToArray(), query.Select(".item").Parent().ToArray());
        Assert.Equal(["root"], wrapper.Parent().ToArray());
    }

    [Fact]
    public void Before_and_After_move_a_real_element_relative_to_another_real_anchor()
    {
        var (context, registry, state) = CreateContext();
        using var _ = context;

        var cut = context.Render(builder =>
        {
            builder.OpenComponent<QueryElement>(0);
            builder.AddComponentParameter(1, nameof(QueryElement.ElementKey), "root");
            builder.AddComponentParameter(2, nameof(QueryElement.TagName), "div");
            builder.AddComponentParameter(3, nameof(QueryElement.Class), "root");
            builder.CloseComponent();
            builder.OpenComponent<QueryElement>(4);
            builder.AddComponentParameter(5, nameof(QueryElement.ElementKey), "anchor");
            builder.AddComponentParameter(6, nameof(QueryElement.TagName), "span");
            builder.AddComponentParameter(7, nameof(QueryElement.Class), "anchor");
            builder.AddComponentParameter(8, nameof(QueryElement.ParentElementKey), "root");
            builder.CloseComponent();
            builder.OpenComponent<QueryElement>(9);
            builder.AddComponentParameter(10, nameof(QueryElement.ElementKey), "mover");
            builder.AddComponentParameter(11, nameof(QueryElement.TagName), "span");
            builder.AddComponentParameter(12, nameof(QueryElement.Class), "mover");
            builder.CloseComponent();
        });
        var query = new Query(state, registry);

        query.Select(".anchor").Before(query.Select(".mover"));

        cut.WaitForAssertion(() =>
        {
            var moverIndex = cut.Markup.IndexOf("class=\"mover\"", StringComparison.Ordinal);
            var anchorIndex = cut.Markup.IndexOf("class=\"anchor\"", StringComparison.Ordinal);
            Assert.True(moverIndex >= 0 && moverIndex < anchorIndex);
        });
        Assert.Equal(["root"], query.Select(".mover").Parent().ToArray());
    }

    [Fact]
    public void ReplaceWith_moves_a_real_element_to_the_target_position_without_duplicate_markup()
    {
        var (context, registry, state) = CreateContext();
        using var _ = context;

        var cut = context.Render(builder =>
        {
            builder.OpenComponent<QueryElement>(0);
            builder.AddComponentParameter(1, nameof(QueryElement.ElementKey), "root");
            builder.AddComponentParameter(2, nameof(QueryElement.TagName), "div");
            builder.AddComponentParameter(3, nameof(QueryElement.Class), "root");
            builder.CloseComponent();
            builder.OpenComponent<QueryElement>(4);
            builder.AddComponentParameter(5, nameof(QueryElement.ElementKey), "target");
            builder.AddComponentParameter(6, nameof(QueryElement.TagName), "span");
            builder.AddComponentParameter(7, nameof(QueryElement.Class), "target");
            builder.AddComponentParameter(8, nameof(QueryElement.ParentElementKey), "root");
            builder.AddComponentParameter(9, nameof(QueryElement.ChildContent), (RenderFragment)(b => b.AddContent(0, "target-content")));
            builder.CloseComponent();
            builder.OpenComponent<QueryElement>(10);
            builder.AddComponentParameter(11, nameof(QueryElement.ElementKey), "replacement");
            builder.AddComponentParameter(12, nameof(QueryElement.TagName), "span");
            builder.AddComponentParameter(13, nameof(QueryElement.Class), "replacement");
            builder.AddComponentParameter(14, nameof(QueryElement.ChildContent), (RenderFragment)(b => b.AddContent(0, "replacement-content")));
            builder.CloseComponent();
        });
        var query = new Query(state, registry);

        query.Select(".target").ReplaceWith(query.Select(".replacement"));

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("target-content", cut.Markup, StringComparison.Ordinal);
            Assert.Equal(1, CountOccurrences(cut.Markup, "replacement-content"));
            var rootStart = cut.Markup.IndexOf("class=\"root\"", StringComparison.Ordinal);
            var rootEnd = cut.Markup.IndexOf("</div>", rootStart, StringComparison.Ordinal);
            var replacementIndex = cut.Markup.IndexOf("replacement-content", StringComparison.Ordinal);
            Assert.True(replacementIndex > rootStart && replacementIndex < rootEnd);
        });
    }

    [Fact]
    public void WrapAll_moves_real_siblings_under_one_generated_wrapper_without_duplicate_markup()
    {
        var (context, registry, state) = CreateContext();
        using var _ = context;

        var cut = context.Render(builder =>
        {
            builder.OpenComponent<QueryElement>(0);
            builder.AddComponentParameter(1, nameof(QueryElement.ElementKey), "root");
            builder.AddComponentParameter(2, nameof(QueryElement.TagName), "div");
            builder.AddComponentParameter(3, nameof(QueryElement.Class), "root");
            builder.CloseComponent();
            builder.OpenComponent<QueryElement>(4);
            builder.AddComponentParameter(5, nameof(QueryElement.ElementKey), "first");
            builder.AddComponentParameter(6, nameof(QueryElement.TagName), "span");
            builder.AddComponentParameter(7, nameof(QueryElement.Class), "item");
            builder.AddComponentParameter(8, nameof(QueryElement.ParentElementKey), "root");
            builder.AddComponentParameter(9, nameof(QueryElement.ChildContent), (RenderFragment)(b => b.AddContent(0, "first-content")));
            builder.CloseComponent();
            builder.OpenComponent<QueryElement>(10);
            builder.AddComponentParameter(11, nameof(QueryElement.ElementKey), "second");
            builder.AddComponentParameter(12, nameof(QueryElement.TagName), "span");
            builder.AddComponentParameter(13, nameof(QueryElement.Class), "item");
            builder.AddComponentParameter(14, nameof(QueryElement.ParentElementKey), "root");
            builder.AddComponentParameter(15, nameof(QueryElement.ChildContent), (RenderFragment)(b => b.AddContent(0, "second-content")));
            builder.CloseComponent();
        });
        var query = new Query(state, registry);

        var wrapper = query.Select(".item").WrapAll("section", "wrapper");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, CountOccurrences(cut.Markup, "class=\"wrapper\""));
            Assert.Equal(1, CountOccurrences(cut.Markup, "first-content"));
            Assert.Equal(1, CountOccurrences(cut.Markup, "second-content"));
            var wrapperStart = cut.Markup.IndexOf("class=\"wrapper\"", StringComparison.Ordinal);
            var wrapperEnd = cut.Markup.IndexOf("</section>", wrapperStart, StringComparison.Ordinal);
            var firstIndex = cut.Markup.IndexOf("first-content", StringComparison.Ordinal);
            var secondIndex = cut.Markup.IndexOf("second-content", StringComparison.Ordinal);
            Assert.True(firstIndex > wrapperStart && firstIndex < secondIndex && secondIndex < wrapperEnd);
        });

        Assert.Equal(wrapper.ToArray(), query.Select(".item").Parent().ToArray());
    }

    [Fact]
    public void Reparenting_a_real_element_via_an_ordinary_parameter_change_does_not_disturb_children_relocated_onto_it()
    {
        var (context, registry, state) = CreateContext();
        using var _ = context;

        var cut = context.Render<HostReparentTestHost>(parameters => parameters
            .Add(c => c.HostParentKey, "p1"));
        var query = new Query(state, registry);
        query.Select(".mover").AppendTo(query.Select(".host"));
        cut.WaitForAssertion(() => AssertMoverIsNestedInsideHost(cut.Markup));

        // An ordinary Razor-driven reparent of the host (ParentElementKey changing — nothing to do with
        // the library's relocation API) must not be treated like the host being destroyed: "mover" must
        // stay nested inside "host", not silently spring back to its own (here: top-level) position.
        cut.Render(parameters => parameters.Add(c => c.HostParentKey, "p2"));

        cut.WaitForAssertion(() => AssertMoverIsNestedInsideHost(cut.Markup));

        // The reparent must not have unregistered "mover" as a real element either: a further structural
        // operation against it should still succeed rather than throwing "not a mounted element".
        var appended = query.Select(".mover").AppendElement("i", "noop");
        Assert.Single(appended.ToArray());

        static void AssertMoverIsNestedInsideHost(string markup)
        {
            var hostStart = markup.IndexOf("class=\"host\"", StringComparison.Ordinal);
            var hostEnd = markup.IndexOf("</div>", hostStart, StringComparison.Ordinal);
            var moverIndex = markup.IndexOf("class=\"mover\"", StringComparison.Ordinal);
            Assert.True(moverIndex > hostStart && moverIndex < hostEnd,
                "Expected 'mover' to still render nested inside 'host' after host's ordinary reparent.");
        }
    }

    [Fact]
    public void Before_throws_instead_of_silently_discarding_placements_when_the_anchor_selection_has_multiple_elements()
    {
        var (context, registry, state) = CreateContext();
        using var _ = context;

        context.Render(builder =>
        {
            builder.OpenComponent<QueryElement>(0);
            builder.AddComponentParameter(1, nameof(QueryElement.ElementKey), "root");
            builder.AddComponentParameter(2, nameof(QueryElement.TagName), "div");
            builder.AddComponentParameter(3, nameof(QueryElement.Class), "root");
            builder.CloseComponent();
            builder.OpenComponent<QueryElement>(4);
            builder.AddComponentParameter(5, nameof(QueryElement.ElementKey), "rowA");
            builder.AddComponentParameter(6, nameof(QueryElement.TagName), "span");
            builder.AddComponentParameter(7, nameof(QueryElement.Class), "row");
            builder.AddComponentParameter(8, nameof(QueryElement.ParentElementKey), "root");
            builder.CloseComponent();
            builder.OpenComponent<QueryElement>(9);
            builder.AddComponentParameter(10, nameof(QueryElement.ElementKey), "rowB");
            builder.AddComponentParameter(11, nameof(QueryElement.TagName), "span");
            builder.AddComponentParameter(12, nameof(QueryElement.Class), "row");
            builder.AddComponentParameter(13, nameof(QueryElement.ParentElementKey), "root");
            builder.CloseComponent();
            builder.OpenComponent<QueryElement>(14);
            builder.AddComponentParameter(15, nameof(QueryElement.ElementKey), "mover");
            builder.AddComponentParameter(16, nameof(QueryElement.TagName), "span");
            builder.AddComponentParameter(17, nameof(QueryElement.Class), "mover");
            builder.CloseComponent();
        });
        var query = new Query(state, registry);

        Assert.Throws<InvalidOperationException>(() => query.Select(".row").Before(query.Select(".mover")));
    }

    [Fact]
    public async System.Threading.Tasks.Task AppendTo_from_a_background_thread_does_not_throw_and_converges_correctly()
    {
        var (context, registry, state) = CreateContext();
        using var _ = context;

        var cut = context.Render(builder =>
        {
            builder.OpenComponent<QueryElement>(0);
            builder.AddComponentParameter(1, nameof(QueryElement.ElementKey), "listA");
            builder.AddComponentParameter(2, nameof(QueryElement.TagName), "ul");
            builder.AddComponentParameter(3, nameof(QueryElement.Class), "listA");
            builder.CloseComponent();
            builder.OpenComponent<QueryElement>(4);
            builder.AddComponentParameter(5, nameof(QueryElement.ElementKey), "listB");
            builder.AddComponentParameter(6, nameof(QueryElement.TagName), "ul");
            builder.AddComponentParameter(7, nameof(QueryElement.Class), "listB");
            builder.CloseComponent();
            builder.OpenComponent<QueryElement>(8);
            builder.AddComponentParameter(9, nameof(QueryElement.ElementKey), "mover");
            builder.AddComponentParameter(10, nameof(QueryElement.TagName), "li");
            builder.AddComponentParameter(11, nameof(QueryElement.Class), "mover");
            builder.CloseComponent();
        });
        var query = new Query(state, registry);

        // Simulates a relocation triggered from a background service / timer / SignalR callback rather
        // than a UI event handler — the exact scenario the dispatch fix (QueryRenderState.RegisterDispatch,
        // wired from QueryHost/here via InvokeAsync) exists to make safe. Before that fix, mutating state
        // off the renderer's thread could either throw or leave the source/host pair out of sync across
        // two separate render batches.
        await System.Threading.Tasks.Task.Run(() => query.Select(".mover").AppendTo(query.Select(".listB")));

        cut.WaitForAssertion(() =>
        {
            var listBStart = cut.Markup.IndexOf("class=\"listB\"", StringComparison.Ordinal);
            var listBEnd = cut.Markup.IndexOf("</ul>", listBStart, StringComparison.Ordinal);
            var moverIndex = cut.Markup.IndexOf("class=\"mover\"", StringComparison.Ordinal);
            Assert.True(moverIndex > listBStart && moverIndex < listBEnd);
        });
    }

    private static int CountOccurrences(string value, string fragment)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(fragment, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += fragment.Length;
        }
        return count;
    }
}
