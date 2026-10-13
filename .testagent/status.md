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
