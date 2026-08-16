export function measure(requests) {
    return requests.map(request => {
        const element = document.querySelector(`[data-rbq-key="${CSS.escape(request.elementKey)}"]`);
        if (!element) {
            return { elementKey: request.elementKey, kind: request.kind, value: null };
        }

        const rect = element.getBoundingClientRect();
        const style = getComputedStyle(element);
        const borderHeight = cssPixels(style.borderTopWidth) + cssPixels(style.borderBottomWidth);
        const borderWidth = cssPixels(style.borderLeftWidth) + cssPixels(style.borderRightWidth);
        let value;
        switch (request.kind) {
            case 0: value = rect.height; break;
            case 1: value = rect.width; break;
            case 2: value = rect.height - borderHeight; break;
            case 3: value = rect.width - borderWidth; break;
            case 4: value = rect.height; break;
            case 5: value = rect.width; break;
            default: value = null;
        }

        return { elementKey: request.elementKey, kind: request.kind, value };
    });
}

function cssPixels(value) {
    const parsed = Number.parseFloat(value);
    return Number.isFinite(parsed) ? parsed : 0;
}
