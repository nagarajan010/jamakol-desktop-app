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
    }

    /// <summary>
    /// Clear all data
    /// </summary>
    public void Clear()
    {
        PlanetGridControl.DataGridControl.ItemsSource = null;
        NatalDetailsText.Text = "No content available";
        AshtakavargaText.Text = "No content available";
    }
}
