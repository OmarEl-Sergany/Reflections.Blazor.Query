using Reflections.Blazor.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Reflections.Blazor.Query.Tests;

public sealed class QueryMeasurementCoordinatorTests
{
    [Fact]
    public async Task Duplicate_requests_coalesce_into_one_post_render_batch_and_cache_the_snapshot()
    {
        var bridge = new FakeMeasurementBridge([new MeasurementReading("panel", MeasurementKind.Height, 42)]);
        await using var coordinator = new QueryMeasurementCoordinator(bridge);
        var state = new QueryRenderState();
        var notifications = 0;
        coordinator.Requested += () => notifications++;

        var first = coordinator.RequestAsync(state, "panel", MeasurementKind.Height).AsTask();
        var duplicate = coordinator.RequestAsync(state, "panel", MeasurementKind.Height).AsTask();

        Assert.Equal(1, notifications);
        Assert.False(first.IsCompleted);
        await coordinator.FlushAsync();

        Assert.Equal(42, await first);
        Assert.Equal(42, await duplicate);
        Assert.Single(bridge.Batches);
        Assert.Single(bridge.Batches[0]);

        Assert.Equal(42, await coordinator.RequestAsync(state, "panel", MeasurementKind.Height));
        Assert.Single(bridge.Batches);
    }

    [Fact]
    public async Task Invalidation_isolated_by_render_state_and_element_key()
    {
        var bridge = new FakeMeasurementBridge(
        [
            new MeasurementReading("one", MeasurementKind.Width, 10),
            new MeasurementReading("two", MeasurementKind.Width, 20),
            new MeasurementReading("layout-one", MeasurementKind.Width, 10)
        ]);
        await using var coordinator = new QueryMeasurementCoordinator(bridge);
        var page = new QueryRenderState();
        var layout = new LayoutQueryRenderState();

        var pageOne = coordinator.RequestAsync(page, "one", MeasurementKind.Width).AsTask();
        var pageTwo = coordinator.RequestAsync(page, "two", MeasurementKind.Width).AsTask();
        var layoutOne = coordinator.RequestAsync(layout, "layout-one", MeasurementKind.Width).AsTask();
        await coordinator.FlushAsync();
        await Task.WhenAll(pageOne, pageTwo, layoutOne);

        coordinator.InvalidateElement(page, "one");
        var refreshedPageOne = coordinator.RequestAsync(page, "one", MeasurementKind.Width).AsTask();
        Assert.Equal(20, await coordinator.RequestAsync(page, "two", MeasurementKind.Width));
        Assert.Equal(10, await coordinator.RequestAsync(layout, "layout-one", MeasurementKind.Width));
        await coordinator.FlushAsync();

        Assert.Equal(10, await refreshedPageOne);
        Assert.Equal(2, bridge.Batches.Count);
        Assert.Single(bridge.Batches[1]);
    }

    [Fact]
    public async Task Application_scope_measurements_survive_page_and_layout_invalidation_until_application_invalidation()
    {
        var bridge = new ApplicationScopeMeasurementBridge();
        await using var coordinator = new QueryMeasurementCoordinator(bridge);
        var page = new QueryRenderState();
        var layout = new LayoutQueryRenderState();
        var application = new ApplicationQueryRenderState();

        Assert.Equal(10, await ReadAsync(coordinator, application));
        coordinator.Invalidate(page);
        coordinator.Invalidate(layout);

        Assert.Equal(10, await ReadAsync(coordinator, application));
        Assert.Single(bridge.Batches);

        coordinator.Invalidate(application);
        Assert.Equal(20, await ReadAsync(coordinator, application));
        Assert.Equal(2, bridge.Batches.Count);
    }

    [Fact]
    public async Task Application_invalidation_cancels_in_flight_measurement_and_rejects_late_result()
    {
        var bridge = new SequencedMeasurementBridge();
        await using var coordinator = new QueryMeasurementCoordinator(bridge);
        var application = new ApplicationQueryRenderState();

        var staleRead = coordinator.RequestAsync(application, "application-panel", MeasurementKind.Height).AsTask();
        var firstFlush = coordinator.FlushAsync().AsTask();
        await bridge.FirstStarted.Task;

        coordinator.Invalidate(application);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => staleRead);
        bridge.CompleteFirst([new MeasurementReading("application-panel", MeasurementKind.Height, 5)]);
        await firstFlush;

        var currentRead = coordinator.RequestAsync(application, "application-panel", MeasurementKind.Height).AsTask();
        var secondFlush = coordinator.FlushAsync().AsTask();
        await bridge.SecondStarted.Task;
        bridge.CompleteSecond([new MeasurementReading("application-panel", MeasurementKind.Height, 25)]);
        await secondFlush;

        Assert.Equal(25, await currentRead);
        Assert.Equal(25, await coordinator.RequestAsync(application, "application-panel", MeasurementKind.Height));
        Assert.Equal(2, bridge.Batches.Count);
    }

    [Fact]
    public async Task Navigation_or_element_disposal_cancels_pending_measurement_without_late_cache_population()
    {
        var bridge = new FakeMeasurementBridge([new MeasurementReading("panel", MeasurementKind.Height, 42)]);
        await using var coordinator = new QueryMeasurementCoordinator(bridge);
        var state = new QueryRenderState();

        var pending = coordinator.RequestAsync(state, "panel", MeasurementKind.Height).AsTask();
        coordinator.InvalidateElement(state, "panel");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        await coordinator.FlushAsync();
        Assert.Empty(bridge.Batches);
    }

    [Fact]
    public async Task Query_selection_measurements_use_the_first_resolved_owned_element()
    {
        var bridge = new FakeMeasurementBridge([new MeasurementReading("second", MeasurementKind.Width, 128)]);
        await using var coordinator = new QueryMeasurementCoordinator(bridge);
        var registry = new QueryElementRegistry();
        registry.Register("first", ".item");
        registry.Register("second", ".item");
        var selection = new Query(new QueryRenderState(), registry, coordinator).Select(".item").Eq(1);

        var width = selection.WidthAsync().AsTask();
        await coordinator.FlushAsync();

        Assert.Equal(128, await width);
    }

    [Fact]
    public async Task Inner_and_outer_measurements_use_distinct_coalesced_metric_requests()
    {
        var bridge = new FakeMeasurementBridge(
        [
            new MeasurementReading("panel", MeasurementKind.InnerHeight, 90),
            new MeasurementReading("panel", MeasurementKind.InnerWidth, 190),
            new MeasurementReading("panel", MeasurementKind.OuterHeight, 100),
            new MeasurementReading("panel", MeasurementKind.OuterWidth, 200)
        ]);
        await using var coordinator = new QueryMeasurementCoordinator(bridge);
        var registry = new QueryElementRegistry();
        registry.Register("panel", ".panel");
        var selection = new Query(new QueryRenderState(), registry, coordinator).Select(".panel");

        var innerHeight = selection.InnerHeightAsync().AsTask();
        var innerWidth = selection.InnerWidthAsync().AsTask();
        var outerHeight = selection.OuterHeightAsync().AsTask();
        var outerWidth = selection.OuterWidthAsync().AsTask();
        await coordinator.FlushAsync();

        Assert.Equal(90, await innerHeight);
        Assert.Equal(190, await innerWidth);
        Assert.Equal(100, await outerHeight);
        Assert.Equal(200, await outerWidth);
        Assert.Equal(
            [MeasurementKind.InnerHeight, MeasurementKind.InnerWidth, MeasurementKind.OuterHeight, MeasurementKind.OuterWidth],
            bridge.Batches[0].Select(request => request.Kind));
    }

    [Fact]
    public async Task Offset_position_and_owned_offset_parent_use_typed_coalesced_snapshots()
    {
        var bridge = new FakeMeasurementBridge(
        [
            new MeasurementReading("child", MeasurementKind.Offset, Point: new QueryPoint(100, 200)),
            new MeasurementReading("child", MeasurementKind.Position, Point: new QueryPoint(12, 24)),
            new MeasurementReading("child", MeasurementKind.OffsetParent, RelatedElementKey: "parent")
        ]);
        await using var coordinator = new QueryMeasurementCoordinator(bridge);
        var registry = new QueryElementRegistry();
        registry.Register("parent", ".parent");
        registry.RegisterWithParent("child", "parent", ".child");
        var selection = new Query(new QueryRenderState(), registry, coordinator).Select(".child");

        var offset = selection.OffsetAsync().AsTask();
        var position = selection.PositionAsync().AsTask();
        var offsetParent = selection.OffsetParentAsync().AsTask();
        await coordinator.FlushAsync();

        Assert.Equal(new QueryPoint(100, 200), await offset);
        Assert.Equal(new QueryPoint(12, 24), await position);
        Assert.Equal("parent", await offsetParent);
        Assert.Equal(
            [MeasurementKind.Offset, MeasurementKind.Position, MeasurementKind.OffsetParent],
            bridge.Batches[0].Select(request => request.Kind));
    }

    [Fact]
    public async Task Concurrent_flushes_do_not_duplicate_an_in_flight_batch()
    {
        var bridge = new DelayedMeasurementBridge();
        await using var coordinator = new QueryMeasurementCoordinator(bridge);
        var pending = coordinator.RequestAsync(new QueryRenderState(), "panel", MeasurementKind.Height).AsTask();

        var firstFlush = coordinator.FlushAsync().AsTask();
        await bridge.Started.Task;
        var secondFlush = coordinator.FlushAsync().AsTask();
        bridge.Complete([new MeasurementReading("panel", MeasurementKind.Height, 42)]);
        await Task.WhenAll(firstFlush, secondFlush);

        Assert.Single(bridge.Batches);
        Assert.Equal(42, await pending);
    }

    [Fact]
    public async Task Invalidating_an_in_flight_page_request_cancels_it_ignores_its_late_result_and_preserves_layout_state()
    {
        var bridge = new SequencedMeasurementBridge();
        await using var coordinator = new QueryMeasurementCoordinator(bridge);
        var page = new QueryRenderState();
        var layout = new LayoutQueryRenderState();

        var stalePage = coordinator.RequestAsync(page, "page", MeasurementKind.Height).AsTask();
        var layoutHeight = coordinator.RequestAsync(layout, "layout", MeasurementKind.Height).AsTask();
        var firstFlush = coordinator.FlushAsync().AsTask();
        await bridge.FirstStarted.Task;

        coordinator.Invalidate(page);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stalePage);

        bridge.CompleteFirst(
        [
            new MeasurementReading("page", MeasurementKind.Height, 10),
            new MeasurementReading("layout", MeasurementKind.Height, 20)
        ]);
        await firstFlush;
        Assert.Equal(20, await layoutHeight);

        var refreshedPage = coordinator.RequestAsync(page, "page", MeasurementKind.Height).AsTask();
        var secondFlush = coordinator.FlushAsync().AsTask();
        await bridge.SecondStarted.Task;
        bridge.CompleteSecond([new MeasurementReading("page", MeasurementKind.Height, 30)]);
        await secondFlush;

        Assert.Equal(30, await refreshedPage);
        Assert.Equal(2, bridge.Batches.Count);
    }

    private sealed class FakeMeasurementBridge(params IReadOnlyList<MeasurementReading> readings) : IQueryMeasurementBridge
    {
        public List<IReadOnlyList<MeasurementRequest>> Batches { get; } = [];

        public ValueTask<IReadOnlyList<MeasurementReading>> MeasureAsync(
            IReadOnlyList<MeasurementRequest> requests,
            CancellationToken cancellationToken)
        {
            Batches.Add(requests.ToArray());
            return ValueTask.FromResult(readings);
        }
    }

    private sealed class ApplicationScopeMeasurementBridge : IQueryMeasurementBridge
    {
        public List<IReadOnlyList<MeasurementRequest>> Batches { get; } = [];

        public ValueTask<IReadOnlyList<MeasurementReading>> MeasureAsync(
            IReadOnlyList<MeasurementRequest> requests,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Batches.Add(requests.ToArray());
            IReadOnlyList<MeasurementReading> readings = requests
                .Select(request => new MeasurementReading(
                    request.ElementKey,
                    request.Kind,
                    Value: Batches.Count == 1 ? 10 : 20))
                .ToArray();
            return ValueTask.FromResult(readings);
        }
    }

    private static async Task<double?> ReadAsync(QueryMeasurementCoordinator coordinator, QueryRenderState state)
    {
        var value = coordinator.RequestAsync(state, "application-panel", MeasurementKind.Width).AsTask();
        await coordinator.FlushAsync();
        return await value;
    }

    private sealed class DelayedMeasurementBridge : IQueryMeasurementBridge
    {
        private readonly TaskCompletionSource<IReadOnlyList<MeasurementReading>> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<IReadOnlyList<MeasurementRequest>> Batches { get; } = [];

        public ValueTask<IReadOnlyList<MeasurementReading>> MeasureAsync(
            IReadOnlyList<MeasurementRequest> requests,
            CancellationToken cancellationToken)
        {
            Batches.Add(requests.ToArray());
            Started.TrySetResult();
            return new ValueTask<IReadOnlyList<MeasurementReading>>(_completion.Task);
        }

        public void Complete(IReadOnlyList<MeasurementReading> readings) => _completion.TrySetResult(readings);
    }

    private sealed class SequencedMeasurementBridge : IQueryMeasurementBridge
    {
        private readonly TaskCompletionSource<IReadOnlyList<MeasurementReading>> _first = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<IReadOnlyList<MeasurementReading>> _second = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _calls;

        public TaskCompletionSource FirstStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SecondStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<IReadOnlyList<MeasurementRequest>> Batches { get; } = [];

        public ValueTask<IReadOnlyList<MeasurementReading>> MeasureAsync(
            IReadOnlyList<MeasurementRequest> requests,
            CancellationToken cancellationToken)
        {
            Batches.Add(requests.ToArray());
            return ++_calls switch
            {
                1 => Start(FirstStarted, _first.Task),
                2 => Start(SecondStarted, _second.Task),
                _ => throw new InvalidOperationException("Only two batches are expected.")
            };
        }

        public void CompleteFirst(IReadOnlyList<MeasurementReading> readings) => _first.TrySetResult(readings);
        public void CompleteSecond(IReadOnlyList<MeasurementReading> readings) => _second.TrySetResult(readings);

        private static ValueTask<IReadOnlyList<MeasurementReading>> Start(
            TaskCompletionSource started,
            Task<IReadOnlyList<MeasurementReading>> completion)
        {
            started.TrySetResult();
            return new ValueTask<IReadOnlyList<MeasurementReading>>(completion);
        }
    }
}
