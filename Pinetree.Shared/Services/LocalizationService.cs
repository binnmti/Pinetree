using System.Globalization;
using System.Resources;
using Microsoft.JSInterop;

namespace Pinetree.Shared.Services;

public class LocalizationService
{
    private readonly IJSRuntime? _jsRuntime;
    private CultureInfo _currentCulture = CultureInfo.CurrentCulture;
    private bool _isInitialized = false;
    
    public event Action? CultureChanged;
    public CultureInfo CurrentCulture => _currentCulture;

    public LocalizationService(IJSRuntime? jsRuntime = null) => _jsRuntime = jsRuntime;

    public async Task InitializeAsync()
    {
        if (_isInitialized || _jsRuntime == null) return;
        
        var detectedLanguage = await DetectPreferredLanguageAsync();
        SetCulture(detectedLanguage);
        _isInitialized = true;
    }

    private async Task<string> DetectPreferredLanguageAsync()
    {
        if (_jsRuntime == null) return "en";

        try
        {
            var savedLanguage = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "preferredLanguage");
            if (IsValidLanguage(savedLanguage)) return savedLanguage;

            var browserLanguage = await _jsRuntime.InvokeAsync<string>("detectBrowserLanguage");
            if (!string.IsNullOrEmpty(browserLanguage))
            {
                var langCode = browserLanguage.Split('-')[0].ToLower();
                if (IsValidLanguage(langCode)) return langCode;
            }
        }
        catch { }

        return "en";
    }

    private static bool IsValidLanguage(string? langCode) => langCode is "en" or "ja";

    public async Task SetCultureAsync(string culture)
    {
        SetCulture(culture);
        
        if (_jsRuntime != null)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "preferredLanguage", culture);
            }
            catch { }
        }
    }

    public void SetCulture(string culture)
    {
        var newCulture = new CultureInfo(culture);
        if (!_currentCulture.Equals(newCulture))
        {
            _currentCulture = newCulture;
            CultureInfo.CurrentCulture = newCulture;
            CultureInfo.CurrentUICulture = newCulture;
            CultureChanged?.Invoke();
        }
    }

    public string GetLocalizedString(string key, Type resourceType)
    {
        try
        {
            var resourceManager = new ResourceManager(resourceType);
            return resourceManager.GetString(key, _currentCulture) ?? key;
        }
        catch
        {
            return key;
        }
    }
}
