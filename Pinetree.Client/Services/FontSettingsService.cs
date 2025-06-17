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
            var fontFamily = await _jsRuntime.InvokeAsync<string>("getCookie", FontFamilyKey);
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
            await _jsRuntime.InvokeVoidAsync("setCookie", FontFamilyKey, fontFamily, 365);
        }
        catch
        {
            // Ignore errors for cookie operations
        }
    }

    public async Task<int> GetFontSizeAsync()
    {
        try
        {
            var fontSize = await _jsRuntime.InvokeAsync<string>("getCookie", FontSizeKey);
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
                await _jsRuntime.InvokeVoidAsync("setCookie", FontSizeKey, fontSize.ToString(), 365);
            }
        }
        catch
        {
            // Ignore errors for cookie operations
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
}