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

    public LocalizationService(IJSRuntime? jsRuntime = null)
    {
        _jsRuntime = jsRuntime;
    }

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
            // 1. LocalStorage から保存された設定を取得（ユーザーが明示的に選択した言語）
            var savedLanguage = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "preferredLanguage");
            if (!string.IsNullOrEmpty(savedLanguage) && IsValidLanguage(savedLanguage))
            {
                Console.WriteLine($"Using saved language preference: {savedLanguage}");
                return savedLanguage;
            }

            // 2. ブラウザの言語設定を確認（複数の方法で試行）
            var browserLanguage = await DetectBrowserLanguageAsync();
            if (!string.IsNullOrEmpty(browserLanguage))
            {
                var langCode = browserLanguage.Split('-')[0].ToLower();
                if (IsValidLanguage(langCode))
                {
                    Console.WriteLine($"Using browser language: {browserLanguage} -> {langCode}");
                    return langCode;
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

    private async Task<string> DetectBrowserLanguageAsync()
    {
        try
        {
            // より堅牢なブラウザ言語検出のためのJavaScript関数を呼び出す
            var language = await _jsRuntime!.InvokeAsync<string>("detectBrowserLanguage");
            Console.WriteLine($"Detected browser language: {language ?? "null"}");
            return language ?? string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calling detectBrowserLanguage: {ex.Message}");
            return string.Empty;
        }
    }

    private static bool IsValidLanguage(string langCode)
    {
        return langCode == "en" || langCode == "ja";
    }

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
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving language preference: {ex.Message}");
            }
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
            var localizedString = resourceManager.GetString(key, _currentCulture);
            return localizedString ?? key;
        }
        catch
        {
            return key;
        }
    }
}
