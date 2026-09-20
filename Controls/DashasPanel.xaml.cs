using System.Windows;
using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology.Controls;

public partial class DashasPanel : UserControl
{
    private readonly SudarshanaDashaCalculator _sudarshanaExpander = new();

    public DashasPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Fill in a Sudarshana level the moment the reader opens it.
    ///
    /// The tree is built a few levels deep because twelve to the power of six is over three
    /// million periods; the rest is arithmetic on the parent's own span, so it costs nothing
    /// until it is asked for.
    /// </summary>
    private void SudarshanaItemExpanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not TreeViewItem item) return;
        if (item.DataContext is not SudarshanaDashaPeriod period) return;

        if (period.SubPeriods.Count == 0 && period.CanExpand)
        {
            _sudarshanaExpander.Expand(period);
        }

        // Give the newly shown level its own children, so each one offers an expander in turn.
        foreach (var child in period.SubPeriods)
        {
            if (child.SubPeriods.Count == 0 && child.CanExpand)
            {
                _sudarshanaExpander.Expand(child);
            }
        }
    }

    /// <summary>
    /// Update Dasha details
    /// </summary>
    public void UpdateDashas(DashaResult? result, SudarshanaDashaResult? sudarshanaResult = null)
    {
        if (result == null)
        {
            CurrentDashaText.Text = "-";
            CurrentDashaDates.Text = "-";
            CurrentDashaLevels.Text = "-";
            DashaTreeView.ItemsSource = null;
        }
        else
        {
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
            else
            {
                CurrentDashaText.Text = "-";
                CurrentDashaDates.Text = "-";
                CurrentDashaLevels.Text = "-";
            }
        }

        UpdateSudarshanaDashas(sudarshanaResult);
    }

    private void UpdateSudarshanaDashas(SudarshanaDashaResult? result)
    {
        if (result == null)
        {
            CurrentSudarshanaText.Text = "-";
            CurrentSudarshanaDates.Text = "-";
            SudarshanaTreeView.ItemsSource = null;
            return;
        }

        SudarshanaTreeView.ItemsSource = result.MainDashas;

        if (result.CurrentPeriod == null)
        {
            CurrentSudarshanaText.Text = "No current period within 144 years";
            CurrentSudarshanaDates.Text = "";
            return;
        }

        CurrentSudarshanaText.Text = result.CurrentDisplay;
        // The deepest link is the one actually running now, so its span is what to date.
        CurrentSudarshanaDates.Text =
            $"L{result.CurrentPeriod.Level}: {result.CurrentPeriod.DateRange}";
    }
}
