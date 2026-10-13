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
