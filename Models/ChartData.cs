namespace JamakolAstrology.Models;

/// <summary>
/// Complete chart data with all calculated positions
/// </summary>
public class ChartData
{
    public BirthData BirthData { get; set; } = new();
    public double JulianDay { get; set; }
    public double AscendantDegree { get; set; }
    public int AscendantSign { get; set; }      // 1-12
    public string AscendantSignName { get; set; } = string.Empty;
    public List<PlanetPosition> Planets { get; set; } = new();
    
    /// <summary>
    /// Ayanamsa value in degrees used for this chart
    /// </summary>
    public double AyanamsaValue { get; set; }
    
    /// <summary>
    /// Gets planets in a specific sign (1-12)
    /// </summary>
    public IEnumerable<PlanetPosition> GetPlanetsInSign(int sign)
    {
        return Planets.Where(p => p.Sign == sign);
    }

    /// <summary>
    /// Gets planets in a specific house (1-12)
    /// </summary>
    public IEnumerable<PlanetPosition> GetPlanetsInHouse(int house)
    {
        return Planets.Where(p => p.House == house);
    }
}
