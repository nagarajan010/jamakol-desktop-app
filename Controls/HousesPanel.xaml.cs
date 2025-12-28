using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology.Controls;

public partial class HousesPanel : UserControl
{
    public HousesPanel()
    {
        InitializeComponent();
        
        // Apply font size from settings if valid
        var settings = AppSettings.Load();
        if (settings != null)
        {
            HousesGrid.FontSize = settings.TableFontSize;
        }
    }

    public void UpdateChart(ChartData? chart)
    {
        if (chart == null || chart.HouseCusps == null)
        {
            HousesGrid.ItemsSource = null;
            return;
        }

        var items = new List<HouseViewItem>();
        
        // Build lookup of planets by house
        var planetsByHouse = chart.Planets
            .Where(p => p.House >= 1 && p.House <= 12)
            .GroupBy(p => p.House)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        foreach (var cusp in chart.HouseCusps)
        {
            // Get planets in this house
            var planetsInHouse = "";
            if (planetsByHouse.TryGetValue(cusp.HouseNumber, out var planets))
            {
                // Use abbreviations for compactness (Su, Mo, Ma, etc.)
                var abbrevs = planets.Select(p => GetPlanetAbbrev(p)).ToList();
                
                // Add "As" for house 1
                if (cusp.HouseNumber == 1)
                {
                    abbrevs.Insert(0, "As");
                }
                
                planetsInHouse = string.Join(", ", abbrevs);
            }
            else if (cusp.HouseNumber == 1)
            {
                planetsInHouse = "As";
            }
            
            items.Add(new HouseViewItem
            {
                HouseName = FormatHouseName(cusp.HouseNumber),
                StartDisplay = cusp.StartDisplay,
                CuspDisplay = cusp.DegreeDisplay,
                EndDisplay = cusp.EndDisplay,
                PlanetsInHouse = planetsInHouse
            });
        }

        HousesGrid.ItemsSource = items;
    }
    
    private string GetPlanetAbbrev(PlanetPosition p)
    {
        // Main planets - use localized abbreviations
        if (ZodiacUtils.PlanetAbbreviations.TryGetValue(p.Planet, out var abbrev))
        {
            // If Tamil, use same abbreviations as chart display
            if (ZodiacUtils.IsTamil)
            {
                return p.Planet switch
                {
                    Models.Planet.Sun => "சூரி",
                    Models.Planet.Moon => "சந்",
                    Models.Planet.Mars => "செவ்",
                    Models.Planet.Mercury => "புத",
                    Models.Planet.Jupiter => "குரு",
                    Models.Planet.Venus => "சுக்",
                    Models.Planet.Saturn => "சனி",
                    Models.Planet.Rahu => "ராகு",
                    Models.Planet.Ketu => "கேது",
                    _ => abbrev
                };
            }
            return abbrev;
        }
        
        // For Aprakash Graha (shadow planets) - use their 2-letter symbol
        if (!string.IsNullOrEmpty(p.Symbol) && p.Symbol.Length <= 2)
        {
            return p.Symbol; // Dh, Vy, Pa, In, Uk
        }
        
        // Fallback: use first 2 letters of name
        if (!string.IsNullOrEmpty(p.Name) && p.Name.Length >= 2)
        {
            return p.Name.Substring(0, 2);
        }
        
        return p.Symbol ?? "";
    }

    private string FormatHouseName(int number)
    {
        if (ZodiacUtils.IsTamil)
            return $"{number}ம் பாவம்";
            
        string suffix = (number % 100 >= 11 && number % 100 <= 13) ? "th"
            : (number % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return $"{number}{suffix}";
    }
}

public class HouseViewItem
{
    public string HouseName { get; set; } = "";
    public string StartDisplay { get; set; } = "";
    public string CuspDisplay { get; set; } = "";
    public string EndDisplay { get; set; } = "";
    public string PlanetsInHouse { get; set; } = "";
}

