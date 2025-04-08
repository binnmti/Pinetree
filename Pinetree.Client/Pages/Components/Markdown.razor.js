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
        element.focus();
        if (document.execCommand && document.queryCommandSupported('insertText')) {
            element.setSelectionRange(start, end);
            document.execCommand('insertText', false, text);
        }
        else {
            const beforeSelection = element.value.substring(0, start);
            const afterSelection = element.value.substring(end);
            element.value = beforeSelection + text + afterSelection;
            const inputEvent = new InputEvent('input', {
                bubbles: true,
                cancelable: true,
                inputType: 'insertText',
                data: text
            });
            element.dispatchEvent(inputEvent);
        }
        element.selectionStart = start;
        element.selectionEnd = start + text.length;
        return true;
    }
    return false;
}
export function setTextAreaValue(element, text, dispatchEvent = true) {
    if (element) {
        const oldValue = element.value;
        element.value = text;
        if (dispatchEvent) {
            const inputEvent = new InputEvent('input', {
                bubbles: true,
                cancelable: true,
                inputType: 'insertFromPaste',
                data: text
            });
            element.dispatchEvent(inputEvent);
        }
        return oldValue;
    }
    return null;
}
export function setCaretPosition(element, start, end) {
    element.focus();
    element.setSelectionRange(start, end);
}
export function initializeTooltips() {
    const tooltipTriggerList = Array.from(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach(tooltipTriggerEl => {
        new window.bootstrap.Tooltip(tooltipTriggerEl);
    });
}
export function setupLinkInterceptor(markdownContainer, dotNetHelper) {
    markdownContainer.addEventListener('click', async (e) => {
        const target = e.target;
        if (target.tagName === 'A') {
            const linkElement = target;
            const href = linkElement.getAttribute('href');
            if (href && href.startsWith('//')) {
                e.preventDefault();
                const id = href.substring(2);
                await dotNetHelper.invokeMethodAsync('HandleMarkdownLinkClick', id);
            }
        }
    });
}
export function setupBeforeUnloadWarning(dotNetHelper) {
    let bypassBeforeUnload = false;
    window.addEventListener('beforeunload', async function (e) {
        if (bypassBeforeUnload) {
            bypassBeforeUnload = false;
            return;
        }
        try {
            const hasPendingChanges = await dotNetHelper.invokeMethodAsync('HasPendingChanges');
            if (hasPendingChanges) {
                e.preventDefault();
                e.returnValue = "";
                return "";
            }
        }
        catch (error) {
            console.error('Error checking for pending changes:', error);
        }
    });
    document.addEventListener('click', async (e) => {
        const target = e.target;
        const anchor = target.closest('a');
        if (anchor && !anchor.getAttribute('data-no-guard')) {
            const href = anchor.getAttribute('href');
            if (href && href.startsWith('//')) {
                return;
            }
            if (href && !href.startsWith('#') && !href.startsWith('javascript:')) {
                try {
                    const hasPendingChanges = await dotNetHelper.invokeMethodAsync('HasPendingChanges');
                    if (hasPendingChanges) {
                        if (confirm('Your changes have not been saved. Are you sure you want to leave this page?')) {
                            bypassBeforeUnload = true;
                        }
                        else {
                            e.preventDefault();
                            e.stopPropagation();
                        }
                    }
                }
                catch (error) {
                    console.error('Navigation guard error:', error);
                }
            }
        }
    }, true);
    const pageUrl = window.location.href;
    window.history.pushState({ page: 1 }, '', pageUrl);
    window.addEventListener('popstate', async function (e) {
        window.history.pushState({ page: 1 }, '', pageUrl);
        try {
            const hasPendingChanges = await dotNetHelper.invokeMethodAsync('HasPendingChanges');
            if (hasPendingChanges) {
                const confirmed = confirm('Your changes have not been saved. Are you sure you want to leave this page?');
                if (confirmed) {
                    window.location.href = document.referrer || '/';
                }
            }
        }
        catch (error) {
            console.error('Error checking for pending changes:', error);
        }
    });
}
export function setupKeyboardShortcuts(element, dotNetHelper) {
    element.addEventListener('keydown', async (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key === 'z') {
            e.preventDefault();
            await dotNetHelper.invokeMethodAsync('HandleUndoShortcut');
            return false;
        }
        if ((e.ctrlKey || e.metaKey) && (e.key === 'y' || (e.shiftKey && e.key === 'z'))) {
            e.preventDefault();
            await dotNetHelper.invokeMethodAsync('HandleRedoShortcut');
            return false;
        }
    });
}
export function enableContinuousList(element) {
    if (!element)
        return;
    //const updateTextArea = (before: string, after: string, marker: string, position: number) => {
    //    element.value = before + '\n' + marker + after;
    //    element.selectionStart = element.selectionEnd = position;
    //    element.dispatchEvent(new Event('input', { bubbles: true }));
    //};
    const handleMatch = (match, currentLine, lineStart, selStart, markerGenerator) => {
        const before = currentLine.trim() === match[0].trim() ? element.value.substring(0, lineStart) : element.value.substring(0, selStart);
        const after = element.value.substring(selStart);
        const marker = markerGenerator(match);
        const position = currentLine.trim() === match[0].trim() ? lineStart + 1 : selStart + 1 + marker.length;
        //updateTextArea(before, after, marker, position);
        element.value = before + '\n' + marker + after;
        element.selectionStart = element.selectionEnd = position;
        element.dispatchEvent(new Event('input', { bubbles: true }));
    };
    element.addEventListener('keydown', (e) => {
        if (e.key !== 'Enter')
            return;
        const text = element.value;
        const selStart = element.selectionStart;
        let lineStart = selStart;
        while (lineStart > 0 && text[lineStart - 1] !== '\n')
            lineStart--;
        const currentLine = text.substring(lineStart, selStart);
        const checkboxPattern = /^(\s*)(-\s+\[[ x]?\])\s+/;
        const bulletPattern = /^(\s*)([-+*]|(\d+)\.|\>)\s+/;
        const checkboxMatch = currentLine.match(checkboxPattern);
        if (checkboxMatch) {
            e.preventDefault();
            handleMatch(checkboxMatch, currentLine, lineStart, selStart, (match) => {
                const indentation = match[1] || '';
                return currentLine.trim() === match[0].trim() ? '' : indentation + '- [ ] ';
            });
            return;
        }
        const bulletMatch = currentLine.match(bulletPattern);
        if (bulletMatch) {
            e.preventDefault();
            handleMatch(bulletMatch, currentLine, lineStart, selStart, (match) => {
                let marker = match[0];
                if (match[3]) {
                    const num = parseInt(match[3]);
                    marker = marker.replace(/\d+/, (num + 1).toString());
                }
                return currentLine.trim() === match[0].trim() ? '' : marker;
            });
        }
    });
}
//export function enableContinuousList(element: HTMLTextAreaElement): void {
//    if (!element) return;
//    element.addEventListener('keydown', (e) => {
//        if (e.key === 'Enter') {
//            const text = element.value;
//            const selStart = element.selectionStart;
//            let lineStart = selStart;
//            while (lineStart > 0 && text[lineStart - 1] !== '\n') {
//                lineStart--;
//            }
//            const currentLine = text.substring(lineStart, selStart);
//            const checkboxPattern = /^(\s*)(-\s+\[[ x]?\])\s+/;
//            const checkboxMatch = currentLine.match(checkboxPattern);
//            if (checkboxMatch) {
//                e.preventDefault();
//                const updateTextArea = (before: string, after: string, marker: string, position: number) => {
//                    element.value = before + '\n' + marker + after;
//                    element.selectionStart = element.selectionEnd = position;
//                    element.dispatchEvent(new Event('input', { bubbles: true }));
//                };
//                const before = text.substring(0, currentLine.trim() === checkboxMatch[0].trim() ? lineStart : selStart);
//                const after = text.substring(selStart);
//                if (currentLine.trim() === checkboxMatch[0].trim()) {
//                    updateTextArea(before, after, '', lineStart + 1);
//                } else {
//                    const indentation = checkboxMatch[1];
//                    const marker = indentation + '- [ ] ';
//                    const position = selStart + 1 + marker.length;
//                    updateTextArea(before, after, marker, position);
//                }
//                return;
//            }
//            const bulletPattern = /^(\s*)([-+*]|(\d+)\.|\>)\s+/;
//            const match = currentLine.match(bulletPattern);
//            if (match) {
//                e.preventDefault();
//                const updateTextArea = (before: string, after: string, marker: string, position: number) => {
//                    element.value = before + '\n' + marker + after;
//                    element.selectionStart = element.selectionEnd = position;
//                    element.dispatchEvent(new Event('input', { bubbles: true }));
//                };
//                const before = text.substring(0, currentLine.trim() === match[0].trim() ? lineStart : selStart);
//                const after = text.substring(selStart);
//                if (currentLine.trim() === match[0].trim()) {
//                    updateTextArea(before, after, '', lineStart + 1);
//                } else {
//                    let marker = match[0];
//                    if (match[3]) { // Numbered list
//                        const num = parseInt(match[3]);
//                        marker = marker.replace(/\d+/, (num + 1).toString());
//                    }
//                    const position = selStart + 1 + marker.length;
//                    updateTextArea(before, after, marker, position);
//                }
//            }
//        }
//    });
//}
//# sourceMappingURL=Markdown.razor.js.map