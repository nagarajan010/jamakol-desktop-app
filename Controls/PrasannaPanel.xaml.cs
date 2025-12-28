using System.Windows.Controls;
using JamakolAstrology.Models;

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
    public void UpdateDetails(PrasannaDetails details)
    {
        // Planet towards Udhayam
        PlanetTowardsUdhayamText.Text = !string.IsNullOrEmpty(details.PlanetTowardsUdhayam) 
            ? $"{details.PlanetTowardsUdhayam.ToUpper()} {details.PlanetTowardsUdhayamPercent:F2}°" 
            : "-";
        
        // Udhayam Lord & Bhava
        UdhayamLordBhavaText.Text = !string.IsNullOrEmpty(details.UdhayamLord) 
            ? $"{details.UdhayamLord.ToUpper()} - {details.UdhayamBhava}" 
            : "-";
        
        // Arudam Bhava
        ArudamBhavaText.Text = details.ArudamBhava > 0 ? details.ArudamBhava.ToString() : "-";
        
        // Planet towards Arudam
        PlanetTowardsArudamText.Text = !string.IsNullOrEmpty(details.PlanetTowardsArudam) 
            ? $"{details.PlanetTowardsArudam.ToUpper()} {details.PlanetTowardsArudamPercent:F2}°" 
            : "-";
        
        // Planet towards Kavippu
        PlanetTowardsKavippuText.Text = !string.IsNullOrEmpty(details.PlanetTowardsKavippu) 
            ? $"{details.PlanetTowardsKavippu.ToUpper()} {details.PlanetTowardsKavippuPercent:F2}°" 
            : "-";
        
        // Bhava in Kavippu
        BhavaInKavippuText.Text = details.BhavaInKavippu > 0 ? details.BhavaInKavippu.ToString() : "-";
        
        // Exalted, Debilitated, Parivarthana
        ExaltedPlanetsText.Text = details.ExaltedPlanets ?? "-";
        DebilitatedPlanetsText.Text = details.DebilitatedPlanets ?? "-";
        ParivarthanaPlanetsText.Text = details.ParivarthanaPlanets ?? "-";
        
        // Emakandam
        EmakandamText.Text = !string.IsNullOrEmpty(details.PlanetTowardsEmakandam) 
            ? $"{details.PlanetTowardsEmakandam.ToUpper()} {details.PlanetTowardsEmakandamPercent:F2}°" 
            : "-";
        
        // Rahu Time
        RahuTimeText.Text = !string.IsNullOrEmpty(details.PlanetInRahuTime) && details.PlanetInRahuTime != "-"
            ? $"{details.PlanetInRahuTime.ToUpper()} {details.PlanetInRahuTimePercent:F2}°" 
            : "-";
        
        // Mrithyu
        MrithyuText.Text = !string.IsNullOrEmpty(details.PlanetTowardsMrithyu) && details.PlanetTowardsMrithyu != "-"
            ? $"{details.PlanetTowardsMrithyu.ToUpper()} {details.PlanetTowardsMrithyuPercent:F2}°" 
            : "-";
    }
}
