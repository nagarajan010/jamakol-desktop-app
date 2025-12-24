namespace JamakolAstrology.Models;

/// <summary>
/// Represents birth data for chart calculation
/// </summary>
public class BirthData
{
    public string Name { get; set; } = string.Empty;
    public DateTime BirthDateTime { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Location { get; set; } = string.Empty;
    public double TimeZoneOffset { get; set; } // In hours (e.g., +5.5 for IST)

    public DateTime UtcDateTime => BirthDateTime.AddHours(-TimeZoneOffset);
}
