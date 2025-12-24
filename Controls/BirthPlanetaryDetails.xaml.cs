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
        var displayData = new System.Collections.Generic.List<PlanetDisplayItem>();
        
        // Add Lagna first
        displayData.Add(new PlanetDisplayItem
        {
            Name = "Lagna",
            SignName = chartData.AscendantSignName.Length > 2 ? chartData.AscendantSignName.Substring(0, 2) : chartData.AscendantSignName,
            DegreeDisplay = $"{(int)chartData.AscendantDegree}°{(int)((chartData.AscendantDegree % 1) * 60)}'",
            NakshatraShort = chartData.AscendantNakshatraName ?? "",
            PadaDisplay = chartData.AscendantNakshatraPada.ToString(),
            RetroDisplay = ""
        });
        
        // Add planets
        displayData.AddRange(chartData.Planets.Select(p => new PlanetDisplayItem
        {
            Name = p.Name,
            SignName = p.SignName.Length > 2 ? p.SignName.Substring(0, 2) : p.SignName,
            DegreeDisplay = $"{(int)p.DegreeInSign}°{(int)((p.DegreeInSign % 1) * 60)}'",
            NakshatraShort = p.NakshatraName,
            PadaDisplay = p.NakshatraPada.ToString(),
            RetroDisplay = p.IsRetrograde ? "R" : ""
        }));

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
