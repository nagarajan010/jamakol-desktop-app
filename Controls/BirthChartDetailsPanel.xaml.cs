using System.Windows;
using System.Windows.Controls;
using JamakolAstrology.Models;

namespace JamakolAstrology.Controls;

public partial class BirthChartDetailsPanel : UserControl
{
    public BirthChartDetailsPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Update the planetary positions grid using ChartData
    /// </summary>
    public void UpdatePlanetaryPositions(ChartData chartData)
    {
        PlanetGridControl.UpdateGrid(chartData);
        KpDetailsControl.UpdateChart(chartData);
        AVDetailsControl.UpdateChart(chartData);
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
            CurrentDashaText.Text = result.CurrentDashaDisplay;
            
            // Format: 15-Oct-2023 to 22-Feb-2024 (showing range of deepest active level)
            var deepest = result.CurrentDehaDasha ?? 
                          result.CurrentPranaDasha ?? 
                          result.CurrentSookshmaDasha ?? 
                          result.CurrentPratyantaraDasha ?? 
                          result.CurrentAntarDasha;
                          
            CurrentDashaDates.Text = deepest != null 
                ? $"{deepest.DisplayName} ends on {deepest.EndDate:dd-MMM-yyyy HH:mm}" 
                : "";

            // Full chain
            string levels = $"{result.CurrentMahaDasha?.Planet} > {result.CurrentAntarDasha?.Planet} > {result.CurrentPratyantaraDasha?.Planet}";
            if (result.CurrentSookshmaDasha != null) levels += $" > {result.CurrentSookshmaDasha.Planet}";
            if (result.CurrentPranaDasha != null) levels += $" > {result.CurrentPranaDasha.Planet}";
            if (result.CurrentDehaDasha != null) levels += $" > {result.CurrentDehaDasha.Planet}";
            
            CurrentDashaLevels.Text = levels;
        }
    }

    /// <summary>
    /// Clear all data
    /// </summary>
    public void Clear()
    {
        PlanetGridControl.DataGridControl.ItemsSource = null;
        KpDetailsControl.UpdateChart(null);
        AVDetailsControl.UpdateChart(null);
        NatalDetailsText.Text = "No content available";
        AshtakavargaText.Text = "No content available";
        UpdateDashas(null);
    }
}
