export function measure(requests) {
    return requests.map(request => {
        const element = document.querySelector(`[data-rbq-key="${CSS.escape(request.elementKey)}"]`);
        if (!element) {
            return { elementKey: request.elementKey, kind: request.kind, value: null };
        }

        const rect = element.getBoundingClientRect();
        let value;
        switch (request.kind) {
            case 0: value = rect.height; break;
            case 1: value = rect.width; break;
            default: value = null; break;
        }

        return { elementKey: request.elementKey, kind: request.kind, value };
    });
}
