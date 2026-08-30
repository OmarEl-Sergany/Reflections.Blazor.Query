const submittable = /^(?:input|select|textarea|button|keygen)$/i;
const submitterTypes = /^(?:submit|button|image|reset|file)$/i;
const checkableType = /^(?:checkbox|radio)$/i;

export function serialize(requests) {
    return requests.map(request => {
        const element = document.querySelector(`[data-rbq-key="${CSS.escape(request.elementKey)}"]`);
        if (!element) {
            return { elementKey: request.elementKey, fields: [] };
        }

        const controls = element.elements && element.elements.length !== undefined
            ? element.elements
            : submittable.test(element.tagName)
                ? [element]
                : element.querySelectorAll("input, select, textarea, button, keygen");

        const fields = [];
        for (const control of controls) {
            if (!control.name || control.disabled) continue;
            if (!submittable.test(control.tagName)) continue;
            const type = (control.type || "").toLowerCase();
            if (submitterTypes.test(type)) continue;
            if (checkableType.test(type) && !control.checked) continue;

            const values = controlValues(control);
            for (const value of values) {
                fields.push({ name: control.name, value });
            }
        }
        return { elementKey: request.elementKey, fields };
    });
}

function controlValues(control) {
    if (control.tagName === "SELECT" && control.multiple) {
        const selected = Array.from(control.selectedOptions || []);
        if (selected.length === 0) return [];
        return selected.map(option => option.value == null ? "" : option.value);
    }
    const value = control.value;
    if (value == null) return [];
    return [value];
}
