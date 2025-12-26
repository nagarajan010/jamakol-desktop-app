namespace JamakolAstrology.Models;

/// <summary>
/// Represents a House Cusp in KP System (Placidus)
/// </summary>
public class HouseCusp
{
    public int HouseNumber { get; set; } // 1-12
    public double Degree { get; set; }   // 0-360
    public string SignName { get; set; } = string.Empty;
    public string DegreeDisplay { get; set; } = string.Empty; // Formatted DD MM SS
    
    public KPLords KpDetails { get; set; } = new();
}
