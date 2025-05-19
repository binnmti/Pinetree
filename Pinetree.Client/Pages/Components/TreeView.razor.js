// デバッグモード (true に設定すると詳細なログが表示されます)
const DEBUG = false;
// ドラッグアンドドロップの状態を管理するグローバル変数
let draggedItemId = null;
let draggedItemTitle = null; // アイテムのタイトルも保存
let dropTargetId = null;
let currentDropTarget = null;
let dotNetHelper = null;
// ドロップ位置の定数
const DROP_POSITION = {
    BEFORE: 'before',
    AFTER: 'after',
    INTO: 'into'
};
// ドロップ位置のUIテキスト
const DROP_POSITION_TEXT = {
    [DROP_POSITION.BEFORE]: '上に移動',
    [DROP_POSITION.AFTER]: '下に移動',
    [DROP_POSITION.INTO]: '子として追加'
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
    // 既存のツリーアイテムにドラッグイベントを設定
    setupDragEvents(container);
    // テキストエリアへのドラッグ&ドロップをサポート
    setupExternalDropTargets();
    // 新しい要素が追加されたときに監視する (ツリーが展開された場合など)
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
    // Markdownエディタでの処理はMarkdown.razor.tsの方で行い、
    // 二重に処理されないようにここでは最小限のコードのみを維持します
    const textAreas = document.querySelectorAll('textarea');
    textAreas.forEach(textArea => {
        textArea.addEventListener('dragover', (e) => {
            e.preventDefault();
            if (e.dataTransfer) {
                e.dataTransfer.dropEffect = 'copy';
            }
        });
        // ドロップイベント処理はMarkdown.razor.tsに移動しました
    });
}
/**
 * Sets up drag events for all tree items in the container
 */
function setupDragEvents(container) {
    // タイトル要素（ツリーアイテムを表す）をすべて検索
    const treeItems = container.querySelectorAll('.title');
    treeItems.forEach(item => {
        if (item instanceof HTMLElement) {
            // 既に設定済みならスキップ
            if (item.dataset.dragInitialized === 'true')
                return;
            // 初期化済みとマーク
            item.dataset.dragInitialized = 'true';
            // ドラッグ可能に設定
            item.setAttribute('draggable', 'true');
            // ドラッグイベントを追加
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
    // data-guid属性を持つ親li要素を検索
    const parentItem = findParentWithGuid(item);
    if (!parentItem)
        return;
    // data-guid属性からGUIDを取得
    draggedItemId = parentItem.dataset.guid || null;
    // アイテムのタイトルを取得
    draggedItemTitle = item.textContent?.trim() || '';
    if (draggedItemId) {
        // ドラッグエフェクトを設定（コピーも許可）
        e.dataTransfer.effectAllowed = 'copyMove';
        // 複数のデータフォーマットを設定
        e.dataTransfer.setData('text/plain', draggedItemId);
        // テキストエリアへのドロップ用にMarkdownリンク形式でデータも設定
        const markdownLink = `[${draggedItemTitle}](//${draggedItemId})`;
        e.dataTransfer.setData('text/markdown', markdownLink);
        e.dataTransfer.setData('application/x-pinetree-link', JSON.stringify({
            id: draggedItemId,
            title: draggedItemTitle,
            markdownLink: markdownLink
        }));
        // スタイルを追加（キャンセルを避けるために次のティックで）
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
    // ドロップを許可するためにデフォルト動作を防止
    e.preventDefault();
    if (e.dataTransfer) {
        // ツリー内での移動の場合はmove、外部要素へのドロップの場合はcopy
        const isTreeItem = e.target?.closest('.title') !== null;
        e.dataTransfer.dropEffect = isTreeItem ? 'move' : 'copy';
    }
}
/**
 * Handles the drag enter event - sets up drop targeting
 */
function handleDragEnter(e, item) {
    // data-guid属性を持つ親li要素を検索
    const parentItem = findParentWithGuid(item);
    if (parentItem) {
        dropTargetId = parentItem.dataset.guid || null;
    }
    // 他の要素からドラッグインジケータをクリア
    document.querySelectorAll('.title').forEach(el => {
        if (el instanceof HTMLElement && el !== item) {
            el.classList.remove('drag-over', 'drag-before', 'drag-after', 'drag-into');
        }
    });
    // ドロップターゲットを示すスタイルを追加
    item.classList.add('drag-over');
    // 現在のターゲットを保存し、ビジュアルを更新
    currentDropTarget = item;
    updateDropFeedback(e, item);
    // 一貫したドラッグフィードバックのためのグローバルトラッキングを設定
    document.removeEventListener('dragover', handleGlobalDragOver);
    document.addEventListener('dragover', handleGlobalDragOver);
    if (DEBUG)
        console.log('Drag enter on:', item.textContent?.trim());
}
/**
 * Handles the drag leave event
 */
function handleDragLeave(e, item) {
    // 子要素に入った場合は無視（バブリングによる誤動作を防止）
    const relatedTarget = e.relatedTarget;
    if (relatedTarget && item.contains(relatedTarget)) {
        return;
    }
    // 本当に離れる場合のみ、このアイテムからスタイリングを削除
    item.classList.remove('drag-over', 'drag-before', 'drag-after', 'drag-into');
    // グローバルdragoverハンドラはdragendまで維持
}
/**
 * Updates visual feedback for the drop target based on determined drop position
 */
function updateDropFeedback(e, item) {
    // 以前の位置クラスを削除
    item.classList.remove('drag-before', 'drag-after', 'drag-into');
    // ドロップ位置を決定して適切なクラスを追加
    const dropPosition = determineDropPosition(e, item);
    // ドロップ位置に基づいてクラスを追加
    item.classList.add(`drag-${dropPosition}`);
    if (DEBUG) {
        const eventType = e instanceof DragEvent ? 'DragEvent' : 'MouseEvent';
        console.log(`${eventType} - ドロップ位置: ${DROP_POSITION_TEXT[dropPosition]}`, e.clientY);
    }
}
/**
 * Global handler for dragover events to update visual feedback
 */
function handleGlobalDragOver(e) {
    e.preventDefault(); // ドロップを機能させるためにデフォルト動作を防止
    if (!currentDropTarget)
        return;
    // 現在のマウス位置に基づいて、保存されたターゲットのフィードバックを更新
    updateDropFeedback(e, currentDropTarget);
}
/**
 * Handles the drop event
 */
function handleDrop(e, item) {
    // デフォルト動作を防止
    e.preventDefault();
    // スタイリングを削除
    item.classList.remove('drag-over', 'drag-before', 'drag-after', 'drag-into');
    // data-guid属性を持つ親li要素を検索
    const parentItem = findParentWithGuid(item);
    if (!parentItem)
        return;
    // ドロップターゲットのGUIDを取得
    dropTargetId = parentItem.dataset.guid || null;
    // ソースとターゲットの両方があり、それらが異なる場合
    if (draggedItemId && dropTargetId && draggedItemId !== dropTargetId) {
        // ドロップターゲットの位置を取得
        const dropPosition = determineDropPosition(e, item);
        // 親li要素を検索
        const targetParentLi = parentItem.closest('li');
        const targetParentId = targetParentLi?.parentElement?.closest('li')?.dataset.guid || null;
        if (DEBUG) {
            console.log(`ドロップ実行: ${draggedItemId} を ${dropTargetId} の ${DROP_POSITION_TEXT[dropPosition]} 位置に配置します`);
        }
        // .NETメソッドを呼び出して移動を処理
        dotNetHelper?.invokeMethodAsync('HandleItemDrop', draggedItemId, dropTargetId, dropPosition, targetParentId)
            .then(() => {
            // ドラッグ状態をクリア
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
    // アイテムの境界矩形を取得
    const rect = item.getBoundingClientRect();
    const mouseY = e.clientY;
    const mouseX = e.clientX;
    // コントロール領域（右側のボタン）にある場合は、into位置は使用しない
    const controlAreaWidth = 40; // コントロールは約40pxの幅と仮定
    const isOverControlArea = mouseX > (rect.right - controlAreaWidth);
    if (isOverControlArea) {
        return mouseY < rect.top + rect.height / 2 ? DROP_POSITION.BEFORE : DROP_POSITION.AFTER;
    }
    // マウス位置と要素の相対的な位置を計算（0〜1の範囲）
    const relativeY = (mouseY - rect.top) / rect.height;
    if (DEBUG) {
        const eventType = e instanceof DragEvent ? 'DragEvent' : 'MouseEvent';
        console.log(`${eventType} - Mouse Y: ${mouseY}, Item top: ${rect.top}, Item bottom: ${rect.bottom}, Height: ${rect.height}`);
        console.log(`${eventType} - Relative Y position: ${relativeY.toFixed(2)}`);
    }
    // 相対位置による判定：上部15%、中央70%、下部15%
    if (relativeY < 0.15) {
        if (DEBUG)
            console.log(`判定: ${DROP_POSITION.BEFORE} (上部15%)`);
        return DROP_POSITION.BEFORE;
    }
    else if (relativeY > 0.85) {
        if (DEBUG)
            console.log(`判定: ${DROP_POSITION.AFTER} (下部15%)`);
        return DROP_POSITION.AFTER;
    }
    else {
        if (DEBUG)
            console.log(`判定: ${DROP_POSITION.INTO} (中央70%)`);
        return DROP_POSITION.INTO;
    }
}
/**
 * Cleans up all drag state
 */
function cleanupDragState() {
    // グローバルイベントリスナーを削除
    document.removeEventListener('dragover', handleGlobalDragOver);
    // ドラッグ状態をクリア
    draggedItemId = null;
    draggedItemTitle = null;
    dropTargetId = null;
    currentDropTarget = null;
}
/**
 * Handles the drag end event
 */
function handleDragEnd(e) {
    // すべてのドラッグスタイリングを削除
    document.querySelectorAll('.title').forEach(item => {
        if (item instanceof HTMLElement) {
            item.classList.remove('dragging', 'drag-over', 'drag-before', 'drag-after', 'drag-into');
        }
    });
    // ドラッグ状態をクリア
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
    // レベルを決定するために祖先ULの数をカウント
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