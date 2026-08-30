using Reflections.Blazor.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Reflections.Blazor.Query.Tests;

public sealed class QueryFormSerializationTests
{
    [Fact]
    public async Task Duplicate_requests_coalesce_into_one_batch_and_cache_the_snapshot()
    {
        var bridge = new FakeFormBridge(
        [
            new SerializationResult("form", [new SerializedField("q", "a b")])
        ]);
        await using var coordinator = new QueryFormCoordinator(bridge);
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("form", "form");
        var notifications = 0;
        coordinator.Requested += () => notifications++;

        var first = coordinator.RequestArrayAsync(state, ["form"]).AsTask();
        var duplicate = coordinator.RequestArrayAsync(state, ["form"]).AsTask();

        Assert.Equal(1, notifications);
        Assert.False(first.IsCompleted);
        await coordinator.FlushAsync();

        Assert.Equal(new[] { new SerializedField("q", "a b") }, await first);
        Assert.Equal(new[] { new SerializedField("q", "a b") }, await duplicate);
        Assert.Single(bridge.Batches);
        Assert.Equal(["form"], bridge.Batches[0].Select(r => r.ElementKey));

        Assert.Equal(new[] { new SerializedField("q", "a b") }, await coordinator.RequestArrayAsync(state, ["form"]));
        Assert.Single(bridge.Batches);
    }

    [Fact]
    public async Task RequestArray_concatenates_fields_across_ordered_element_keys()
    {
        var bridge = new FakeFormBridge(
        [
            new SerializationResult("a", [new SerializedField("x", "1")]),
            new SerializationResult("b", [new SerializedField("y", "2"), new SerializedField("z", "3")])
        ]);
        await using var coordinator = new QueryFormCoordinator(bridge);
        var state = new QueryRenderState();

        var fields = coordinator.RequestArrayAsync(state, ["a", "b"]).AsTask();
        await coordinator.FlushAsync();

        Assert.Equal(
            [new SerializedField("x", "1"), new SerializedField("y", "2"), new SerializedField("z", "3")],
            await fields);
        Assert.Single(bridge.Batches);
        Assert.Equal(["a", "b"], bridge.Batches[0].Select(r => r.ElementKey));
    }

    [Fact]
    public async Task Invalidation_isolated_by_render_state_and_element_key()
    {
        var bridge = new FakeFormBridge(
        [
            new SerializationResult("one", [new SerializedField("q", "1")]),
            new SerializationResult("two", [new SerializedField("q", "2")])
        ]);
        await using var coordinator = new QueryFormCoordinator(bridge);
        var page = new QueryRenderState();
        var layout = new LayoutQueryRenderState();

        var pageOne = coordinator.RequestArrayAsync(page, ["one"]).AsTask();
        var pageTwo = coordinator.RequestArrayAsync(page, ["two"]).AsTask();
        var layoutOne = coordinator.RequestArrayAsync(layout, ["one"]).AsTask();
        await coordinator.FlushAsync();
        await Task.WhenAll(pageOne, pageTwo, layoutOne);

        coordinator.InvalidateElement(page, "one");
        var refreshed = coordinator.RequestArrayAsync(page, ["one"]).AsTask();
        Assert.Equal([new SerializedField("q", "2")], await coordinator.RequestArrayAsync(page, ["two"]));
        Assert.Equal([new SerializedField("q", "1")], await coordinator.RequestArrayAsync(layout, ["one"]));
        await coordinator.FlushAsync();

        Assert.Equal([new SerializedField("q", "1")], await refreshed);
        Assert.Equal(2, bridge.Batches.Count);
        Assert.Single(bridge.Batches[1]);
    }

    [Fact]
    public async Task Navigation_or_element_disposal_cancels_pending_without_late_cache_population()
    {
        var bridge = new FakeFormBridge([new SerializationResult("form", [new SerializedField("q", "1")])]);
        await using var coordinator = new QueryFormCoordinator(bridge);
        var state = new QueryRenderState();

        var pending = coordinator.RequestArrayAsync(state, ["form"]).AsTask();
        coordinator.InvalidateElement(state, "form");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        await coordinator.FlushAsync();
        Assert.Empty(bridge.Batches);
    }

    [Fact]
    public async Task Query_selection_SerializeArray_returns_fields_for_resolved_elements()
    {
        var bridge = new FakeFormBridge(
        [
            new SerializationResult("form", [new SerializedField("q", "hello")])
        ]);
        await using var coordinator = new QueryFormCoordinator(bridge);
        var registry = new QueryElementRegistry();
        registry.Register("form", "form");
        var selection = new Query(new QueryRenderState(), registry, forms: coordinator).Select("form");

        var fields = selection.SerializeArrayAsync().AsTask();
        await coordinator.FlushAsync();

        Assert.Equal([new SerializedField("q", "hello")], await fields);
    }

    [Fact]
    public async Task Query_selection_Serialize_builds_url_encoded_query_string()
    {
        var bridge = new FakeFormBridge(
        [
            new SerializationResult("form",
            [
                new SerializedField("q", "a b"),
                new SerializedField("tag", "c#"),
                new SerializedField("name", "J&R")
            ])
        ]);
        await using var coordinator = new QueryFormCoordinator(bridge);
        var registry = new QueryElementRegistry();
        registry.Register("form", "form");
        var selection = new Query(new QueryRenderState(), registry, forms: coordinator).Select("form");

        var query = selection.SerializeAsync().AsTask();
        await coordinator.FlushAsync();

        Assert.Equal("q=a+b&tag=c%23&name=J%26R", await query);
    }

    [Fact]
    public async Task Empty_selection_returns_empty_fields_without_a_batch()
    {
        var bridge = new FakeFormBridge([]);
        await using var coordinator = new QueryFormCoordinator(bridge);
        var registry = new QueryElementRegistry();
        var selection = new Query(new QueryRenderState(), registry, forms: coordinator).Select(".missing");

        var fields = await selection.SerializeArrayAsync();
        var query = await selection.SerializeAsync();

        Assert.Empty(fields);
        Assert.Equal(string.Empty, query);
        Assert.Empty(bridge.Batches);
    }

    private sealed class FakeFormBridge(params IReadOnlyList<SerializationResult> results) : IQueryFormBridge
    {
        public List<IReadOnlyList<SerializationRequest>> Batches { get; } = [];

        public ValueTask<IReadOnlyList<SerializationResult>> SerializeAsync(
            IReadOnlyList<SerializationRequest> requests,
            CancellationToken cancellationToken)
        {
            Batches.Add(requests.ToArray());
            return ValueTask.FromResult(results);
        }
    }
}
