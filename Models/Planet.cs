namespace JamakolAstrology.Models;

/// <summary>
/// Vedic planets enumeration
/// </summary>
public enum Planet
{
    Sun = 0,
    Moon = 1,
    Mars = 4,
    Mercury = 2,
    Jupiter = 5,
    Venus = 3,
    Saturn = 6,
    Rahu = 11,    // Mean North Node
    Ketu = -1     // Calculated from Rahu (opposite)
}

/// <summary>
/// Represents a planet's position in the chart
/// </summary>
public class PlanetPosition
{
    public Planet Planet { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public double Speed { get; set; }
    public bool IsRetrograde => Speed < 0;
    public int Sign { get; set; }           // 1-12 (Aries to Pisces)
    public string SignName { get; set; } = string.Empty;
    public double DegreeInSign { get; set; }
    public int House { get; set; }          // 1-12
    public int Nakshatra { get; set; }      // 1-27
    public string NakshatraName { get; set; } = string.Empty;
    public int NakshatraPada { get; set; }  // 1-4
    
    public KPLords KpDetails { get; set; } = new();
}
