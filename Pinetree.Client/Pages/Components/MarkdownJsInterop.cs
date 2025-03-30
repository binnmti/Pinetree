using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Pinetree.Client.Pages.Components;

public class MarkdownJsInterop(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask = new(() =>
        jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/Markdown.js").AsTask());

    public async ValueTask<TextSelection> GetSelectionAsync(ElementReference element)
    {
        await _moduleTask.Value;
        return await jsRuntime.InvokeAsync<TextSelection>("window.MarkdownJS.getTextAreaSelection", element);
    }

    public async ValueTask<bool> ReplaceSelectionAsync(ElementReference element, string text)
    {
        await _moduleTask.Value;
        return await jsRuntime.InvokeAsync<bool>("window.MarkdownJS.replaceTextAreaSelection", element, text);
    }

    public async ValueTask SetCaretPositionAsync(ElementReference element, int start, int end)
    {
        await _moduleTask.Value;
        await jsRuntime.InvokeVoidAsync("window.MarkdownJS.setCaretPosition", element, start, end);
    }

    public async ValueTask SetupLinkInterceptorAsync<T>(ElementReference container, DotNetObjectReference<T> dotNetRef) where T : class
    {
        await _moduleTask.Value;
        await jsRuntime.InvokeVoidAsync("window.MarkdownJS.setupLinkInterceptor", container, dotNetRef);
    }

    public async ValueTask InitializeTooltipsAsync()
    {
        await _moduleTask.Value;
        await jsRuntime.InvokeVoidAsync("window.MarkdownJS.initializeTooltips");
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
