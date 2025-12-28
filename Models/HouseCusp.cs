namespace JamakolAstrology.Models;

/// <summary>
/// Represents a House Cusp in KP System (Placidus)
/// </summary>
public class HouseCusp
{
    public int HouseNumber { get; set; } // 1-12
    public double Degree { get; set; }   // Cusp degree (0-360)
    public double StartDegree { get; set; } // Start of house
    public double EndDegree { get; set; }   // End of house
    public string SignName { get; set; } = string.Empty;
    public string DegreeDisplay { get; set; } = string.Empty; // Formatted cusp DD MM SS
    public string StartDisplay { get; set; } = string.Empty;  // Formatted start
    public string EndDisplay { get; set; } = string.Empty;    // Formatted end
    
    public KPLords KpDetails { get; set; } = new();
}

