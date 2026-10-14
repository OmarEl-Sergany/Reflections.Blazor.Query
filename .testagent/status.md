# Batch 0 lifecycle hardening — status

## Implemented tests

- `Invalidating_an_in_flight_page_request_cancels_it_ignores_its_late_result_and_preserves_layout_state`
- `Invalidating_an_in_flight_page_serialization_cancels_it_and_ignores_its_late_result`

## Test-quality review

- Both tests hold the bridge operation in flight before invalidating its page render state.
- Both tests assert cancellation of the original caller and a fresh second bridge batch after the old operation completes.
- The measurement test also proves layout state remains valid while the page state is invalidated.
- Assertions exercise coordinator completion behavior directly; a cached late value would cause the second request to complete without a second bridge batch and fail the test.

## Validation

- Focused unit suite: 91 passed.
- Full solution: 91 unit tests + 27 component tests passed; build succeeded with 0 warnings and 0 errors.

---

# Replace operations — status

## Implemented tests

- `ReplaceWith_moves_a_virtual_replacement_to_the_target_position_and_unregisters_the_target`
- `ReplaceAll_moves_the_receiver_to_the_target_position`
- `Replace_is_an_alias_for_ReplaceWith`
- `ReplaceWith_rejects_self_replacement`
- `ReplaceWith_moves_a_real_element_to_the_target_position_without_duplicate_markup`
- `Generated_metadata_advertises_all_supported_structural_operations`

## Test-quality review

- Unit tests assert exact ordered virtual-child output and selector/registry removal, not merely state-change notifications.
- The component test asserts removal of the old real target, exactly one occurrence of the real replacement, and placement inside the target's former host.
- Self-replacement is explicitly rejected before relocation/removal can erase the selected element.

## Validation

- Focused unit suite: 97 passed.
- Focused component suite: 28 passed.
- Full solution: 98 unit tests + 28 component tests passed; build succeeded with 0 warnings and 0 errors.

---

# WrapAll — status

## Implemented tests

- `WrapAll_creates_one_wrapper_and_preserves_selected_virtual_sibling_order`
- `WrapAll_rejects_a_cross_parent_selection_before_creating_a_wrapper`
- `WrapAll_moves_real_siblings_under_one_generated_wrapper_without_duplicate_markup`
- `Generated_metadata_advertises_all_supported_structural_operations`

## Test-quality review

- The virtual-node test asserts one wrapper, exact child ordering, and reparented selector output.
- The cross-parent test proves validation happens before a wrapper is created or either source parent is mutated.
- The component test proves two real Razor elements are emitted once each, in original order, beneath the generated wrapper.

## Validation

- Focused unit suite: 100 passed.
- Focused component suite: 29 passed.
- Full solution: 100 unit tests + 29 component tests passed; build succeeded with 0 warnings and 0 errors.

---

# WrapInner — status

## Implemented tests

- `WrapInner_moves_existing_owned_children_under_a_component_owned_wrapper`
- `WrapInner_rejects_a_virtual_target_before_creating_a_wrapper`
- `Clear_removes_component_owned_inner_wrapper_mappings`
- `WrapInner_moves_component_owned_Razor_content_and_existing_owned_children_into_one_wrapper`
- `Generated_metadata_advertises_all_supported_structural_operations`

## Test-quality review

- The unit test asserts the exact parent/child topology after existing managed children move beneath the generated wrapper.
- The invalid-target test proves virtual targets fail before any virtual node is created.
- The component test asserts both captured Razor content and a managed child render exactly once within the wrapper and retain order.
- The clear-state regression proves render-fragment mappings cannot outlive a render-state reset.

## Validation

- Focused unit suite: 103 passed.
- Focused component suite: 30 passed.
- Full solution: 103 unit tests + 30 component tests passed; build succeeded with 0 warnings and 0 errors.

---

# Component query scopes — status

## Implemented tests

- `Component_scope_resolves_and_traverses_only_its_own_registered_elements`
- `Component_scope_empty_does_not_remove_a_descendant_owned_by_another_scope`

## Test-quality review

- Scoped resolution and `Parent()` assertions prove identical selectors cannot cross the registry scope boundary.
- The cleanup assertion proves `Empty()` excludes nested keys registered to a different component scope.
- An initial regression exposed unscoped `PushStack` ordering; scope filtering now preserves legacy input order when no scope is active.

## Validation

- Focused unit suite: 105 passed.
- Focused component suite: 30 passed.

---

# Events foundation — status

## Implemented tests

- `On_and_Trigger_invoke_all_library_owned_handlers`
- `One_removes_its_handler_after_the_first_trigger_and_Off_removes_remaining_handlers`
- `On_renders_a_Blazor_owned_click_binding_that_dispatches_the_registered_handler`

## Test-quality review

- Unit tests cover event-name normalization, ordered multi-handler dispatch, one-shot consumption, and removal.
- The component test dispatches a real bUnit click against the rendered QueryElement rather than calling the state dispatcher directly.

## Validation

- Focused unit suite: 107 passed.
- Focused component suite: 31 passed.
# Typed and delegated events — status

## Delivered

- `DelegateJS` and `Undelegate` keep delegated handler registrations in `QueryRenderState`, keyed by a mounted `QueryElement` root and a finite set of bubbling event names.
- `queryEvents.js` registers pre-start Blazor custom event types and returns only the closest `data-rbq-key`; it never owns listeners, callbacks, selector evaluation, or DOM mutation.
- C# verifies that a target is a descendant of the delegating root, belongs to the same component scope, and matches the delegated selector before invoking a handler.
- Server, WebAssembly, and Auto sample hosts now defer Blazor startup, register the RCL custom event types, then start Blazor.

## Assertion review

- `DelegateJS_dispatches_only_matching_descendants_in_the_root_scope` proves the scope and descendant boundary before successful dispatch.
- `Undelegate_removes_only_the_matching_delegated_handler` proves per-handler removal does not remove siblings.
- `DelegateJS_rejects_non_bubbling_events_and_virtual_roots` blocks event types that cannot safely delegate and roots that lack a component render callback.

## Validation

- Unit tests: 116 passed.
- Component tests: 32 passed after the typed-event component regression was added.

---

# Typed event arguments — status

## Delivered

- `On<TEventArgs>`, `Off<TEventArgs>`, and `Trigger<TEventArgs>` preserve supported native Blazor event arguments without reflection dispatch.
- The event descriptor map renders the correct `EventCallback<TEventArgs>` from the normalized browser event name, allowing typed and existing untyped callbacks to share one binding.
- Mismatched event/name argument types fail before any registration is stored.

## Assertion review

- `Typed_On_and_Trigger_preserve_the_native_event_payload_and_share_dispatch_with_untyped_handlers` proves payload identity and compatibility with untyped handlers.
- `Typed_On_rejects_an_event_argument_type_that_does_not_match_the_browser_event` proves pre-mutation contract validation.
- `Typed_Off_removes_only_the_matching_typed_handler` proves targeted typed removal.
- `Typed_On_renders_a_Blazor_owned_click_binding_that_preserves_mouse_event_arguments` proves a rendered Blazor click binding supplies the typed payload.

---

# Event registration lifecycle — status

## Delivered

- Per-element cleanup removes untyped, typed, and delegated-root registrations when a real element unregisters or a virtual node is removed.
- Virtual-host removal cascades through virtual descendants; unrelated live registrations are preserved.

## Assertion review

- `Removing_a_virtual_parent_removes_its_event_registrations_and_those_of_virtual_descendants_only` proves recursive cleanup and live-key isolation.
- `Disposing_QueryElement_releases_its_untyped_typed_and_delegated_event_registrations` proves lifecycle cleanup via a real component disposal.

---

# Native event descriptor completion — status

## Delivered

- Event name validation and render callback construction now share one descriptor table. `onsubmit` uses the framework-supported `EventArgs` callback type; load and error use their corresponding Blazor argument types.
- `Trigger<TEventArgs>` dispatches only typed registrations, preserving the semantic distinction from parameterless synthetic `Trigger`.
- Unknown typed event names and argument mismatches fail before registrations are changed.

## Assertion review

- `Typed_On_rejects_event_names_without_a_typed_Blazor_descriptor` and `Typed_On_rejects_an_event_argument_type_that_does_not_match_the_browser_event` cover descriptor validation.
- `Typed_Trigger_does_not_dispatch_untyped_handlers_or_silently_skip_an_untyped_only_registration` covers the typed trigger boundary.
- `Typed_Submit_renders_the_framework_event_args_callback_and_dispatches_it` proves form submit dispatch uses the framework callback contract.
- `Typed_On_and_Trigger_preserve_the_native_event_payload_without_invoking_untyped_handlers` proves event payload identity and typed-only dispatch.
- Validation: 122 unit and 34 component tests pass.

---

# Typed event helper APIs — status

## Delivered

- Added native-argument overloads to click/change/submit, focus, keyboard, mouse, context-menu, double-click, load/error, and unload helper methods.
- Kept the existing no-argument trigger overloads. Resize and scroll remain generic event handlers because their useful values are not included in Blazor's callback payload.

## Assertion review

- `Typed_event_helpers_bind_to_their_native_argument_types_and_keep_synthetic_trigger_overloads` proves overload composition and unchanged synthetic trigger behavior.
- `Typed_event_helpers_deliver_mouse_keyboard_and_change_payloads_from_Blazor` verifies browser args reach the event helpers in a real rendered component.

---

# Event compatibility aliases — status

## Delivered

- Added generic `Bind<TEventArgs>`, `Unbind<TEventArgs>`, and `TriggerHandler<TEventArgs>` forwarding to the typed handler pipeline.
- Preserved existing untyped signatures and marked Bind/Unbind as legacy aliases in docs.

## Assertion review

- `Typed_Bind_Unbind_and_TriggerHandler_preserve_payload_and_remove_only_the_requested_handler` proves payload delivery and targeted unbinding.
- `TriggerHandler_without_event_arguments_remains_an_untyped_source_compatible_alias` covers the existing overload.
- `Generated_metadata_advertises_typed_event_compatibility_aliases` verifies generated API metadata.

---

# Hover event helper completion — status

## Delivered

- Added typed `Hover` composition for `MouseEnter` and `MouseLeave`; existing untyped overload remains available.
- Reconciled README checkboxes and the Sprint 4 event helper checklist with implemented APIs.

## Assertion review

- `Typed_Hover_composes_mouse_enter_and_leave_callbacks` checks typed composition at the state dispatch boundary.
- `Typed_Hover_delivers_mouse_enter_and_leave_payloads_from_Blazor` checks both native payloads through a rendered component.
- Validation: 127 unit + 36 component tests passed; full solution build completed with 0 warnings and 0 errors.

---

# Select event helper completion — status

## Delivered

- Added typed `SelectJS(EventArgs)` registration while preserving the parameterless synthetic trigger.
- Documentation now identifies `SelectJS` as a Blazor-owned text-selection event instead of an interop escape hatch.

## Assertion review

- `Typed_SelectJS_registers_native_event_args_and_keeps_synthetic_trigger` covers typed and synthetic dispatch.
- `Typed_SelectJS_delivers_native_select_event_args_from_Blazor` covers rendered event delivery.
- Validation: 128 unit + 37 component tests passed; full solution build completed with 0 warnings and 0 errors.

---

# Awaitable native event handlers — status

## Delivered

- Added `Func<Task>` and `Func<TEventArgs, Task>` overloads for `On`, `One`, `Bind`, native event helpers, and `Hover`; `OffAsync` / `UnbindAsync` remove async delegates without changing legacy null-call overload resolution.
- Added `TriggerAsync` overloads. Synchronous `Trigger` remains non-blocking and unchanged; native Blazor event callbacks await both sync and async registrations and preserve typed native payloads.
- Async one-shot handlers are removed before invocation; async registrations participate in element-key discovery and are removed with virtual-node disposal or `Clear`.
- Updated generator metadata, README, and the Feature 4469 implementation plan.

## Assertion review

- `Async_On_and_TriggerAsync_awaits_typed_handler_and_preserves_event_payload`
- `Async_One_removes_the_handler_before_awaited_dispatch_and_runs_only_once`
- `Async_Off_removes_only_the_matching_handler_and_keeps_sync_trigger_compatible`
- `Async_event_registrations_are_removed_with_their_virtual_node_and_clear`
- `Async_typed_event_helper_awaits_handler_completion_and_preserves_native_payload`
- `Generated_metadata_advertises_awaitable_event_operations`

---

# Sprint 5 first increment — test quality and validation

## Delivered slice

`QuerySelection.FadeTo(TimeSpan, double)` stores opacity and transition CSS under each resolved generated element key. The updates are wrapped in one `QueryRenderState.BeginBatch()`; the library does not call browser APIs, write DOM nodes, or own a timer for this operation. CSS transition timing is performed by the browser after Blazor updates the stylesheet.

## Test-gap review

Bounded public outcomes reviewed: exact opacity and transition values for all matched keys; single-notification batching; zero-match no-op; opacity endpoints 0 and 1; rejection of NaN, infinities, out-of-range values, and negative duration before mutation; zero/sub-millisecond durations; invariant CSS number formatting; generated API metadata; and actual `QueryHost` style-tree output. Each is asserted by the tests listed in `plan.md`.

No temporary mutations were run; mutation execution was not requested. Browser-specific transition timing and cancellation are not claimed by this synchronous CSS-state API and remain for later Sprint 5 work.

## Assertion-quality review

- New unit coverage: 8 test methods / 13 xUnit cases. Assertions verify exact collection contents, state-change counts, empty-state invariants, exceptions, and operation metadata; no assertion-free or always-true tests found.
- New component coverage: 1 test verifies the concrete style output produced by `QueryHost` for a mounted keyed `QueryElement`.
- Test cases map to the stated contract; no additional assertion types were needed merely to increase diversity.

## Validation

- `dotnet test Reflections.Blazor.Query.slnx --no-restore` — passed: 147 unit + 39 component tests (186 total).
- `dotnet build Reflections.Blazor.Query.slnx --no-restore` — succeeded with 0 errors. Four NU1903 warnings remain for `Microsoft.AspNetCore.Components.WebAssembly.Server` 10.0.11 in the Auto and WebAssembly host projects (GHSA-g27p-qf6j-c96c and GHSA-qjpx-jqm5-h2c4); dependency remediation is outside this effect slice.
- `git diff --check` — clean.

## Sprint status

Sprint 5 is in progress. `FadeTo` is the first completed vertical slice. `Animate`, other fade/slide APIs, queue/dequeue/delay operations, stop/finish/cancellation, completion promises, browser-mode smoke tests, and performance/reconnect validation remain open.

---

# Sprint 5 second increment — FadeIn and FadeOut quality review

## Delivered

- Added `FadeIn(TimeSpan)` and `FadeOut(TimeSpan)` with generated-key animation state and static keyframes rendered by `QueryHost`.
- `FadeIn` clears Query's hidden state first. `FadeOut` ends at `visibility:hidden` while preserving layout; `Hide()` remains a separate opt-in layout removal.
- `Show()`/`Hide()` cancel only library-owned effect animation. An explicitly assigned `animation` declaration is retained.
- Added the operations to generated metadata, README capability matrix, and project plan.

## Assertion review

- `Fade_effects_set_per_key_animation_and_are_advertised` checks both selected keys, exact rules, generated metadata, and one batched notification.
- `FadeIn_shows_hidden_elements_before_applying_animation` verifies state transition out of the hidden set.
- `FadeOut_keeps_element_in_layout_and_can_be_cancelled_by_Show` verifies FadeOut does not set hidden state and that Show clears the effect rule.
- `Hide_cancels_library_fade_without_removing_explicit_animation_css` verifies ownership behavior on the same concrete key selector.
- `Fade_effects_reject_negative_duration_before_mutation` covers both operations and confirms no CSS or hidden-state mutation.
- `Fade_effects_are_no_ops_for_empty_selections` verifies both effects leave state unchanged when no elements resolve.
- `FadeIn_and_FadeOut_render_keyframes_and_animation_from_QueryHost_state` verifies real component output for static keyframes and sequential effects.

## Validation

- `dotnet test Reflections.Blazor.Query.slnx --no-restore` — passed: 155 unit + 40 component tests (195 total).
- `dotnet build Reflections.Blazor.Query.slnx --no-restore` — succeeded with 0 errors. Four existing NU1903 warnings remain for `Microsoft.AspNetCore.Components.WebAssembly.Server` 10.0.11 in WASM/Auto demo hosts (GHSA-g27p-qf6j-c96c and GHSA-qjpx-jqm5-h2c4).
- `git diff --check` — clean; only line-ending normalization warnings were printed.

Browser animation timing was not tested in a real browser. Sprint 5 remains open for `Animate`, `FadeToggle`, slide effects, queues, delays, stop/finish/cancellation, completion promises, and runtime/performance verification.

---

# Sprint 5 third increment — FadeToggle quality review

## Delivered

- Added `FadeToggle(TimeSpan)` and metadata. Its contract toggles only Query-managed visibility; it does not inspect computed browser style or externally mutated DOM.
- Added a separate per-key fade-out intent marker, distinct from `Hide()`'s layout removal state. `FadeOut` records the target as managed-hidden; `FadeIn`/`Show`/`Hide` clear or synchronize that marker.
- The update is batched across all matched keys; empty selections remain a no-op.
- Updated README and implementation plan.

## Assertion review

- `FadeToggle_alternates_effect_for_repeated_calls` covers out-then-in behavior and managed hidden state.
- `FadeToggle_uses_Query_managed_hidden_state` covers a `Hide` starting state and explicit `Show` reset after fade-out.
- `FadeToggle_batches_all_targets_and_empty_selection_is_no_op` covers multi-target coalescing and no-match state preservation.
- `FadeToggle_validates_duration_before_state_changes` covers rejection before mutation and accepted zero duration.
- `FadeToggle_is_advertised_in_generated_operation_metadata` checks generated inventory.
- `FadeToggle_updates_the_rendered_QueryHost_animation_rule` verifies both toggle directions reach the rendered style tree.

## Validation

Pending final test/build run for this increment.

## Validation

- `dotnet test Reflections.Blazor.Query.slnx --no-restore` — passed: 160 unit + 41 component tests (201 total).
- `dotnet build Reflections.Blazor.Query.slnx --no-restore` — succeeded with 0 errors. Four existing NU1903 warnings remain for the WASM/Auto demo hosts (GHSA-g27p-qf6j-c96c and GHSA-qjpx-jqm5-h2c4).
- `git diff --check` — clean; Git printed only line-ending normalization notices.

Browser timing was not exercised. Sprint 5 remains open for `Animate`, slide effects, queues/delays, stop/finish/cancellation, completion promises, and runtime/performance verification.

---

# Sprint 5 tenth increment — measured slide effects

## Delivered

- `SlideDown`, `SlideUp`, and `SlideToggle` enqueue work on the default `fx` queue; they do not start until `Dequeue()`.
- Slide height comes from the read-only, batched `scrollHeight` measurement bridge. Temporary height/overflow/visibility/animation declarations live in a separate render-state overlay and are removed at completion, preserving caller-owned CSS.
- A library JS initializer maps native `animationend` to the registered `rbqanimationend` Blazor custom event. It observes the browser event but does not mutate the DOM; terminal render state is applied by Blazor.
- `Finish`, `Stop`, queue teardown, and render-state clearing settle or cancel in-flight slide work. Fixed guessed max-height is not used.
- Added generator metadata, component coverage, README and project plan updates, and measurement-boundary documentation.

## Test-gap review

- Queue opt-in, validation, ScrollHeight request/target, slide-down natural-height restoration, slide-up delayed hide, toggle direction at execution, pending/active Finish, Stop during measurement, teardown, caller CSS restoration, and advertised metadata are covered by `QueryEffectsTests`.
- Blazor component rendering and independent per-element completion through `rbqanimationend` are covered by `Slide_effects_render_measured_target_and_complete_from_Blazor_animation_event`.
- These tests do not emulate browser layout, native animation timing, JS initializer registration in a running host, or cross-render-mode dispatch. Interactive Server/WebAssembly/Auto browser verification remains outstanding.

## Assertion-quality review

- `SlideDown_measures_scroll_height_and_restores_natural_layout_after_animation_end` asserts the bridge request and measured target, then verifies natural layout after completion.
- `SlideUp_hides_only_after_animation_end` asserts visibility remains until the completion event.
- `SlideToggle_uses_query_visibility_at_execution_time` checks both selected directions based on Query-owned state.
- `SlideFinish_completes_active_effect_without_animation_end`, `SlideStop_cancels_active_measurement_and_restores_initial_visibility`, and `Slide_teardown_cancels_queue_and_clears_temporary_styles` exercise cancellation/finish cleanup paths.
- Component test asserts distinct element completion gates rather than merely checking API presence. No assertion-free or tautological tests identified in this increment.

## Validation

- `dotnet test Reflections.Blazor.Query.slnx --no-restore` — passed: 204 unit + 43 component tests (247 total).
- `dotnet build Reflections.Blazor.Query.slnx --no-restore` — succeeded with 0 errors; four NU1903 warnings remain for `Microsoft.AspNetCore.Components.WebAssembly.Server` 10.0.11 in WASM/Auto demos (GHSA-g27p-qf6j-c96c and GHSA-qjpx-jqm5-h2c4).
- `git diff --check` — clean; Git printed only line-ending normalization notices.
- `node --check src/Reflections.Blazor.Query/wwwroot/Reflections.Blazor.Query.lib.module.js` — passed.

---

# Sprint 5 eleventh increment — Server browser smoke

## Delivered

- Added a `Toggle slide` control and stable slide target to the Interactive Server demo, providing a repeatable manual smoke-test surface.
- Used an isolated Playwright CLI browser session. The RCL `Reflections.Blazor.Query.*.lib.module.js` initializer and `queryMeasurement.*.js` static assets both returned HTTP 200; the Blazor Server circuit connected and the browser console reported no errors.
- Observed slide-down's `rbq-slide-down` animation at `0px`, then natural `24px` height with animation state removed. Observed slide-up finish at `display:none` and `height:auto`.

## Remaining coverage

- This is a real-browser Server smoke check, not a committed automated E2E suite. WebAssembly, Auto client takeover, navigation/disposal, and circuit reconnect still need repeatable browser coverage. Performance baselines and Release/package static-asset checks also remain open.

## Validation

- `dotnet test Reflections.Blazor.Query.slnx --no-restore` — passed: 204 unit + 43 component tests (247 total).
- `dotnet build Reflections.Blazor.Query.slnx --no-restore` — succeeded with 0 errors and four NU1903 warnings for `Microsoft.AspNetCore.Components.WebAssembly.Server` 10.0.11 (two advisories in each of the WASM/Auto server demos).
- `git diff --check` — clean; only line-ending normalization notices.

---

# Sprint 5 twelfth increment — cross-mode browser smoke

## Delivered

- Added the same slide-toggle proof control and stable target to the WebAssembly and Auto demo routes, complementing the prior Server proof control.
- In WebAssembly, verified both slide-up and slide-down in a real browser. Browser requests included the WebAssembly runtime/native `.wasm` assets and the fingerprinted library JS initializer/measurement module; slide-down ran `rbq-slide-down` from `0px` to natural `24px`, and slide-up completed to `display:none`/`height:auto`.
- In Auto, verified first visit established the Server SignalR circuit and the slide completed. After reloading with the runtime cached, verified WebAssembly assets were present (202 `.wasm` resources observed), no new SignalR connection was logged, and both slide directions completed through the same Blazor event path.
- Rechecked Server smoke evidence from the preceding increment. Isolated Playwright browser closed after testing; no browser artifacts/dependencies were retained in the repository.

## Remaining coverage

- This remains a manual real-browser smoke run, not committed automated E2E/CI coverage. Navigation during active effects, disposal, circuit reconnect, and promise/queue cleanup need deterministic browser scenarios.
- No performance baseline or Release/packaged-artifact validation was performed. Sprint 5 remains open.

## Validation

- `dotnet test Reflections.Blazor.Query.slnx --no-restore` — passed: 204 unit + 43 component tests (247 total).
- `dotnet build Reflections.Blazor.Query.slnx --no-restore` — succeeded with 0 errors and four NU1903 warnings for `Microsoft.AspNetCore.Components.WebAssembly.Server` 10.0.11 (two advisories in each of the WASM/Auto server demos).
- `git diff --check` — clean; only line-ending normalization notices.

---

# Sprint 5 fourth increment — Animate quality review

## Delivered

- Added `Animate(IReadOnlyDictionary<string, string>, TimeSpan)` to apply a deterministic property transition and target values per resolved element key, within one render-state batch.
- CSS property names are restricted to CSS identifier characters; values remain caller-supplied like the existing `Css` API.
- The API updates target state synchronously and does not claim transition completion, cancellation, queueing, or a completion promise.
- Added generated metadata and updated README/project plan.

## Assertion review

- `Animate_sets_transition_and_targets_for_each_resolved_element_in_one_batch` checks exact rules for two keys and one notification.
- `Animate_rejects_invalid_properties_and_negative_duration_before_mutation` covers invalid property syntax, empty value, null map, and negative duration with unchanged state.
- `Animate_is_a_no_op_for_empty_properties_or_selection` checks both empty property map and no-match selection.
- `Animate_formats_duration_invariantly_and_orders_properties` checks one-tick duration, non-English culture, custom CSS property syntax, and stable ordering.
- `Animate_is_advertised_in_generated_operation_metadata` checks generated inventory.
- `Animate_renders_transition_and_target_values_through_QueryHost` verifies the mounted style tree.

## Validation

- `dotnet test Reflections.Blazor.Query.slnx --no-restore` — passed: 165 unit + 42 component tests (207 total).
- `dotnet build Reflections.Blazor.Query.slnx --no-restore` — succeeded with 0 errors. Four existing NU1903 warnings remain for the WASM/Auto demo host dependency `Microsoft.AspNetCore.Components.WebAssembly.Server` 10.0.11 (GHSA-g27p-qf6j-c96c and GHSA-qjpx-jqm5-h2c4).
- `git diff --check` — clean; Git printed only line-ending normalization notices.

Slide effects remain a separate measurement-aware API design: do not approximate measured layout behavior with guessed `max-height` values.

---

# Sprint 5 fifth increment — Queue API quality review

## Delivered

- Added per-element named async queues through `Queue`, explicit awaited `Dequeue`, and pending-only `ClearQueue`; default name is `fx`.
- Same-element drains are sequential and concurrent `Dequeue` calls share the active drain; separate element queues can progress concurrently.
- Token-aware callbacks receive cancellation when their element or render state is removed. Cleanup contains exceptions from consumer cancellation callbacks so one callback cannot abort teardown.
- Queue state does not cause render notifications. Callback faults propagate to the awaiting caller; pending actions remain available for a later dequeue.
- Updated generated operation metadata, README capability/status matrix, and project plan. Slide effects remain explicitly deferred pending measured-height capture and Blazor-owned completion semantics.

## Test-gap review

Covered caller-visible outcomes: delayed start until explicit dequeue; per-element order; cross-element independent progress; coalesced same-queue dequeue; pending-only clear; active cancellation on element/state teardown; teardown despite throwing token callbacks; argument validation; safe empty queues; error propagation/retry; and generated metadata. No browser rendering assertion is applicable because these APIs orchestrate callbacks and do not render or mutate DOM.

No mutation run was requested. Remaining queue boundaries are explicit: tokenless `Func<Task>` callbacks cannot be preempted, callback reentrancy that awaits `Dequeue` on the same queue is not supported, and `Delay`, `Stop`, `Finish`, and completion promises remain future work.

## Assertion-quality review

- Nine queue test methods assert execution order, synchronization/cancellation, exact exception behavior, metadata, and unchanged render-notification counts; none is assertion-free or relies on an always-true assertion.
- The teardown regression test verifies that a throwing cancellation callback does not escape `Clear()` and that the active action still observes cancellation.

## Validation

- `dotnet test Reflections.Blazor.Query.slnx --no-restore` — passed: 175 unit + 42 component tests (217 total).
- `dotnet build Reflections.Blazor.Query.slnx --no-restore` — succeeded with 0 errors. Four NU1903 advisory warnings remain for `Microsoft.AspNetCore.Components.WebAssembly.Server` 10.0.11 in WASM/Auto demo projects (GHSA-g27p-qf6j-c96c and GHSA-qjpx-jqm5-h2c4).
- `git diff --check` — clean (Git may print line-ending normalization notices).

---

# Sprint 5 sixth increment — Delay quality review

## Delivered

- Added `Delay(TimeSpan, string queueName = "fx")` as cancellable queued work; it begins only when `Dequeue` starts the queue.
- Added duration/name validation, generated operation metadata, README capability status, and project-plan status.
- Kept the boundary explicit: this does not implement public stop/finish, completion promises, or autonomous timer scheduling.

## Test-gap and assertion-quality review

- `Delay_waits_in_queue_and_then_runs_the_next_action` checks deferred start and successful progression through the sequence.
- `Delay_is_canceled_by_render_state_teardown_and_validates_before_enqueue` checks invalid inputs before mutation and cancellation of a long-running delay during state teardown.
- `Queue_operations_are_advertised_in_generated_operation_metadata` now includes the Delay operation.
- These assertions match the documented contract; no assertion-free or tautological tests were found. No browser test is applicable to the queue-only delay primitive.

## Validation

- `dotnet test Reflections.Blazor.Query.slnx --no-restore` — passed: 177 unit + 42 component tests (219 total).
- `dotnet build Reflections.Blazor.Query.slnx --no-restore` — succeeded with 0 errors; four NU1903 warnings remain for `Microsoft.AspNetCore.Components.WebAssembly.Server` 10.0.11 in WASM/Auto demos (GHSA-g27p-qf6j-c96c and GHSA-qjpx-jqm5-h2c4).
- `git diff --check` — clean; Git only reported line-ending normalization notices.

---

# Sprint 5 seventh increment — Stop quality review

## Delivered

- Added awaitable `Stop(bool clearQueue = false, string queueName = "fx")` and generated metadata.
- Stop signals cooperative cancellation to the active drain and awaits its termination. Pending work is preserved by default for a subsequent explicit `Dequeue`; `clearQueue: true` discards it.
- Tokenless callbacks are not preemptible. Stop waits for them to complete; if they complete normally, their drain can also complete normally. No jump-to-end behavior is claimed.
- Updated README and project plan to distinguish Stop from `ClearQueue`, teardown cancellation, and future `Finish`/promise work.

## Test-gap review

Public outcomes covered: token-aware cancellation; retained pending callbacks and later restart; explicit pending clear; non-preemptible tokenless callback awaiting; absent/pending-only queue behavior; queue-name validation; no render notifications; and generated metadata. Roslyn static pairing maps `QueryRenderState.cs` to `QueryEffectQueueTests.cs` and other state suites; this is source/test pairing only, not line or branch coverage.

No mutations were executed. No remaining high-risk Stop outcome in the stated contract is untested; automatic start, jump-to-end, and `Finish` remain intentionally outside scope.

## Assertion-quality review

- `Stop_cancels_active_work_and_preserves_pending_for_a_later_dequeue` asserts active-token cancellation, pending-work non-execution before restart, execution after restart, drain termination, and returned selection identity.
- `Stop_with_clearQueue_discards_pending_work` asserts cancellation, subsequent empty dequeue, and pending callback non-execution.
- `Stop_waits_for_tokenless_active_callback_before_returning` asserts Stop remains incomplete until explicit release and then both Stop and the drain complete.
- `Stop_handles_inactive_queues_and_validates_queue_name` asserts safe inactive behavior, validation failure, pending callback discard, and unchanged render notifications.
- No assertion-free or tautological tests found. State, negative, exception, and asynchronous-completion assertions directly pin the documented outcomes.

## Validation

- `dotnet test tests/Reflections.Blazor.Query.Tests/Reflections.Blazor.Query.Tests.csproj --no-restore` — passed: 181 unit tests.
- `dotnet test Reflections.Blazor.Query.slnx --no-restore` — passed: 181 unit + 42 component tests (223 total).
- `dotnet build Reflections.Blazor.Query.slnx --no-restore` — succeeded with 0 errors; four NU1903 warnings remain for `Microsoft.AspNetCore.Components.WebAssembly.Server` 10.0.11 in WASM/Auto demos (GHSA-g27p-qf6j-c96c and GHSA-qjpx-jqm5-h2c4).
- `git diff --check` — clean; Git only printed line-ending normalization notices.

---

# Sprint 5 eighth increment — Finish quality review

## Delivered

- Queue entries now carry an execution delegate and optional finish delegate. Existing callback overloads remain compatible; new overloads accept explicit completion actions.
- `Finish` skips queued `Delay` items, invokes finish delegates for finishable pending work, and runs ordinary callback work normally. For an active finishable item it signals that item's linked cancellation token, awaits the run delegate, then invokes the finish delegate once. Active tokenless callbacks are awaited.
- `Finish` also settles library-owned render-state CSS: fade animation declarations are replaced with terminal opacity/visibility; library-owned CSS transitions are removed while target declarations remain. `Show`/`Hide` clear terminal styles owned by a finished fade. Caller-owned animation/transition declarations are retained.
- Added generated metadata and updated README/project plan.

## Test-gap review

Caller-visible partitions covered: pending finish action versus run action; active cancellation paths where the callback throws or handles cancellation; exactly-once completion for each selected key; skipped long delays; ordinary callback fallback; tokenless active callback serialization; empty queue and bad-input handling; no queue-triggered render notifications; fade/transition terminal CSS, preservation of caller-supplied CSS, and reset through `Show`. No mutation run was requested; no uncovered high-risk outcome in this contract was identified during source/assertion mapping.

## Assertion-quality review

- `Finish_uses_completion_actions_for_pending_finishable_items` pins completion-vs-run selection and notification invariance.
- `Finish_cancels_active_finishable_work_and_invokes_its_completion_once` and `Finish_invokes_active_finish_action_when_callback_handles_cancellation` cover both active callback termination paths and exact finish counts.
- `Finish_skips_queued_delays_and_runs_non_finishable_callbacks` uses a long delay with a bounded timeout, proving delay skip without a timing-sensitive sleep threshold.
- `Finish_waits_for_active_tokenless_work_before_finishing_pending_items` verifies no early finish and no normal execution of finishable pending work.
- `Finish_handles_empty_queues_and_validates_inputs` checks null delegates, blank names, empty queues, and unchanged state notifications.
- `Finish_completes_library_fades_and_transitions_at_their_target_state` checks concrete final CSS, preservation of caller-owned declarations, target-value retention, and `Show` cleanup.
- Assertions are state, negative, exception, and asynchronous completion checks; no assertion-free or tautological tests found.

## Validation

- `dotnet test tests/Reflections.Blazor.Query.Tests/Reflections.Blazor.Query.Tests.csproj --no-restore` — passed: 188 unit tests.
- `dotnet test Reflections.Blazor.Query.slnx --no-restore` — passed: 188 unit + 42 component tests (230 total).
- `dotnet build Reflections.Blazor.Query.slnx --no-restore` — succeeded with 0 errors; four NU1903 warnings remain for `Microsoft.AspNetCore.Components.WebAssembly.Server` 10.0.11 in WASM/Auto demos (GHSA-g27p-qf6j-c96c and GHSA-qjpx-jqm5-h2c4).
- `git diff --check` — clean; only Git line-ending normalization notices were emitted.

---

# Sprint 5 ninth increment — Promise quality review

## Delivered

- Added `QuerySelection.Promise(string queueName = "fx")` as an observer of selected queues' current completion; it does not start queue work.
- Added a queue-owned completion signal. It follows pending work across `Stop` preservation, resolves on drain/clear/finish, and cancels on element or render-state teardown. A callback failure is still thrown by `Dequeue`; the promise represents empty-queue completion, not callback success.
- Added generated metadata and updated the README and project plan.
- Teardown test found that `QueryRenderState.Clear()` previously canceled queue callbacks without settling promise observers; the clear path now cancels those completion signals.

## Test-gap and assertion-quality review

- `Promise_waits_for_explicit_dequeue_and_resolves_when_queue_drains` asserts no implicit start and exact completion after explicit dequeue.
- `Promise_waits_for_every_selected_queue` holds one selected queue open after another completes, proving aggregate completion cannot resolve early.
- `Promise_handles_empty_and_missing_queues_and_validates_name` covers immediate no-work completion and bad-input rejection.
- `Promise_remains_pending_after_failure_until_pending_work_is_retried` proves a failed drain leaves promise pending while queued work remains, then resolves after retry drains it.
- `Promise_resolves_when_pending_work_is_cleared`, `Stop_with_clearQueue_discards_pending_work`, `Promise_tracks_preserved_work_across_stop`, and `Finish_uses_completion_actions_for_pending_finishable_items` cover discard, queue replacement, finish, and teardown cancellation.
- `Queue_operations_are_advertised_in_generated_operation_metadata` pins the generated inventory.
- Assertions check observable state, non-completion before each gate, selected completion and errors; no assertion-free or tautological tests identified.

## Validation

- `dotnet test tests/Reflections.Blazor.Query.Tests/Reflections.Blazor.Query.Tests.csproj --no-restore --filter FullyQualifiedName~QueryEffectQueueTests` — passed: 29 queue tests.
- `dotnet test Reflections.Blazor.Query.slnx --no-restore` — passed: 195 unit + 42 component tests (237 total).

---

# Sprint 5 reconnect and async-slide increment — status

- Converted `SlideDown`, `SlideUp`, and `SlideToggle` to awaitable direct methods; added explicit queue aliases and generator metadata for `QueueSlideDown`, `QueueSlideUp`, and `QueueSlideToggle`.
- Added reconnect reconciliation to the RCL JS initializer. On the standard Blazor reconnect-modal `hide` event it inspects Query-keyed slide CSS animations; only finished animations get one synthetic native `animationend` dispatch. Active animations remain active and no DOM styles/structure are mutated.
- Added a Server-only browser test route and two Playwright tests using a controlled `_blazor` WebSocket disconnect. One proves an in-progress animation continues; the other observes the native event while offline, checks the queue stays pending, and verifies exactly one synthetic replay after reconnection.
- Added local browser package instructions and updated README, implementation plan, and measurement/effect boundary.

## Validation

- `dotnet test Reflections.Blazor.Query.slnx --no-restore` — passed: 204 unit + 45 component tests (249 total).
- `npm test` in `tests/BrowserE2E` — passed: 2/2 reconnect scenarios.
- Server, WebAssembly, and Auto demo builds — succeeded. WebAssembly/Auto retain two NU1903 advisories for `Microsoft.AspNetCore.Components.WebAssembly.Server` 10.0.11.
- `git diff --check` — passed (only Git line-ending normalization notices).

## Remaining

- CI integration and cross-mode performance evidence remain open for Sprint 5; Sprint 2 is closed in the repository.

---

# Sprint 2 closure — Feature 4467

- Finalized scoped application state through `ApplicationQueryRenderState` / `ApplicationQuery`, registered by `AddBlazorQuery`; verified service-scope isolation and persistence across navigation until explicit application invalidation.
- Completed the read-only, queued/coalesced browser measurement boundary for dimensions, offsets, positions, and offset parent. The measured data is cached separately from render-driving state and does not mutate DOM or trigger implicit rerenders.
- Added browser measurement proof pages and Playwright coverage for Server, WebAssembly, and Auto. Each validates dimensions/position against deterministic layout values and verifies library-owned element markup remains unchanged; Auto repeats after warm reload.
- Updated the implementation plan, measurement contract, and README validation totals.

## Requirement | Evidence

| Requirement | Evidence |
|---|---|
| App DI lifetime and isolation | `Application_query_state_is_scoped_to_each_Blazor_application_service_scope`; `Registered_application_query_state_survives_navigation_until_application_clear` |
| Page/layout/application cache lifetime and cancellation | `Application_scope_measurements_survive_page_and_layout_invalidation_until_application_invalidation`; `Application_invalidation_cancels_in_flight_measurement_and_rejects_late_result`; `Invalidating_an_in_flight_page_request_cancels_it_ignores_its_late_result_and_preserves_layout_state` |
| Browser metrics without DOM mutation | `read-only measurements return browser layout values without mutating owned elements` in Server, WebAssembly, and Auto; Auto reloads and validates the warm client path |
| .NET regression suite | `dotnet test Reflections.Blazor.Query.slnx -c Release` — 206 unit + 48 component tests passed (254 total) |
| Release compilation | `dotnet build Reflections.Blazor.Query.slnx -c Release --no-restore` — succeeded, 0 warnings, 0 errors |
| Browser lifecycle suite | Server 7/7, WebAssembly 2/2, Auto 2/2 Playwright cases passed |
| Patch hygiene | `git diff --check` — passed; only Git line-ending normalization notices |

Assertion review: scope tests assert cache contents and cancellation/completion outcomes; browser assertions compare exact measured values and owned attributes before/after. No assertion-free or tautological cases identified.

## Sprint 5 remote closure audit — 2026-10-14

- Read-only Azure DevOps audit found pipeline definitions 14 and 15, both using `/azure-pipelines.yml`, and no recorded build runs.
- Feature work item 4470 (“Effects and queues”) is assigned to Omar El Sergany but remains in `New` state.
- The remote feature branch is at `30906c9b4298d7a2ab7fe3f81908ec879bb69ea4`; this working tree has uncommitted changes, so a pipeline run against the existing remote ref would not validate the checked-out source.
- Host-specific browser performance evidence is not present. The BenchmarkDotNet report intentionally covers only in-process coordinator/render-state paths and cannot close that gate.
- Sprint 5 is therefore still open pending host performance evidence and a pipeline run/artifact for the exact source revision. No remote state was changed in this audit.

## Sprint 5 host-performance implementation and final local evidence

- Added `/host-performance` proof routes to Server, WebAssembly, and Auto demos. Each route submits nine read-only measurement kinds concurrently for a fresh generated key, validates dimensions/position/parent results, and records .NET handler duration.
- Added `host measurement performance baseline` Playwright coverage: 25 uncached batches per path, capture-click-to-render timing, a distinct 10,000-call browser-only `getBoundingClientRect()` baseline, raw JSON report attachment, and p50/p95 summaries. Auto asserts its initial Server circuit and cached-WebAssembly reload path.
- Updated CI to publish browser/JUnit/performance evidence as `BrowserE2EAndHostPerformance`; updated the project plan, README, and performance baseline with exact scope and attribution caveats.
- Final local evidence: `dotnet test Reflections.Blazor.Query.slnx -c Release --no-build` — 206 unit + 48 component tests passed; Release solution build — 0 warnings/errors; Playwright Server 8/8, WebAssembly 3/3, Auto 3/3; `dotnet list Reflections.Blazor.Query.slnx package --vulnerable --include-transitive` — no vulnerable packages; runtime package archive contains README, `lib/net10.0/Reflections.Blazor.Query.dll`, and RCL static assets; `git diff --check` passed.
- Exact behavioral test: `host measurement performance baseline`; assertion review verifies runtime path, unique-key uncached batch, all returned metric values, timing presence, and the owned offset-parent key. No latency threshold was added because this is a baseline, not a product SLO.
- Local baseline snapshot is in `docs/PERFORMANCE-BASELINE.md`; raw reports are under ignored `output/playwright/{server,wasm,auto}`. Server handler time includes the JS-interop circuit round trip and is not represented as pure CPU or isolated transport latency.
- Still pending: commit/push this validated tree, run Azure pipeline definition 14 on the pushed SHA, verify build result and published browser/performance/package artifacts, then transition work item 4470 from `New` to the process-valid completed state. Sprint 5 remains open until those remote checks succeed.
