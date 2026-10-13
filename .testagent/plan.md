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
