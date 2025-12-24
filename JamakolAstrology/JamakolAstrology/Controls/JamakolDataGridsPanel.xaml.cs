using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology.Controls;

/// <summary>
/// Jamakol Data Grids Panel - displays Jama Graha and Planetary Positions
/// </summary>
public partial class JamakolDataGridsPanel : UserControl
{
    public JamakolDataGridsPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Update the Jama Graha grid
    /// </summary>
    public void UpdateJamaGrahaGrid(List<JamaGrahaPosition> jamaGrahas, List<SpecialPoint> specialPoints)
    {
        // Create a list of all items first
        var allItems = new List<JamaGrahaGridItem>();
        
        // 1. Add Special Points
        foreach (var point in specialPoints)
        {
            double deg = point.DegreeInSign;
            string dms = $"{(int)deg}°{(int)((deg % 1) * 60)}'{(int)(((deg % 1) * 60 % 1) * 60)}\"";
            string signShort = point.Sign.Length > 2 ? point.Sign.Substring(0, 2) : point.Sign;
            string nakWithType = $"{point.NakshatraName} ({point.Pada})";
            allItems.Add(new JamaGrahaGridItem { Name = point.Name, DegreeDisplay = dms, NakshatraName = nakWithType, SignName = signShort });
        }
        
        // 2. Add Planets
        foreach (var graha in jamaGrahas)
        {
            string signShort = graha.SignName.Length > 2 ? graha.SignName.Substring(0, 2) : graha.SignName;
            string nakWithType = $"{graha.NakshatraName} ({graha.Pada})";
            allItems.Add(new JamaGrahaGridItem { Name = graha.Name, DegreeDisplay = graha.DegreeDisplay, NakshatraName = nakWithType, SignName = signShort });
        }

        // 3. Define priority order
        var priorityNames = new[] { "Udayam", "Aarudam", "Kavippu" };
        var orderedItems = new List<JamaGrahaGridItem>();
        
        // 4. Extract priority items in order
        foreach (var name in priorityNames)
        {
             var item = allItems.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
             if (item != null)
             {
                 orderedItems.Add(item);
                 allItems.Remove(item);
             }
        }
        
        // 5. Append remaining items
        orderedItems.AddRange(allItems);

        JamaGrahaGrid.ItemsSource = orderedItems;
    }

    /// <summary>
    /// Update the Planetary Positions grid
    /// </summary>
    public void UpdatePlanetGrid(JamakolData jamakolData)
    {
        var displayData = jamakolData.PlanetPositions.Select(p => new JamakolPlanetGridItem
        {
            EnglishName = p.EnglishName,
            SignEnglish = p.SignEnglish.Length > 2 ? p.SignEnglish.Substring(0, 2) : p.SignEnglish,
            DegreeDisplay = p.DegreeDisplay, 
            NakshatraEnglish = $"{p.NakshatraEnglish} ({p.NakshatraPada})"
        }).ToList();

        PlanetGrid.ItemsSource = displayData;
    }
}

/// <summary>
/// Display item for Jama Graha grid
/// </summary>
public class JamaGrahaGridItem
{
    public string Name { get; set; } = "";
    public string DegreeDisplay { get; set; } = "";
    public string NakshatraName { get; set; } = "";
    public string SignName { get; set; } = "";
}

/// <summary>
/// Display item for Planet grid
/// </summary>
public class JamakolPlanetGridItem
{
    public string EnglishName { get; set; } = "";
    public string SignEnglish { get; set; } = "";
    public string DegreeDisplay { get; set; } = "";
    public string NakshatraEnglish { get; set; } = "";
}
