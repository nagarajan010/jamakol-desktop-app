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
    
    // Date/Time components for BC date support (Year can be negative)
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
    public int Hour { get; set; }
    public int Minute { get; set; }
    public int Second { get; set; }
    
    /// <summary>Returns true if this is a BC date (Year <= 0)</summary>
    public bool IsBCDate => Year <= 0;
    
    /// <summary>Query date and time - for backward compatibility and AD dates only</summary>
    public DateTime QueryDateTime 
    { 
        get
        {
            if (Year > 0 && Year <= 9999)
            {
                try { return new DateTime(Year, Month, Day, Hour, Minute, Second); }
                catch { return DateTime.MinValue; }
            }
            return DateTime.MinValue;
        }
        set
        {
            // Only update Year/Month/Day from the DateTime if it's a valid date
            // (not DateTime.MinValue, which is used as fallback for BC dates)
            // This prevents JSON deserialization from overwriting BC date values
            if (value != DateTime.MinValue)
            {
                Year = value.Year;
                Month = value.Month;
                Day = value.Day;
                Hour = value.Hour;
                Minute = value.Minute;
                Second = value.Second;
            }
        }
    }
    
    /// <summary>Location latitude</summary>
    public double Latitude { get; set; }
    
    /// <summary>Location longitude</summary>
    public double Longitude { get; set; }
    
    /// <summary>Timezone offset in hours</summary>
    public double Timezone { get; set; }
    
    /// <summary>Location name</summary>
    public string Location { get; set; } = "";
    
    /// <summary>Chart type: "Jamakol" or "BirthChart"</summary>
    public string ChartType { get; set; } = "Jamakol";
    
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
