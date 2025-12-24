namespace JamakolAstrology.Models;

/// <summary>
/// Jama Graha position data
/// </summary>
public class JamaGrahaPosition
{
    public string Name { get; set; } = "";
    public string Symbol { get; set; } = "";
    public int House { get; set; }
    public int Sign { get; set; }
    public string SignName { get; set; } = "";
    public double Degree { get; set; }
    public double DegreeInSign { get; set; }
    public int Nakshatra { get; set; }
    public string NakshatraName { get; set; } = "";
    public int Pada { get; set; }

    // Formatted display properties
    public string DegreeDisplay => $"{(int)DegreeInSign}°{(int)((DegreeInSign % 1) * 60)}'{(int)(((DegreeInSign % 1) * 60 % 1) * 60)}\"";
    public string TimeFormat => $"{(int)DegreeInSign}:{(int)((DegreeInSign % 1) * 60):D2}";
}
