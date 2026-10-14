# Batch 0 lifecycle hardening — plan

1. Add delayed bridges to the measurement and form coordinator test fixtures.
2. Start a flush, wait until the bridge observes the request, then invalidate the page render state.
3. Assert the original task is cancelled; complete the old bridge request; issue a new request and assert it reaches a new bridge batch rather than using late cached data.
4. In the measurement test, hold a layout-state request alongside the page request and prove page invalidation does not cancel or invalidate that layout result.
5. Run the focused unit-test project, review assertions against coordinator completion logic, then run the full solution tests.

---

# Replace operations — plan

1. Add one structural helper in `QuerySelection` that validates distinct source/target keys, relocates the replacement through `AttachSibling`, then removes the target through its existing lifecycle path.
2. Expose `ReplaceWith`, receiver-order `ReplaceAll`, and `Replace` alias APIs.
3. Add unit tests for virtual replacement ordering, alias semantics, and self-replacement rejection.
4. Add a bUnit regression for real-element replacement to prove one renderer output and no duplicate source markup.
5. Run focused suites, full solution validation, and record the assertion review.

---

# WrapAll — plan

1. Add `WrapAll` and a preflight helper to `QuerySelection`.
2. Verify every selected element has the same non-null registered parent before creating state.
3. Create the wrapper before the first selected sibling and attach selected elements in existing render order.
4. Add virtual ordering and mixed-parent rejection tests, plus a bUnit real-element rendering test.
5. Update generated metadata and public README, then run focused and full solution validation.

---

# WrapInner — plan

1. Register the component-owned `ChildContent` fragment with `QueryRenderState` during `QueryElement` parameter processing and remove it on disposal.
2. Add render-state mappings between an owner key and its inner wrapper; render the captured fragment only through that wrapper.
3. Add `WrapInner` to `QuerySelection`: validate a mounted real target, snapshot its direct owned children, create the wrapper, move those children beneath it, then activate the content mapping.
4. Add unit tests for reparenting and invalid virtual targets, plus a bUnit test proving Razor content renders once inside the wrapper.
5. Advertise the operation in generated metadata and public documentation; run focused and full solution validation.

---

# Component query scopes — plan

1. Extend registry entries with an optional scope key and provide scope membership filtering.
2. Add `IQueryScope`, a `QueryScope` implementation, and `QueryScopeHost` to create/cascade lexical scope identities from the existing scoped `Query` service.
3. Carry an optional scope key through `Query` and `QuerySelection`; filter every returned selection and guard destructive descendant traversal.
4. Make QueryElement obtain a scope from an explicit parameter or the cascading host and register accordingly.
5. Add unit isolation tests and a bUnit cascade test, then update metadata, README, wiki, and Feature 4468 after full validation.

---

# Events foundation — plan

1. Store handlers in QueryRenderState and expose normalized names, registration, removal, and dispatch.
2. Add fluent On, One, Off, and Trigger APIs to QuerySelection.
3. Render handlers through QueryElement and virtual-node output.
4. Add unit coverage for multiple handlers, one-shot removal, and off behavior plus a component click regression.

---

# Typed and delegated events — plan

1. Replace the Action-only handler representation with a typed dispatch envelope while retaining existing untyped APIs.
2. Render typed EventCallbacks from QueryElement for real and virtual nodes.
3. Add a read-only JS target-key resolver and an injectable bridge contract.
4. Add DelegateJS and Undelegate with root and component-scope validation.
5. Cover typed invocation, rejected out-of-scope targets, and browser event dispatch in unit/component tests.

---

# Typed event arguments — plan

1. Add a finite normalized event-name to native Blazor event-args descriptor map.
2. Store typed handlers alongside existing untyped handlers and dispatch both from one event record.
3. Render the descriptor-selected `EventCallback<TEventArgs>` for real and virtual nodes.
4. Add direct tests for payload preservation, shared typed/untyped dispatch, and type mismatch rejection.

---

# Event registration lifecycle — plan

1. Centralize per-key event cleanup in `QueryRenderState` for untyped, typed, and delegated-root maps.
2. Invoke it from real-element unregistration and virtual-node removal before recursive child cleanup.
3. Add unit coverage for recursive virtual cleanup and live-key isolation, plus a bUnit disposal regression.

---

# Native event descriptor completion — plan

1. Centralize native event name and argument type metadata; drive both validation and render binding from the same descriptor.
2. Correct form-submit binding to `SubmitEventArgs` and model other supported events with the corresponding Blazor event args.
3. Make typed `Trigger` validate registrations and dispatch typed handlers only; keep untyped `Trigger` semantics unchanged.
4. Add table-driven descriptor validation and rendered callback tests for mouse, keyboard, change, focus, and submit events.

---

# Typed event helper APIs — plan

1. Add event-specific `Action<TEventArgs>` overloads to helpers whose descriptor types are established.
2. Keep parameterless helper overloads for existing synthetic triggers.
3. Add unit API composition assertions and bUnit payload delivery tests for mouse, keyboard, and change events.
4. Update generated metadata and README/project-plan matrix.

---

# Event compatibility aliases — plan

1. Add typed overloads for `Bind` and `Unbind` forwarding to typed `On`/`Off`.
2. Add typed `TriggerHandler<TEventArgs>` forwarding to typed local dispatch.
3. Test targeted unbinding, typed payload dispatch, and existing parameterless aliases.
4. Mark compatibility APIs in metadata and documentation.

---

# Hover event helper completion — plan

1. Add a `Hover(Action<MouseEventArgs>, Action<MouseEventArgs>)` overload that composes typed mouse-enter and mouse-leave helpers.
2. Test both callback payload paths in a rendered component, preserving the existing untyped helper.
3. Advertise `Hover` and `TypedHover` metadata and reconcile README event checkboxes.

---

# Select event helper completion — plan

1. Add `SelectJS(Action<EventArgs>)` and metadata for `TypedSelectJS`.
2. Test typed registration and verify the existing `SelectJS()` synthetic trigger.
3. Add a bUnit rendered event test and remove inaccurate JS-escape-hatch documentation.

---

# Awaitable native event handlers — plan

1. Extend untyped and typed registrations with async entries, exact `OffAsync` overloads, one-shot support, event-name discovery, and cleanup.
2. Add `TriggerAsync` on `QuerySelection` and `QueryRenderState`; keep synchronous trigger semantics non-blocking and compatible.
3. Make native Blazor event descriptors return/await tasks, including the fallback callback path.
4. Add unit assertions for async payload dispatch, exact removal, one-shot consumption, and synchronous compatibility.
5. Add bUnit coverage proving a native event callback awaits handler completion and delivers its event args.
6. Update source-generator metadata and user docs; run focused tests and the full solution validation.

---

# Sprint 5 first-increment test plan

| Requirement | Test location | Planned evidence |
|---|---|---|
| `FadeTo` writes opacity and transition for every resolved key | `tests/Reflections.Blazor.Query.Tests/QueryEffectsTests.cs` | `FadeTo_sets_target_opacity_and_transition_for_each_resolved_element` asserts exact key-scoped CSS rules for multiple selected elements |
| Multi-element `FadeTo` coalesces state notification | `QueryEffectsTests.cs` | `FadeTo_batches_all_element_rules_into_one_render_state_notification` counts `QueryRenderState.Changed` notifications |
| Invalid opacity and negative duration are rejected before state mutation | `QueryEffectsTests.cs` | `FadeTo_rejects_non_finite_or_out_of_range_opacity_before_mutation` and `FadeTo_rejects_negative_duration_before_mutation` assert exceptions and unchanged rules |
| Generated metadata advertises `FadeTo` | `QueryEffectsTests.cs` | `FadeTo_is_advertised_in_generated_operation_metadata` asserts the operation inventory |
| Blazor output renders the render-state CSS rules | `tests/Reflections.Blazor.Query.ComponentTests/QueryEffectsComponentTests.cs` | `FadeTo_renders_transition_and_opacity_through_the_QueryHost_style_tree` renders `QueryHost` + keyed `QueryElement` and asserts CSS output |
| Empty selection and locale-independent/time-precise CSS formatting | `QueryEffectsTests.cs` | `FadeTo_is_a_no_op_when_the_selection_matches_no_elements` and `FadeTo_formats_css_numbers_invariantly_and_preserves_TimeSpan_tick_precision` |
| Valid opacity endpoints and zero duration are accepted | `QueryEffectsTests.cs` | `FadeTo_accepts_both_opacity_endpoints` asserts both endpoint declarations and `0ms` transition output |
| No DOM writes/timing/queue behavior is implied | Public API and plan text | Document explicit synchronous CSS-transition boundary; browser animation timing is not asserted by unit tests |

---

# Sprint 5 second-increment test plan — FadeIn and FadeOut

| Requirement | Test | Evidence |
|---|---|---|
| Both effects set key-scoped animation rules and generated metadata, batched once | `Fade_effects_set_per_key_animation_and_are_advertised` | Checks two selected keys, exact animation values, both metadata entries, and one notification |
| FadeIn clears hidden state | `FadeIn_shows_hidden_elements_before_applying_animation` | Starts hidden and asserts `IsHidden` false after call |
| FadeOut retains layout and Show cancels effect-owned animation | `FadeOut_keeps_element_in_layout_and_can_be_cancelled_by_Show` | Asserts not hidden, expected animation, and cleared animation after Show |
| Hide cancels only library-owned animation | `Hide_cancels_library_fade_without_removing_explicit_animation_css` | Replaces with explicit animation CSS, hides, and asserts custom value remains |
| Negative durations rejected before mutation | `Fade_effects_reject_negative_duration_before_mutation` | Tests both public operations and verifies empty CSS/unchanged hidden state |
| Empty selections are no-ops | `Fade_effects_are_no_ops_for_empty_selections` | Calls both effects on a no-match selection and asserts no rules |
| Host renders keyframes and active effect declarations | `FadeIn_and_FadeOut_render_keyframes_and_animation_from_QueryHost_state` | bUnit renders both static keyframes and sequential fade-out/fade-in rules |

Validation: run focused unit/component projects, then full solution tests/build and `git diff --check`.

Validation: focused unit and component test projects, then full solution tests and build. Re-read every assertion against implementation before reporting.

---

# Sprint 5 third-increment test plan — FadeToggle

| Requirement | Test | Evidence |
|---|---|---|
| Toggle fades visible targets out and toggles them back in | `FadeToggle_alternates_effect_for_repeated_calls` | Exact fade-out then fade-in rules and state marker behavior |
| Toggle treats Hide/FadeOut state as hidden and respects Show | `FadeToggle_uses_Query_managed_hidden_state` | Covers hidden-by-Hide and prior FadeOut paths plus Show reset |
| Multi-target toggle batches; empty selections do not mutate | `FadeToggle_batches_all_targets_and_empty_selection_is_no_op` | One notification for multiple matched keys and no rules for no match |
| Negative duration is rejected before mutation; zero is accepted | `FadeToggle_validates_duration_before_state_changes` | Negative exception/state invariant and zero-millisecond declaration |
| Generated metadata and rendered style output include FadeToggle | `FadeToggle_is_advertised_in_metadata` and component render test | Metadata assertion and bUnit host output |

Validation: focused unit/component tests followed by full solution tests/build and `git diff --check`.

---

# Sprint 5 host performance — plan

| Requirement | Evidence |
|---|---|
| Distinct uncached measurement operation per sample on Server/WebAssembly/Auto | `host measurement performance baseline` Playwright scenario; sample route increments a generated element key for every request |
| Report .NET handler and browser click-to-render timing independently | Page emits handler duration; test observes click capture to result-render mutation and preserves raw samples |
| Separate local browser layout-read overhead | Same test records a 1,000-read direct `getBoundingClientRect()` browser-only baseline |
| Prove Auto's Server then warmed WebAssembly paths | Assert initial SignalR circuit and `data-runtime=server`; reload, assert no additional circuit and `data-runtime=wasm`; measure each path independently |
| Avoid unsupported transport attribution | Summary labels Server handler as interop/circuit aggregate and browser timing as end-to-end; no value is described as isolated SignalR or pure server processing |

Run host-specific Playwright suites, then full .NET solution validation and `git diff --check`. Preserve reports under ignored `output/playwright/<host>` and use them to update the checked-in performance snapshot before the Azure pipeline run.

---

# Sprint 2 measurement-scope and browser coverage — plan

| Requirement | Planned test/evidence |
|---|---|
| Application scope is explicitly registered and available through its query service | DI registration/component test resolves `ApplicationQuery` and the same `ApplicationQueryRenderState` instance |
| Application measurement cache is independent from page/layout and retained until app invalidation | `Application_scope_measurements_survive_page_and_layout_invalidation_until_application_invalidation` |
| Explicit application invalidation cancels pending work and rejects late results | `Application_invalidation_cancels_in_flight_measurement_and_rejects_late_result` |
| All public measurement metrics execute through real browser JS in each interactive host | Playwright measurement spec run for Server, WASM, and Auto; assert deterministic dimension values, finite position snapshots, owned parent key, and no style/attribute changes |
| Browser work does not invalidate earlier page/layout contracts | Existing `Invalidating_an_in_flight_page_request_cancels_it_ignores_its_late_result_and_preserves_layout_state`, `Navigation_or_element_disposal_cancels_pending_measurement_without_late_cache_population`, and navigation component tests remain passing |
| Documentation accurately closes Sprint 2 | Update Sprint 2 checklist and boundary docs only after all test/build/browser commands pass |

Implementation phases: add the app-scope state/query registrations and focused tests; create host-compatible metric proof routes and Playwright coverage; execute unit/component/browser matrix; re-read all assertions and reconcile docs.

---

# Sprint 5 fifth-increment test plan — Queue APIs

| Requirement | Test | Evidence |
|---|---|---|
| Enqueue order, explicit start, and sequential awaiting | `Queue_waits_for_Dequeue_and_drains_actions_in_order` | Actions record start/end and demonstrate no work before Dequeue plus sequential execution |
| Per-element isolation and concurrent Dequeue safety | `Queue_runs_each_element_independently_and_coalesces_concurrent_dequeue` | Two keys progress independently while same-key Dequeue shares one drain |
| ClearQueue removes pending only | `ClearQueue_preserves_active_action_and_discards_pending_actions` | Active token remains uncanceled; pending callback never runs |
| Clear/removal cancels active work and pending work | `RenderState_Clear_cancels_active_and_discards_queued_actions`; `Remove_cancels_element_queue` | Await observes cancellation and pending work is not invoked |
| Validation and empty queue behavior | `Queue_validates_name_and_callback_before_mutation`; `Dequeue_and_ClearQueue_are_safe_for_empty_queues` | Exceptions before state change and safe no-op calls |
| Callback fault recovery and metadata | `Dequeue_propagates_action_exception_and_allows_retry`; `Queue_operations_are_advertised_in_generated_metadata` | Awaited exception, later callback drains on retry, generated names asserted |

Validation: focused unit tests, then full solution tests/build and `git diff --check`.

---

# Sprint 5 sixth-increment test plan — Delay

| Requirement | Test | Evidence |
|---|---|---|
| Delay is queued, waits for its duration, then allows the next action | `Delay_waits_in_queue_and_then_runs_the_next_action` | Asserts no action runs before explicit dequeue and the following action runs after the delay completes |
| Teardown cancels a long delay and invalid inputs fail before enqueue | `Delay_is_canceled_by_render_state_teardown_and_validates_before_enqueue` | Verifies negative duration and blank name errors, then confirms state clear cancels the active delay |
| Generated metadata includes Delay | `Queue_operations_are_advertised_in_generated_operation_metadata` | Asserts `Delay` is present in supported operation metadata |

Validation: focused queue tests, full solution tests/build, and `git diff --check`.

---

# Sprint 5 seventh-increment test plan — Stop

| Requirement | Test | Evidence |
|---|---|---|
| Stop cancels active token-aware work, awaits it, and preserves pending work by default | `Stop_cancels_active_work_and_preserves_pending_for_a_later_dequeue` | Confirms cancellation is observed, pending callback does not run before a later explicit dequeue, then drains |
| `clearQueue: true` discards pending callbacks | `Stop_with_clearQueue_discards_pending_work` | Active callback observes cancellation and pending callback remains uncalled |
| Stop awaits tokenless callbacks to prevent overlap | `Stop_waits_for_tokenless_active_callback_before_returning` | Verifies Stop remains incomplete until current action is released |
| Inactive/pending-only Stop and validation are safe | `Stop_handles_inactive_queues_and_validates_queue_name` | Covers safe no-op, pending-only clear, blank name rejection, and notification invariance |
| Stop is advertised in generated metadata | `Queue_operations_are_advertised_in_generated_operation_metadata` | Asserts Stop is present |

Validation: focused queue unit tests, full solution tests/build, and `git diff --check`.

---

# Sprint 5 eighth-increment test plan — Finish

| Requirement | Test | Evidence |
|---|---|---|
| Pending finishable work uses its completion action rather than normal execution | `Finish_uses_completion_actions_for_pending_finishable_items` | Asserts exact action order and that normal run delegates are bypassed |
| Active token-aware finishable work is canceled, awaited, and completed once | `Finish_cancels_active_finishable_work_and_invokes_its_completion_once` | Awaits active entry and verifies cancellation, single finish callback, and drain completion |
| Delay is skipped but pending ordinary callbacks execute | `Finish_skips_queued_delays_and_runs_non_finishable_callbacks` | Uses a long delay and verifies completion without waiting for it plus ordinary callback execution |
| Active tokenless work is awaited without concurrent completion action | `Finish_waits_for_active_tokenless_work_before_finishing_pending_items` | Verifies finish callback remains uncalled until active work is released |
| Finish callback runs once whether active work throws cancellation or handles it and returns | `Finish_cancels_active_finishable_work_and_invokes_its_completion_once`; `Finish_invokes_active_finish_action_when_callback_handles_cancellation` | Covers both active callback termination paths |
| Library-owned fades and transitions are settled without removing target CSS values | `Finish_completes_library_fades_and_transitions_at_their_target_state` | Asserts fade terminal opacity/visibility, preserved Animate target, and removed owned animation/transition declarations |
| Empty queues, validation, selection-wide awaiting, and notifications | `Finish_handles_empty_queues_and_validates_inputs`; multi-key case in `Finish_cancels_active_finishable_work_and_invokes_its_completion_once` | Covers empty/no-op behavior, invalid queue/name/delegates, all selected keys, and no Changed events |
| Generated inventory advertises Finish | `Queue_operations_are_advertised_in_generated_operation_metadata` | Asserts metadata membership |

Validation: focused queue unit tests, full solution tests/build, and `git diff --check`.

---

# Sprint 5 ninth-increment test plan — Promise

| Requirement | Test | Evidence |
|---|---|---|
| Promise observes but does not start queued work, then resolves when dequeue drains it | `Promise_waits_for_explicit_dequeue_and_resolves_when_queue_drains` | Checks pending state before dequeue and completion afterward |
| Promise waits for every selected element queue | `Promise_waits_for_every_selected_queue` | Independent gated callbacks prove no early resolution |
| Empty selection, absent queue, and invalid names | `Promise_handles_empty_and_missing_queues_and_validates_name` | Immediate completion and pre-observation argument rejection |
| Pending work left after a callback fault keeps Promise pending until retry | `Promise_remains_pending_after_failure_until_pending_work_is_retried` | First drain throws, promise stays pending, retry empties and resolves |
| Clear, stop-with-clear, finish, and preserved stop lifecycle | `Promise_resolves_when_pending_work_is_cleared`; `Stop_with_clearQueue_discards_pending_work`; `Promise_tracks_preserved_work_across_stop`; `Finish_uses_completion_actions_for_pending_finishable_items` | Covers intentional discard via both clear APIs, same completion across queue replacement, and finish drain |
| Element/render-state teardown cancels observation | `Promise_is_canceled_when_render_state_is_cleared` | Observes cancellation rather than a stranded task |
| Generated inventory advertises Promise | `Queue_operations_are_advertised_in_generated_operation_metadata` | Asserts exact metadata membership |

Validation: focused queue unit tests, full solution tests/build, and `git diff --check`.

---

# Sprint 5 tenth-increment test plan — measured slide effects

| Requirement | Test | Evidence |
|---|---|---|
| APIs enqueue without starting, validate duration/queue, and handle empty selection | `Slide_effects_enqueue_without_start_and_validate_inputs` | No measurement request before dequeue; invalid values leave state untouched |
| `scrollHeight` read is batched, cache-invalidated, and uses the read-only bridge | `SlideDown_measures_scroll_height_and_emits_target_animation`; `QueryMeasurementCoordinator` bridge test | Verifies request kind/key and measured CSS target |
| Slide down completes to natural layout and slide up hides only after animation end | `SlideDown_restores_natural_height_after_animation_end`; `SlideUp_hides_only_after_animation_end` | Queue remains pending until matching typed event; terminal state asserted |
| Toggle chooses direction from Query-owned visibility | `SlideToggle_uses_query_visibility_at_execution_time` | Tests both hidden and visible selected states |
| Finish, Stop, teardown, and independent event completion | `SlideFinish_applies_terminal_visibility_without_waiting_for_measurement_or_event`; `SlideFinish_completes_active_effect_without_animation_end`; `SlideStop_cancels_active_measurement_and_restores_initial_visibility`; `Slide_teardown_cancels_queue_and_clears_temporary_styles`; component integration test | Covers pending/active finish, stop, teardown, and completion of separately selected elements |
| Caller CSS survives temporary overlays and cleanup | `Slide_effects_restore_caller_owned_styles` | Assertions before, during, and after temporary ownership |
| Metadata and rendered keyframes/custom event completion | `Slide_effects_validate_inputs_before_enqueuing_and_are_advertised`; `Slide_effects_render_measured_target_and_complete_from_Blazor_animation_event` | Metadata plus rendered CSS and completion through the registered Blazor custom event |

Validation: focused unit/component tests, full solution tests/build, and `git diff --check`.

---

# Sprint 5 eleventh increment plan — browser/runtime exit evidence

1. Inspect existing demo hosting, test infrastructure, and the packaged RCL static-web-assets manifest before adding a browser harness; avoid coupling test code to user browser profiles.
2. Add deterministic browser scenarios for JS initializer registration, measured slide-down/up/toggle completion, and navigation cleanup.
3. Run the same scenario against Server, WebAssembly, and Auto. Record which host and actual runtime was used; Auto must distinguish initial server execution from client takeover.
4. Add a small, repeatable benchmark for 1/10/100/1000 selected keys, separating measurement batch counts, allocations, and client rendering time from Server transport latency.
5. Update the Sprint 5 exit record only with observed evidence. Dependency advisories and Release/packed-asset checks remain independent release gates.

The prior session's automation inventory found no repository Playwright project. Inspect installed/available test tooling and project conventions first; add dependencies only through the existing repository's approved package workflow. If a host/browser cannot be started in this environment, document the exact limitation and retain the gate as open instead of claiming completion.

Completed manual smoke slice: expose slide toggles in the Server, WebAssembly, and Auto demos and use an isolated Playwright CLI browser. Verified the RCL custom-event initializer and measurement module load; both slide directions restore natural/terminal state in Server and WebAssembly. In Auto, observed initial Server SignalR execution, then WebAssembly execution after a cached-runtime reload (202 `.wasm` resources, no new SignalR connection). Component tests now cover navigation and element disposal during an active slide; full-solution evidence is 204 unit + 45 component tests. Still required: committed repeatable E2E tests, including a real Server circuit disconnect/reconnect case that distinguishes a still-running browser animation from an animation whose completion event was missed; performance baselines; and release/package checks. Microsoft documents the RCL `beforeServerStart` initializer and reconnect state events, but a reconnect notification alone cannot establish that an animation completed. The browser-side reconciliation must be based on the actual animation state and must not write DOM state.

---

# Sprint 5 reconnect and async-slide implementation plan

1. Convert `SlideDown`, `SlideUp`, and `SlideToggle` to awaitable direct operations; retain queue composition with `QueueSlideDown`, `QueueSlideUp`, and `QueueSlideToggle`; add all public names to generated operation metadata.
2. Add a Server demo route with readiness/status signals for a real circuit scenario.
3. Add Playwright tests that proxy and deliberately close the actual `_blazor` WebSocket, prevent reconnect until the test releases it, and separately assert an in-progress animation and a native animation-end event while offline.
4. Implement reconnect recovery only after browser confirmation: inspect finished Query slide `CSSAnimation` instances, replay the registered Blazor event once, and never style or mutate DOM nodes from JavaScript.
5. Update README, measurement/effect boundary, sprint plan, and evidence record. Run unit/component suite, three demo builds, browser tests, and diff validation.

Completion evidence: `a slide that is still running after reconnect completes normally` and `a slide completed while disconnected settles after reconnect`; CI integration, WebAssembly/Auto E2E expansion, performance baselines, and release gates remain open.

---

# Sprint 5 fourth-increment test plan — Animate

| Requirement | Test | Evidence |
|---|---|---|
| Apply transition and every property to each match in one batch | `Animate_sets_transition_and_targets_for_each_resolved_element_in_one_batch` | Exact CSS rules on multiple keys and one Changed event |
| Reject invalid input before mutation | `Animate_rejects_invalid_properties_and_negative_duration_before_mutation` | Invalid property, null value, and negative duration leave rules empty |
| Empty maps/selections are no-ops | `Animate_is_a_no_op_for_empty_properties_or_selection` | Both empty inputs yield no rules or notifications |
| CSS output deterministic and culture invariant | `Animate_formats_duration_invariantly_and_orders_properties` | fr-FR culture, precise duration, and stable transition string |
| Metadata and component render | `Animate_is_advertised_in_generated_operation_metadata`; component test | Inventory plus QueryHost style-tree output |

Validation: focused unit/component tests followed by full solution tests/build and `git diff --check`.
