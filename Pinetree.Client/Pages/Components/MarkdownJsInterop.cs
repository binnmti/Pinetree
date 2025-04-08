﻿using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Pinetree.Client.Pages.Components;

public class MarkdownJsInterop(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                                                             "import", "./Pages/Components/Markdown.razor.js").AsTask());

    public async ValueTask<TextSelection> GetSelectionAsync(ElementReference element)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<TextSelection>("getTextAreaSelection", element);
    }

    public async ValueTask<bool> ReplaceSelectionAsync(ElementReference element, string text)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<bool>("replaceTextAreaSelection", element, text);
    }

    public async ValueTask SetCaretPositionAsync(ElementReference element, int start, int end)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("setCaretPosition", element, start, end);
    }

    public async ValueTask SetupLinkInterceptorAsync<T>(ElementReference container, DotNetObjectReference<T> dotNetRef) where T : class
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("setupLinkInterceptor", container, dotNetRef);
    }

    public async ValueTask SetupBeforeUnloadWarningAsync<T>(DotNetObjectReference<T> dotNetRef) where T : class
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("setupBeforeUnloadWarning", dotNetRef);
    }

    public async ValueTask SetTextAreaValueAsync(ElementReference element, string text, bool dispatchEvent = true)
    {
        var module = await _moduleTask.Value;
        await module.InvokeAsync<string>("setTextAreaValue", element, text, dispatchEvent);
    }

    public async ValueTask PerformUndoAsync(ElementReference element)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("performUndo", element);
    }

    public async ValueTask PerformRedoAsync(ElementReference element)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("performRedo", element);
    }

    public async ValueTask SetupKeyboardShortcutsAsync(ElementReference element)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("setupKeyboardShortcuts", element);
    }

    public async ValueTask InitializeTooltipsAsync()
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("initializeTooltips");
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }

    public class TextSelection
    {
        public string Text { get; set; } = "";
        public int Start { get; set; }
        public int End { get; set; }
    }
}
