export function measure(requests) {
    return requests.map(request => {
        const element = document.querySelector(`[data-rbq-key="${CSS.escape(request.elementKey)}"]`);
        if (!element) {
            return { elementKey: request.elementKey, kind: request.kind, value: null, point: null, relatedElementKey: null };
        }

        const rect = element.getBoundingClientRect();
        const style = getComputedStyle(element);
        const borderHeight = cssPixels(style.borderTopWidth) + cssPixels(style.borderBottomWidth);
        const borderWidth = cssPixels(style.borderLeftWidth) + cssPixels(style.borderRightWidth);
        let value;
        let point = null;
        let relatedElementKey = null;
        switch (request.kind) {
            case 0: value = rect.height; break;
            case 1: value = rect.width; break;
            case 2: value = rect.height - borderHeight; break;
            case 3: value = rect.width - borderWidth; break;
            case 4: value = rect.height; break;
            case 5: value = rect.width; break;
            case 6:
                point = { left: rect.left + window.scrollX, top: rect.top + window.scrollY };
                break;
            case 7: {
                const offsetParent = element.offsetParent;
                if (offsetParent) {
                    const parentRect = offsetParent.getBoundingClientRect();
                    const parentStyle = getComputedStyle(offsetParent);
                    point = {
                        left: rect.left - parentRect.left - cssPixels(parentStyle.borderLeftWidth) + offsetParent.scrollLeft,
                        top: rect.top - parentRect.top - cssPixels(parentStyle.borderTopWidth) + offsetParent.scrollTop
                    };
                }
                break;
            }
            case 8: {
                const offsetParent = element.offsetParent;
                relatedElementKey = offsetParent?.getAttribute("data-rbq-key") ?? null;
                break;
            }
            default: value = null;
        }

        return { elementKey: request.elementKey, kind: request.kind, value, point, relatedElementKey };
    });
}

function cssPixels(value) {
    const parsed = Number.parseFloat(value);
    return Number.isFinite(parsed) ? parsed : 0;
}
