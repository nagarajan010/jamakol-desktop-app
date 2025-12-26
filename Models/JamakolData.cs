using System.Collections.Generic;
using JamakolAstrology.Services; // For ZodiacUtils if needed, but Models shouldn't depend on Services. 
// ZodiacUtils seems to be a static helper. Ideally it should be in Helpers.
// Checking dependencies: JamakolPlanetPosition uses ZodiacUtils.PlanetNames
// I will keep using JamakolAstrology.Services for now if ZodiacUtils is there, 
// OR I should check where ZodiacUtils is. It is in Services/ZodiacUtils.cs.

namespace JamakolAstrology.Models;

/// <summary>
/// Jamakol calculation results
/// </summary>
public class JamakolData
{
    public ChartData ChartData { get; set; } = new();
    public Dictionary<int, int> HouseValues { get; set; } = new();
    public List<JamakolPlanetPosition> PlanetPositions { get; set; } = new();
    public int LagnaValue { get; set; }
    public int SuryaValue { get; set; }
    public int ChandraValue { get; set; }
}

/// <summary>
/// Jamakol planet position with both Tamil and English labels
/// </summary>
public class JamakolPlanetPosition
{
    public Planet Planet { get; set; }
    public string TamilName { get; set; } = "";
    public int Sign { get; set; }
    public string SignTamilName { get; set; } = "";
    public double Degree { get; set; }
    public double DegreeInSign { get; set; }
    public int Nakshatra { get; set; }
    public string NakshatraTamilName { get; set; } = "";
    public int NakshatraPada { get; set; }
    public bool IsRetrograde { get; set; }
    public int JamakolValue { get; set; }
    public double Speed { get; set; }
    public string Gati { get; set; } = string.Empty;

    // English name properties for display
    public string EnglishName { get; set; } = "";
    public string Symbol { get; set; } = ""; // Short abbreviation (Su, Mo, Dh, Vy, etc.)
    public string SignEnglish => JamakolAstrology.Services.ZodiacUtils.SignNames[Sign];
    public string NakshatraEnglish => JamakolAstrology.Services.ZodiacUtils.NakshatraNames[Nakshatra];

    // Formatted properties for display
    public string DegreeDisplay => $"{(int)DegreeInSign}°{(int)((DegreeInSign % 1) * 60)}'{(int)(((DegreeInSign % 1) * 60 % 1) * 60)}\"";
    public string PadaDisplay => NakshatraPada.ToString();
    public string RetroDisplay => IsRetrograde ? "(R)" : "";
    
    public bool IsCombust { get; set; }
    public string CombustionFlag => IsCombust ? "C" : "";
}
