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
//# sourceMappingURL=Markdown.razor.js.map