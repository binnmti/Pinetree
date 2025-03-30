/**
 * Markdown interaction utilities for Blazor
 */

// Type definition for the DotNet namespace
declare namespace DotNet {
    interface DotNetObject {
        invokeMethodAsync<T>(methodName: string, ...args: any[]): Promise<T>;
        dispose(): void;
    }
}

// Declare global namespace for our functions
declare global {
    interface Window {
        MarkdownJS: {
            getTextAreaSelection: (element: HTMLTextAreaElement) => { text: string; start: number; end: number };
            replaceTextAreaSelection: (element: HTMLTextAreaElement, text: string) => boolean;
            setCaretPosition: (element: HTMLTextAreaElement, start: number, end: number) => void;
            initializeTooltips: () => void;
            setupLinkInterceptor: (container: HTMLElement, dotNetRef: DotNet.DotNetObject) => void;
        };
    }
}

// Create the global object to hold our functions
window.MarkdownJS = {
    /**
     * Gets the current text selection from a textarea
     */
    getTextAreaSelection: (element: HTMLTextAreaElement) => {
        if (element) {
            return {
                text: element.value.substring(element.selectionStart, element.selectionEnd),
                start: element.selectionStart,
                end: element.selectionEnd
            };
        }
        return { text: '', start: 0, end: 0 };
    },

    /**
     * Replaces the selected text in a textarea
     */
    replaceTextAreaSelection: (element: HTMLTextAreaElement, text: string) => {
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
    },

    /**
     * Sets the caret position in a textarea
     */
    setCaretPosition: (element: HTMLTextAreaElement, start: number, end: number) => {
        element.focus();
        element.setSelectionRange(start, end);
    },

    /**
     * Initializes Bootstrap tooltips
     */
    initializeTooltips: () => {
        const tooltipTriggerList = Array.from(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.forEach(tooltipTriggerEl => {
            new (window as any).bootstrap.Tooltip(tooltipTriggerEl);
        });
    },

    /**
     * Sets up a link interceptor for special markdown links
     */
    setupLinkInterceptor: (container: HTMLElement, dotNetRef: DotNet.DotNetObject) => {
        container.addEventListener('click', (e: Event) => {
            const target = e.target as HTMLElement;
            if (target.tagName === 'A') {
                const linkElement = target as HTMLAnchorElement;
                const href = linkElement.getAttribute('href');
                if (href && href.startsWith('//')) {
                    e.preventDefault();
                    const id = href.substring(2);
                    dotNetRef.invokeMethodAsync('HandleMarkdownLinkClick', id);
                }
            }
        });
    }
};

// Required for TypeScript modules
export { };
