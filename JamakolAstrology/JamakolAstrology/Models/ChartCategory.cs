namespace JamakolAstrology.Models;

/// <summary>
/// Category for organizing saved charts
/// </summary>
public class ChartCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>Category name</summary>
    public string Name { get; set; } = "";
    
    /// <summary>Hex color code (e.g., "#FF5733")</summary>
    public string Color { get; set; } = "#4A90D9";
}
