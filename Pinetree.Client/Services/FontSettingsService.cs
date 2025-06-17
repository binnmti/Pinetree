using Microsoft.JSInterop;

namespace Pinetree.Client.Services;

public class FontSettingsService
{
    private readonly IJSRuntime _jsRuntime;
    private const string FontFamilyKey = "pinetree_font_family";
    private const string FontSizeKey = "pinetree_font_size";

    public FontSettingsService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string> GetFontFamilyAsync()
    {
        try
        {
            var fontFamily = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", FontFamilyKey);
            return string.IsNullOrEmpty(fontFamily) ? "system-ui, -apple-system, sans-serif" : fontFamily;
        }
        catch
        {
            return "system-ui, -apple-system, sans-serif";
        }
    }

    public async Task SetFontFamilyAsync(string fontFamily)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", FontFamilyKey, fontFamily);
        }
        catch
        {
            // Ignore errors for localStorage operations
        }
    }

    public async Task<int> GetFontSizeAsync()
    {
        try
        {
            var fontSize = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", FontSizeKey);
            return int.TryParse(fontSize, out var size) && size >= 10 && size <= 24 ? size : 14;
        }
        catch
        {
            return 14;
        }
    }

    public async Task SetFontSizeAsync(int fontSize)
    {
        try
        {
            if (fontSize >= 10 && fontSize <= 24)
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", FontSizeKey, fontSize.ToString());
            }
        }
        catch
        {
            // Ignore errors for localStorage operations
        }
    }

    public static string[] GetAvailableFontFamilies()
    {
        return new[]
        {
            "system-ui, -apple-system, sans-serif",
            "Georgia, serif",
            "Times, serif", 
            "Arial, sans-serif",
            "Helvetica, sans-serif",
            "Verdana, sans-serif",
            "Monaco, 'Courier New', monospace",
            "'JetBrains Mono', 'Fira Code', monospace"
        };
    }

    public static string GetFontDisplayName(string fontFamily)
    {
        return fontFamily switch
        {
            "system-ui, -apple-system, sans-serif" => "System Default",
            "Georgia, serif" => "Georgia",
            "Times, serif" => "Times",
            "Arial, sans-serif" => "Arial",
            "Helvetica, sans-serif" => "Helvetica", 
            "Verdana, sans-serif" => "Verdana",
            "Monaco, 'Courier New', monospace" => "Monaco",
            "'JetBrains Mono', 'Fira Code', monospace" => "JetBrains Mono",
            _ => "System Default"
        };
    }
}