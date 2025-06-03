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
export async function openFileDialogAndGetBlobUrl() {
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
    }
    catch (error) {
        console.error("Error handling file:", error);
        return { blobUrl: '', fileName: '' };
    }
}
export async function clearAllImagesFromIndexedDB() {
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
export function setupMarkdownClickListener(container, dotNetHelper) {
    // This function is specifically for read-only view mode
    // It only sets up link handling for markdown internal links
    setupLinkInterceptor(container, dotNetHelper);
}
export function setupAllEventListeners(container, textArea, dotNetHelper) {
    initializeIndexedDB();
    setupLinkInterceptor(container, dotNetHelper);
    setupBeforeUnloadWarning(dotNetHelper);
    setupKeyboardShortcuts(textArea, dotNetHelper);
    enableContinuousList(textArea);
    initializeTooltips();
    setupScrollSync(textArea, container);
    setupDropZone(textArea, dotNetHelper);
}
function initializeTooltips() {
    const tooltipTriggerList = Array.from(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach(tooltipTriggerEl => {
        new window.bootstrap.Tooltip(tooltipTriggerEl);
    });
}
function setupLinkInterceptor(markdownContainer, dotNetHelper) {
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
function setupBeforeUnloadWarning(dotNetHelper) {
    let bypassBeforeUnload = false;
    let isNavigationBlocked = false;
    let preventNextPopstate = false;
    // Standard beforeunload handler remains the same
    window.addEventListener('beforeunload', async function (e) {
        if (bypassBeforeUnload) {
            bypassBeforeUnload = false;
            return;
        }
        try {
            const hasPendingChanges = await dotNetHelper.invokeMethodAsync('HasPendingChanges');
            if (hasPendingChanges) {
                e.preventDefault();
                e.returnValue = '';
                return '';
            }
        }
        catch (error) {
            console.error('Error checking for pending changes:', error);
        }
    });
    // Navigation click handler remains the same
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
    // Enhanced browser navigation handling
    if ('navigation' in window && window.navigation) {
        const navigation = window.navigation;
        navigation.addEventListener('navigate', async (e) => {
            // Skip if it's a download or form submission
            if (e.downloadRequest || e.formData)
                return;
            // Skip if it's a same-page navigation (hash change)
            if (e.navigationType === 'push' && e.destination.url === location.href)
                return;
            // Check if navigation is already being handled
            if (isNavigationBlocked)
                return;
            try {
                const hasPendingChanges = await dotNetHelper.invokeMethodAsync('HasPendingChanges');
                if (hasPendingChanges) {
                    e.intercept({
                        async handler() {
                            isNavigationBlocked = true;
                            const confirmed = confirm('Your changes have not been saved. Are you sure you want to leave this page?');
                            isNavigationBlocked = false;
                            if (!confirmed) {
                                throw new Error('Navigation cancelled by user');
                            }
                        }
                    });
                }
            }
            catch (error) {
                console.error('Navigation API error:', error);
                isNavigationBlocked = false;
            }
        });
    }
    else {
        // Improved fallback for browsers without Navigation API
        // Create a "fake" history entry to intercept back button
        const currentUrl = window.location.href;
        const state = { pinetreeEditor: true, originalUrl: currentUrl };
        // Replace current state with our marked state
        window.history.replaceState(state, '', currentUrl);
        // Push a duplicate state to create a buffer
        window.history.pushState({ ...state, isDuplicate: true }, '', currentUrl);
        const handleNavigation = async (e) => {
            // Check if we're navigating away from our editor state
            const currentState = window.history.state;
            const isLeavingEditor = currentState && currentState.pinetreeEditor && !e.state?.pinetreeEditor;
            if (isLeavingEditor && !isNavigationBlocked) {
                try {
                    const hasPendingChanges = await dotNetHelper.invokeMethodAsync('HasPendingChanges');
                    if (hasPendingChanges) {
                        isNavigationBlocked = true;
                        // Immediately push state back to prevent navigation
                        window.history.pushState(currentState, '', currentState.originalUrl);
                        // Show confirmation dialog
                        setTimeout(async () => {
                            const confirmed = confirm('Your changes have not been saved. Are you sure you want to leave this page?');
                            if (confirmed) {
                                // User confirmed, allow navigation
                                bypassBeforeUnload = true;
                                preventNextPopstate = true;
                                window.history.back();
                            }
                            isNavigationBlocked = false;
                        }, 0);
                    }
                }
                catch (error) {
                    console.error('Navigation check error:', error);
                    isNavigationBlocked = false;
                }
            }
            else if (preventNextPopstate) {
                // This popstate is from our confirmed navigation
                preventNextPopstate = false;
            }
        };
        window.addEventListener('popstate', handleNavigation);
        // Handle forward button by monitoring history length
        let lastHistoryLength = window.history.length;
        const checkHistoryChange = setInterval(() => {
            if (window.history.length !== lastHistoryLength) {
                lastHistoryLength = window.history.length;
                // Re-establish our guard state if needed
                const currentState = window.history.state;
                if (!currentState || !currentState.pinetreeEditor) {
                    window.history.replaceState(state, '', currentUrl);
                    window.history.pushState({ ...state, isDuplicate: true }, '', currentUrl);
                }
            }
        }, 100);
        // Store the cleanup function
        dotNetHelper._navigationCleanup = () => {
            window.removeEventListener('popstate', handleNavigation);
            clearInterval(checkHistoryChange);
        };
    }
}
// Add cleanup method
export function cleanupNavigationHandlers(dotNetHelper) {
    if (dotNetHelper._navigationCleanup) {
        dotNetHelper._navigationCleanup();
    }
    if (dotNetHelper._navigationObserver) {
        dotNetHelper._navigationObserver.disconnect();
    }
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
        if (e.key === 'Tab') {
            e.preventDefault();
            const start = element.selectionStart;
            const end = element.selectionEnd;
            if (start === end) {
                const before = element.value.substring(0, start);
                const after = element.value.substring(end);
                element.value = before + '  ' + after;
                element.selectionStart = element.selectionEnd = start + 2;
            }
            else {
                const selectedText = element.value.substring(start, end);
                const lines = selectedText.split('\n');
                if (e.shiftKey) {
                    const indentedLines = lines.map(line => line.startsWith('  ') ? line.substring(2) : line);
                    const newText = indentedLines.join('\n');
                    element.value = element.value.substring(0, start) + newText + element.value.substring(end);
                    element.selectionStart = start;
                    element.selectionEnd = start + newText.length;
                }
                else {
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
export function enableContinuousList(element) {
    if (!element)
        return;
    const handleMatch = (match, currentLine, lineStart, selStart, markerGenerator) => {
        const before = currentLine.trim() === match[0].trim() ? element.value.substring(0, lineStart) : element.value.substring(0, selStart);
        const after = element.value.substring(selStart);
        const marker = markerGenerator(match);
        const position = currentLine.trim() === match[0].trim() ? lineStart + 1 : selStart + 1 + marker.length;
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
export function setupScrollSync(textArea, previewContainer) {
    if (!textArea || !previewContainer)
        return;
    let isScrolling = false;
    let scrollTimeout = null;
    textArea.addEventListener('scroll', () => {
        if (isScrolling)
            return;
        isScrolling = true;
        const scrollPercentage = textArea.scrollTop / (textArea.scrollHeight - textArea.clientHeight);
        const previewTargetPosition = scrollPercentage * (previewContainer.scrollHeight - previewContainer.clientHeight);
        previewContainer.scrollTop = previewTargetPosition;
        if (scrollTimeout)
            clearTimeout(scrollTimeout);
        scrollTimeout = window.setTimeout(() => {
            isScrolling = false;
            scrollTimeout = null;
        }, 50);
    });
    previewContainer.addEventListener('scroll', () => {
        if (isScrolling)
            return;
        isScrolling = true;
        const scrollPercentage = previewContainer.scrollTop / (previewContainer.scrollHeight - previewContainer.clientHeight);
        const textAreaTargetPosition = scrollPercentage * (textArea.scrollHeight - textArea.clientHeight);
        textArea.scrollTop = textAreaTargetPosition;
        if (scrollTimeout)
            clearTimeout(scrollTimeout);
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
function openFileDialog(maxSizeMB) {
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
            }
            else {
                resolve(null);
            }
        };
        input.onclick = () => {
            input.oncancel = () => resolve(null);
        };
        input.click();
    });
}
async function saveFileToIndexedDB(file) {
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
            }
            catch (error) {
                reject('Error in IndexedDB transaction: ' + error);
            }
        };
    });
}
async function getImageBlobUrl(id) {
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
                }
                else {
                    reject('Image not found');
                }
            };
            getRequest.onerror = () => {
                reject('Error retrieving from IndexedDB: ' + getRequest.error);
            };
        };
    });
}
function setupDropZone(dropZoneElement, dotNetHelper) {
    if (!dropZoneElement)
        return;
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
            }
            catch (error) {
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
export async function replaceBlobUrlsInContent(content, pineconeGuid, contentRef) {
    // Regular expression to find markdown image syntax with blob URLs
    const regex = /!\[(.*?)\]\((blob:[^)]+)\)/g;
    let match;
    let contentCopy = content;
    let changed = false;
    // Array to store all the matches and their processing promises
    const processingTasks = [];
    // Find all blob URLs in the content
    while ((match = regex.exec(content)) !== null) {
        const fullMatch = match[0]; // The entire match: ![alt](blob:url)
        const altText = match[1]; // The alt text part
        const blobUrl = match[2]; // The blob URL part
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
async function processBlobUrl(originalText, altText, blobUrl, pineconeGuid) {
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
    }
    catch (error) {
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
function blobToBase64(blob) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onloadend = () => {
            const base64String = reader.result;
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
function getExtensionFromAltText(altText) {
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
async function uploadImageToServer(base64, extension, pineconeGuid) {
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
    }
    catch (error) {
        console.error('Error uploading image to server:', error);
        return null;
    }
}
async function initializeIndexedDB() {
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
            }
            catch (error) {
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
//# sourceMappingURL=Markdown.razor.js.map