using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology.Controls;

public partial class HousesPanel : UserControl
{
    private ChartData? _currentChart;
    
    /// <summary>
    /// Event raised when house system is changed via context menu
    /// </summary>
    public event EventHandler<HouseSystem>? HouseSystemChanged;
    
    public HousesPanel()
    {
        InitializeComponent();
        
        // Apply font size from settings if valid
        var settings = AppSettings.Load();
        if (settings != null)
        {
            HousesGrid.FontSize = settings.TableFontSize;
        }
        
        UpdateMenuCheckmarks();
    }
    
    private void UpdateMenuCheckmarks()
    {
        var settings = AppSettings.Load();
        char currentSystem = (char)settings.HouseSystem;
        
        foreach (var item in HouseSystemMenu.Items)
        {
            if (item is MenuItem menuItem && menuItem.Tag != null)
            {
                string tag = menuItem.Tag.ToString()!;
                // House system checkmarks (single char tags)
                if (tag.Length == 1)
                {
                    menuItem.IsChecked = (tag[0] == currentSystem);
                }
            }
        }
        
        // Update cusp reference checkmarks
        MenuCuspMiddle.IsChecked = settings.CuspAsMiddle;
        MenuCuspStart.IsChecked = !settings.CuspAsMiddle;
    }
    
    private void HouseSystem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag != null)
        {
            string tag = menuItem.Tag.ToString()!;
            if (tag.Length == 1)
            {
                char systemChar = tag[0];
                var houseSystem = (HouseSystem)systemChar;
                
                // Save to settings
                var settings = AppSettings.Load();
                settings.HouseSystem = houseSystem;
                settings.Save();
                
                // Update checkmarks
                UpdateMenuCheckmarks();
                
                // Notify parent to recalculate
                HouseSystemChanged?.Invoke(this, houseSystem);
            }
        }
    }
    
    private void CuspReference_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag != null)
        {
            string tag = menuItem.Tag.ToString()!;
            bool isMiddle = (tag == "Middle");
            
            var settings = AppSettings.Load();
            settings.CuspAsMiddle = isMiddle;
            settings.Save();
            
            UpdateMenuCheckmarks();
            
            // Notify parent to recalculate
            HouseSystemChanged?.Invoke(this, settings.HouseSystem);
        }
    }
    
    private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
    {
        if (_currentChart == null || _currentChart.HouseCusps == null) return;
        
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("House\tStart\tCusp\tEnd\tPlanets");
        
        foreach (var cusp in _currentChart.HouseCusps)
        {
            var planets = GetPlanetsInHouse(cusp.HouseNumber);
            sb.AppendLine($"{cusp.HouseNumber}\t{cusp.StartDisplay}\t{cusp.DegreeDisplay}\t{cusp.EndDisplay}\t{planets}");
        }
        
        System.Windows.Clipboard.SetText(sb.ToString());
    }
    
    private string GetPlanetsInHouse(int houseNumber)
    {
        if (_currentChart == null) return "";
        
        var planetsByHouse = new Dictionary<int, List<PlanetPosition>>();
        for (int h = 1; h <= 12; h++) planetsByHouse[h] = new List<PlanetPosition>();
        
        foreach (var planet in _currentChart.Planets)
        {
            int houseNum = FindHouseForPlanet(planet.Longitude, _currentChart.HouseCusps);
            if (houseNum >= 1 && houseNum <= 12) planetsByHouse[houseNum].Add(planet);
        }
        
        if (planetsByHouse.TryGetValue(houseNumber, out var planets) && planets.Count > 0)
        {
            var abbrevs = planets.Select(p => GetPlanetAbbrev(p)).ToList();
            if (houseNumber == 1) abbrevs.Insert(0, "As");
            return string.Join(", ", abbrevs);
        }
        return houseNumber == 1 ? "As" : "";
    }

    public void UpdateChart(ChartData? chart)
    {
        _currentChart = chart;
        
        if (chart == null || chart.HouseCusps == null)
        {
            HousesGrid.ItemsSource = null;
            return;
        }

        var settings = AppSettings.Load();
        bool cuspAsMiddle = settings.CuspAsMiddle;
        
        // Update column headers based on mode
        if (cuspAsMiddle)
        {
            Col1Header.Header = ZodiacUtils.IsTamil ? "தொடக்கம்" : "Start";
            Col2Header.Header = ZodiacUtils.IsTamil ? "கொனை" : "Cusp";
            Col3Header.Header = ZodiacUtils.IsTamil ? "முடிவு" : "End";
        }
        else
        {
            Col1Header.Header = ZodiacUtils.IsTamil ? "கொனை" : "Cusp";
            Col2Header.Header = ZodiacUtils.IsTamil ? "நடுவு" : "Middle";
            Col3Header.Header = ZodiacUtils.IsTamil ? "முடிவு" : "End";
        }

        var items = new List<HouseViewItem>();
        
        // Build lookup of planets by house - using actual house cusp ranges
        var planetsByHouse = new Dictionary<int, List<PlanetPosition>>();
        for (int h = 1; h <= 12; h++)
        {
            planetsByHouse[h] = new List<PlanetPosition>();
        }
        
        // For each planet, find which house it falls into based on cusp ranges
        foreach (var planet in chart.Planets)
        {
            int houseNum = FindHouseForPlanet(planet.Longitude, chart.HouseCusps);
            if (houseNum >= 1 && houseNum <= 12)
            {
                planetsByHouse[houseNum].Add(planet);
            }
        }
        
        foreach (var cusp in chart.HouseCusps)
        {
            // Get planets in this house
            var planetsInHouse = "";
            if (planetsByHouse.TryGetValue(cusp.HouseNumber, out var planets) && planets.Count > 0)
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
            
            // Calculate middle point for "start" mode
            string middleDisplay = "";
            if (!cuspAsMiddle)
            {
                // Middle = midpoint between cusp and end
                double mid = ZodiacUtils.NormalizeDegree((cusp.Degree + cusp.EndDegree) / 2);
                // Handle wrap-around
                if (cusp.EndDegree < cusp.Degree)
                {
                    mid = ZodiacUtils.NormalizeDegree((cusp.Degree + cusp.EndDegree + 360) / 2);
                }
                middleDisplay = FormatDegreeWithSign(mid);
            }
            
            items.Add(new HouseViewItem
            {
                HouseName = FormatHouseName(cusp.HouseNumber),
                Col1Display = cuspAsMiddle ? cusp.StartDisplay : cusp.DegreeDisplay,    // Start or Cusp
                Col2Display = cuspAsMiddle ? cusp.DegreeDisplay : middleDisplay,        // Cusp or Middle
                Col3Display = cusp.EndDisplay,                                          // End
                PlanetsInHouse = planetsInHouse
            });
        }

        HousesGrid.ItemsSource = items;
    }
    
    private string FormatDegreeWithSign(double degree)
    {
        degree = ZodiacUtils.NormalizeDegree(degree);
        int signIndex = ZodiacUtils.DegreeToSign(degree);
        double degInSign = ZodiacUtils.DegreeInSign(degree);
        
        int deg = (int)degInSign;
        double minVal = (degInSign - deg) * 60;
        int min = (int)minVal;
        int sec = (int)((minVal - min) * 60);
        
        string[] signAbbr = { "", "Ar", "Ta", "Ge", "Cn", "Le", "Vi", "Li", "Sc", "Sg", "Cp", "Aq", "Pi" };
        
        return $"{deg} {signAbbr[signIndex]} {min:D2}' {sec:D2}\"";
    }
    
    /// <summary>
    /// Find which house a planet falls into based on house cusp Start/End ranges
    /// </summary>
    private int FindHouseForPlanet(double planetLongitude, List<HouseCusp> cusps)
    {
        planetLongitude = ZodiacUtils.NormalizeDegree(planetLongitude);
        
        foreach (var cusp in cusps)
        {
            double start = ZodiacUtils.NormalizeDegree(cusp.StartDegree);
            double end = ZodiacUtils.NormalizeDegree(cusp.EndDegree);
            
            // Handle wrap-around case (e.g., start=350°, end=20°)
            if (start <= end)
            {
                // Normal case: start < end
                if (planetLongitude >= start && planetLongitude < end)
                {
                    return cusp.HouseNumber;
                }
            }
            else
            {
                // Wrap-around case: house spans 0°
                if (planetLongitude >= start || planetLongitude < end)
                {
                    return cusp.HouseNumber;
                }
            }
        }
        
        // Fallback: shouldn't happen, but return house 1
        return 1;
    }
    
    private string GetPlanetAbbrev(PlanetPosition p)
    {
        // Check if this is an Aprakash Graha (shadow planet) FIRST
        // Aprakash Graha have non-standard names (Dhooma, Vyatipata, etc.)
        bool isStandardPlanet = ZodiacUtils.PlanetNames.ContainsKey(p.Planet) && 
                               p.Name == ZodiacUtils.PlanetNames[p.Planet];
        
        if (!isStandardPlanet)
        {
            // For Aprakash Graha - use their symbol
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
    public string Col1Display { get; set; } = "";  // Start (middle mode) or Cusp (start mode)
    public string Col2Display { get; set; } = "";  // Cusp (middle mode) or Middle (start mode)
    public string Col3Display { get; set; } = "";  // End
    public string PlanetsInHouse { get; set; } = "";
}



