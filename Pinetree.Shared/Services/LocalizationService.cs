using System.Globalization;
using System.Resources;
using Microsoft.JSInterop;

namespace Pinetree.Shared.Services;

public class LocalizationService
{
    private readonly IJSRuntime? _jsRuntime;
    private CultureInfo _currentCulture = new CultureInfo("en"); // Default to English
    private bool _isInitialized = false;
    
    public event Action? CultureChanged;
    
    public CultureInfo CurrentCulture => _currentCulture;
    public bool IsInitialized => _isInitialized;

    public LocalizationService(IJSRuntime? jsRuntime = null) 
    {
        _jsRuntime = jsRuntime;
        // Set default culture immediately
        CultureInfo.CurrentCulture = _currentCulture;
        CultureInfo.CurrentUICulture = _currentCulture;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized || _jsRuntime == null) 
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                CultureChanged?.Invoke(); // Trigger UI update even if JSRuntime is null
            }
            return;
        }
        
        var detectedLanguage = await DetectPreferredLanguageAsync();
        SetCulture(detectedLanguage);
        _isInitialized = true;
        CultureChanged?.Invoke(); // Ensure UI updates after initialization
    }

    private async Task<string> DetectPreferredLanguageAsync()
    {
        if (_jsRuntime == null) return "en";

        try
        {
            // 1. LocalStorage から保存された設定を取得（ユーザーが明示的に選択した言語）
            var savedLanguage = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "preferredLanguage");
            if (IsValidLanguage(savedLanguage)) 
            {
                Console.WriteLine($"Using saved language: {savedLanguage}");
                return savedLanguage;
            }

            // 2. ブラウザ言語を検出
            var browserLanguage = await _jsRuntime.InvokeAsync<string>("detectBrowserLanguage");
            if (!string.IsNullOrEmpty(browserLanguage))
            {
                var langCode = browserLanguage.Split('-')[0].ToLower();
                if (IsValidLanguage(langCode)) 
                {
                    Console.WriteLine($"Using browser language: {langCode} (from {browserLanguage})");
                    return langCode;
                }
                else
                {
                    Console.WriteLine($"Browser language {langCode} not supported, using default");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error detecting language: {ex.Message}");
        }

        // 3. デフォルトは英語
        Console.WriteLine("Using default language: en");
        return "en";
    }

    private static bool IsValidLanguage(string? langCode) => langCode is "en" or "ja";

    public async Task SetCultureAsync(string culture)
    {
        SetCulture(culture);
        
        // ユーザーが手動で変更した場合は LocalStorage に保存
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
