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
            "Roboto, sans-serif",
            "Open Sans, sans-serif",
            "Lato, sans-serif",
            "Montserrat, sans-serif",
            "Poppins, sans-serif",
            "Source Sans Pro, sans-serif",
            "Monaco, 'Courier New', monospace",
            "'JetBrains Mono', 'Fira Code', monospace",
            "'Cascadia Code', 'Consolas', monospace",
            "'Source Code Pro', monospace",
            "'Ubuntu Mono', monospace",
            "'Roboto Mono', monospace"
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
            "Roboto, sans-serif" => "Roboto",
            "Open Sans, sans-serif" => "Open Sans",
            "Lato, sans-serif" => "Lato",
            "Montserrat, sans-serif" => "Montserrat",
            "Poppins, sans-serif" => "Poppins",
            "Source Sans Pro, sans-serif" => "Source Sans Pro",
            "Monaco, 'Courier New', monospace" => "Monaco",
            "'JetBrains Mono', 'Fira Code', monospace" => "JetBrains Mono",
            "'Cascadia Code', 'Consolas', monospace" => "Cascadia Code",
            "'Source Code Pro', monospace" => "Source Code Pro",
            "'Ubuntu Mono', monospace" => "Ubuntu Mono",
            "'Roboto Mono', monospace" => "Roboto Mono",
            _ => "System Default"
        };
    }
}