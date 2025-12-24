using System;
using System.IO;
using System.Text.Json;
using JamakolAstrology.Models;

namespace JamakolAstrology.Models;

/// <summary>
/// Application settings for sunrise, ayanamsha, etc.
/// </summary>
public class AppSettings
{
    /// <summary>Selected ayanamsha type</summary>
    public AyanamshaType Ayanamsha { get; set; } = AyanamshaType.Lahiri;

    /// <summary>Default location name</summary>
    public string DefaultLocationName { get; set; } = "Chennai";

    /// <summary>Default location latitude</summary>
    public double DefaultLatitude { get; set; } = 13.0827; // Chennai

    /// <summary>Default location longitude</summary>
    public double DefaultLongitude { get; set; } = 80.2707; // Chennai

    /// <summary>Default timezone offset in hours</summary>
    public double DefaultTimezone { get; set; } = 5.5; // IST

    /// <summary>Sunrise calculation mode</summary>
    public SunriseCalculationMode SunriseMode { get; set; } = SunriseCalculationMode.TipApparent; // Default: Tip of disk appears on horizon

    /// <summary>Font size for text inside chart cells (planets, special points)</summary>
    public double ChartFontSize { get; set; } = 14;

    /// <summary>Font size for Jama Graha corner boxes</summary>
    public double JamaGrahaFontSize { get; set; } = 10;
    
    /// <summary>Font size for DataGrid tables (Jama Graha, Planets, Panchanga)</summary>
    public double TableFontSize { get; set; } = 11;
    
    /// <summary>Font size for Input Toolbar (Date, Time, Location)</summary>
    public double InputFontSize { get; set; } = 12;

    /// <summary>Calculation mode for Prasanna section</summary>
    public PrasannaCalcMode PrasannaMode { get; set; } = PrasannaCalcMode.JamaGrahaBoxSign;

    /// <summary>Default Tab Selection (0=Birth, 1=Jamakol)</summary>
    public int DefaultTabIndex { get; set; } = 0;

    // Settings saved to user's AppData folder (writable even with Program Files install)
    private static string SettingsFolder
    {
        get
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JamakolAstrology");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }
    }
    private static string SettingsFilePath => Path.Combine(SettingsFolder, "settings.json");

    /// <summary>
    /// Save settings to JSON file
    /// </summary>
    public void Save()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(this, options);
            File.WriteAllText(SettingsFilePath, jsonString);
        }
        catch (Exception)
        {
            // Ignore save errors
        }
    }

    /// <summary>
    /// Load settings from JSON file. Returns default if file not found or invalid.
    /// </summary>
    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string jsonString = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(jsonString);
                return settings ?? new AppSettings();
            }
        }
        catch (Exception)
        {
            // Ignore load errors, return default
        }
        return new AppSettings();
    }
}

/// <summary>
/// Calculation modes for Prasanna section
/// </summary>
public enum PrasannaCalcMode
{
    /// <summary>Use Jama Graha box sign position (ignores fixed sign calculation)</summary>
    JamaGrahaBoxSign = 0,

    /// <summary>Use Jama Graha real calculated degree</summary>
    JamaGrahaRealDegree = 1
}

/// <summary>
/// Modes for calculating sunrise/sunset
/// </summary>
public enum SunriseCalculationMode
{
    /// <summary>Center of Sun's disk is truly on the horizon</summary>
    CenterTrue = 0,

    /// <summary>Tip of Sun's disk is truly on the horizon</summary>
    TipTrue = 1,

    /// <summary>Tip of Sun's disk appears to be on the horizon (with refraction)</summary>
    TipApparent = 2,

    /// <summary>Center of Sun's disk appears to be on the horizon (with refraction)</summary>
    CenterApparent = 3
}

/// <summary>
/// Ayanamsha types supported by the application
/// </summary>
public enum AyanamshaType
{
    /// <summary>Lahiri (Chitrapaksha) - Most commonly used in India</summary>
    Lahiri = 1,

    /// <summary>Raman - B.V. Raman's ayanamsha</summary>
    Raman = 3,

    /// <summary>KP Krishnamurti - Used in KP system</summary>
    KP_Krishnamurti = 5,

    /// <summary>True Chitra - Based on fixed star Spica</summary>
    TrueChitra = 27,

    /// <summary>Fagan-Bradley - Western sidereal</summary>
    FaganBradley = 0,

    /// <summary>De Luce</summary>
    DeLuce = 4,

    /// <summary>Djwhal Khul</summary>
    DjwhalKhul = 12,

    /// <summary>Yukteshwar - Sri Yukteshwar's ayanamsha</summary>
    Yukteshwar = 7,

    /// <summary>JN Bhasin</summary>
    JNBhasin = 8,

    /// <summary>Usha-Shashi</summary>
    UshaShashi = 6
}

/// <summary>
/// Extension methods for AyanamshaType
/// </summary>
public static class AyanamshaExtensions
{
    /// <summary>Get display name for ayanamsha type</summary>
    public static string GetDisplayName(this AyanamshaType ayanamsha)
    {
        return ayanamsha switch
        {
            AyanamshaType.Lahiri => "Lahiri (Chitrapaksha)",
            AyanamshaType.Raman => "Raman",
            AyanamshaType.KP_Krishnamurti => "KP (Krishnamurti)",
            AyanamshaType.TrueChitra => "True Chitra",
            AyanamshaType.FaganBradley => "Fagan-Bradley",
            AyanamshaType.DeLuce => "De Luce",
            AyanamshaType.DjwhalKhul => "Djwhal Khul",
            AyanamshaType.Yukteshwar => "Yukteshwar",
            AyanamshaType.JNBhasin => "JN Bhasin",
            AyanamshaType.UshaShashi => "Usha-Shashi",
            _ => ayanamsha.ToString()
        };
    }
}
