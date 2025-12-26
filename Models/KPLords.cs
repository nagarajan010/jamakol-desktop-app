namespace JamakolAstrology.Models;

/// <summary>
/// Holds the hierarchy of lords for a KP position
/// </summary>
public class KPLords
{
    public string SignLord { get; set; } = string.Empty;
    public string StarLord { get; set; } = string.Empty;      // Nakshatra Lord
    public string SubLord { get; set; } = string.Empty;
    public string SubSubLord { get; set; } = string.Empty;    // Pratyantar
    public string SookshmaLord { get; set; } = string.Empty;
    public string PranaLord { get; set; } = string.Empty;
    public string DehaLord { get; set; } = string.Empty;
}
