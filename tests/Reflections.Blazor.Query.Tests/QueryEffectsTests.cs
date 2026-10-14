using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Reflections.Blazor.Query.Tests;

public sealed class QueryEffectsTests
{
    [Fact]
    public void FadeTo_sets_target_opacity_and_transition_for_each_resolved_element()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".fade");
        registry.Register("second", ".fade");

        var result = new Query(state, registry).Select(".fade").FadeTo(TimeSpan.FromMilliseconds(250), 0.35);

        Assert.Equal(2, result.Length);
        AssertCss(state, "first", "transition", "opacity 250ms ease");
        AssertCss(state, "first", "opacity", 0.35.ToString("0.#################", CultureInfo.InvariantCulture));
        AssertCss(state, "second", "transition", "opacity 250ms ease");
        AssertCss(state, "second", "opacity", 0.35.ToString("0.#################", CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Finish_completes_library_fades_and_transitions_at_their_target_state()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("fade-out", ".fade-out");
        registry.Register("fade-in", ".fade-in");
        registry.Register("transition", ".transition");
        registry.Register("custom", ".custom");
        var query = new Query(state, registry);
        query.Select(".fade-out").FadeOut(TimeSpan.FromSeconds(5));
        query.Select(".fade-in").FadeIn(TimeSpan.FromSeconds(5));
        query.Select(".transition").Animate(
            new Dictionary<string, string> { ["left"] = "10px" },
            TimeSpan.FromSeconds(5));
        query.Select(".custom").FadeOut(TimeSpan.FromSeconds(5))
            .Css("animation", "caller-animation 2s linear")
            .Css("transition", "color 2s ease");

        await query.Select(".fade-out").Add(".fade-in").Add(".transition").Add(".custom").Finish();

        AssertCss(state, "fade-out", "opacity", "0");
        AssertCss(state, "fade-out", "visibility", "hidden");
        AssertCss(state, "fade-in", "opacity", "1");
        AssertCss(state, "fade-in", "visibility", "visible");
        AssertCss(state, "transition", "left", "10px");
        Assert.Contains(state.Rules, rule => rule.Selector == ".custom"
            && rule.Property == "animation" && rule.Value == "caller-animation 2s linear");
        Assert.Contains(state.Rules, rule => rule.Selector == ".custom"
            && rule.Property == "transition" && rule.Value == "color 2s ease");
        Assert.DoesNotContain(state.Rules, rule =>
            rule.Property == "animation"
            && rule.Selector is "[data-rbq-key=\"fade-out\"]" or "[data-rbq-key=\"fade-in\"]");
        Assert.DoesNotContain(state.Rules, rule =>
            rule.Property == "transition" && rule.Selector == "[data-rbq-key=\"transition\"]");

        query.Select(".fade-out").Show();
        Assert.DoesNotContain(state.Rules, rule =>
            rule.Selector == "[data-rbq-key=\"fade-out\"]"
            && (rule.Property == "opacity" || rule.Property == "visibility"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void FadeTo_accepts_both_opacity_endpoints(double opacity)
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".fade");

        new Query(state, registry).Select(".fade").FadeTo(TimeSpan.Zero, opacity);

        AssertCss(state, "first", "transition", "opacity 0ms ease");
        AssertCss(state, "first", "opacity", opacity.ToString("0.#################", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void FadeTo_batches_all_element_rules_into_one_render_state_notification()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".fade");
        registry.Register("second", ".fade");
        var changedCount = 0;
        state.Changed += () => changedCount++;

        new Query(state, registry).Select(".fade").FadeTo(TimeSpan.FromSeconds(1), 0.5);

        Assert.Equal(1, changedCount);
        Assert.Equal(4, state.Rules.Count);
    }

    [Fact]
    public void FadeTo_is_a_no_op_when_the_selection_matches_no_elements()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();

        var result = new Query(state, registry).Select(".missing").FadeTo(TimeSpan.FromMilliseconds(100), 0.5);

        Assert.Equal(0, result.Length);
        Assert.Empty(state.Rules);
    }

    [Fact]
    public void FadeTo_formats_css_numbers_invariantly_and_preserves_TimeSpan_tick_precision()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var state = new QueryRenderState();
            var registry = new QueryElementRegistry();
            registry.Register("first", ".fade");

            new Query(state, registry).Select(".fade").FadeTo(TimeSpan.FromTicks(1), 0.25);

            AssertCss(state, "first", "transition", "opacity 0.0001ms ease");
            AssertCss(state, "first", "opacity", "0.25");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void FadeTo_rejects_non_finite_or_out_of_range_opacity_before_mutation(double opacity)
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".fade");

        Assert.Throws<ArgumentOutOfRangeException>(() => new Query(state, registry)
            .Select(".fade")
            .FadeTo(TimeSpan.FromMilliseconds(100), opacity));

        Assert.Empty(state.Rules);
    }

    [Fact]
    public void FadeTo_rejects_negative_duration_before_mutation()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".fade");

        Assert.Throws<ArgumentOutOfRangeException>(() => new Query(state, registry)
            .Select(".fade")
            .FadeTo(TimeSpan.FromMilliseconds(-1), 0.5));

        Assert.Empty(state.Rules);
    }

    [Fact]
    public void FadeTo_is_advertised_in_generated_operation_metadata()
    {
        Assert.Contains("FadeTo", QueryGeneratedMetadata.SupportedOperations);
    }

    [Theory]
    [InlineData("FadeIn", "rbq-fade-in")]
    [InlineData("FadeOut", "rbq-fade-out")]
    public void Fade_effects_set_per_key_animation_and_are_advertised(string effect, string animationName)
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".fade");
        registry.Register("second", ".fade");
        var changedCount = 0;
        state.Changed += () => changedCount++;

        var selection = new Query(state, registry).Select(".fade");
        if (effect == "FadeIn") selection.FadeIn(TimeSpan.FromMilliseconds(125));
        else selection.FadeOut(TimeSpan.FromMilliseconds(125));

        AssertCss(state, "first", "animation", $"{animationName} 125ms ease both");
        AssertCss(state, "second", "animation", $"{animationName} 125ms ease both");
        Assert.Contains(effect, QueryGeneratedMetadata.SupportedOperations);
        Assert.Equal(1, changedCount);
    }

    [Fact]
    public void FadeIn_shows_hidden_elements_before_applying_animation()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".fade");
        var selection = new Query(state, registry).Select(".fade");
        selection.Hide();

        selection.FadeIn(TimeSpan.Zero);

        Assert.False(state.IsHidden("first"));
        AssertCss(state, "first", "animation", "rbq-fade-in 0ms ease both");
    }

    [Fact]
    public void Fade_effects_are_no_ops_for_empty_selections()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();

        var selection = new Query(state, registry).Select(".missing");
        selection.FadeIn(TimeSpan.Zero).FadeOut(TimeSpan.Zero);

        Assert.Empty(state.Rules);
    }

    [Fact]
    public void FadeToggle_alternates_effect_for_repeated_calls()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".fade");
        var selection = new Query(state, registry).Select(".fade");

        selection.FadeToggle(TimeSpan.FromMilliseconds(180));
        AssertCss(state, "first", "animation", "rbq-fade-out 180ms ease both");
        Assert.True(state.IsEffectHidden("first"));

        selection.FadeToggle(TimeSpan.FromMilliseconds(180));
        AssertCss(state, "first", "animation", "rbq-fade-in 180ms ease both");
        Assert.False(state.IsEffectHidden("first"));
    }

    [Fact]
    public void FadeToggle_uses_Query_managed_hidden_state()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".fade");
        var selection = new Query(state, registry).Select(".fade");
        selection.Hide();

        selection.FadeToggle(TimeSpan.Zero);
        AssertCss(state, "first", "animation", "rbq-fade-in 0ms ease both");
        Assert.False(state.IsEffectHidden("first"));

        selection.FadeOut(TimeSpan.FromMilliseconds(100));
        selection.Show();
        selection.FadeToggle(TimeSpan.FromMilliseconds(100));
        AssertCss(state, "first", "animation", "rbq-fade-out 100ms ease both");
    }

    [Fact]
    public void FadeToggle_batches_all_targets_and_empty_selection_is_no_op()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".fade");
        registry.Register("second", ".fade");
        var changedCount = 0;
        state.Changed += () => changedCount++;

        new Query(state, registry).Select(".fade").FadeToggle(TimeSpan.FromMilliseconds(50));
        Assert.Equal(1, changedCount);
        AssertCss(state, "first", "animation", "rbq-fade-out 50ms ease both");
        AssertCss(state, "second", "animation", "rbq-fade-out 50ms ease both");

        state.Clear();
        changedCount = 0;
        new Query(state, registry).Select(".missing").FadeToggle(TimeSpan.Zero);
        Assert.Equal(0, changedCount);
        Assert.Empty(state.Rules);
    }

    [Fact]
    public void FadeToggle_validates_duration_before_state_changes()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".fade");
        var selection = new Query(state, registry).Select(".fade");

        Assert.Throws<ArgumentOutOfRangeException>(() => selection.FadeToggle(TimeSpan.FromTicks(-1)));
        Assert.Empty(state.Rules);
        Assert.False(state.IsEffectHidden("first"));

        selection.FadeToggle(TimeSpan.Zero);
        AssertCss(state, "first", "animation", "rbq-fade-out 0ms ease both");
    }

    [Fact]
    public void FadeToggle_is_advertised_in_generated_operation_metadata()
    {
        Assert.Contains("FadeToggle", QueryGeneratedMetadata.SupportedOperations);
    }

    [Fact]
    public void Animate_sets_transition_and_targets_for_each_resolved_element_in_one_batch()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".animated");
        registry.Register("second", ".animated");
        var changedCount = 0;
        state.Changed += () => changedCount++;
        IReadOnlyDictionary<string, string> targets = new Dictionary<string, string>
        {
            ["opacity"] = "0.5",
            ["transform"] = "scale(0.9)"
        };

        var selection = new Query(state, registry).Select(".animated").Animate(targets, TimeSpan.FromMilliseconds(300));

        Assert.Equal(2, selection.Length);
        foreach (var key in new[] { "first", "second" })
        {
            AssertCss(state, key, "transition", "opacity 300ms ease, transform 300ms ease");
            AssertCss(state, key, "opacity", "0.5");
            AssertCss(state, key, "transform", "scale(0.9)");
        }
        Assert.Equal(1, changedCount);
    }

    [Fact]
    public void Animate_rejects_invalid_properties_and_negative_duration_before_mutation()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".animated");
        var selection = new Query(state, registry).Select(".animated");

        Assert.Throws<ArgumentOutOfRangeException>(() => selection.Animate(
            new Dictionary<string, string> { ["opacity"] = "0" }, TimeSpan.FromTicks(-1)));
        Assert.Throws<ArgumentException>(() => selection.Animate(
            new Dictionary<string, string> { ["opacity;display"] = "none" }, TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentException>(() => selection.Animate(
            new Dictionary<string, string> { ["opacity"] = " " }, TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentNullException>(() => selection.Animate(null!, TimeSpan.Zero));

        Assert.Empty(state.Rules);
    }

    [Fact]
    public void Animate_is_a_no_op_for_empty_properties_or_selection()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".animated");
        var changedCount = 0;
        state.Changed += () => changedCount++;

        new Query(state, registry).Select(".animated").Animate(
            new Dictionary<string, string>(), TimeSpan.FromMilliseconds(50));
        new Query(state, registry).Select(".missing").Animate(
            new Dictionary<string, string> { ["opacity"] = "0" }, TimeSpan.FromMilliseconds(50));

        Assert.Empty(state.Rules);
        Assert.Equal(0, changedCount);
    }

    [Fact]
    public void Animate_formats_duration_invariantly_and_orders_properties()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var state = new QueryRenderState();
            var registry = new QueryElementRegistry();
            registry.Register("first", ".animated");
            IReadOnlyDictionary<string, string> targets = new Dictionary<string, string>
            {
                ["z-index"] = "2",
                ["--offset"] = "1px"
            };

            new Query(state, registry).Select(".animated").Animate(targets, TimeSpan.FromTicks(1));

            AssertCss(state, "first", "transition", "--offset 0.0001ms ease, z-index 0.0001ms ease");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Animate_is_advertised_in_generated_operation_metadata()
    {
        Assert.Contains("Animate", QueryGeneratedMetadata.SupportedOperations);
    }

    [Fact]
    public void FadeOut_keeps_element_in_layout_and_can_be_cancelled_by_Show()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".fade");
        var selection = new Query(state, registry).Select(".fade");

        selection.FadeOut(TimeSpan.FromMilliseconds(200));

        Assert.False(state.IsHidden("first"));
        AssertCss(state, "first", "animation", "rbq-fade-out 200ms ease both");

        selection.Show();

        Assert.DoesNotContain(state.Rules, rule => rule.Property == "animation");
    }

    [Fact]
    public void Hide_cancels_library_fade_without_removing_explicit_animation_css()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".fade");
        var selection = new Query(state, registry).Select(".fade");
        selection.FadeOut(TimeSpan.FromMilliseconds(200));
        state.SetCss("[data-rbq-key=\"first\"]", "animation", "custom 1s linear");

        selection.Hide();

        AssertCss(state, "first", "animation", "custom 1s linear");
        Assert.True(state.IsHidden("first"));
    }

    [Fact]
    public async Task SlideDown_measures_scroll_height_and_restores_natural_layout_after_animation_end()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".slide");
        state.Hide("first");
        var bridge = new FakeMeasurementBridge([new MeasurementReading("first", MeasurementKind.ScrollHeight, 84)]);
        var measurements = new QueryMeasurementCoordinator(bridge);
        measurements.Requested += () => _ = measurements.FlushAsync();
        var selection = new Query(state, registry, measurements).Select(".slide");

        var slide = selection.SlideDown(TimeSpan.FromMilliseconds(180)).AsTask();
        await WaitForRuleAsync(state, "animation", "rbq-slide-down 180ms ease both");

        Assert.Single(bridge.Batches);
        Assert.Equal(MeasurementKind.ScrollHeight, Assert.Single(bridge.Batches[0]).Kind);
        AssertCss(state, "first", "--rbq-slide-height", "84px");
        AssertCss(state, "first", "height", "var(--rbq-slide-height)");
        Assert.False(state.IsHidden("first"));
        Assert.False(slide.IsCompleted);
        Assert.False(state.IsHidden("first"));

        await state.TriggerAsync("first", "rbqanimationend", EventArgs.Empty);
        await slide;

        Assert.DoesNotContain(state.Rules, rule => rule.Selector == "[data-rbq-key=\"first\"]"
            && rule.Property is "--rbq-slide-height" or "height" or "overflow" or "visibility" or "animation");
        Assert.False(state.IsHidden("first"));
    }

    [Fact]
    public async Task SlideUp_hides_only_after_animation_end()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".slide");
        var measurements = CreateMeasurements("first", 62);
        var selection = new Query(state, registry, measurements).Select(".slide");
        var slide = selection.SlideUp(TimeSpan.FromMilliseconds(90)).AsTask();
        await WaitForRuleAsync(state, "animation", "rbq-slide-up 90ms ease both");
        Assert.False(state.IsHidden("first"));
        Assert.False(slide.IsCompleted);

        await state.TriggerAsync("first", "rbqanimationend", EventArgs.Empty);
        await slide;

        Assert.True(state.IsHidden("first"));
        Assert.DoesNotContain(state.Rules, rule => rule.Property == "height" || rule.Property == "animation");
    }

    [Fact]
    public async Task SlideToggle_uses_query_owned_visibility_when_called()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".slide");
        var measurements = CreateMeasurements("first", 47);
        var selection = new Query(state, registry, measurements).Select(".slide");
        state.Hide("first");
        var slideDown = selection.SlideToggle(TimeSpan.FromMilliseconds(75)).AsTask();
        await WaitForRuleAsync(state, "animation", "rbq-slide-down 75ms ease both");
        await state.TriggerAsync("first", "rbqanimationend", EventArgs.Empty);
        await slideDown;

        Assert.False(state.IsHidden("first"));

        var slideUp = selection.SlideToggle(TimeSpan.FromMilliseconds(55)).AsTask();
        await WaitForRuleAsync(state, "animation", "rbq-slide-up 55ms ease both");
        await state.TriggerAsync("first", "rbqanimationend", EventArgs.Empty);
        await slideUp;
        Assert.True(state.IsHidden("first"));
    }

    [Fact]
    public async Task SlideFinish_applies_terminal_visibility_without_waiting_for_measurement_or_event()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".slide");
        state.Hide("first");
        var selection = new Query(state, registry, CreateMeasurements("first", 100)).Select(".slide");
        var slide = selection.SlideDown(TimeSpan.FromSeconds(1)).AsTask();

        await selection.Finish();
        await slide;

        Assert.False(state.IsHidden("first"));
        Assert.DoesNotContain(state.Rules, rule => rule.Property is "height" or "animation");
    }

    [Fact]
    public async Task SlideFinish_completes_active_effect_without_animation_end()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".slide");
        state.Hide("first");
        var selection = new Query(state, registry, CreateMeasurements("first", 100)).Select(".slide");
        var slide = selection.SlideDown(TimeSpan.FromSeconds(1)).AsTask();
        await WaitForRuleAsync(state, "animation", "rbq-slide-down 1000ms ease both");

        await selection.Finish();
        await slide;

        Assert.False(state.IsHidden("first"));
        Assert.DoesNotContain(state.Rules, rule => rule.Property is "height" or "animation" or "overflow");
    }

    [Fact]
    public async Task SlideStop_cancels_active_measurement_and_restores_initial_visibility()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".slide");
        state.Hide("first");
        var measurements = CreateMeasurements("first", 100);
        var selection = new Query(state, registry, measurements).Select(".slide");
        var slide = selection.SlideDown(TimeSpan.FromSeconds(1)).AsTask();
        await WaitForRuleAsync(state, "animation", "rbq-slide-down 1000ms ease both");

        await selection.Stop();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => slide);

        Assert.True(state.IsHidden("first"));
        Assert.DoesNotContain(state.Rules, rule => rule.Property is "height" or "animation" or "visibility");
    }

    [Fact]
    public async Task Slide_teardown_cancels_queue_and_clears_temporary_styles()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".slide");
        state.Hide("first");
        var selection = new Query(state, registry, CreateMeasurements("first", 100)).Select(".slide");
        var slide = selection.SlideDown(TimeSpan.FromSeconds(1)).AsTask();
        await WaitForRuleAsync(state, "animation", "rbq-slide-down 1000ms ease both");

        state.Clear();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => slide);
        Assert.Empty(state.Rules);
        Assert.False(state.IsHidden("first"));
    }

    [Fact]
    public async Task Slide_effects_restore_caller_owned_css_after_temporary_overlay()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".slide");
        var selector = "[data-rbq-key=\"first\"]";
        state.SetCss(selector, "height", "auto");
        state.SetCss(selector, "overflow", "visible");
        state.SetCss(selector, "animation", "caller-animation 2s linear");
        var selection = new Query(state, registry, CreateMeasurements("first", 35)).Select(".slide");
        var slide = selection.SlideUp(TimeSpan.FromMilliseconds(40)).AsTask();
        await WaitForRuleAsync(state, "animation", "rbq-slide-up 40ms ease both");

        await state.TriggerAsync("first", "rbqanimationend", EventArgs.Empty);
        await slide;

        AssertCss(state, "first", "height", "auto");
        AssertCss(state, "first", "overflow", "visible");
        AssertCss(state, "first", "animation", "caller-animation 2s linear");
    }

    [Fact]
    public async Task Slide_effects_validate_inputs_before_enqueuing_and_are_advertised()
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".slide");
        var selection = new Query(state, registry, CreateMeasurements("first", 20)).Select(".slide");

        Assert.Throws<ArgumentOutOfRangeException>(() => selection.SlideDown(TimeSpan.FromTicks(-1)));
        Assert.Throws<ArgumentException>(() => selection.QueueSlideUp(TimeSpan.Zero, " "));
        Assert.Throws<ArgumentException>(() => selection.QueueSlideToggle(TimeSpan.Zero, " "));
        Assert.Contains("SlideDown", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("SlideUp", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("SlideToggle", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("QueueSlideDown", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("QueueSlideUp", QueryGeneratedMetadata.SupportedOperations);
        Assert.Contains("QueueSlideToggle", QueryGeneratedMetadata.SupportedOperations);
        Assert.Empty(state.Rules);

        var emptySelection = new Query(state, registry).Select(".missing");
        Assert.Same(emptySelection, await emptySelection.SlideDown(TimeSpan.Zero));
        Assert.Same(emptySelection, await emptySelection.SlideUp(TimeSpan.Zero));
        Assert.Same(emptySelection, await emptySelection.SlideToggle(TimeSpan.Zero));
        Assert.Throws<InvalidOperationException>(() => new Query(state, registry).Select(".slide").SlideDown(TimeSpan.Zero));
    }


    private static QueryMeasurementCoordinator CreateMeasurements(string key, double height)
    {
        var coordinator = new QueryMeasurementCoordinator(
            new FakeMeasurementBridge([new MeasurementReading(key, MeasurementKind.ScrollHeight, height)]));
        coordinator.Requested += () => _ = coordinator.FlushAsync();
        return coordinator;
    }

    private static async Task WaitForRuleAsync(QueryRenderState state, string property, string value)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (state.Rules.Any(rule => rule.Property == property && rule.Value == value))
                return;
            await Task.Delay(1);
        }

        Assert.Contains(state.Rules, rule => rule.Property == property && rule.Value == value);
    }

    private sealed class FakeMeasurementBridge(IReadOnlyList<MeasurementReading> readings) : IQueryMeasurementBridge
    {
        public List<IReadOnlyList<MeasurementRequest>> Batches { get; } = [];

        public ValueTask<IReadOnlyList<MeasurementReading>> MeasureAsync(
            IReadOnlyList<MeasurementRequest> requests,
            CancellationToken cancellationToken)
        {
            Batches.Add([.. requests]);
            return ValueTask.FromResult(readings);
        }
    }

    [Theory]
    [InlineData("FadeIn")]
    [InlineData("FadeOut")]
    public void Fade_effects_reject_negative_duration_before_mutation(string effect)
    {
        var state = new QueryRenderState();
        var registry = new QueryElementRegistry();
        registry.Register("first", ".fade");
        var selection = new Query(state, registry).Select(".fade");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            if (effect == "FadeIn") selection.FadeIn(TimeSpan.FromTicks(-1));
            else selection.FadeOut(TimeSpan.FromTicks(-1));
        });

        Assert.Empty(state.Rules);
        Assert.False(state.IsHidden("first"));
    }

    private static void AssertCss(QueryRenderState state, string key, string property, string value) =>
        Assert.Contains(state.Rules, rule =>
            rule.Selector == $"[data-rbq-key=\"{key}\"]"
            && rule.Property == property
            && rule.Value == value);
}
