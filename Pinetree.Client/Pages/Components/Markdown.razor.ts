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
        element.selectionStart = start;
        element.selectionEnd = start + text.length;
        return true;
    }
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

export async function openFileDialogAndGetBlobUrl(): Promise<{ blobUrl: string, fileName: string }> {
    try {
        const maxSizeMB = 5;
        const file = await openFileDialog(maxSizeMB);
        if (!file) {
            return { blobUrl: '', fileName: '' };
        }

        const imageId = await saveFileToIndexedDB(file);
        const blobUrl = await getImageBlobUrl(imageId);

        return {
            blobUrl: blobUrl,
            fileName: file.name
        };
    } catch (error) {
        console.error("Error handling file:", error);
        return { blobUrl: '', fileName: '' };
    }
}

export async function clearAllImagesFromIndexedDB(): Promise<void> {
    return new Promise((resolve, reject) => {
        const dbName = 'PinetreeImageDB';
        const storeName = 'images';
        const request = indexedDB.open(dbName, 1);

        request.onerror = () => {
            reject('IndexedDB error: ' + request.error);
        };

        request.onsuccess = () => {
            const db = request.result;
            const transaction = db.transaction([storeName], 'readwrite');
            const store = transaction.objectStore(storeName);

            const clearRequest = store.clear();

            clearRequest.onsuccess = () => {
                console.log('All images cleared from IndexedDB');
                resolve();
            };

            clearRequest.onerror = () => {
                reject('Error clearing images: ' + clearRequest.error);
            };
        };
    });
}

export function setupAllEventListeners(
    container: HTMLElement,
    textArea: HTMLTextAreaElement,
    dotNetHelper: DotNetObject
): void {
    setupLinkInterceptor(container, dotNetHelper);
    setupBeforeUnloadWarning(dotNetHelper);
    setupKeyboardShortcuts(textArea, dotNetHelper);
    enableContinuousList(textArea);
    initializeTooltips();
    setupScrollSync(textArea, container);
    setupDropZone(textArea, dotNetHelper);
}

interface DotNetObject {
    invokeMethodAsync<T>(methodName: string, ...args: any[]): Promise<T>;
    dispose(): void;
}

function initializeTooltips(): void {
    const tooltipTriggerList = Array.from(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach(tooltipTriggerEl => {
        new (window as any).bootstrap.Tooltip(tooltipTriggerEl);
    });
}

function setupLinkInterceptor(markdownContainer: HTMLElement, dotNetHelper: DotNetObject): void {
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

function setupBeforeUnloadWarning(dotNetHelper: DotNetObject): void {
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
                return undefined;
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
        if (e.key === 'Tab') {
            e.preventDefault();

            const start = element.selectionStart;
            const end = element.selectionEnd;

            if (start === end) {
                const before = element.value.substring(0, start);
                const after = element.value.substring(end);
                element.value = before + '  ' + after;
                element.selectionStart = element.selectionEnd = start + 2;
            } else {
                const selectedText = element.value.substring(start, end);
                const lines = selectedText.split('\n');

                if (e.shiftKey) {
                    const indentedLines = lines.map(line =>
                        line.startsWith('  ') ? line.substring(2) : line
                    );
                    const newText = indentedLines.join('\n');

                    element.value = element.value.substring(0, start) + newText + element.value.substring(end);
                    element.selectionStart = start;
                    element.selectionEnd = start + newText.length;
                } else {
                    const indentedLines = lines.map(line => '  ' + line);
                    const newText = indentedLines.join('\n');

                    element.value = element.value.substring(0, start) + newText + element.value.substring(end);
                    element.selectionStart = start;
                    element.selectionEnd = start + newText.length;
                }
            }

            element.dispatchEvent(new Event('input', { bubbles: true }));
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

export function setupScrollSync(textArea: HTMLTextAreaElement, previewContainer: HTMLElement): void {
    if (!textArea || !previewContainer) return;

    let isScrolling = false;
    let scrollTimeout: number | null = null;

    textArea.addEventListener('scroll', () => {
        if (isScrolling) return;
        isScrolling = true;

        const scrollPercentage = textArea.scrollTop / (textArea.scrollHeight - textArea.clientHeight);
        const previewTargetPosition = scrollPercentage * (previewContainer.scrollHeight - previewContainer.clientHeight);
        previewContainer.scrollTop = previewTargetPosition;

        if (scrollTimeout) clearTimeout(scrollTimeout);
        scrollTimeout = window.setTimeout(() => {
            isScrolling = false;
            scrollTimeout = null;
        }, 50);
    });

    previewContainer.addEventListener('scroll', () => {
        if (isScrolling) return;
        isScrolling = true;

        const scrollPercentage = previewContainer.scrollTop / (previewContainer.scrollHeight - previewContainer.clientHeight);
        const textAreaTargetPosition = scrollPercentage * (textArea.scrollHeight - textArea.clientHeight);
        textArea.scrollTop = textAreaTargetPosition;

        if (scrollTimeout) clearTimeout(scrollTimeout);
        scrollTimeout = window.setTimeout(() => {
            isScrolling = false;
            scrollTimeout = null;
        }, 50);
    });

    const observer = new MutationObserver(() => {
        if (textArea.scrollHeight > 0 && previewContainer.scrollHeight > 0) {
            const scrollPercentage = textArea.scrollTop / (textArea.scrollHeight - textArea.clientHeight);
            const previewTargetPosition = scrollPercentage * (previewContainer.scrollHeight - previewContainer.clientHeight);
            previewContainer.scrollTop = previewTargetPosition;
        }
    });

    observer.observe(previewContainer, {
        childList: true,
        subtree: true,
        characterData: true
    });
}

function openFileDialog(maxSizeMB: number): Promise<File | null> {
    return new Promise((resolve) => {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = 'image/*';

        input.onchange = (e) => {
            const files = input.files;
            if (files && files.length > 0) {
                const file = files[0];

                const maxSizeBytes = maxSizeMB * 1024 * 1024;
                if (file.size > maxSizeBytes) {
                    alert(`Image size is too large. Please select an image under ${maxSizeMB}MB.`);
                    resolve(null);
                    return;
                }

                resolve(file);
            } else {
                resolve(null);
            }
        };

        input.onclick = () => {
            input.oncancel = () => resolve(null);
        };

        input.click();
    });
}

async function saveFileToIndexedDB(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
        const dbName = 'PinetreeImageDB';
        const storeName = 'images';
        const request = indexedDB.open(dbName, 1);

        request.onupgradeneeded = (event) => {
            const db = request.result;
            if (!db.objectStoreNames.contains(storeName)) {
                db.createObjectStore(storeName, { keyPath: 'id' });
            }
        };

        request.onerror = (event) => {
            reject('IndexedDB error: ' + request.error);
        };

        request.onsuccess = (event) => {
            try {
                const db = request.result;
                const transaction = db.transaction([storeName], 'readwrite');
                const store = transaction.objectStore(storeName);

                const id = 'img_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);

                const imageData = {
                    id: id,
                    name: file.name,
                    type: file.type,
                    blob: file,
                    date: new Date()
                };

                const addRequest = store.add(imageData);

                addRequest.onsuccess = () => {
                    resolve(id);
                };

                addRequest.onerror = () => {
                    reject('Error saving to IndexedDB: ' + addRequest.error);
                };

                transaction.oncomplete = () => {
                    console.log('Transaction completed: saved image to IndexedDB');
                };
            } catch (error) {
                reject('Error in IndexedDB transaction: ' + error);
            }
        };
    });
}

async function getImageBlobUrl(id: string): Promise<string> {
    return new Promise((resolve, reject) => {
        const dbName = 'PinetreeImageDB';
        const storeName = 'images';
        const request = indexedDB.open(dbName, 1);

        request.onerror = () => {
            reject('IndexedDB error: ' + request.error);
        };

        request.onsuccess = () => {
            const db = request.result;
            const transaction = db.transaction([storeName], 'readonly');
            const store = transaction.objectStore(storeName);

            const getRequest = store.get(id);

            getRequest.onsuccess = () => {
                if (getRequest.result) {
                    const blob = getRequest.result.blob;
                    const blobUrl = URL.createObjectURL(blob);
                    resolve(blobUrl);
                } else {
                    reject('Image not found');
                }
            };

            getRequest.onerror = () => {
                reject('Error retrieving from IndexedDB: ' + getRequest.error);
            };
        };
    });
}

function setupDropZone(
    dropZoneElement: HTMLElement,
    dotNetHelper: DotNetObject
): void {
    if (!dropZoneElement) return;

    dropZoneElement.addEventListener('dragover', (e) => {
        e.preventDefault();
        e.stopPropagation();
        dropZoneElement.classList.add('drag-over');
    });

    dropZoneElement.addEventListener('dragleave', () => {
        dropZoneElement.classList.remove('drag-over');
    });

    dropZoneElement.addEventListener('drop', async (e) => {
        e.preventDefault();
        e.stopPropagation();
        dropZoneElement.classList.remove('drag-over');

        if (e.dataTransfer?.files && e.dataTransfer.files.length > 0) {
            const file = e.dataTransfer.files[0];

            const maxSizeMB = 5;
            const maxSizeBytes = maxSizeMB * 1024 * 1024;
            if (file.size > maxSizeBytes) {
                alert(`Image size is too large. Please select an image under ${maxSizeMB}MB.`);
                return;
            }

            try {
                const imageId = await saveFileToIndexedDB(file);
                const blobUrl = await getImageBlobUrl(imageId);

                await dotNetHelper.invokeMethodAsync('HandleDroppedFile', {
                    blobUrl: blobUrl,
                    fileName: file.name
                });
            } catch (error) {
                console.error("Error handling file:", error);
            }
        }
    });
}

export async function getAllImagesFromIndexedDB(): Promise<{ id: string, name: string, blobUrl: string }[]> {
    return new Promise((resolve, reject) => {
        const dbName = 'PinetreeImageDB';
        const storeName = 'images';
        const request = indexedDB.open(dbName, 1);

        request.onerror = () => {
            reject('IndexedDB error: ' + request.error);
        };

        request.onsuccess = () => {
            const db = request.result;
            const transaction = db.transaction([storeName], 'readonly');
            const store = transaction.objectStore(storeName);

            const images: { id: string, name: string, blobUrl: string }[] = [];
            const cursorRequest = store.openCursor();

            cursorRequest.onsuccess = (event) => {
                const cursor = cursorRequest.result;
                if (cursor) {
                    const image = cursor.value;
                    const blobUrl = URL.createObjectURL(image.blob);
                    images.push({
                        id: image.id,
                        name: image.name,
                        blobUrl: blobUrl
                    });
                    cursor.continue();
                } else {
                    resolve(images);
                }
            };

            cursorRequest.onerror = () => {
                reject('Error retrieving images from IndexedDB: ' + cursorRequest.error);
            };
        };
    });
}

export async function getImageBase64(id: string): Promise<string> {
    return new Promise((resolve, reject) => {
        const dbName = 'PinetreeImageDB';
        const storeName = 'images';
        const request = indexedDB.open(dbName, 1);

        request.onerror = () => {
            reject('IndexedDB error: ' + request.error);
        };

        request.onsuccess = () => {
            const db = request.result;
            const transaction = db.transaction([storeName], 'readonly');
            const store = transaction.objectStore(storeName);

            const getRequest = store.get(id);

            getRequest.onsuccess = () => {
                if (getRequest.result) {
                    const blob = getRequest.result.blob;
                    const reader = new FileReader();
                    reader.onloadend = () => {
                        const base64data = reader.result as string;
                        // "data:image/jpeg;base64," などのプレフィックスを削除
                        const base64 = base64data.substring(base64data.indexOf(',') + 1);
                        resolve(base64);
                    };
                    reader.readAsDataURL(blob);
                } else {
                    reject('Image not found');
                }
            };

            getRequest.onerror = () => {
                reject('Error retrieving from IndexedDB: ' + getRequest.error);
            };
        };
    });
}

