"use strict";
// @ts-nocheck
/**
 * Markdown interaction utilities for Blazor
 */
(function () {
    // Declare global namespace for our functions
    window.MarkdownJS = {
        /**
         * Gets the current text selection from a textarea
         */
        getTextAreaSelection: (element) => {
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
        replaceTextAreaSelection: (element, text) => {
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
        setCaretPosition: (element, start, end) => {
            element.focus();
            element.setSelectionRange(start, end);
        },
        /**
         * Initializes Bootstrap tooltips
         */
        initializeTooltips: () => {
            const tooltipTriggerList = Array.from(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
            tooltipTriggerList.forEach(tooltipTriggerEl => {
                new window.bootstrap.Tooltip(tooltipTriggerEl);
            });
        },
        /**
         * Sets up a link interceptor for special markdown links
         */
        setupLinkInterceptor: (container, dotNetRef) => {
            container.addEventListener('click', (e) => {
                const target = e.target;
                if (target.tagName === 'A') {
                    const linkElement = target;
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
})();
//# sourceMappingURL=Markdown.js.map