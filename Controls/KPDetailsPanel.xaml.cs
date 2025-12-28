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
        
        // Apply font size from settings
        var settings = AppSettings.Load();
        if (settings != null)
        {
            KPGrid.FontSize = settings.TableFontSize;
        }
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
        
        // Add Lagna (Ascendant)
        if (chart.HouseCusps != null && chart.HouseCusps.Count > 0)
        {
            var lagna = chart.HouseCusps[0]; // 1st House Cusp is Lagna
            items.Add(new KPViewItem
            {
                Name = ZodiacUtils.IsTamil ? "லக்னம் (As)" : "Lagna (As)",
                DegreeDisplay = lagna.DegreeDisplay,
                SignLord = lagna.KpDetails.SignLord,
                StarLord = lagna.KpDetails.StarLord,
                SubLord = lagna.KpDetails.SubLord,
                SubSubLord = lagna.KpDetails.SubSubLord,
                SookshmaLord = lagna.KpDetails.SookshmaLord,
                PranaLord = lagna.KpDetails.PranaLord,
                DehaLord = lagna.KpDetails.DehaLord
            });
        }
        
        // Add Planets
        foreach (var p in chart.Planets)
        {
            items.Add(new KPViewItem
            {
                Name = $"{(ZodiacUtils.IsTamil ? ZodiacUtils.GetPlanetName(p.Planet) : p.Name)} ({p.Symbol})",
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
        LocalizeHeaders();
    }

    private void LocalizeHeaders()
    {
        if (KPGrid.Columns.Count < 7) return;
        
        bool isTa = ZodiacUtils.IsTamil;
        KPGrid.Columns[0].Header = isTa ? "கிரகம்/முனை" : "Body/Cusp";
        KPGrid.Columns[1].Header = isTa ? "நட்சத்திர அதிபதி" : "Nakshatra Lord";
        KPGrid.Columns[2].Header = isTa ? "உப அதிபதி" : "Sub Lord";
        KPGrid.Columns[3].Header = isTa ? "உப-உப அதிபதி" : "Prati-Sub"; // Or Prati-Sub transliterated? "பிரதி-உப"
        KPGrid.Columns[4].Header = isTa ? "சூட்சுமம்" : "Sookshma";
        KPGrid.Columns[5].Header = isTa ? "பிராணன்" : "Prana";
        KPGrid.Columns[6].Header = isTa ? "தேகம்" : "Deha";
    }
    
    public void ClearChart()
    {
        KPGrid.ItemsSource = null;
    }
    
    // Helper to format ordinal
    private string FormatHouseName(int number)
    {
        if (ZodiacUtils.IsTamil)
            return $"{number}ம் பாவம்";
            
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
