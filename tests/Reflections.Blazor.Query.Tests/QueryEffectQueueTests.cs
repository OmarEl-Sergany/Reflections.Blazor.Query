using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Reflections.Blazor.Query.Tests;

public sealed class QueryEffectQueueTests
{
    [Fact]
    public async Task Queue_waits_for_Dequeue_and_drains_actions_in_order()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        var stages = new List<string>();

        selection
            .Queue(async () =>
            {
                stages.Add("first-start");
                await Task.Yield();
                stages.Add("first-end");
            })
            .Queue(() =>
            {
                stages.Add("second");
                return Task.CompletedTask;
            });

        Assert.Empty(stages);
        Assert.Same(selection, await selection.Dequeue());
        Assert.Equal(["first-start", "first-end", "second"], stages);
    }

    [Fact]
    public async Task Delay_waits_in_queue_and_then_runs_the_next_action()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        var delayElapsed = false;
        var nextRan = false;

        selection.Delay(TimeSpan.FromMilliseconds(20)).Queue(() =>
        {
            delayElapsed = true;
            nextRan = true;
            return Task.CompletedTask;
        });

        Assert.False(delayElapsed);
        await selection.Dequeue();

        Assert.True(delayElapsed);
        Assert.True(nextRan);
    }

    [Fact]
    public async Task Delay_is_canceled_by_render_state_teardown_and_validates_before_enqueue()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");

        Assert.Throws<ArgumentOutOfRangeException>(() => selection.Delay(TimeSpan.FromTicks(-1)));
        Assert.Throws<ArgumentException>(() => selection.Delay(TimeSpan.Zero, " "));

        var dequeue = selection.Delay(TimeSpan.FromDays(1)).Dequeue().AsTask();
        state.Clear();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dequeue);
    }

    [Fact]
    public async Task Stop_cancels_active_work_and_preserves_pending_for_a_later_dequeue()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        var entered = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingRan = false;
        selection.Queue(async cancellationToken =>
        {
            entered.SetResult(cancellationToken);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        });
        selection.Queue(() =>
        {
            pendingRan = true;
            return Task.CompletedTask;
        });

        var drain = selection.Dequeue().AsTask();
        var activeToken = await entered.Task;
        Assert.Same(selection, await selection.Stop());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => drain);

        Assert.True(activeToken.IsCancellationRequested);
        Assert.False(pendingRan);
        await selection.Dequeue();
        Assert.True(pendingRan);
    }

    [Fact]
    public async Task Stop_with_clearQueue_discards_pending_work()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingRan = false;
        selection.Queue(async cancellationToken =>
        {
            entered.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        });
        selection.Queue(() =>
        {
            pendingRan = true;
            return Task.CompletedTask;
        });
        var promise = selection.Promise().AsTask();

        var drain = selection.Dequeue().AsTask();
        await entered.Task;
        await selection.Stop(clearQueue: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => drain);
        await promise;
        await selection.Dequeue();
        Assert.False(pendingRan);
    }

    [Fact]
    public async Task Stop_waits_for_tokenless_active_callback_before_returning()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        selection.Queue(async () =>
        {
            entered.SetResult();
            await release.Task;
        });

        var drain = selection.Dequeue().AsTask();
        await entered.Task;
        var stop = selection.Stop().AsTask();

        Assert.False(stop.IsCompleted);
        release.SetResult();
        await stop;
        await drain;
    }

    [Fact]
    public async Task Stop_handles_inactive_queues_and_validates_queue_name()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        var pendingRan = false;
        var changedCount = 0;
        state.Changed += () => changedCount++;

        await selection.Stop();
        selection.Queue(() =>
        {
            pendingRan = true;
            return Task.CompletedTask;
        });
        await selection.Stop();
        await Assert.ThrowsAsync<ArgumentException>(async () => await selection.Stop(queueName: " "));
        await selection.Stop(clearQueue: true);
        await selection.Dequeue();

        Assert.False(pendingRan);
        Assert.Equal(0, changedCount);
    }

    [Fact]
    public async Task Finish_uses_completion_actions_for_pending_finishable_items()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        var stages = new List<string>();
        var changedCount = 0;
        state.Changed += () => changedCount++;

        selection
            .Queue(
                () =>
                {
                    stages.Add("run");
                    return Task.CompletedTask;
                },
                () =>
                {
                    stages.Add("finish");
                    return Task.CompletedTask;
                });
        var promise = selection.Promise().AsTask();

        await selection.Finish();
        await promise;

        Assert.Equal(["finish"], stages);
        Assert.Equal(0, changedCount);
    }

    [Fact]
    public async Task Finish_cancels_active_finishable_work_and_invokes_its_completion_once()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        registry.Register("second", ".target");
        var selection = new Query(state, registry).Select(".target");
        var enteredCount = 0;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var activeTokens = new System.Collections.Concurrent.ConcurrentBag<CancellationToken>();
        var finishCount = 0;
        selection.Queue(
            async cancellationToken =>
            {
                activeTokens.Add(cancellationToken);
                if (Interlocked.Increment(ref enteredCount) == 2)
                    entered.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            },
            _ =>
            {
                Interlocked.Increment(ref finishCount);
                return Task.CompletedTask;
            });

        var drain = selection.Dequeue().AsTask();
        await entered.Task;

        Assert.Same(selection, await selection.Finish().AsTask().WaitAsync(TimeSpan.FromSeconds(2)));
        await drain.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(2, finishCount);
        Assert.All(activeTokens, token => Assert.True(token.IsCancellationRequested));
    }

    [Fact]
    public async Task Finish_skips_queued_delays_and_runs_non_finishable_callbacks()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        var ordinaryCallbackRan = false;
        selection
            .Delay(TimeSpan.FromDays(1))
            .Queue(() =>
            {
                ordinaryCallbackRan = true;
                return Task.CompletedTask;
            });

        await selection.Finish().AsTask().WaitAsync(TimeSpan.FromSeconds(2));

        Assert.True(ordinaryCallbackRan);
    }

    [Fact]
    public async Task Finish_invokes_active_finish_action_when_callback_handles_cancellation()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finishCount = 0;
        selection.Queue(
            async cancellationToken =>
            {
                entered.SetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // This callback cooperatively handles cancellation and completes normally.
                }
            },
            _ =>
            {
                finishCount++;
                return Task.CompletedTask;
            });

        var drain = selection.Dequeue().AsTask();
        await entered.Task;
        await selection.Finish();
        await drain;

        Assert.Equal(1, finishCount);
    }

    [Fact]
    public async Task Finish_waits_for_active_tokenless_work_before_finishing_pending_items()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finishRan = false;
        var normalPendingRan = false;
        selection.Queue(async () =>
        {
            entered.SetResult();
            await release.Task;
        });
        selection.Queue(
            () =>
            {
                normalPendingRan = true;
                return Task.CompletedTask;
            },
            () =>
            {
                finishRan = true;
                return Task.CompletedTask;
            });

        var drain = selection.Dequeue().AsTask();
        await entered.Task;
        var finish = selection.Finish().AsTask();

        Assert.False(finish.IsCompleted);
        Assert.False(finishRan);
        release.SetResult();
        await finish;
        await drain;

        Assert.True(finishRan);
        Assert.False(normalPendingRan);
    }

    [Fact]
    public async Task Finish_handles_empty_queues_and_validates_inputs()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        var changedCount = 0;
        state.Changed += () => changedCount++;

        Assert.Throws<ArgumentNullException>(() => selection.Queue((Func<Task>)null!, (Func<Task>)(() => Task.CompletedTask)));
        Assert.Throws<ArgumentNullException>(() => selection.Queue(() => Task.CompletedTask, (Func<Task>)null!));
        Assert.Throws<ArgumentException>(() => selection.Queue(
            () => Task.CompletedTask,
            () => Task.CompletedTask,
            " "));
        await Assert.ThrowsAsync<ArgumentException>(async () => await selection.Finish(" "));
        await selection.Finish();

        Assert.Equal(0, changedCount);
    }

    [Fact]
    public async Task Promise_waits_for_explicit_dequeue_and_resolves_when_queue_drains()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        var callbackRan = false;
        selection.Queue(() =>
        {
            callbackRan = true;
            return Task.CompletedTask;
        });

        var promise = selection.Promise().AsTask();

        Assert.False(callbackRan);
        Assert.False(promise.IsCompleted);
        Assert.Same(selection, await selection.Dequeue());
        Assert.Same(selection, await promise);
        Assert.True(callbackRan);
    }

    [Fact]
    public async Task Promise_waits_for_every_selected_queue()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        registry.Register("second", ".target");
        var selection = new Query(state, registry).Select(".target");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSecond = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = 0;
        selection.Queue(async () =>
        {
            var ordinal = Interlocked.Increment(ref started);
            if (ordinal == 2)
                entered.SetResult();
            await (ordinal == 1 ? releaseFirst.Task : releaseSecond.Task);
        });

        var promise = selection.Promise().AsTask();
        var dequeue = selection.Dequeue().AsTask();
        await entered.Task;
        releaseFirst.SetResult();
        await Task.Yield();

        Assert.False(promise.IsCompleted);
        releaseSecond.SetResult();
        await dequeue;
        await promise;
    }

    [Fact]
    public async Task Promise_handles_empty_and_missing_queues_and_validates_name()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        var emptySelection = new Query(state, registry).Select(".missing");

        Assert.Same(selection, await selection.Promise());
        Assert.Same(emptySelection, await emptySelection.Promise());
        await Assert.ThrowsAsync<ArgumentException>(async () => await selection.Promise(" "));
    }

    [Fact]
    public async Task Promise_remains_pending_after_failure_until_pending_work_is_retried()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        var shouldFail = true;
        var laterRan = false;
        selection.Queue(() => shouldFail
            ? Task.FromException(new InvalidOperationException("queue failure"))
            : Task.CompletedTask);
        selection.Queue(() =>
        {
            laterRan = true;
            return Task.CompletedTask;
        });
        var promise = selection.Promise().AsTask();

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await selection.Dequeue());

        Assert.False(promise.IsCompleted);
        Assert.False(laterRan);
        shouldFail = false;
        await selection.Dequeue();
        await promise;
        Assert.True(laterRan);
    }

    [Fact]
    public async Task Promise_resolves_when_pending_work_is_cleared()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        selection.Queue(() => Task.CompletedTask);
        var promise = selection.Promise().AsTask();

        selection.ClearQueue();

        Assert.Same(selection, await promise);
    }

    [Fact]
    public async Task Promise_tracks_preserved_work_across_stop()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingRan = false;
        selection.Queue(async cancellationToken =>
        {
            entered.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        });
        selection.Queue(() =>
        {
            pendingRan = true;
            return Task.CompletedTask;
        });
        var promise = selection.Promise().AsTask();
        var dequeue = selection.Dequeue().AsTask();
        await entered.Task;

        await selection.Stop();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dequeue);
        Assert.False(promise.IsCompleted);
        await selection.Dequeue();
        await promise;

        Assert.True(pendingRan);
    }

    [Fact]
    public async Task Promise_is_canceled_when_render_state_is_cleared()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        selection.Queue(() => Task.CompletedTask);
        var promise = selection.Promise().AsTask();

        state.Clear();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => promise);
    }

    [Fact]
    public async Task Queue_runs_each_element_independently_and_coalesces_concurrent_dequeue()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        registry.Register("second", ".target");
        var selection = new Query(state, registry).Select(".target");
        var started = 0;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        selection.Queue(async cancellationToken =>
        {
            Interlocked.Increment(ref started);
            await release.Task.WaitAsync(cancellationToken);
        });

        var firstDequeue = selection.Dequeue().AsTask();
        var concurrentDequeue = selection.Dequeue().AsTask();
        Assert.Equal(2, started);

        release.SetResult();
        await Task.WhenAll(firstDequeue, concurrentDequeue);
    }

    [Fact]
    public async Task ClearQueue_preserves_active_action_and_discards_pending_actions()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        var entered = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingRan = false;
        selection.Queue(async cancellationToken =>
        {
            entered.SetResult(cancellationToken);
            await release.Task.WaitAsync(cancellationToken);
        });
        selection.Queue(() =>
        {
            pendingRan = true;
            return Task.CompletedTask;
        });

        var dequeue = selection.Dequeue().AsTask();
        var activeToken = await entered.Task;
        selection.ClearQueue();

        Assert.False(activeToken.IsCancellationRequested);
        release.SetResult();
        await dequeue;
        Assert.False(pendingRan);
    }

    [Fact]
    public async Task RenderState_Clear_cancels_active_and_discards_queued_actions()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingRan = false;
        selection.Queue(async cancellationToken =>
        {
            entered.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        });
        selection.Queue(() =>
        {
            pendingRan = true;
            return Task.CompletedTask;
        });

        var dequeue = selection.Dequeue().AsTask();
        await entered.Task;
        state.Clear();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dequeue);
        Assert.False(pendingRan);
    }

    [Fact]
    public async Task RenderState_Clear_continues_when_a_cancellation_callback_throws()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        var entered = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        selection.Queue(async cancellationToken =>
        {
            using var registration = cancellationToken.Register(() => throw new InvalidOperationException("callback failure"));
            entered.SetResult(cancellationToken);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        });

        var dequeue = selection.Dequeue().AsTask();
        var activeToken = await entered.Task;

        state.Clear();

        Assert.True(activeToken.IsCancellationRequested);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dequeue);
    }

    [Fact]
    public async Task Remove_cancels_element_queue()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingRan = false;
        selection.Queue(async cancellationToken =>
        {
            entered.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        });
        selection.Queue(() =>
        {
            pendingRan = true;
            return Task.CompletedTask;
        });

        var dequeue = selection.Dequeue().AsTask();
        await entered.Task;
        selection.Remove();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dequeue);
        Assert.False(pendingRan);
    }

    [Fact]
    public async Task Queue_validates_name_and_callback_before_mutation()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");

        Assert.Throws<ArgumentNullException>(() => selection.Queue((Func<Task>)null!));
        Assert.Throws<ArgumentException>(() => selection.Queue(() => Task.CompletedTask, " "));
        await Assert.ThrowsAsync<ArgumentException>(async () => await selection.Dequeue(" "));
        Assert.Throws<ArgumentException>(() => selection.ClearQueue(" "));

        await selection.Dequeue();
    }

    [Fact]
    public async Task Dequeue_and_ClearQueue_are_safe_for_empty_queues()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        var changedCount = 0;
        state.Changed += () => changedCount++;

        selection.ClearQueue();
        await selection.Dequeue();

        Assert.Equal(0, changedCount);
    }

    [Fact]
    public async Task Dequeue_propagates_action_exception_and_allows_retry()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".target");
        var selection = new Query(state, registry).Select(".target");
        var laterRan = false;
        selection.Queue(() => Task.FromException(new InvalidOperationException("queue failure")));
        selection.Queue(() =>
        {
            laterRan = true;
            return Task.CompletedTask;
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await selection.Dequeue());
        Assert.Equal("queue failure", exception.Message);
        Assert.False(laterRan);

        await selection.Dequeue();
        Assert.True(laterRan);
    }

    [Fact]
    public void Queue_operations_are_advertised_in_generated_operation_metadata()
    {
        Assert.Contains("Queue", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("Dequeue", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("ClearQueue", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("Delay", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("Stop", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("Finish", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("Promise", QueryGeneratedMetadata.SupportedOperations);
    }
}
