using System.Windows.Controls;
using JamakolAstrology.Models;

namespace JamakolAstrology.Controls;

public partial class DashasPanel : UserControl
{
    public DashasPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Update Dasha details
    /// </summary>
    public void UpdateDashas(DashaResult? result)
    {
        if (result == null)
        {
            CurrentDashaText.Text = "-";
            CurrentDashaDates.Text = "-";
            CurrentDashaLevels.Text = "-";
            DashaTreeView.ItemsSource = null;
            return;
        }

        // Set TreeView source
        DashaTreeView.ItemsSource = result.MahaDashas;

        // Set Current Dasha Texts
        if (result.CurrentAntarDasha != null)
        {
            // Format: Jupiter / Saturn / Mercury
            // Localize planets for display
            string GetLocPlanet(string p) => Services.ZodiacUtils.IsTamil && Enum.TryParse<Planet>(p, true, out var pl) 
                ? Services.ZodiacUtils.GetPlanetName(pl) : p;

            CurrentDashaText.Text = $"{GetLocPlanet(result.CurrentMahaDasha?.Planet ?? "-")} / {GetLocPlanet(result.CurrentAntarDasha?.Planet ?? "-")} / {GetLocPlanet(result.CurrentPratyantaraDasha?.Planet ?? "-")}";
            
            // Format: 15-Oct-2023 to 22-Feb-2024 (showing range of deepest active level)
            var deepest = result.CurrentDehaDasha ?? 
                          result.CurrentPranaDasha ?? 
                          result.CurrentSookshmaDasha ?? 
                          result.CurrentPratyantaraDasha ?? 
                          result.CurrentAntarDasha;
            
            if (deepest != null)
            {
                string endsOn = Services.ZodiacUtils.IsTamil ? "முடிவு" : "ends on";
                string dateStr = Helpers.TimeFormatHelper.FormatJulianDay(deepest.EndJulianDay, true);
                CurrentDashaDates.Text = $"{deepest.DisplayName} {endsOn} {dateStr}";
            }
            else
            {
                CurrentDashaDates.Text = "";
            }

            // Full chain
            string p1 = GetLocPlanet(result.CurrentMahaDasha?.Planet ?? "-");
            string p2 = GetLocPlanet(result.CurrentAntarDasha?.Planet ?? "-");
            string p3 = GetLocPlanet(result.CurrentPratyantaraDasha?.Planet ?? "-");
            
            string levels = $"{p1} > {p2} > {p3}";
            if (result.CurrentSookshmaDasha != null) levels += $" > {GetLocPlanet(result.CurrentSookshmaDasha.Planet)}";
            if (result.CurrentPranaDasha != null) levels += $" > {GetLocPlanet(result.CurrentPranaDasha.Planet)}";
            if (result.CurrentDehaDasha != null) levels += $" > {GetLocPlanet(result.CurrentDehaDasha.Planet)}";
            
            CurrentDashaLevels.Text = levels;
        }
    }
}
