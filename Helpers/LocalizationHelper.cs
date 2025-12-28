using System.Globalization;
using System.Threading;
using JamakolAstrology.Resources;

namespace JamakolAstrology.Helpers;

/// <summary>
/// Helper class for managing application localization
/// </summary>
public static class LocalizationHelper
{
    /// <summary>
    /// Supported language codes
    /// </summary>
    public static readonly string[] SupportedLanguages = { "en", "ta" };

    /// <summary>
    /// Get display name for a language code
    /// </summary>
    public static string GetLanguageDisplayName(string languageCode)
    {
        return languageCode switch
        {
            "en" => "English",
            "ta" => "தமிழ் (Tamil)",
            _ => languageCode
        };
    }

    /// <summary>
    /// Set the application language
    /// </summary>
    /// <param name="languageCode">Language code (e.g., "en", "ta")</param>
    public static void SetLanguage(string languageCode)
    {
        try
        {
            var culture = new CultureInfo(languageCode);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
        catch
        {
            // Fall back to English if the culture is not supported
            var englishCulture = new CultureInfo("en");
            Thread.CurrentThread.CurrentCulture = englishCulture;
            Thread.CurrentThread.CurrentUICulture = englishCulture;
        }
    }

    /// <summary>
    /// Get the current language code
    /// </summary>
    public static string GetCurrentLanguage()
    {
        return Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
    }

    /// <summary>
    /// Get a localized string from resources
    /// </summary>
    public static string GetString(string key)
    {
        try
        {
            var value = Strings.ResourceManager.GetString(key, Thread.CurrentThread.CurrentUICulture);
            return value ?? key;
        }
        catch
        {
            return key;
        }
    }
}
