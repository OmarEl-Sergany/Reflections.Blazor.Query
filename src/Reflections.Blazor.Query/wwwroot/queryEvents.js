(() => {
    const delegatedEvents = [
        "click", "change", "submit", "focusin", "focusout", "select", "keydown", "keyup", "keypress",
        "mousedown", "mouseup", "mousemove", "mouseover", "mouseout", "contextmenu", "dblclick", "scroll"
    ];

    const closestQueryElementKey = event => {
        const target = event?.target;
        return target instanceof Element
            ? target.closest("[data-rbq-key]")?.getAttribute("data-rbq-key") ?? null
            : null;
    };

    window.ReflectionsBlazorQuery = window.ReflectionsBlazorQuery || {};
    window.ReflectionsBlazorQuery.registerDelegatedEvents = () => {
        for (const eventName of delegatedEvents) {
            Blazor.registerCustomEventType(`rbq${eventName}`, {
                browserEventName: eventName,
                createEventArgs: event => ({ elementKey: closestQueryElementKey(event) })
            });
        }
    };
})();
