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

    formatText(editorId, prefix, suffix) {
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
                const newPosition = new monaco.Position(
                    selection.startLineNumber,
                    selection.startColumn + prefix.length
                );
                editor.setPosition(newPosition);
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

