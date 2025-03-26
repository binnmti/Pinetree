﻿using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Pinetree.Client.Services;

public class TextAreaInterop(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                                                             "import", "../Pages/Components/Markdown.razor.js").AsTask());

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
