namespace JamakolAstrology.Models;

/// <summary>
/// Represents a special point in the chart (Aarudam, Udayam, Kavippu)
/// </summary>
public class SpecialPoint
{
    /// <summary>Name of the special point (Aarudam, Udayam, Kavippu)</summary>
    public string Name { get; set; } = "";

    /// <summary>Short symbol (AR, UD, KV)</summary>
    public string Symbol { get; set; } = "";

    /// <summary>Sign name (Aries, Taurus, etc.)</summary>
    public string Sign { get; set; } = "";

    /// <summary>Sign index (0-11)</summary>
    public int SignIndex { get; set; }

    /// <summary>House number (1-12)</summary>
    public int House => SignIndex + 1;

    /// <summary>Degree within the sign (0-30)</summary>
    public double DegreeInSign { get; set; }

    /// <summary>Absolute longitude (0-360)</summary>
    public double AbsoluteLongitude { get; set; }

    /// <summary>Nakshatra name</summary>
    public string NakshatraName { get; set; } = "";

    /// <summary>Nakshatra pada (1-4)</summary>
    public int Pada { get; set; }

    /// <summary>Format degree as DD:MM</summary>
    public string DegreeDisplay => $"{(int)DegreeInSign}:{(int)((DegreeInSign % 1) * 60):D2}";
}

/// <summary>
/// Container for all three special points: Udayam, Arudam, Kavippu
/// </summary>
public class SpecialPoints
{
    public SpecialPoint Udayam { get; set; } = new();
    public SpecialPoint Arudam { get; set; } = new();
    public SpecialPoint Kavippu { get; set; } = new();
}
