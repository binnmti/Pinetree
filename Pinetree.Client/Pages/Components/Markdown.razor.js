export function getTextAreaSelection(element) {
    if (element) {
        return {
            text: element.value.substring(element.selectionStart, element.selectionEnd),
            start: element.selectionStart,
            end: element.selectionEnd
        };
    }
    return { text: '', start: 0, end: 0 };
}
export function replaceTextAreaSelection(element, text) {
    if (element) {
        const start = element.selectionStart;
        const end = element.selectionEnd;
        const beforeSelection = element.value.substring(0, start);
        const afterSelection = element.value.substring(end);
        element.value = beforeSelection + text + afterSelection;
        element.selectionStart = start;
        element.selectionEnd = start + text.length;
        return true;
    }
    return false;
}
export function setupLinkInterceptor(container, dotNetRef) {
    container.addEventListener('click', (e) => {
        const target = e.target;
        if (target.tagName === 'A') {
            const linkElement = target;
            const href = linkElement.getAttribute('href');
            if (href && href.startsWith('/Edit/')) {
                e.preventDefault();
                const id = href.substring(6);
                dotNetRef.invokeMethodAsync('HandleMarkdownLinkClick', id);
            }
        }
    });
}
//# sourceMappingURL=Markdown.razor.js.map