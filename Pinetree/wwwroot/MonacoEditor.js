window.monacoInterop = {
    editors: {},
    
    initialize: function (editorId, language, theme, value, dotNetHelper) {
        require.config({ paths: { 'vs': 'lib/monaco-editor/min/vs' }});
        
        require(['vs/editor/editor.main'], function() {
            // エディタのインスタンスを作成
            const editor = monaco.editor.create(document.getElementById(editorId), {
                value: value,
                language: language,
                theme: theme,
                automaticLayout: true,
                minimap: { enabled: true }
            });

            // エディタのインスタンスを保存
            monacoInterop.editors[editorId] = {
                instance: editor,
                dotNetHelper: dotNetHelper
            };
            
            // 変更イベントをハンドル
            editor.onDidChangeModelContent(function () {
                const newValue = editor.getValue();
                dotNetHelper.invokeMethodAsync('OnContentChanged', newValue);
            });
        });
    },

    // 選択範囲を取得する関数
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

    // 選択範囲を置き換える関数
    replaceTextAreaSelection: function (editorId, text) {
        const editorInstance = this.editors[editorId];
        if (!editorInstance || !editorInstance.instance) {
            console.error(`Editor with ID "${editorId}" not found.`);
            return false;
        }

        const editor = editorInstance.instance;
        const selection = editor.getSelection();
        const model = editor.getModel();

        if (selection && model) {
            const range = new monaco.Range(
                selection.startLineNumber,
                selection.startColumn,
                selection.endLineNumber,
                selection.endColumn
            );

            // テキストを置き換え
            editor.executeEdits("replaceSelection", [{ range: range, text: text }]);

            // カーソルを置き換えたテキストの最後に移動
            const newPosition = editor.getPosition();
            editor.setPosition(newPosition);
            editor.focus();

            return true;
        }

        return false;
    },

    // カーソル位置を設定する関数
    setCaretPosition: function (editorId, start, end) {
        const editorInstance = this.editors[editorId];
        if (!editorInstance || !editorInstance.instance) {
            console.error(`Editor with ID "${editorId}" not found.`);
            return;
        }

        const editor = editorInstance.instance;
        const model = editor.getModel();

        if (model) {
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
        }
    },

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
                // カーソル位置をprefixとsuffixの間にする
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

    setCaretPosition(editorId, offset) {
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

    getValue(editorId) {
        const editorInstance = this.editors[editorId];
        if (!editorInstance || !editorInstance.instance) {
            console.error(`Editor with ID "${editorId}" not found.`);
            return '';
        }

        return editorInstance.instance.getValue();
    },

    setValue: function (editorId, value) {
        const editor = monacoInterop.editors[editorId];
        if (editor && editor.instance) {
            editor.instance.setValue(value);
        }
    },
    
    focus: function (editorId) {
        const editor = monacoInterop.editors[editorId];
        if (editor && editor.instance) {
            editor.instance.focus();
        }
    },
    
    dispose: function (editorId) {
        const editor = monacoInterop.editors[editorId];
        if (editor && editor.instance) {
            editor.instance.dispose();
            delete monacoInterop.editors[editorId];
        }
    }
};

