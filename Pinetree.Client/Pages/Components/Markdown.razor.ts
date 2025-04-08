export function getTextAreaSelection(element: HTMLTextAreaElement): { text: string; start: number; end: number } {
    if (element) {
        return {
            text: element.value.substring(element.selectionStart, element.selectionEnd),
            start: element.selectionStart,
            end: element.selectionEnd
        };
    }
    return { text: '', start: 0, end: 0 };
}

export function replaceTextAreaSelection(element: HTMLTextAreaElement, text: string): boolean {
    if (element) {
        const start = element.selectionStart;
        const end = element.selectionEnd;
        element.focus();
        if (document.execCommand && document.queryCommandSupported('insertText')) {
            element.setSelectionRange(start, end);
            document.execCommand('insertText', false, text);
        } else {
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
        return true;    }
    return false;
}

export function setTextAreaValue(element: HTMLTextAreaElement, text: string, dispatchEvent = true) {
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

export function setCaretPosition(element: HTMLTextAreaElement, start: number, end: number) {
    element.focus();
    element.setSelectionRange(start, end);
}

export function initializeTooltips(): void {
    const tooltipTriggerList = Array.from(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach(tooltipTriggerEl => {
        new (window as any).bootstrap.Tooltip(tooltipTriggerEl);
    });
}

interface DotNetObject {
    invokeMethodAsync<T>(methodName: string, ...args: any[]): Promise<T>;
    dispose(): void;
}

export function setupLinkInterceptor(markdownContainer: HTMLElement, dotNetHelper: DotNetObject): void {
    markdownContainer.addEventListener('click', async (e: Event) => {
        const target = e.target as HTMLElement;
        if (target.tagName === 'A') {
            const linkElement = target as HTMLAnchorElement;
            const href = linkElement.getAttribute('href');
            if (href && href.startsWith('//')) {
                e.preventDefault();
                const id = href.substring(2);
                await dotNetHelper.invokeMethodAsync('HandleMarkdownLinkClick', id);
            }
        }
    });
}

export function setupBeforeUnloadWarning(dotNetHelper: DotNetObject): void {
    let bypassBeforeUnload = false;
    window.addEventListener('beforeunload', async function (e: BeforeUnloadEvent) {
        if (bypassBeforeUnload) {
            bypassBeforeUnload = false;
            return;
        }
        try {
            const hasPendingChanges = await dotNetHelper.invokeMethodAsync<boolean>('HasPendingChanges');
            if (hasPendingChanges) {
                e.preventDefault();
                e.returnValue = "";
                return "";
            }
        } catch (error) {
            console.error('Error checking for pending changes:', error);
        }
    });

    document.addEventListener('click', async (e: MouseEvent) => {
        const target = e.target as HTMLElement;
        const anchor = target.closest('a') as HTMLAnchorElement;
        if (anchor && !anchor.getAttribute('data-no-guard')) {
            const href = anchor.getAttribute('href');
            if (href && href.startsWith('//')) {
                return;
            }
            if (href && !href.startsWith('#') && !href.startsWith('javascript:')) {
                try {
                    const hasPendingChanges = await dotNetHelper.invokeMethodAsync<boolean>('HasPendingChanges');
                    if (hasPendingChanges) {
                        if (confirm('Your changes have not been saved. Are you sure you want to leave this page?')) {
                            bypassBeforeUnload = true;
                        } else {
                            e.preventDefault();
                            e.stopPropagation();
                        }
                    }
                } catch (error) {
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
            const hasPendingChanges = await dotNetHelper.invokeMethodAsync<boolean>('HasPendingChanges');
            if (hasPendingChanges) {
                const confirmed = confirm('Your changes have not been saved. Are you sure you want to leave this page?');
                if (confirmed) {
                    window.location.href = document.referrer || '/';
                }
            }
        } catch (error) {
            console.error('Error checking for pending changes:', error);
        }
    });
}

export function setupKeyboardShortcuts(element: HTMLTextAreaElement, dotNetHelper: DotNetObject) {
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

export function enableContinuousList(element: HTMLTextAreaElement): void {
    if (!element) return;

    const handleMatch = (
        match: RegExpMatchArray,
        currentLine: string,
        lineStart: number,
        selStart: number,
        markerGenerator: (match: RegExpMatchArray) => string
    ) => {
        const before = currentLine.trim() === match[0].trim() ? element.value.substring(0, lineStart) : element.value.substring(0, selStart);
        const after = element.value.substring(selStart);
        const marker = markerGenerator(match);
        const position = currentLine.trim() === match[0].trim() ? lineStart + 1 : selStart + 1 + marker.length;
        element.value = before + '\n' + marker + after;
        element.selectionStart = element.selectionEnd = position;
        element.dispatchEvent(new Event('input', { bubbles: true }));
    };

    element.addEventListener('keydown', (e) => {
        if (e.key !== 'Enter') return;

        const text = element.value;
        const selStart = element.selectionStart;
        let lineStart = selStart;
        while (lineStart > 0 && text[lineStart - 1] !== '\n') lineStart--;

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
