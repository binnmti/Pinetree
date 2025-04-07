using Microsoft.JSInterop;

namespace Pinetree.Client.Services;

public static class JsInterop
{
    public static ValueTask<bool> ConfirmAsync(this IJSRuntime jsRuntime, string message)
        => jsRuntime.InvokeAsync<bool>("confirm", message);

    public static ValueTask AlertAsync(this IJSRuntime jsRuntime, string message)
        => jsRuntime.InvokeVoidAsync("alert", message);

    public static ValueTask<string> PromptAsync(this IJSRuntime jsRuntime, string message, string defaultValue = "")
        => jsRuntime.InvokeAsync<string>("prompt", message, defaultValue);

    // TODO: Replace eval usage with a dedicated JS function for better security and performance.
    // See PR #15 discussion for alternative implementation approaches.
    public static ValueTask<string> GetTextAreaValueAsync(this IJSRuntime jsRuntime)
        => jsRuntime.InvokeAsync<string>("eval", $"document.querySelector('textarea').value");

    // This calling method does not match the design intent of Blazor/JSInterop.
    public static ValueTask CopyToClipboardAsync(this IJSRuntime jsRuntime, string text)
        => jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
}
