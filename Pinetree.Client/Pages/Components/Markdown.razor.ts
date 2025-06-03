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

export function setupMarkdownClickListener(container: HTMLElement, dotNetHelper: DotNetObject): void {
    // This function is specifically for read-only view mode
    // It only sets up link handling for markdown internal links
    setupLinkInterceptor(container, dotNetHelper);
}

export function setupAllEventListeners(
    container: HTMLElement,
    textArea: HTMLTextAreaElement,
    dotNetHelper: DotNetObject
): void {
    initializeIndexedDB();
    setupLinkInterceptor(container, dotNetHelper);
    setupBeforeUnloadWarning(dotNetHelper);
    setupKeyboardShortcuts(textArea, dotNetHelper);
    enableContinuousList(textArea);
    initializeTooltips();
    setupScrollSync(textArea, container);
    setupDropZone(textArea, dotNetHelper);
    setupClipboardPaste(textArea, dotNetHelper);
    setupTextAreaScrollBehavior(textArea);
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

    // TODO: When you press the back button on the browser, a warning will be displayed, but you will be forced to go back whether you press yes or no. The current workaround causes various problems with the back button on the browser, so turn off the function for now.
    //    const pageUrl = window.location.href;
    //    window.history.pushState({ page: 1 }, '', pageUrl);
    //    window.addEventListener('popstate', async function (e) {
    //        window.history.pushState({ page: 1 }, '', pageUrl);
    //        try {
    //            const hasPendingChanges = await dotNetHelper.invokeMethodAsync<boolean>('HasPendingChanges');
    //            if (hasPendingChanges) {
    //                const confirmed = confirm('Your changes have not been saved. Are you sure you want to leave this page?');
    //                if (confirmed) {
    //                    window.location.href = document.referrer || '/';
    //                }
    //            }
    //        } catch (error) {
    //            console.error('Error checking for pending changes:', error);
    //        }
    //    });
}

// Add cleanup method
export function cleanupNavigationHandlers(dotNetHelper: DotNetObject): void {
    if ((dotNetHelper as any)._navigationCleanup) {
        (dotNetHelper as any)._navigationCleanup();
    }
    if ((dotNetHelper as any)._navigationObserver) {
        (dotNetHelper as any)._navigationObserver.disconnect();
    }
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

                const id = 'img_' + Date.now() + '_' + Math.random().toString(36).substring(2, 9);

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
        
        // Set copy effect for TreeView items
        if (e.dataTransfer?.types.includes('application/x-pinetree-link')) {
            e.dataTransfer.dropEffect = 'copy';
        }
    });

    dropZoneElement.addEventListener('dragleave', () => {
        dropZoneElement.classList.remove('drag-over');
    });

    dropZoneElement.addEventListener('drop', async (e) => {
        e.preventDefault();
        e.stopPropagation();
        dropZoneElement.classList.remove('drag-over');

        // Handle dropped files (existing functionality)
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
        // Handle dropped TreeView items
        else if (e.dataTransfer?.types.includes('application/x-pinetree-link')) {
            // Get the Markdown link format from the dataTransfer
            const markdownLink = e.dataTransfer.getData('text/markdown');
            if (markdownLink && dropZoneElement instanceof HTMLTextAreaElement) {
                // Insert the markdown link at the cursor position
                const textArea = dropZoneElement;
                const start = textArea.selectionStart;
                const end = textArea.selectionEnd;
                const value = textArea.value;
                const newValue = value.substring(0, start) + markdownLink + value.substring(end);
                textArea.value = newValue;
                
                // Trigger input event to detect changes
                const inputEvent = new InputEvent('input', {
                    bubbles: true,
                    cancelable: true,
                    inputType: 'insertText',
                    data: markdownLink
                });
                textArea.dispatchEvent(inputEvent);
                
                // Update cursor position
                textArea.selectionStart = textArea.selectionEnd = start + markdownLink.length;
                textArea.focus();
            }
        }
    });
}

/**
 * Replaces all blob URLs in the given content with permanent URLs by uploading the images to the server.
 * 
 * @param content The markdown content containing blob URLs
 * @param contentRef Optional reference to the textarea element to update its value
 * @returns Promise resolving to the updated content with blob URLs replaced by permanent URLs
 */
export async function replaceBlobUrlsInContent(
    content: string,
    pineconeGuid: string,
    contentRef?: HTMLTextAreaElement
): Promise<string> {
    // Regular expression to find markdown image syntax with blob URLs
    const regex = /!\[(.*?)\]\((blob:[^)]+)\)/g;
    let match: RegExpExecArray | null;
    let contentCopy = content;
    let changed = false;

    // Array to store all the matches and their processing promises
    const processingTasks: Promise<{
        originalText: string;
        newText: string | null;
    }>[] = [];

    // Find all blob URLs in the content
    while ((match = regex.exec(content)) !== null) {
        const fullMatch = match[0];       // The entire match: ![alt](blob:url)
        const altText = match[1];         // The alt text part
        const blobUrl = match[2];         // The blob URL part

        // Create a task for processing each blob URL
        const task = processBlobUrl(fullMatch, altText, blobUrl, pineconeGuid);
        processingTasks.push(task);
    }

    // Wait for all processing tasks to complete
    const results = await Promise.all(processingTasks);

    // Apply all replacements
    for (const result of results) {
        if (result.newText) {
            contentCopy = contentCopy.replace(result.originalText, result.newText);
            changed = true;
        }
    }

    // If contentRef is provided and content changed, update the textarea
    if (changed && contentRef) {
        contentRef.value = contentCopy;
        // Dispatch input event to trigger any listeners
        contentRef.dispatchEvent(new Event('input', { bubbles: true }));
    }

    return contentCopy;
}

/**
 * Processes a single blob URL by fetching its content and uploading it to the server.
 * 
 * @param originalText The original markdown image text with blob URL
 * @param altText The alt text of the image
 * @param blobUrl The blob URL to process
 * @returns Promise resolving to an object containing the original text and its replacement
 */
async function processBlobUrl(
    originalText: string,
    altText: string,
    blobUrl: string,
    pineconeGuid: string
): Promise<{ originalText: string; newText: string | null }> {
    try {
        // Fetch the blob from the URL
        const response = await fetch(blobUrl);
        if (!response.ok) {
            throw new Error(`Failed to fetch blob: ${response.status} ${response.statusText}`);
        }

        const blob = await response.blob();

        // Convert blob to base64
        const base64 = await blobToBase64(blob);

        // Get file extension from alt text or default to jpg
        const extension = getExtensionFromAltText(altText);

        // Upload the image to the server
        const uploadResult = await uploadImageToServer(base64, extension, pineconeGuid);

        if (uploadResult && uploadResult.url) {
            // Create the new markdown image text with the permanent URL
            const newText = `![${altText}](${uploadResult.url})`;
            console.log(`Replaced: ${originalText} with ${newText}`);
            return { originalText, newText };
        }

        return { originalText, newText: null };
    } catch (error) {
        console.error(`Error processing blob URL ${blobUrl}:`, error);
        return { originalText, newText: null };
    }
}

/**
 * Converts a Blob to base64 string.
 * 
 * @param blob The blob to convert
 * @returns Promise resolving to the base64 string (without data URL prefix)
 */
function blobToBase64(blob: Blob): Promise<string> {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onloadend = () => {
            const base64String = reader.result as string;
            // Remove the data URL prefix (e.g., "data:image/jpeg;base64,")
            const base64 = base64String.substring(base64String.indexOf(',') + 1);
            resolve(base64);
        };
        reader.onerror = reject;
        reader.readAsDataURL(blob);
    });
}

/**
 * Extracts a file extension from the alt text, or returns a default extension.
 * 
 * @param altText The alt text that might contain a filename with extension
 * @returns The extension with a leading dot, or .jpg as default
 */
function getExtensionFromAltText(altText: string): string {
    // Try to extract extension from the alt text (which might be a filename)
    const lastDotIndex = altText.lastIndexOf('.');
    if (lastDotIndex > 0 && lastDotIndex < altText.length - 1) {
        const extension = altText.substring(lastDotIndex).toLowerCase();
        // Check if it's a common image extension
        if (['.jpg', '.jpeg', '.png', '.gif', '.webp', '.bmp', '.svg'].includes(extension)) {
            return extension;
        }
    }

    // Default extension
    return '.jpg';
}

/**
 * Uploads an image to the server.
 * 
 * @param base64 The base64-encoded image data (without data URL prefix)
 * @param extension The file extension with a leading dot
 * @returns Promise resolving to the upload result containing the URL
 */
async function uploadImageToServer(
    base64: string,
    extension: string,
    pineconeGuid: string
): Promise<{ url: string } | null> {
    try {
        const response = await fetch(`/api/Images/upload?extension=${extension}&pineconeGuid=${pineconeGuid}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(base64)
        });

        if (!response.ok) {
            throw new Error(`Upload failed: ${response.status} ${response.statusText}`);
        }

        return await response.json();
    } catch (error) {
        console.error('Error uploading image to server:', error);
        return null;
    }
}

async function initializeIndexedDB(): Promise<void> {
    const dbName = 'PinetreeImageDB';
    const storeName = 'images';

    return new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, 1);

        request.onupgradeneeded = (event) => {
            console.log('Initializing IndexedDB...');
            const db = request.result;

            if (!db.objectStoreNames.contains(storeName)) {
                db.createObjectStore(storeName, { keyPath: 'id' });
                console.log(`Created object store: ${storeName}`);
            }
        };

        request.onerror = (event) => {
            console.error(`Error initializing IndexedDB: ${request.error}`);
            reject(`Error initializing IndexedDB: ${request.error}`);
        };

        request.onsuccess = () => {
            const db = request.result;
            console.log(`IndexedDB initialized successfully with version ${db.version}`);

            try {
                const transaction = db.transaction([storeName], 'readonly');
                console.log(`Object store ${storeName} exists and is accessible`);
                resolve();
            } catch (error) {
                console.error(`Object store ${storeName} check failed:`, error);

                db.close();
                const deleteRequest = indexedDB.deleteDatabase(dbName);

                deleteRequest.onsuccess = () => {
                    console.log(`Database ${dbName} deleted due to missing object store`);
                    initializeIndexedDB()
                        .then(resolve)
                        .catch(reject);
                };

                deleteRequest.onerror = () => {
                    reject(`Failed to delete database: ${deleteRequest.error}`);
                };
            }
        };
    });
}

function setupClipboardPaste(
    textArea: HTMLTextAreaElement,
    dotNetHelper: DotNetObject
): void {
    if (!textArea) return;

    textArea.addEventListener('paste', async (e: ClipboardEvent) => {
        // Check if clipboard contains files
        if (e.clipboardData && e.clipboardData.files && e.clipboardData.files.length > 0) {
            const file = e.clipboardData.files[0];
            
            // Check if the file is an image
            if (!file.type.startsWith('image/')) {
                return; // Let default paste behavior handle non-image files
            }

            e.preventDefault(); // Prevent default paste behavior for images

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
                    fileName: file.name || 'pasted-image'
                });
            } catch (error) {
                console.error("Error handling pasted image:", error);
            }
        }
        // Check if clipboard contains image data (not files)
        else if (e.clipboardData && e.clipboardData.items) {
            const items = Array.from(e.clipboardData.items);
            const imageItem = items.find(item => item.type.startsWith('image/'));
            
            if (imageItem) {
                e.preventDefault(); // Prevent default paste behavior for images
                
                const file = imageItem.getAsFile();
                if (!file) return;

                const maxSizeMB = 5;
                const maxSizeBytes = maxSizeMB * 1024 * 1024;
                
                if (file.size > maxSizeBytes) {
                    alert(`Image size is too large. Please select an image under ${maxSizeMB}MB.`);
                    return;
                }

                try {
                    const imageId = await saveFileToIndexedDB(file);
                    const blobUrl = await getImageBlobUrl(imageId);

                    // Generate a filename based on current timestamp
                    const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
                    const extension = file.type.split('/')[1] || 'png';
                    const fileName = `pasted-image-${timestamp}.${extension}`;

                    await dotNetHelper.invokeMethodAsync('HandleDroppedFile', {
                        blobUrl: blobUrl,
                        fileName: fileName
                    });
                } catch (error) {
                    console.error("Error handling pasted image:", error);
                }
            }
        }
    });
}

function setupTextAreaScrollBehavior(textArea: HTMLTextAreaElement): void {
    if (!textArea) return;

    let lastUserScrollTop = 0;
    let lastSelectionStart = 0;
    let lastSelectionEnd = 0;
    let isTyping = false;

    const saveScrollPosition = () => {
        lastUserScrollTop = textArea.scrollTop;
        lastSelectionStart = textArea.selectionStart;
        lastSelectionEnd = textArea.selectionEnd;
    };

    textArea.addEventListener('scroll', () => {
        if (!isTyping) {
            saveScrollPosition();
        }
    });

    textArea.addEventListener('keydown', () => {
        isTyping = true;
        saveScrollPosition();
    });

    textArea.addEventListener('keyup', () => {
        isTyping = false;
    });

    textArea.addEventListener('input', (e) => {
        const currentSelectionStart = textArea.selectionStart;
        const isSmallChange = Math.abs(currentSelectionStart - lastSelectionStart) < 10;

        if (isSmallChange) {
            requestAnimationFrame(() => {
                const lineHeight = parseInt(getComputedStyle(textArea).lineHeight) || 18;
                const visibleLines = Math.floor(textArea.clientHeight / lineHeight);
                const currentLine = textArea.value.substring(0, currentSelectionStart).split('\n').length;
                const scrollLine = Math.floor(lastUserScrollTop / lineHeight) + 1;

                const isInVisibleArea = currentLine >= scrollLine &&
                    currentLine <= (scrollLine + visibleLines - 4); // 下部余白を考慮

                if (isInVisibleArea) {
                    textArea.scrollTop = lastUserScrollTop;
                } else {
                    saveScrollPosition();
                }
            });
        } else {
            saveScrollPosition();
        }
    });

    textArea.addEventListener('focus', saveScrollPosition);

    textArea.addEventListener('mousedown', saveScrollPosition);
}