using System;
using System.Collections.Generic;
using JamakolAstrology.Models;

namespace JamakolAstrology.Models;

/// <summary>
/// Aggregated result containing all calculated chart data
/// </summary>
public class CompositeChartResult
{
    public ChartData ChartData { get; set; } = new();
    public JamakolData JamakolData { get; set; } = new();
    public List<JamaGrahaPosition> JamaGrahas { get; set; } = new();
    public List<SpecialPoint> SpecialPoints { get; set; } = new();
    public PrasannaDetails PrasannaDetails { get; set; } = new();
    public PanchangaDetails PanchangaDetails { get; set; } = new();
    
    // Metadata for display
    public string DayLord { get; set; } = "";
    public DateTime VedicDate { get; set; }
    public DateTime Sunrise { get; set; }
    public DateTime Sunset { get; set; }
}
