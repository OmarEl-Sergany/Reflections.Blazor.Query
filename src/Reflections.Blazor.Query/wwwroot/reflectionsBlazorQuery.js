window.ReflectionsBlazorQuery = { setCss: (selector, property, value) => document.querySelectorAll(selector).forEach(e => e.style.setProperty(property, value)) };
