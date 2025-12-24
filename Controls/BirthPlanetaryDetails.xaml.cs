using System.Linq;
using System.Windows.Controls;
using JamakolAstrology.Models;

namespace JamakolAstrology.Controls;

/// <summary>
/// Interaction logic for BirthPlanetaryDetails.xaml
/// </summary>
public partial class BirthPlanetaryDetails : UserControl
{
    public BirthPlanetaryDetails()
    {
        InitializeComponent();
    }

    public void UpdateDetails(ChartData chartData)
    {
        var displayData = chartData.Planets.Select(p => new PlanetDisplayItem
        {
            Name = p.Name,
            SignName = p.SignName.Length > 2 ? p.SignName.Substring(0, 2) : p.SignName,
            DegreeDisplay = $"{(int)p.DegreeInSign}°{(int)((p.DegreeInSign % 1) * 60)}'",
            NakshatraShort = p.NakshatraName,
            PadaDisplay = p.NakshatraPada.ToString(),
            RetroDisplay = p.IsRetrograde ? "R" : ""
        }).ToList();

        PlanetGrid.ItemsSource = displayData;
    }
}

public class PlanetDisplayItem
{
    public string Name { get; set; } = "";
    public string SignName { get; set; } = "";
    public string DegreeDisplay { get; set; } = "";
    public string NakshatraShort { get; set; } = "";
    public string PadaDisplay { get; set; } = "";
    public string RetroDisplay { get; set; } = "";
}
