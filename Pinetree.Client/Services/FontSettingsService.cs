using Microsoft.JSInterop;

namespace Pinetree.Client.Services;

public class FontSettingsService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private const string FontFamilyKey = "pinetree_font_family";
    private const string FontSizeKey = "pinetree_font_size";
    private IJSObjectReference? _jsModule;

    public FontSettingsService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    private async Task<IJSObjectReference> GetJsModuleAsync()
    {
        if (_jsModule == null)
        {
            _jsModule = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./Pages/Components/Markdown.razor.js");
        }
        return _jsModule;
    }

    public async Task<string> GetFontFamilyAsync()
    {
        try
        {
            var jsModule = await GetJsModuleAsync();
            var fontFamily = await jsModule.InvokeAsync<string>("getCookie", FontFamilyKey);
            return string.IsNullOrEmpty(fontFamily) ? "system-ui, -apple-system, sans-serif" : fontFamily;
        }
        catch (JSException jsEx)
        {
            Console.WriteLine($"JavaScript error getting font family: {jsEx.Message}");
            return "system-ui, -apple-system, sans-serif";
        }
        catch (JSDisconnectedException jsDisconnectedEx)
        {
            Console.WriteLine($"JavaScript runtime disconnected while getting font family: {jsDisconnectedEx.Message}");
            return "system-ui, -apple-system, sans-serif";
        }
        catch (InvalidOperationException invOpEx) when (invOpEx.Message.Contains("JavaScript"))
        {
            Console.WriteLine($"JavaScript not available while getting font family: {invOpEx.Message}");
            return "system-ui, -apple-system, sans-serif";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting font family from cookie: {ex.Message}");
            return "system-ui, -apple-system, sans-serif";
        }
    }

    public async Task SetFontFamilyAsync(string fontFamily)
    {
        try
        {
            var jsModule = await GetJsModuleAsync();
            await jsModule.InvokeVoidAsync("setCookie", FontFamilyKey, fontFamily, 365);
        }
        catch (JSException jsEx)
        {
            Console.WriteLine($"JavaScript error setting font family: {jsEx.Message}");
        }
        catch (JSDisconnectedException jsDisconnectedEx)
        {
            Console.WriteLine($"JavaScript runtime disconnected while setting font family: {jsDisconnectedEx.Message}");
        }
        catch (InvalidOperationException invOpEx) when (invOpEx.Message.Contains("JavaScript"))
        {
            Console.WriteLine($"JavaScript not available while setting font family: {invOpEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting font family cookie: {ex.Message}");
        }
    }

    public async Task<int> GetFontSizeAsync()
    {
        try
        {
            var jsModule = await GetJsModuleAsync();
            var fontSize = await jsModule.InvokeAsync<string>("getCookie", FontSizeKey);
            return int.TryParse(fontSize, out var size) && size >= 10 && size <= 24 ? size : 14;
        }
        catch (JSException jsEx)
        {
            Console.WriteLine($"JavaScript error getting font size: {jsEx.Message}");
            return 14;
        }
        catch (JSDisconnectedException jsDisconnectedEx)
        {
            Console.WriteLine($"JavaScript runtime disconnected while getting font size: {jsDisconnectedEx.Message}");
            return 14;
        }
        catch (InvalidOperationException invOpEx) when (invOpEx.Message.Contains("JavaScript"))
        {
            Console.WriteLine($"JavaScript not available while getting font size: {invOpEx.Message}");
            return 14;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting font size from cookie: {ex.Message}");
            return 14;
        }
    }

    public async Task SetFontSizeAsync(int fontSize)
    {
        try
        {
            if (fontSize >= 10 && fontSize <= 24)
            {
                var jsModule = await GetJsModuleAsync();
                await jsModule.InvokeVoidAsync("setCookie", FontSizeKey, fontSize.ToString(), 365);
            }
        }
        catch (JSException jsEx)
        {
            Console.WriteLine($"JavaScript error setting font size: {jsEx.Message}");
        }
        catch (JSDisconnectedException jsDisconnectedEx)
        {
            Console.WriteLine($"JavaScript runtime disconnected while setting font size: {jsDisconnectedEx.Message}");
        }
        catch (InvalidOperationException invOpEx) when (invOpEx.Message.Contains("JavaScript"))
        {
            Console.WriteLine($"JavaScript not available while setting font size: {invOpEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting font size cookie: {ex.Message}");
        }
    }

    public static string[] GetAvailableFontFamilies()
    {
        return new[]
        {
            "system-ui, -apple-system, sans-serif",
            "Georgia, serif",
            "Times, 'Times New Roman', serif",
            "Arial, sans-serif",
            "Helvetica, sans-serif",
            "Verdana, sans-serif",
            "Calibri, sans-serif",
            "Segoe UI, sans-serif",
            "Tahoma, sans-serif",
            "Trebuchet MS, sans-serif",
            "Impact, sans-serif",
            "Monaco, 'Courier New', monospace",
            "Consolas, 'Courier New', monospace",
            "'Courier New', monospace"
        };
    }

    public static string GetFontDisplayName(string fontFamily)
    {
        return fontFamily switch
        {
            "system-ui, -apple-system, sans-serif" => "System Default",
            "Georgia, serif" => "Georgia",
            "Times, 'Times New Roman', serif" => "Times",
            "Arial, sans-serif" => "Arial",
            "Helvetica, sans-serif" => "Helvetica",
            "Verdana, sans-serif" => "Verdana",
            "Calibri, sans-serif" => "Calibri",
            "Segoe UI, sans-serif" => "Segoe UI",
            "Tahoma, sans-serif" => "Tahoma",
            "Trebuchet MS, sans-serif" => "Trebuchet MS",
            "Impact, sans-serif" => "Impact",
            "Monaco, 'Courier New', monospace" => "Monaco",
            "Consolas, 'Courier New', monospace" => "Consolas",
            "'Courier New', monospace" => "Courier New",
            _ => "System Default"
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule != null)
        {
            await _jsModule.DisposeAsync();
        }
    }
}