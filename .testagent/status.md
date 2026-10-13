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
