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

    /// <summary>Use fixed sign boxes in Jamakol chart (show only boxes with planets)</summary>
    public bool UseFixedSignBoxes { get; set; } = false;

    /// <summary>Application language code (en, ta)</summary>
    public string Language { get; set; } = "en";

    /// <summary>House calculation system (Placidus, Koch, etc.)</summary>
    public HouseSystem HouseSystem { get; set; } = HouseSystem.Placidus;

    /// <summary>Whether cusp is treated as middle of house (true) or start (false)</summary>
    public bool CuspAsMiddle { get; set; } = true;

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
    FaganBradley = 0,
    Lahiri = 1,
    DeLuce = 2,
    Raman = 3,
    UshaShashi = 4,
    Krishnamurti = 5,
    DjwhalKhul = 6,
    Yukteshwar = 7,
    JNBhasin = 8,
    Babylonian_Kugler1 = 9,
    Babylonian_Kugler2 = 10,
    Babylonian_Kugler3 = 11,
    Babylonian_Huber = 12,
    Babylonian_EtaPiscium = 13,
    Babylonian_Aldebaran15Tau = 14,
    Hipparchus = 15,
    Sassanian = 16,
    GalacticCenter_0Sag = 17,
    J2000 = 18,
    J1900 = 19,
    B1950 = 20,
    Suryasiddhanta = 21,
    Suryasiddhanta_MeanSun = 22,
    Aryabhata = 23,
    Aryabhata_MeanSun = 24,
    SS_Revati = 25,
    SS_Citra = 26,
    TrueChitra = 27,
    TrueRevati = 28,
    TruePushya = 29,
    GalacticCenter_GilBrand = 30,
    GalacticCenter_MulaBol = 31,
    Skydram = 32,
    TrueMula_ChandraHari = 33,
    Dhruva = 34
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
            AyanamshaType.FaganBradley => "Fagan-Bradley",
            AyanamshaType.Lahiri => "Lahiri (Chitrapaksha)",
            AyanamshaType.DeLuce => "De Luce",
            AyanamshaType.Raman => "Raman",
            AyanamshaType.UshaShashi => "Usha-Shashi",
            AyanamshaType.Krishnamurti => "KP (Krishnamurti)",
            AyanamshaType.DjwhalKhul => "Djwhal Khul",
            AyanamshaType.Yukteshwar => "Yukteshwar",
            AyanamshaType.JNBhasin => "JN Bhasin",
            AyanamshaType.Babylonian_Kugler1 => "Babylonian (Kugler 1)",
            AyanamshaType.Babylonian_Kugler2 => "Babylonian (Kugler 2)",
            AyanamshaType.Babylonian_Kugler3 => "Babylonian (Kugler 3)",
            AyanamshaType.Babylonian_Huber => "Babylonian (Huber)",
            AyanamshaType.Babylonian_EtaPiscium => "Babylonian (Eta Piscium)",
            AyanamshaType.Babylonian_Aldebaran15Tau => "Babylonian (Aldebaran = 15 Tau)",
            AyanamshaType.Hipparchus => "Hipparchus",
            AyanamshaType.Sassanian => "Sassanian",
            AyanamshaType.GalacticCenter_0Sag => "Galactic Center (0 Sag)",
            AyanamshaType.J2000 => "J2000",
            AyanamshaType.J1900 => "J1900",
            AyanamshaType.B1950 => "B1950",
            AyanamshaType.Suryasiddhanta => "Suryasiddhanta",
            AyanamshaType.Suryasiddhanta_MeanSun => "Suryasiddhanta (Mean Sun)",
            AyanamshaType.Aryabhata => "Aryabhata",
            AyanamshaType.Aryabhata_MeanSun => "Aryabhata (Mean Sun)",
            AyanamshaType.SS_Revati => "SS Revati",
            AyanamshaType.SS_Citra => "SS Citra",
            AyanamshaType.TrueChitra => "True Chitra (Spica)",
            AyanamshaType.TrueRevati => "True Revati",
            AyanamshaType.TruePushya => "True Pushya",
            AyanamshaType.GalacticCenter_GilBrand => "Galactic Center (Gil Brand)",
            AyanamshaType.GalacticCenter_MulaBol => "Galactic Center (Mula Bol)",
            AyanamshaType.Skydram => "Skydram (Rabinowitz)",
            AyanamshaType.TrueMula_ChandraHari => "True Mula (Chandra Hari)",
            AyanamshaType.Dhruva => "Dhruva",
            _ => ayanamsha.ToString()
        };
    }
}
