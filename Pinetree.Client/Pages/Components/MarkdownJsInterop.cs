using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Pinetree.Client.Pages.Components;

public class MarkdownJsInterop(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                                                             "import", "./Pages/Components/Markdown.razor.js").AsTask());

    public class TextSelection
    {
        public string Text { get; set; } = "";
        public int Start { get; set; }
        public int End { get; set; }
    }

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

    public async ValueTask SetTextAreaValueAsync(ElementReference element, string text, bool dispatchEvent = true)
    {
        var module = await _moduleTask.Value;
        await module.InvokeAsync<string>("setTextAreaValue", element, text, dispatchEvent);
    }

    public async ValueTask SetupAllEventListenersAsync<T>(ElementReference container, ElementReference textArea, DotNetObjectReference<T> dotNetRef) where T : class
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("setupAllEventListeners", container, textArea, dotNetRef);
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
}
