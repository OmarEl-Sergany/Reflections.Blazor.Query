# Component Query Scoping

## Current contract

Component-level query isolation is implemented through `QueryScopeHost` and the cascaded `IQueryScope` capability. It uses the application's existing scoped services; it does not create a nested DI container or expose a `ComponentBase`/DOM reference.

```razor
<QueryScopeHost>
    <AccordionControl />
</QueryScopeHost>
```

Descendant `QueryElement` components inherit the scope key unless they explicitly provide `ScopeKey`. A reusable control can receive the nearest scope and query through it:

```razor
@code {
    [CascadingParameter] private IQueryScope QueryScope { get; set; } = default!;

    private void ToggleBody() => QueryScope.Select(".accordion-body").ToggleClass("show");
}
```

`IQueryScope.Select` filters initial selector resolution and subsequent selection/traversal results to the same scope key. It also constrains structural operations and cleanup so a scoped selection cannot take ownership of or remove an element registered to a different scope. Nested `QueryScopeHost` instances establish a separate scope; an outer query does not absorb nested-scope elements.

The scope is a **logical API isolation boundary**, not a security boundary. Code that can resolve the unrestricted `BlazorQuery` or state services can still intentionally orchestrate outside the component scope. Use the scoped capability in reusable controls and reserve `BlazorQuery`, `LayoutQuery`, and `ApplicationQuery` for deliberate broader orchestration.

## Ownership and lifecycle

- `QueryElement` registers its stable key, parent key, selector metadata, and explicit or cascaded scope key in `QueryElementRegistry`.
- `QueryScopeHost` creates one stable scope per component instance and cascades it to its child content.
- Each scoped selection retains its scope key across fluent operations and filters keys before applying state changes.
- Disposing a `QueryElement` unregisters that element; removing scoped content follows the existing element/render-state ownership rules.
- Component scopes share the app's normal service lifetime. No per-component `IServiceScope` is created.

## Evidence and remaining verification

Unit tests cover selection isolation between sibling scopes, traversal within a scope, and preserving a nested-scope descendant when an outer scope empties its content. Renderer-level bUnit tests additionally cover the public host contract: `QueryScopeHost_cascades_isolated_scopes_across_repeated_and_nested_hosts` verifies distinct scope keys, nested/repeated host isolation, and mutation confinement; `QueryScopeHost_rerender_preserves_scope_and_removing_descendant_unregisters_it` verifies stable scope identity and element unregister/register through conditional rerender.
