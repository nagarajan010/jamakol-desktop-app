namespace JamakolAstrology.Models;

/// <summary>
/// Tag for labeling saved charts
/// </summary>
public class ChartTag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>Tag name</summary>
    public string Name { get; set; } = "";
    
    /// <summary>Hex color code (e.g., "#FF5733")</summary>
    public string Color { get; set; } = "#28A745";
}
