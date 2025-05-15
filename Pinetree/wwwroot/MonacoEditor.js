window.monacoInterop = {
    editors: {}, // エディタインスタンスを格納するオブジェクト
    
    /**
     * Monaco Editor を初期化する
     * @param {string} editorId - エディタのDOM要素ID
     * @param {string} language - 言語モード (markdown, javascript など)
     * @param {string} theme - テーマ名
     * @param {string} value - 初期テキスト
     * @param {object} dotNetHelper - .NET メソッド呼び出し用のヘルパーオブジェクト
     */
    initialize: function (editorId, language, theme, value, dotNetHelper) {
        console.log('Monaco Editor initialization started for:', editorId);
        
        // IndexedDBを初期化
        initializeIndexedDB();
        
        // Monaco Editorのパスを設定
        require.config({ paths: { 'vs': 'lib/monaco-editor/min/vs' }});
        
        require(['vs/editor/editor.main'], function() {
            console.log('Monaco Editor modules loaded, creating editor instance:', editorId, language);
            
            // マークダウン用のカスタムリンクを設定
            if (language === 'markdown') {
                // マークダウン言語定義を拡張してカスタムリンクをサポート
                monaco.languages.registerLinkProvider('markdown', {
                    provideLinks: function (model) {
                        const text = model.getValue();
                        const links = [];
                        
                        // マークダウンリンク: [text](//GUID) の形式
                        const linkRegex = /\[([^\]]+)\]\(\/\/([a-zA-Z0-9-]+)\)/g;
                        let match;
                        
                        while ((match = linkRegex.exec(text)) !== null) {
                            const linkText = match[1];
                            const guid = match[2];
                            const start = model.getPositionAt(match.index);
                            const end = model.getPositionAt(match.index + match[0].length);
                            
                            links.push({
                                range: {
                                    startLineNumber: start.lineNumber,
                                    startColumn: start.column,
                                    endLineNumber: end.lineNumber,
                                    endColumn: end.column
                                },
                                url: `//${guid}`,
                                tooltip: `Navigate to: ${linkText}`
                            });
                        }
                        
                        return { links };
                    }
                });
            }
            
            // エディタのインスタンスを作成
            const editor = monaco.editor.create(document.getElementById(editorId), {
                value: value,
                language: language,
                theme: theme,
                automaticLayout: true,
                minimap: { enabled: true },
                wordWrap: 'on',
                scrollBeyondLastLine: false,
                lineNumbers: 'on',
                roundedSelection: true,
                scrollbar: {
                    vertical: 'auto',
                    horizontal: 'auto',
                    useShadows: true
                }
            });

            // エディタのインスタンスを保存
            monacoInterop.editors[editorId] = {
                instance: editor,
                dotNetHelper: dotNetHelper
            };
            
            console.log(`Editor instance saved for ${editorId}, dotNetHelper available: ${!!dotNetHelper}`);
            
            // 変更イベントをハンドル
            editor.onDidChangeModelContent(function () {
                console.log('Content change detected');
                const newValue = editor.getValue();
                dotNetHelper.invokeMethodAsync('OnContentChanged', newValue)
                    .catch(error => {
                        console.error('Error invoking OnContentChanged:', error);
                    });
            });
            
            // リンククリック時の処理を設定
            if (language === 'markdown') {
                editor.onMouseDown(function (e) {
                    if (e.target.type === monaco.editor.MouseTargetType.CONTENT_TEXT) {
                        try {
                            const model = editor.getModel();
                            if (!model) return;
                            
                            const position = e.target.position;
                            const lineContent = model.getLineContent(position.lineNumber);
                            
                            // カスタムリンク [text](//GUID) の検出
                            const linkRegex = /\[([^\]]+)\]\(\/\/([a-zA-Z0-9-]+)\)/g;
                            let match;
                            
                            while ((match = linkRegex.exec(lineContent)) !== null) {
                                // リンクの範囲を計算
                                const startIdx = match.index;
                                const endIdx = startIdx + match[0].length;
                                
                                // クリック位置がリンク内かチェック
                                if (position.column >= startIdx + 1 && position.column <= endIdx + 1) {
                                    const guid = match[2];
                                    e.preventDefault();
                                    dotNetHelper.invokeMethodAsync('HandleMarkdownLinkClick', guid)
                                        .catch(error => {
                                            console.error('Error invoking HandleMarkdownLinkClick:', error);
                                        });
                                    break;
                                }
                            }
                        } catch (error) {
                            console.error("Error in markdown link click handler:", error);
                        }
                    }
                });
                
                // マークダウンの便利機能をセットアップ
                console.log('Setting up markdown helpers for', editorId);
                
                // マークダウンリスト項目の自動継続
                editor.onKeyDown(function (e) {
                    if (e.keyCode !== monaco.KeyCode.Enter) return;
                    
                    try {
                        const model = editor.getModel();
                        const position = editor.getPosition();
                        
                        if (!model || !position) return;
                        
                        // 現在のカーソル位置の行番号（エンターを押す前の行）
                        const currentLineNumber = position.lineNumber;
                        
                        // カーソル位置の行のコンテンツを取得
                        const lineContent = model.getLineContent(currentLineNumber);
                        console.log('Enter key pressed at line:', currentLineNumber, 'content:', lineContent);

                        // チェックボックスパターンと箇条書きパターンの検出
                        const checkboxPattern = /^(\s*)(- \[[ x]?\])\s*/;  // 修正箇所
                        const bulletPattern = /^(\s*)([-+*]|(\d+)\.|\>)\s+/;
                        
                        const checkboxMatch = lineContent.match(checkboxPattern);
                        const bulletMatch = lineContent.match(bulletPattern);
                        
                        console.log('Checkbox match:', checkboxMatch);
                        console.log('Bullet match:', bulletMatch);
                        
                        if (checkboxMatch || bulletMatch) {
                            // マッチしたパターンを使用
                            const match = checkboxMatch || bulletMatch;
                            
                            // 空のアイテムの場合はリスト終了
                            if (lineContent.trim() === match[0].trim()) {
                                console.log('Empty list item detected, terminating list');
                                e.preventDefault();
                                editor.executeEdits('removeMarker', [{
                                    range: new monaco.Range(
                                        currentLineNumber, 
                                        1, 
                                        currentLineNumber + 1, 
                                        1
                                    ),
                                    text: '\n'
                                }]);
                                return;
                            }
                            
                            // マーカー生成
                            let marker = '';
                            
                            if (checkboxMatch) {
                                // チェックボックス形式
                                const indentation = match[1] || '';
                                marker = indentation + '- [ ] ';
                            } else if (bulletMatch) {
                                // 通常のリストマーカー
                                if (bulletMatch[3]) {
                                    // 番号付きリスト
                                    const num = parseInt(bulletMatch[3]);
                                    marker = bulletMatch[0].replace(/\d+/, (num + 1).toString());
                                } else {
                                    // 箇条書き
                                    marker = bulletMatch[0];
                                }
                            }
                            
                            console.log('Generated marker:', marker);
                            
                            // リスト継続処理
                            e.preventDefault();
                            editor.executeEdits('insertListItem', [{
                                range: new monaco.Range(
                                    position.lineNumber,
                                    position.column,
                                    position.lineNumber,
                                    position.column
                                ),
                                text: '\n' + marker
                            }]);
                            console.log('List continued with:', marker);
                        }
                    } catch (e) {
                        console.error('Error in markdown list continuation:', e);
                    }
                });
                
                // Tabキー押下時のインデント処理（カスタム）
                editor.addCommand(monaco.KeyCode.Tab, function() {
                    const selection = editor.getSelection();
                    const model = editor.getModel();
                    
                    if (!selection || !model) return;
                    
                    // 選択範囲がある場合は複数行のインデント
                    if (selection.startLineNumber !== selection.endLineNumber) {
                        const range = new monaco.Range(
                            selection.startLineNumber,
                            1,
                            selection.endLineNumber,
                            model.getLineMaxColumn(selection.endLineNumber)
                        );
                        
                        const lines = model.getValueInRange(range).split('\n');
                        const indentedLines = lines.map(line => '  ' + line);
                        const indentedText = indentedLines.join('\n');
                        
                        editor.executeEdits('', [{
                            range: range,
                            text: indentedText,
                            forceMoveMarkers: true
                        }]);
                    } else {
                        // 選択範囲がない場合は現在位置に2スペースを挿入
                        const position = editor.getPosition();
                        if (position) {
                            const range = new monaco.Range(
                                position.lineNumber,
                                position.column,
                                position.lineNumber,
                                position.column
                            );
                            
                            editor.executeEdits('', [{
                                range: range,
                                text: '  ',
                                forceMoveMarkers: true
                            }]);
                        }
                    }
                });
                
                // 画像ドロップハンドラをセットアップ
                monacoInterop.setupImageDropHandler(editorId, dotNetHelper);
            }
            
            // キーバインドを設定 - MonacoのデフォルトのUndoRedoを無効化して独自のUndo/Redoを使用
            editor.createContextKey('customUndoRedoEnabled', true);
            
            // デフォルトのキーバインドをオーバーライド
            editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyZ, function() {
                dotNetHelper.invokeMethodAsync('HandleUndoShortcut')
                    .catch(error => {
                        console.error('Error invoking HandleUndoShortcut:', error);
                    });
            }, 'customUndoRedoEnabled');
            
            editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyY, function() {
                dotNetHelper.invokeMethodAsync('HandleRedoShortcut')
                    .catch(error => {
                        console.error('Error invoking HandleRedoShortcut:', error);
                    });
            }, 'customUndoRedoEnabled');
            
            // エディタにフォーカスを当てる
            editor.focus();
            
            // 初期化完了を通知
            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync('HandleEditorInitialized')
                    .catch(error => {
                        console.error('Error invoking HandleEditorInitialized:', error);
                    });
            }
        });
    },

    /**
     * 選択範囲を取得する
     * @param {string} editorId - エディタID
     * @returns {object} テキストと選択範囲情報
     */
    getTextAreaSelection: function (editorId) {
        const editorInstance = this.editors[editorId];
        if (!editorInstance || !editorInstance.instance) {
            console.error(`Editor with ID "${editorId}" not found.`);
            return { text: '', start: 0, end: 0 };
        }

        const editor = editorInstance.instance;
        const selection = editor.getSelection();
        const model = editor.getModel();

        if (selection && model) {
            const selectedText = model.getValueInRange(selection);

            // 文字列内での位置（オフセット）を計算
            const startOffset = model.getOffsetAt({
                lineNumber: selection.startLineNumber,
                column: selection.startColumn
            });

            const endOffset = model.getOffsetAt({
                lineNumber: selection.endLineNumber,
                column: selection.endColumn
            });

            return {
                text: selectedText,
                start: startOffset,
                end: endOffset
            };
        }

        return { text: '', start: 0, end: 0 };
    },

    /**
     * 選択範囲を置き換える
     * @param {string} editorId - エディタID
     * @param {string} text - 置き換えるテキスト
     * @returns {boolean} 成功したかどうか
     */
    replaceTextAreaSelection: function (editorId, text) {
        console.log(`replaceTextAreaSelection called for editorId: ${editorId}, text: ${text.substring(0, 50)}...`);
        
        const editorInstance = this.editors[editorId];
        if (!editorInstance || !editorInstance.instance) {
            console.error(`Editor with ID "${editorId}" not found.`);
            return false;
        }

        const editor = editorInstance.instance;
        const selection = editor.getSelection();
        const model = editor.getModel();

        console.log(`Selection: ${selection ? 'valid' : 'invalid'}, Model: ${model ? 'valid' : 'invalid'}`);
        
        if (selection && model) {
            console.log(`Selection range: L${selection.startLineNumber}:C${selection.startColumn} - L${selection.endLineNumber}:C${selection.endColumn}`);
            
            const range = new monaco.Range(
                selection.startLineNumber,
                selection.startColumn,
                selection.endLineNumber,
                selection.endColumn
            );

            try {
                // テキストを置き換え
                console.log(`Executing edit operation with text length: ${text.length}`);
                editor.executeEdits("replaceSelection", [{ range: range, text: text }]);

                // カーソルを置き換えたテキストの最後に移動
                const newPosition = editor.getPosition();
                if (newPosition) {
                    console.log(`New cursor position: L${newPosition.lineNumber}:C${newPosition.column}`);
                    editor.setPosition(newPosition);
                    editor.focus();
                }

                console.log('Text replacement successful');
                return true;
            } catch (error) {
                console.error('Error during text replacement:', error);
                return false;
            }
        } else {
            console.error('Selection or model is invalid, cannot replace text');
            return false;
        }
    },

    /**
     * カーソル位置を設定する
     * @param {string} editorId - エディタID
     * @param {number} start - 開始位置（オフセット）
     * @param {number} end - 終了位置（オフセット、省略可能）
     */
    setCaretPosition: function (editorId, start, end) {
        const editorInstance = this.editors[editorId];
        if (!editorInstance || !editorInstance.instance) {
            console.error(`Editor with ID "${editorId}" not found.`);
            return;
        }

        const editor = editorInstance.instance;
        const model = editor.getModel();

        if (model) {
            try {
                // オフセットから位置情報に変換
                const startPosition = model.getPositionAt(start);
                const endPosition = model.getPositionAt(end || start);

                // 選択範囲を設定
                editor.setSelection(new monaco.Range(
                    startPosition.lineNumber,
                    startPosition.column,
                    endPosition.lineNumber,
                    endPosition.column
                ));

                editor.focus();
                console.log(`Caret position set to: start=${start}, end=${end || start}`);
            } catch (error) {
                console.error('Error setting caret position:', error);
            }
        } else {
            console.error('Model not available, cannot set caret position');
        }
    },

    /**
     * テキストを整形する（マークダウン用）
     * @param {string} editorId - エディタID
     * @param {string} prefix - 整形用プレフィックス
     * @param {string} suffix - 整形用サフィックス
     */
    formatText: function (editorId, prefix, suffix) {
        const editorInstance = this.editors[editorId];
        if (!editorInstance || !editorInstance.instance) {
            console.error(`Editor with ID "${editorId}" not found.`);
            return;
        }

        const editor = editorInstance.instance;
        const selection = editor.getSelection();
        const model = editor.getModel();

        if (selection && model) {
            const selectedText = model.getValueInRange(selection);
            const range = new monaco.Range(
                selection.startLineNumber,
                selection.startColumn,
                selection.endLineNumber,
                selection.endColumn
            );

            const newText = selectedText.length > 0
                ? `${prefix}${selectedText}${suffix}`
                : `${prefix}${suffix}`;

            editor.executeEdits("formatText", [{ range: range, text: newText }]);

            if (selectedText.length === 0) {
                // カーソル位置をprefixとsuffixの間に設定
                const newPos = model.getPositionAt(
                    model.getOffsetAt({
                        lineNumber: selection.startLineNumber,
                        column: selection.startColumn
                    }) + prefix.length
                );

                editor.setPosition(newPos);
                editor.focus();
            }
        }
    },

    /**
     * 現在のカーソル位置を指定したオフセット分移動する
     * @param {string} editorId - エディタID
     * @param {number} offset - オフセット
     */
    moveCaretPosition: function (editorId, offset) {
        const editorInstance = this.editors[editorId];
        if (!editorInstance || !editorInstance.instance) {
            console.error(`Editor with ID "${editorId}" not found.`);
            return;
        }

        const editor = editorInstance.instance;
        const position = editor.getPosition();

        if (position) {
            const newPosition = new monaco.Position(position.lineNumber, position.column + offset);
            editor.setPosition(newPosition);
            editor.focus();
        }
    },

    /**
     * エディタのテキスト値を取得する
     * @param {string} editorId - エディタID
     * @returns {string} エディタの現在のテキスト
     */
    getValue: function (editorId) {
        const editorInstance = this.editors[editorId];
        if (!editorInstance || !editorInstance.instance) {
            console.error(`Editor with ID "${editorId}" not found.`);
            return '';
        }

        return editorInstance.instance.getValue();
    },

    /**
     * エディタのテキスト値を設定する
     * @param {string} editorId - エディタID
     * @param {string} value - 設定するテキスト
     */
    setValue: function (editorId, value) {
        const editorInstance = this.editors[editorId];
        if (editorInstance && editorInstance.instance) {
            editorInstance.instance.setValue(value);
        }
    },
    
    /**
     * エディタにフォーカスを当てる
     * @param {string} editorId - エディタID
     */
    focus: function (editorId) {
        const editorInstance = this.editors[editorId];
        if (editorInstance && editorInstance.instance) {
            editorInstance.instance.focus();
        }
    },
    
    /**
     * エディタを破棄する
     * @param {string} editorId - エディタID
     */
    dispose: function (editorId) {
        const editorInstance = this.editors[editorId];
        if (editorInstance && editorInstance.instance) {
            editorInstance.instance.dispose();
            delete this.editors[editorId];
        }
    },

    /**
     * イメージのドロップイベントを処理する
     * @param {string} editorId - エディタID 
     * @param {object} dotNetHelper - .NETメソッド呼び出し用のヘルパーオブジェクト
     */
    setupImageDropHandler: function (editorId, dotNetHelper) {
        const editorElement = document.getElementById(editorId);
        if (!editorElement) {
            console.error(`Editor element with ID "${editorId}" not found.`);
            return;
        }
        
        const editorInstance = this.editors[editorId];
        if (!editorInstance || !editorInstance.instance) {
            console.error(`Editor instance with ID "${editorId}" not found.`);
            return;
        }
        
        // Monaco Editorのドロップゾーンを取得（画面に表示されている実際の要素）
        const monacoElements = editorElement.getElementsByClassName('monaco-editor');
        if (monacoElements.length === 0) {
            console.error(`Monaco editor elements not found for editorId "${editorId}".`);
            return;
        }
        
        const dropTarget = monacoElements[0];
        
        // カスタムCSSを追加
        if (!document.querySelector('style#monaco-drop-styles')) {
            const style = document.createElement('style');
            style.id = 'monaco-drop-styles';
            style.textContent = `
                .monaco-editor.drag-over {
                    box-shadow: inset 0 0 0 2px #0078d7;
                }
            `;
            document.head.appendChild(style);
        }
        
        // ドラッグオーバー時のスタイル
        dropTarget.addEventListener('dragover', function(e) {
            e.preventDefault();
            e.stopPropagation();
            dropTarget.classList.add('drag-over');
        });
        
        // ドラッグリーブ時のスタイルリセット
        dropTarget.addEventListener('dragleave', function() {
            dropTarget.classList.remove('drag-over');
        });
        
        // ドロップイベント処理
        dropTarget.addEventListener('drop', async function(e) {
            e.preventDefault();
            e.stopPropagation();
            dropTarget.classList.remove('drag-over');
            
            console.log('Image drop event detected');
            
            // dotNetHelperが利用可能かチェック
            if (!dotNetHelper) {
                console.error('dotNetHelper is not available for image drop handling');
                return;
            }
            
            // ファイルがドロップされたか確認
            if (!e.dataTransfer?.files || e.dataTransfer.files.length === 0) {
                console.log('No files in drop event');
                return;
            }
            
            const file = e.dataTransfer.files[0];
            console.log('Dropped file:', file.name, 'Type:', file.type);
            
            // 画像ファイルかどうかチェック
            if (!file.type.startsWith('image/')) {
                console.log('Not an image file, ignoring:', file.type);
                return;
            }
            
            console.log('Image file detected, processing...');
            
            // エディタインスタンスを取得
            const editor = editorInstance.instance;
            if (!editor) {
                console.error('Editor instance not found');
                return;
            }
            
            try {
                // フォーカスをエディタに設定
                editor.focus();
                
                // IndexedDBに画像を保存
                const imageId = await saveFileToIndexedDB(file);
                console.log('Image saved to IndexedDB with ID:', imageId);
                
                // 保存した画像のBlobURLを取得
                const blobUrl = await getImageBlobUrl(imageId);
                console.log('Blob URL generated:', blobUrl ? blobUrl.substring(0, 50) + '...' : 'null');
                
                if (!blobUrl) {
                    console.error('Failed to generate blob URL for the image');
                    return;
                }
                
                console.log('Calling HandleDroppedFile with file:', file.name);
                
                try {
                    // C#のHandleDroppedFileメソッドを呼び出し
                    const result = await dotNetHelper.invokeMethodAsync('HandleDroppedFile', {
                        blobUrl: blobUrl,
                        fileName: file.name
                    });
                    
                    console.log('HandleDroppedFile completed with result:', result);
                    
                    // C#側での処理が失敗した場合はJavaScript側でフォールバック処理
                    if (result !== "Success") {
                        console.log('C# handler returned failure, inserting text directly as fallback');
                        
                        // 現在の選択範囲を取得
                        const selection = editor.getSelection();
                        
                        // マークダウン形式のテキストを作成
                        const markdownText = `![${file.name}](${blobUrl})\n\n`;
                        
                        if (selection) {
                            // 選択範囲にテキストを挿入
                            const range = new monaco.Range(
                                selection.startLineNumber,
                                selection.startColumn,
                                selection.endLineNumber,
                                selection.endColumn
                            );
                            
                            editor.executeEdits("dropImage", [{ 
                                range: range, 
                                text: markdownText 
                            }]);
                            
                            // カーソル位置を修正して挿入後のテキストに移動
                            const newPos = editor.getPosition();
                            if (newPos) {
                                editor.setPosition(newPos);
                            }
                            
                            console.log('Direct text insertion completed');
                        }
                        else {
                            // 選択範囲がない場合はカーソル位置にテキストを挿入
                            const position = editor.getPosition();
                            if (position) {
                                const range = new monaco.Range(
                                    position.lineNumber,
                                    position.column,
                                    position.lineNumber,
                                    position.column
                                );
                                
                                editor.executeEdits("dropImage", [{ 
                                    range: range, 
                                    text: markdownText 
                                }]);
                                
                                console.log('Text inserted at cursor position');
                            }
                            else {
                                // カーソル位置も不明な場合は末尾に挿入
                                const lastLine = editor.getModel().getLineCount();
                                const lastCol = editor.getModel().getLineMaxColumn(lastLine);
                                
                                const range = new monaco.Range(
                                    lastLine,
                                    lastCol,
                                    lastLine,
                                    lastCol
                                );
                                
                                editor.executeEdits("dropImage", [{ 
                                    range: range, 
                                    text: markdownText 
                                }]);
                                
                                console.log('Text appended to end of document');
                            }
                        }
                        
                        // 変更を通知
                        const newValue = editor.getValue();
                        dotNetHelper.invokeMethodAsync('OnContentChanged', newValue)
                            .catch(error => {
                                console.error('Error notifying content change:', error);
                            });
                    }
                } catch (error) {
                    console.error('HandleDroppedFile error:', error);
                }
            } catch (error) {
                console.error('Error processing dropped image:', error);
            }
        });
        
        console.log('Image drop handler setup complete for', editorId);
    },

    /**
     * テキストエリアの値を設定し、イベントを発火する（主に初期化用）
     * @param {string} editorId - エディタID
     * @param {string} text - 設定するテキスト
     * @param {boolean} dispatchEvent - イベントを発火するかどうか
     */
    setTextAreaValue: function (editorId, text, dispatchEvent = true) {
        const editorInstance = this.editors[editorId];
        if (!editorInstance || !editorInstance.instance) {
            console.error(`Editor with ID "${editorId}" not found.`);
            return;
        }

        editorInstance.instance.setValue(text);
        
        if (dispatchEvent && editorInstance.dotNetHelper) {
            editorInstance.dotNetHelper.invokeMethodAsync('OnContentChanged', text)
                .catch(error => {
                    console.error('Error invoking OnContentChanged:', error);
                });
        }
    },
    
    /**
     * エディタとプレビュー間のスクロール同期をセットアップ
     * @param {string} editorId - エディタID
     * @param {HTMLElement} previewElement - プレビュー表示要素
     */
    setupScrollSync: function(editorId, previewElement) {
        const editorInstance = this.editors[editorId];
        if (!editorInstance || !editorInstance.instance || !previewElement) return;
        
        const editor = editorInstance.instance;
        let isScrolling = false;
        let scrollTimeout = null;
        
        // エディタのスクロールイベント
        editor.onDidScrollChange(e => {
            if (isScrolling) return;
            isScrolling = true;
            
            try {
                const scrollHeight = editor.getScrollHeight();
                const scrollTop = editor.getScrollTop();
                const clientHeight = editor.getLayoutInfo().height;
                
                if (scrollHeight > clientHeight) {
                    const scrollPercentage = scrollTop / (scrollHeight - clientHeight);
                    const previewTargetPosition = scrollPercentage * (previewElement.scrollHeight - previewElement.clientHeight);
                    
                    // プレビューのスクロール位置を設定
                    previewElement.scrollTop = previewTargetPosition;
                }
            } catch (err) {
                console.error('Error in editor scroll sync:', err);
            }
            
            if (scrollTimeout) clearTimeout(scrollTimeout);
            scrollTimeout = setTimeout(() => {
                isScrolling = false;
                scrollTimeout = null;
            }, 100);
        });
        
        // プレビューのスクロールイベント
        previewElement.addEventListener('scroll', () => {
            if (isScrolling) return;
            isScrolling = true;
            
            try {
                const scrollHeight = previewElement.scrollHeight;
                const scrollTop = previewElement.scrollTop;
                const clientHeight = previewElement.clientHeight;
                
                if (scrollHeight > clientHeight) {
                    const scrollPercentage = scrollTop / (scrollHeight - clientHeight);
                    const editorTargetPosition = scrollPercentage * (editor.getScrollHeight() - editor.getLayoutInfo().height);
                    
                    // エディタのスクロール位置を設定
                    editor.setScrollTop(editorTargetPosition);
                }
            } catch (err) {
                console.error('Error in preview scroll sync:', err);
            }
            
            if (scrollTimeout) clearTimeout(scrollTimeout);
            scrollTimeout = setTimeout(() => {
                isScrolling = false;
                scrollTimeout = null;
            }, 100);
        });
        
        // プレビュー内容の変更を監視してスクロール同期を調整
        const observer = new MutationObserver(() => {
            if (editor.getScrollHeight() > 0 && previewElement.scrollHeight > 0) {
                const scrollPercentage = editor.getScrollTop() / (editor.getScrollHeight() - editor.getLayoutInfo().height);
                const previewTargetPosition = scrollPercentage * (previewElement.scrollHeight - previewElement.clientHeight);
                previewElement.scrollTop = previewTargetPosition;
            }
        });

        observer.observe(previewElement, {
            childList: true,
            subtree: true,
            characterData: true
        });
        
        console.log('Scroll sync setup complete between editor and preview');
    },

    /**
     * ファイルダイアログを開いて画像を選択し、ブロブURLを取得する
     * @param {number} maxSizeMB - 最大サイズ（MB）
     * @returns {Promise<object>} 画像情報（ブロブURLとファイル名）
     */
    openFileDialogAndGetBlobUrl: function (maxSizeMB = 5) {
        return new Promise((resolve, reject) => {
            const input = document.createElement('input');
            input.type = 'file';
            input.accept = 'image/*';

            input.onchange = async (e) => {
                const files = input.files;
                if (files && files.length > 0) {
                    const file = files[0];

                    const maxSizeBytes = maxSizeMB * 1024 * 1024;
                    if (file.size > maxSizeBytes) {
                        alert(`Image size is too large. Please select an image under ${maxSizeMB}MB.`);
                        resolve(null);
                        return;
                    }

                    try {
                        const imageId = await saveFileToIndexedDB(file);
                        const blobUrl = await getImageBlobUrl(imageId);

                        resolve({
                            blobUrl: blobUrl,
                            fileName: file.name
                        });
                    } catch (error) {
                        console.error("Error handling file:", error);
                        reject(error);
                    }
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
}

// IndexedDB functions for image handling (from Markdown.razor.ts)
// These are defined outside of monacoInterop to avoid cluttering the main object

let indexedDBInitialized = false;

/**
 * IndexedDBを初期化する
 */
function initializeIndexedDB() {
    if (indexedDBInitialized) return;
    
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
        console.error('IndexedDB initialization error:', request.error);
    };
    
    request.onsuccess = (event) => {
        console.log('IndexedDB initialized successfully');
        indexedDBInitialized = true;
    };
}

/**
 * ファイルをIndexedDBに保存する
 * @param {File} file - 保存するファイル
 * @returns {Promise<string>} ファイルのID
 */
function saveFileToIndexedDB(file) {
    return new Promise((resolve, reject) => {
        initializeIndexedDB();
        
        const dbName = 'PinetreeImageDB';
        const storeName = 'images';
        const request = indexedDB.open(dbName, 1);
        
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

/**
 * IndexedDBからブロブURLを取得する
 * @param {string} id - ファイルID
 * @returns {Promise<string>} ブロブURL
 */
function getImageBlobUrl(id) {
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

/**
 * コンテンツ内のブロブURLを永続URLに置き換える
 * @param {string} content - ブロブURLを含むコンテンツ
 * @returns {Promise<string>} 更新されたコンテンツ
 */
function replaceBlobUrlsInContent(content) {
    // Regular expression to find markdown image syntax with blob URLs
    const regex = /!\[(.*?)\]\((blob:[^)]+)\)/g;
    let match;
    let contentCopy = content;
    let changed = false;

    // Array to store all the matches and their processing promises
    const processingTasks = [];

    // Find all blob URLs in the content
    while ((match = regex.exec(content)) !== null) {
        const fullMatch = match[0];       // The entire match: ![alt](blob:url)
        const altText = match[1];         // The alt text part
        const blobUrl = match[2];         // The blob URL part

        // Create a task for processing each blob URL
        const task = processBlobUrl(fullMatch, altText, blobUrl);
        processingTasks.push(task);
    }

    // Return a promise that resolves when all blob URLs are processed
    return Promise.all(processingTasks).then(results => {
        // Apply all replacements
        for (const result of results) {
            if (result.newText) {
                contentCopy = contentCopy.replace(result.originalText, result.newText);
                changed = true;
            }
        }
        
        return contentCopy;
    });
}

/**
 * 単一のブロブURLを処理する
 * @param {string} originalText - オリジナルテキスト
 * @param {string} altText - 代替テキスト
 * @param {string} blobUrl - ブロブURL
 * @returns {Promise<object>} 処理結果
 */
async function processBlobUrl(originalText, altText, blobUrl) {
    try {
        // ここで実際にはサーバーへ画像をアップロードするコードが必要
        // 現在はダミー実装
        return {
            originalText: originalText,
            newText: null // サーバー側の実装ができたら修正
        };
    } catch (error) {
        console.error('Error processing blob URL:', error);
        return {
            originalText: originalText,
            newText: null
        };
    }
}

/**
 * IndexedDBからすべての画像を消去する
 * @returns {Promise<string>} 処理結果
 */
function clearAllImagesFromIndexedDB() {
    return new Promise((resolve, reject) => {
        const dbName = 'PinetreeImageDB';
        const storeName = 'images';
        const request = indexedDB.open(dbName, 1);

        request.onerror = (event) => {
            reject('IndexedDB error: ' + request.error);
        };

        request.onsuccess = (event) => {
            try {
                const db = request.result;
                const transaction = db.transaction([storeName], 'readwrite');
                const store = transaction.objectStore(storeName);

                const clearRequest = store.clear();

                clearRequest.onsuccess = () => {
                    resolve('All images cleared successfully');
                };

                clearRequest.onerror = () => {
                    reject('Error clearing images: ' + clearRequest.error);
                };
            } catch (error) {
                reject('Error clearing images: ' + error);
            }
        };
    });
}

// このファイルが読み込まれたときにIDBが初期化されるように
// indexedDBの可用性を確保するためのグローバルチェック
if (typeof window !== 'undefined') {
    console.log('Checking IndexedDB availability');
    try {
        if (!window.indexedDB) {
            console.error('IndexedDB is not available in this browser');
        }
    } catch (e) {
        console.error('Error checking IndexedDB:', e);
    }
}

