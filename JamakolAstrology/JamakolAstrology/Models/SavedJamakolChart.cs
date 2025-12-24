namespace JamakolAstrology.Models;

/// <summary>
/// Result status for a saved Jamakol chart prediction
/// </summary>
public enum ChartResult
{
    Pending = 0,
    Success = 1,
    Failure = 2
}

/// <summary>
/// Represents a saved Jamakol chart with prediction details
/// </summary>
public class SavedJamakolChart
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>Name/title for the saved chart</summary>
    public string Name { get; set; } = "";
    
    /// <summary>Query date and time</summary>
    public DateTime QueryDateTime { get; set; }
    
    /// <summary>Location latitude</summary>
    public double Latitude { get; set; }
    
    /// <summary>Location longitude</summary>
    public double Longitude { get; set; }
    
    /// <summary>Timezone offset in hours</summary>
    public double Timezone { get; set; }
    
    /// <summary>Result status: Pending, Success, or Failure</summary>
    public ChartResult Result { get; set; } = ChartResult.Pending;
    
    /// <summary>Prediction text entered by user</summary>
    public string Prediction { get; set; } = "";
    
    /// <summary>Optional category ID</summary>
    public Guid? CategoryId { get; set; }
    
    /// <summary>List of tag IDs</summary>
    public List<Guid> TagIds { get; set; } = new();
    
    /// <summary>Serialized chart calculation data</summary>
    public string ChartDataJson { get; set; } = "";
    
    /// <summary>Serialized Jama Graha positions</summary>
    public string JamaGrahaDataJson { get; set; } = "";
    
    /// <summary>Serialized special points data</summary>
    public string SpecialPointsDataJson { get; set; } = "";
    
    /// <summary>Created timestamp</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>Last updated timestamp</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
