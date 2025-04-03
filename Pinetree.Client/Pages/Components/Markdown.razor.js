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
    window.addEventListener('beforeunload', async function (e) {
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
            if (href && !href.startsWith('#') && !href.startsWith('javascript:')) {
                try {
                    const hasPendingChanges = await dotNetHelper.invokeMethodAsync('HasPendingChanges');
                    if (hasPendingChanges) {
                        if (!confirm('Your changes have not been saved. Are you sure you want to leave this page?')) {
                            e.preventDefault();
                            e.stopPropagation();
                            return false;
                        }
                    }
                }
                catch (error) {
                    console.error('Navigation guard error:', error);
                }
            }
        }
    }, true);
    //window.addEventListener('popstate', async function (e) {
    //    try {
    //        const hasPendingChanges = await dotNetHelper.invokeMethodAsync<boolean>('HasPendingChanges');
    //        if (hasPendingChanges) {
    //            if (!confirm('Your changes have not been saved. Are you sure you want to leave this page?')) {
    //                window.history.back();
    //                //history.pushState(null, "", location.href);
    //                //history.go(2);
    //                e.preventDefault();
    //            }
    //        }
    //    } catch (error) {
    //        console.error('Error checking for pending changes on navigation:', error);
    //    }
    //});
}
//# sourceMappingURL=Markdown.razor.js.map