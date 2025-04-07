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

    // TODO: eval is not good
    public static ValueTask<string> GetTextAreaValueAsync(this IJSRuntime jsRuntime)
        => jsRuntime.InvokeAsync<string>("eval", $"document.querySelector('textarea').value");

    public static ValueTask CopyToClipboardAsync(this IJSRuntime jsRuntime, string text)
        => jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", $"{text}");
}
