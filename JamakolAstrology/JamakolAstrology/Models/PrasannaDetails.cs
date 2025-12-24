namespace JamakolAstrology.Models;

/// <summary>
/// Represents the Prasanna (Horary) analysis details
/// </summary>
public class PrasannaDetails
{
    // Planet towards Udhayam
    public string? PlanetTowardsUdhayam { get; set; }
    public double PlanetTowardsUdhayamPercent { get; set; }
    
    // Udhayam Lord & Bhava
    public string? UdhayamLord { get; set; }
    public int UdhayamBhava { get; set; }
    
    // Arudam details
    public int ArudamBhava { get; set; }
    public string? PlanetTowardsArudam { get; set; }
    public double PlanetTowardsArudamPercent { get; set; }
    
    // Kavippu details
    public string? PlanetTowardsKavippu { get; set; }
    public double PlanetTowardsKavippuPercent { get; set; }
    public int BhavaInKavippu { get; set; }
    
    // Exalted/Debilitated/Parivarthana
    public string? ExaltedPlanets { get; set; }
    public string? DebilitatedPlanets { get; set; }
    public string? ParivarthanaPlanets { get; set; }
    
    // Emakandam (Saturn related)
    public string? PlanetTowardsEmakandam { get; set; }
    public double PlanetTowardsEmakandamPercent { get; set; }
    
    // Rahu Time and Mrithyu (to be implemented later)
    public string? PlanetInRahuTime { get; set; }
    public double PlanetInRahuTimePercent { get; set; }
    public string? PlanetTowardsMrithyu { get; set; }
    public double PlanetTowardsMrithyuPercent { get; set; }
}
