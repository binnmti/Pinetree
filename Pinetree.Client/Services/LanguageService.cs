using System.Globalization;

namespace Pinetree.Client.Services;

public class LanguageService
{
    public event Action? OnLanguageChanged;

    public CultureInfo CurrentCulture { get; private set; } = CultureInfo.CurrentCulture;

    public void SetLanguage(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        OnLanguageChanged?.Invoke();
    }

    public string[] GetSupportedLanguages()
    {
        return new[] { "en", "ja" };
    }

    public string GetLanguageDisplayName(string cultureName)
    {
        return cultureName switch
        {
            "en" => "English",
            "ja" => "日本語",
            _ => cultureName
        };
    }
}