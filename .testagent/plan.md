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
