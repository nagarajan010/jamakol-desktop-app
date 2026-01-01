namespace JamakolAstrology.Models;

/// <summary>
/// Ruling Planets (RP) for a specific moment
/// Used in KP Horary astrology to identify active/favorable planets
/// </summary>
public class RulingPlanets
{
    public DateTime JudgmentTime { get; set; }
    
    // Lagna (Ascendant) rulers
    public string LagnaSignLord { get; set; } = "";
    public string LagnaStarLord { get; set; } = "";
    public string LagnaSubLord { get; set; } = "";
    
    // Moon rulers
    public string MoonSignLord { get; set; } = "";
    public string MoonStarLord { get; set; } = "";
    public string MoonSubLord { get; set; } = "";
    
    // Day Lord (Vara)
    public string DayLord { get; set; } = "";
    
    /// <summary>
    /// Combined ruling planets ordered by frequency (most repeated = strongest)
    /// </summary>
    public List<RulingPlanetEntry> CombinedRulers { get; set; } = new();
}

/// <summary>
/// Entry for a ruling planet with its count (how many times it appears)
/// </summary>
public class RulingPlanetEntry
{
    public string Planet { get; set; } = "";
    public int Count { get; set; }
    public string Sources { get; set; } = ""; // e.g., "Lagna Sign, Moon Star, Day"
}
