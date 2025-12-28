using System.Windows.Controls;
using JamakolAstrology.Models;
using System.Linq;
using JamakolAstrology.Services;

namespace JamakolAstrology.Controls;

/// <summary>
/// Prasanna Panel UserControl - displays Prasanna Details Special
/// </summary>
public partial class PrasannaPanel : UserControl
{
    public PrasannaPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Update the panel with Prasanna details
    /// </summary>
    /// <summary>
    /// Update the panel with Prasanna details
    /// </summary>
    public void UpdateDetails(PrasannaDetails details)
    {
        // Planet towards Udhayam
        PlanetTowardsUdhayamText.Text = !string.IsNullOrEmpty(details.PlanetTowardsUdhayam) 
            ? $"{GetLocalizedPlanet(details.PlanetTowardsUdhayam).ToUpper()} {details.PlanetTowardsUdhayamPercent:F2}°" 
            : "-";
        
        // Udhayam Lord & Bhava
        UdhayamLordBhavaText.Text = !string.IsNullOrEmpty(details.UdhayamLord) 
            ? $"{GetLocalizedPlanet(details.UdhayamLord).ToUpper()} - {details.UdhayamBhava}" 
            : "-";
        
        // Arudam Bhava
        ArudamBhavaText.Text = details.ArudamBhava > 0 ? details.ArudamBhava.ToString() : "-";
        
        // Planet towards Arudam
        PlanetTowardsArudamText.Text = !string.IsNullOrEmpty(details.PlanetTowardsArudam) 
            ? $"{GetLocalizedPlanet(details.PlanetTowardsArudam).ToUpper()} {details.PlanetTowardsArudamPercent:F2}°" 
            : "-";
        
        // Planet towards Kavippu
        PlanetTowardsKavippuText.Text = !string.IsNullOrEmpty(details.PlanetTowardsKavippu) 
            ? $"{GetLocalizedPlanet(details.PlanetTowardsKavippu).ToUpper()} {details.PlanetTowardsKavippuPercent:F2}°" 
            : "-";
        
        // Bhava in Kavippu
        BhavaInKavippuText.Text = details.BhavaInKavippu > 0 ? details.BhavaInKavippu.ToString() : "-";
        
        // Exalted, Debilitated, Parivarthana
        // Assuming these are comma separated strings? We should split and localize if possible.
        // For now, let's just localize if simple match.
        ExaltedPlanetsText.Text = LocalizeCsv(details.ExaltedPlanets);
        DebilitatedPlanetsText.Text = LocalizeCsv(details.DebilitatedPlanets);
        ParivarthanaPlanetsText.Text = LocalizeCsv(details.ParivarthanaPlanets);
        
        // Emakandam
        EmakandamText.Text = !string.IsNullOrEmpty(details.PlanetTowardsEmakandam) 
            ? $"{GetLocalizedPlanet(details.PlanetTowardsEmakandam).ToUpper()} {details.PlanetTowardsEmakandamPercent:F2}°" 
            : "-";
        
        // Rahu Time
        RahuTimeText.Text = !string.IsNullOrEmpty(details.PlanetInRahuTime) && details.PlanetInRahuTime != "-"
            ? $"{GetLocalizedPlanet(details.PlanetInRahuTime).ToUpper()} {details.PlanetInRahuTimePercent:F2}°" 
            : "-";
        
        // Mrithyu
        MrithyuText.Text = !string.IsNullOrEmpty(details.PlanetTowardsMrithyu) && details.PlanetTowardsMrithyu != "-"
            ? $"{GetLocalizedPlanet(details.PlanetTowardsMrithyu).ToUpper()} {details.PlanetTowardsMrithyuPercent:F2}°" 
            : "-";
    }

    private string LocalizeCsv(string? input)
    {
        if (string.IsNullOrEmpty(input) || input == "-") return "-";
        if (!ZodiacUtils.IsTamil) return input;
        
        var parts = input.Split(new[] { ',', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        var localized = parts.Select(p => GetLocalizedPlanet(p)).ToList();
        return string.Join(", ", localized);
    }

    private string GetLocalizedPlanet(string englishName)
    {
        if (string.IsNullOrEmpty(englishName)) return "";
        if (!ZodiacUtils.IsTamil) return englishName;
        
        // Special case for Snake
        if (englishName.Equals("Snake", System.StringComparison.OrdinalIgnoreCase) || 
            englishName.Equals("SNAKE", System.StringComparison.OrdinalIgnoreCase))
        {
            return "பாம்பு"; // Pambu
        }

        // Try standard planets
        if (System.Enum.TryParse<Planet>(englishName, true, out var p))
        {
            return ZodiacUtils.GetPlanetName(p);
        }
        
        return englishName; // Fallback
    }
}
