const registeredBlazorInstances = new WeakSet();
const registeredReconnectInstances = new WeakSet();
const reconciledSlideAnimations = new WeakSet();

function registerAnimationEnd(blazor) {
    if (registeredBlazorInstances.has(blazor)) {
        return;
    }

    blazor.registerCustomEventType("rbqanimationend", {
        browserEventName: "animationend",
        createEventArgs: () => ({})
    });
    registeredBlazorInstances.add(blazor);
}

function registerSlideReconnectRecovery(blazor) {
    if (registeredReconnectInstances.has(blazor)) {
        return;
    }

    const reconnectModal = document.getElementById("components-reconnect-modal");
    if (!reconnectModal) {
        return;
    }

    reconnectModal.addEventListener("components-reconnect-state-changed", event => {
        if (event.detail?.state !== "hide") {
            return;
        }

        for (const element of document.querySelectorAll("[data-rbq-key]")) {
            for (const animation of element.getAnimations()) {
                if (!(animation instanceof CSSAnimation)
                    || animation.animationName !== "rbq-slide-down" && animation.animationName !== "rbq-slide-up"
                    || animation.playState !== "finished"
                    || reconciledSlideAnimations.has(animation)) {
                    continue;
                }

                reconciledSlideAnimations.add(animation);
                element.dispatchEvent(new AnimationEvent("animationend", {
                    bubbles: true,
                    animationName: animation.animationName
                }));
            }
        }
    });

    registeredReconnectInstances.add(blazor);
}

export function afterWebStarted(blazor) {
    registerAnimationEnd(blazor);
    registerSlideReconnectRecovery(blazor);
}

export function afterStarted(blazor) {
    registerAnimationEnd(blazor);
    registerSlideReconnectRecovery(blazor);
}
