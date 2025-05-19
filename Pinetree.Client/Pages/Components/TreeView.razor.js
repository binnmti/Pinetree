// Debug mode (set to true to display detailed logs)
const DEBUG = false;
// Global variables for managing drag and drop state
let draggedItemId = null;
let draggedItemTitle = null; // Also store the item's title
let dropTargetId = null;
let currentDropTarget = null;
let dotNetHelper = null;
// Drop position constants
const DROP_POSITION = {
    BEFORE: 'before',
    AFTER: 'after',
    INTO: 'into'
};
// Drop position UI text
const DROP_POSITION_TEXT = {
    [DROP_POSITION.BEFORE]: 'Move above',
    [DROP_POSITION.AFTER]: 'Move below',
    [DROP_POSITION.INTO]: 'Add as child'
};
/**
 * Initializes the drag and drop functionality for the TreeView component
 * @param container The container element that holds all the tree items
 * @param helper The DotNet helper object for invoking C# methods
 */
export function setupTreeViewDragDrop(container, helper) {
    if (!container)
        return;
    dotNetHelper = helper;
    // Set up drag events for existing tree items
    setupDragEvents(container);
    // Support drag & drop to text areas
    setupExternalDropTargets();
    // Monitor when new elements are added (e.g., when tree is expanded)
    const observer = new MutationObserver(() => {
        setupDragEvents(container);
    });
    observer.observe(container, {
        childList: true,
        subtree: true
    });
}
/**
 * Sets up drop targets outside of the tree view (e.g., text areas)
 *
 * Note: The actual drop handling is implemented in the Markdown.razor.ts file's
 * setupDropZone function to avoid duplicate handlers.
 */
function setupExternalDropTargets() {
    // The processing in the Markdown editor is done in Markdown.razor.ts
    // Only maintain minimal code here to avoid duplicate processing
    const textAreas = document.querySelectorAll('textarea');
    textAreas.forEach(textArea => {
        textArea.addEventListener('dragover', (e) => {
            e.preventDefault();
            if (e.dataTransfer) {
                e.dataTransfer.dropEffect = 'copy';
            }
        });
        // Drop event handling has been moved to Markdown.razor.ts
    });
}
/**
 * Sets up drag events for all tree items in the container
 */
function setupDragEvents(container) {
    // Find all title elements (representing tree items)
    const treeItems = container.querySelectorAll('.title');
    treeItems.forEach(item => {
        if (item instanceof HTMLElement) {
            // Skip if already initialized
            if (item.dataset.dragInitialized === 'true')
                return;
            // Mark as initialized
            item.dataset.dragInitialized = 'true';
            // Set as draggable
            item.setAttribute('draggable', 'true');
            // Add drag events
            item.addEventListener('dragstart', e => handleDragStart(e, item));
            item.addEventListener('dragover', e => handleDragOver(e));
            item.addEventListener('dragenter', e => handleDragEnter(e, item));
            item.addEventListener('dragleave', e => handleDragLeave(e, item));
            item.addEventListener('drop', e => handleDrop(e, item));
            item.addEventListener('dragend', handleDragEnd);
        }
    });
}
/**
 * Handles the drag start event
 */
function handleDragStart(e, item) {
    if (!e.dataTransfer)
        return;
    // Find parent li element with data-guid attribute
    const parentItem = findParentWithGuid(item);
    if (!parentItem)
        return;
    // Get GUID from data-guid attribute
    draggedItemId = parentItem.dataset.guid || null;
    // Get the item's title
    draggedItemTitle = item.textContent?.trim() || '';
    if (draggedItemId) {
        // Set drag effect (allow copy too)
        e.dataTransfer.effectAllowed = 'copyMove';
        // Set multiple data formats
        e.dataTransfer.setData('text/plain', draggedItemId);
        // Also set data in Markdown link format for dropping into text areas
        const markdownLink = `[${draggedItemTitle}](//${draggedItemId})`;
        e.dataTransfer.setData('text/markdown', markdownLink);
        e.dataTransfer.setData('application/x-pinetree-link', JSON.stringify({
            id: draggedItemId,
            title: draggedItemTitle,
            markdownLink: markdownLink
        }));
        // Add style (in next tick to avoid cancellation)
        setTimeout(() => {
            if (item)
                item.classList.add('dragging');
        }, 0);
        if (DEBUG)
            console.log('Drag start:', draggedItemId, draggedItemTitle);
    }
}
/**
 * Handles the drag over event - must be present to allow drops
 */
function handleDragOver(e) {
    // Prevent default to allow drop
    e.preventDefault();
    if (e.dataTransfer) {
        // Use move effect for moves within the tree, copy for drops to external elements
        const isTreeItem = e.target?.closest('.title') !== null;
        e.dataTransfer.dropEffect = isTreeItem ? 'move' : 'copy';
    }
}
/**
 * Handles the drag enter event - sets up drop targeting
 */
function handleDragEnter(e, item) {
    // Find parent li element with data-guid attribute
    const parentItem = findParentWithGuid(item);
    if (parentItem) {
        dropTargetId = parentItem.dataset.guid || null;
    }
    // Clear drag indicators from other elements
    document.querySelectorAll('.title').forEach(el => {
        if (el instanceof HTMLElement && el !== item) {
            el.classList.remove('drag-over', 'drag-before', 'drag-after', 'drag-into');
        }
    });
    // Add style to indicate drop target
    item.classList.add('drag-over');
    // Save current target and update visuals
    currentDropTarget = item;
    updateDropFeedback(e, item);
    // Set up global tracking for consistent drag feedback
    document.removeEventListener('dragover', handleGlobalDragOver);
    document.addEventListener('dragover', handleGlobalDragOver);
    if (DEBUG)
        console.log('Drag enter on:', item.textContent?.trim());
}
/**
 * Handles the drag leave event
 */
function handleDragLeave(e, item) {
    // Ignore if entered a child element (prevent issues due to bubbling)
    const relatedTarget = e.relatedTarget;
    if (relatedTarget && item.contains(relatedTarget)) {
        return;
    }
    // Only remove styling from this item if actually leaving
    item.classList.remove('drag-over', 'drag-before', 'drag-after', 'drag-into');
    // Global dragover handler is maintained until dragend
}
/**
 * Updates visual feedback for the drop target based on determined drop position
 */
function updateDropFeedback(e, item) {
    // Remove previous position classes
    item.classList.remove('drag-before', 'drag-after', 'drag-into');
    // Determine drop position and add appropriate class
    const dropPosition = determineDropPosition(e, item);
    // Add class based on drop position
    item.classList.add(`drag-${dropPosition}`);
    if (DEBUG) {
        const eventType = e instanceof DragEvent ? 'DragEvent' : 'MouseEvent';
        console.log(`${eventType} - Drop position: ${DROP_POSITION_TEXT[dropPosition]}`, e.clientY);
    }
}
/**
 * Global handler for dragover events to update visual feedback
 */
function handleGlobalDragOver(e) {
    e.preventDefault(); // Prevent default to allow drop
    if (!currentDropTarget)
        return;
    // Update feedback for the saved target based on current mouse position
    updateDropFeedback(e, currentDropTarget);
}
/**
 * Handles the drop event
 */
function handleDrop(e, item) {
    // Prevent default action
    e.preventDefault();
    // Remove styling
    item.classList.remove('drag-over', 'drag-before', 'drag-after', 'drag-into');
    // Find parent li element with data-guid attribute
    const parentItem = findParentWithGuid(item);
    if (!parentItem)
        return;
    // Get the GUID of the drop target
    dropTargetId = parentItem.dataset.guid || null;
    // If both source and target exist and are different
    if (draggedItemId && dropTargetId && draggedItemId !== dropTargetId) {
        // Get the drop position
        const dropPosition = determineDropPosition(e, item);
        // Find parent li element
        const targetParentLi = parentItem.closest('li');
        const targetParentId = targetParentLi?.parentElement?.closest('li')?.dataset.guid || null;
        if (DEBUG) {
            console.log(`Drop execution: Placing ${draggedItemId} at ${DROP_POSITION_TEXT[dropPosition]} position of ${dropTargetId}`);
        }
        // Call .NET method to handle the move
        dotNetHelper?.invokeMethodAsync('HandleItemDrop', draggedItemId, dropTargetId, dropPosition, targetParentId)
            .then(() => {
            // Clear drag state
            cleanupDragState();
        })
            .catch(error => {
            console.error("Error handling item drop:", error);
            cleanupDragState();
        });
    }
    else {
        cleanupDragState();
    }
}
/**
 * Determines the drop position based on mouse position and item dimensions
 * @returns 'into' for dropping as child, 'before' for dropping before, 'after' for dropping after
 */
function determineDropPosition(e, item) {
    // Get the item's bounding rectangle
    const rect = item.getBoundingClientRect();
    const mouseY = e.clientY;
    const mouseX = e.clientX;
    // If in control area (buttons on the right), don't use into position
    const controlAreaWidth = 40; // Assume controls are about 40px wide
    const isOverControlArea = mouseX > (rect.right - controlAreaWidth);
    if (isOverControlArea) {
        return mouseY < rect.top + rect.height / 2 ? DROP_POSITION.BEFORE : DROP_POSITION.AFTER;
    }
    // Calculate relative position of mouse to the element (0-1 range)
    const relativeY = (mouseY - rect.top) / rect.height;
    if (DEBUG) {
        const eventType = e instanceof DragEvent ? 'DragEvent' : 'MouseEvent';
        console.log(`${eventType} - Mouse Y: ${mouseY}, Item top: ${rect.top}, Item bottom: ${rect.bottom}, Height: ${rect.height}`);
        console.log(`${eventType} - Relative Y position: ${relativeY.toFixed(2)}`);
    }
    // Determine by relative position: top 15%, middle 70%, bottom 15%
    if (relativeY < 0.15) {
        if (DEBUG)
            console.log(`Determination: ${DROP_POSITION.BEFORE} (top 15%)`);
        return DROP_POSITION.BEFORE;
    }
    else if (relativeY > 0.85) {
        if (DEBUG)
            console.log(`Determination: ${DROP_POSITION.AFTER} (bottom 15%)`);
        return DROP_POSITION.AFTER;
    }
    else {
        if (DEBUG)
            console.log(`Determination: ${DROP_POSITION.INTO} (middle 70%)`);
        return DROP_POSITION.INTO;
    }
}
/**
 * Cleans up all drag state
 */
function cleanupDragState() {
    // Remove global event listener
    document.removeEventListener('dragover', handleGlobalDragOver);
    // Clear drag state
    draggedItemId = null;
    draggedItemTitle = null;
    dropTargetId = null;
    currentDropTarget = null;
}
/**
 * Handles the drag end event
 */
function handleDragEnd(e) {
    // Remove all drag styling
    document.querySelectorAll('.title').forEach(item => {
        if (item instanceof HTMLElement) {
            item.classList.remove('dragging', 'drag-over', 'drag-before', 'drag-after', 'drag-into');
        }
    });
    // Clear drag state
    cleanupDragState();
}
/**
 * Finds the parent list item with a guid data attribute
 */
function findParentWithGuid(element) {
    let current = element;
    while (current && !current.dataset.guid) {
        const parent = current.parentElement;
        if (!parent)
            break;
        current = parent;
    }
    return current.dataset.guid ? current : null;
}
/**
 * Gets the hierarchical level of a tree node
 */
function getNodeLevel(element) {
    // Count number of ancestor ULs to determine level
    let level = 0;
    let parent = element.parentElement;
    while (parent) {
        if (parent.tagName === 'UL') {
            level++;
        }
        parent = parent.parentElement;
    }
    return level;
}
//# sourceMappingURL=TreeView.razor.js.map