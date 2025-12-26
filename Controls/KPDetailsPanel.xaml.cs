using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology.Controls;

public partial class KPDetailsPanel : UserControl
{
    public KPDetailsPanel()
    {
        InitializeComponent();
    }

    public void UpdateChart(ChartData? chart)
    {
        if (chart == null)
        {
            KPGrid.ItemsSource = null;
            return;
        }
        
        var items = new List<KPViewItem>();
        // ... (lines 26-38 assumed unchanged logic)
        
        // Add Planets
        foreach (var p in chart.Planets)
        {
            items.Add(new KPViewItem
            {
                Name = $"{p.Name} ({p.Symbol})",
                DegreeDisplay = ZodiacUtils.FormatDegreeInSign(p.Longitude),
                SignLord = p.KpDetails.SignLord,
                StarLord = p.KpDetails.StarLord,
                SubLord = p.KpDetails.SubLord,
                SubSubLord = p.KpDetails.SubSubLord,
                SookshmaLord = p.KpDetails.SookshmaLord,
                PranaLord = p.KpDetails.PranaLord,
                DehaLord = p.KpDetails.DehaLord
            });
        }
        
        // Add House Cusps
        if (chart.HouseCusps != null)
        {
            foreach (var cusp in chart.HouseCusps)
            {
                items.Add(new KPViewItem
                {
                    Name = FormatHouseName(cusp.HouseNumber),
                    DegreeDisplay = cusp.DegreeDisplay,
                    SignLord = cusp.KpDetails.SignLord,
                    StarLord = cusp.KpDetails.StarLord,
                    SubLord = cusp.KpDetails.SubLord,
                    SubSubLord = cusp.KpDetails.SubSubLord,
                    SookshmaLord = cusp.KpDetails.SookshmaLord,
                    PranaLord = cusp.KpDetails.PranaLord,
                    DehaLord = cusp.KpDetails.DehaLord
                });
            }
        }
        
        KPGrid.ItemsSource = items;
    }
    
    public void ClearChart()
    {
        KPGrid.ItemsSource = null;
    }
    
    // Helper to format ordinal
    private string FormatHouseName(int number)
    {
        string suffix = (number % 100 >= 11 && number % 100 <= 13) ? "th"
            : (number % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return $"{number}{suffix} House";
    }
}

public class KPViewItem
{
    public string Name { get; set; } = "";
    public string DegreeDisplay { get; set; } = "";
    public string SignLord { get; set; } = "";
    public string StarLord { get; set; } = "";
    public string SubLord { get; set; } = "";
    public string SubSubLord { get; set; } = "";
    public string SookshmaLord { get; set; } = "";
    public string PranaLord { get; set; } = "";
    public string DehaLord { get; set; } = "";
}
