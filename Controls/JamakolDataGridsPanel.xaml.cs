using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
    /// Handle mouse wheel to bubble scroll up to parent ScrollViewer
    /// </summary>
    private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled) return;
        
        e.Handled = true;
        var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = UIElement.MouseWheelEvent,
            Source = sender
        };
        
        // Find parent ScrollViewer and scroll it
        var parent = VisualTreeHelper.GetParent((DependencyObject)sender);
        while (parent != null && !(parent is ScrollViewer))
        {
            parent = VisualTreeHelper.GetParent(parent);
        }
        
        if (parent is ScrollViewer scrollViewer)
        {
            scrollViewer.RaiseEvent(eventArg);
        }
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
            allItems.Add(new JamaGrahaGridItem 
            { 
                Name = point.Name, 
                DegreeDisplay = dms, 
                NakshatraName = point.NakshatraName, 
                Pada = point.Pada.ToString(),
                SignName = signShort 
            });
        }
        
        // 2. Add Planets
        foreach (var graha in jamaGrahas)
        {
            string signShort = graha.SignName.Length > 2 ? graha.SignName.Substring(0, 2) : graha.SignName;
            allItems.Add(new JamaGrahaGridItem 
            { 
                Name = graha.Name, 
                DegreeDisplay = graha.DegreeDisplay, 
                NakshatraName = graha.NakshatraName, 
                Pada = graha.Pada.ToString(),
                SignName = signShort 
            });
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
        var displayData = new List<JamakolPlanetGridItem>();
        
        // Add Lagna at the top
        var chartData = jamakolData.ChartData;
        if (chartData != null && chartData.AscendantDegree > 0)
        {
            double degInSign = chartData.AscendantDegree % 30;
            string signShort = chartData.AscendantSignName.Length > 2 ? chartData.AscendantSignName.Substring(0, 2) : chartData.AscendantSignName;
            string degDisplay = $"{(int)degInSign}°{(int)((degInSign % 1) * 60)}'{(int)(((degInSign % 1) * 60 % 1) * 60)}\"";
            
            // Get Nakshatra for Lagna
            int nakIndex = (int)(chartData.AscendantDegree / 13.333333);
            int pada = ((int)(chartData.AscendantDegree % 13.333333 / 3.333333)) + 1;
            string nakName = nakIndex >= 0 && nakIndex < 27 
                ? JamakolAstrology.Services.ZodiacUtils.NakshatraNames[nakIndex] 
                : "";
            
            displayData.Add(new JamakolPlanetGridItem
            {
                EnglishName = "Lagna",
                SignEnglish = signShort,
                DegreeDisplay = degDisplay,
                NakshatraEnglish = nakName,
                Pada = pada.ToString()
            });
        }
        
        // Add planets
        displayData.AddRange(jamakolData.PlanetPositions.Select(p => new JamakolPlanetGridItem
        {
            EnglishName = p.EnglishName,
            SignEnglish = p.SignEnglish.Length > 2 ? p.SignEnglish.Substring(0, 2) : p.SignEnglish,
            DegreeDisplay = p.DegreeDisplay, 
            NakshatraEnglish = p.NakshatraEnglish,
            Pada = p.NakshatraPada.ToString()
        }));

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
    public string Pada { get; set; } = "";
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
    public string Pada { get; set; } = "";
}
