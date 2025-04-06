using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Pinetree.Client.Pages.Components;

public static class MarkdownJsInterop
{
    private static async ValueTask<IJSObjectReference> GetModuleAsync(this IJSRuntime jsRuntime)
    {
        return await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./Pages/Components/Markdown.razor.js");
    }

    public static async ValueTask<TextSelection> GetSelectionAsync(this IJSRuntime jsRuntime, ElementReference element)
    {
        var module = await jsRuntime.GetModuleAsync();
        try
        {
            return await module.InvokeAsync<TextSelection>("getTextAreaSelection", element);
        }
        finally
        {
            await module.DisposeAsync();
        }
    }

    public static async ValueTask<bool> ReplaceSelectionAsync(this IJSRuntime jsRuntime, ElementReference element, string text)
    {
        var module = await jsRuntime.GetModuleAsync();
        try
        {
            return await module.InvokeAsync<bool>("replaceTextAreaSelection", element, text);
        }
        finally
        {
            await module.DisposeAsync();
        }
    }

    public static async ValueTask SetCaretPositionAsync(this IJSRuntime jsRuntime, ElementReference element, int start, int end)
    {
        var module = await jsRuntime.GetModuleAsync();
        try
        {
            await module.InvokeVoidAsync("setCaretPosition", element, start, end);
        }
        finally
        {
            await module.DisposeAsync();
        }
    }

    public static async ValueTask SetupLinkInterceptorAsync<T>(this IJSRuntime jsRuntime, ElementReference container, DotNetObjectReference<T> dotNetRef) where T : class
    {
        var module = await jsRuntime.GetModuleAsync();
        try
        {
            await module.InvokeVoidAsync("setupLinkInterceptor", container, dotNetRef);
        }
        finally
        {
            await module.DisposeAsync();
        }
    }

    public static async ValueTask SetupBeforeUnloadWarningAsync<T>(this IJSRuntime jsRuntime, DotNetObjectReference<T> dotNetRef) where T : class
    {
        var module = await jsRuntime.GetModuleAsync();
        try
        {
            await module.InvokeVoidAsync("setupBeforeUnloadWarning", dotNetRef);
        }
        finally
        {
            await module.DisposeAsync();
        }
    }

    public static async ValueTask InitializeTooltipsAsync(this IJSRuntime jsRuntime)
    {
        var module = await jsRuntime.GetModuleAsync();
        try
        {
            await module.InvokeVoidAsync("initializeTooltips");
        }
        finally
        {
            await module.DisposeAsync();
        }
    }

    public class TextSelection
    {
        public string Text { get; set; } = "";
        public int Start { get; set; }
        public int End { get; set; }
    }
}
