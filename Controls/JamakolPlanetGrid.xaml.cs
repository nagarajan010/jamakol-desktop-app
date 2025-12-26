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
/// Planetary Positions Grid for Jamakol - displays planet positions
/// </summary>
public partial class JamakolPlanetGrid : UserControl
{
    public JamakolPlanetGrid()
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
    /// Clear selection when DataGrid loads
    /// </summary>
    private void DataGrid_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is DataGrid dataGrid)
        {
            dataGrid.UnselectAll();
            dataGrid.SelectedIndex = -1;
        }
    }

    /// <summary>
    /// Update the grid with Jamakol planet data
    /// </summary>
    public void UpdateGrid(JamakolData jamakolData)
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
                ? ZodiacUtils.NakshatraNames[nakIndex] 
                : "";
            
            displayData.Add(new JamakolPlanetGridItem
            {
                EnglishName = "Lagna",
                SignEnglish = signShort,
                DegreeDisplay = degDisplay,
                NakshatraEnglish = nakName,
                Pada = pada.ToString(),

                Gati = "",
                RetroDisplay = ""
            });
        }
        
        // Add planets
        displayData.AddRange(jamakolData.PlanetPositions.Select(p => new JamakolPlanetGridItem
        {
            EnglishName = p.EnglishName,
            SignEnglish = p.SignEnglish.Length > 2 ? p.SignEnglish.Substring(0, 2) : p.SignEnglish,
            DegreeDisplay = p.DegreeDisplay, 
            NakshatraEnglish = p.NakshatraEnglish,
            Pada = p.NakshatraPada.ToString(),

            Gati = p.Gati, // Now populated from JamakolPlanetPosition
            RetroDisplay = p.IsRetrograde ? "R" : "",
            CombustionFlag = p.CombustionFlag
        }));

        DataGridControl.ItemsSource = displayData;
    }
    
    /// <summary>
    /// Update the grid with birth chart data (ChartData)
    /// </summary>
    public void UpdateGrid(ChartData chartData)
    {
        var displayData = new List<JamakolPlanetGridItem>();
        
        // Add Lagna first
        double ascDeg = chartData.AscendantDegree;
        int ascD = (int)ascDeg;
        double ascMFull = (ascDeg - ascD) * 60;
        int ascM = (int)ascMFull;
        double ascS = (ascMFull - ascM) * 60;
        
        displayData.Add(new JamakolPlanetGridItem
        {
            EnglishName = "Lagna",
            SignEnglish = chartData.AscendantSignName.Length > 2 ? chartData.AscendantSignName.Substring(0, 2) : chartData.AscendantSignName,
            DegreeDisplay = $"{ascD}°{ascM:00}'{ascS:00.00}\"",
            NakshatraEnglish = chartData.AscendantNakshatraName ?? "",
            Pada = chartData.AscendantNakshatraPada.ToString(),

            Gati = "",
            RetroDisplay = ""
        });
        
        // Add planets
        displayData.AddRange(chartData.Planets.Select(p => {
             double pDeg = p.DegreeInSign;
             int pD = (int)pDeg;
             double pMFull = (pDeg - pD) * 60;
             int pM = (int)pMFull;
             double pS = (pMFull - pM) * 60;

             return new JamakolPlanetGridItem
             {
                 EnglishName = p.Name,
                 SignEnglish = p.SignName.Length > 2 ? p.SignName.Substring(0, 2) : p.SignName,
                 DegreeDisplay = $"{pD}°{pM:00}'{pS:00.00}\"",
                 NakshatraEnglish = p.NakshatraName,
                 Pada = p.NakshatraPada.ToString(),
                 Gati = p.Gati,
                 RetroDisplay = p.IsRetrograde ? "R" : "",
                 CombustionFlag = p.CombustionFlag
             };
        }));

        DataGridControl.ItemsSource = displayData;
    }
}

/// <summary>
/// Display item for Jamakol Planet grid
/// </summary>
public class JamakolPlanetGridItem
{
    public string EnglishName { get; set; } = "";
    public string SignEnglish { get; set; } = "";
    public string DegreeDisplay { get; set; } = "";
    public string NakshatraEnglish { get; set; } = "";
    public string Pada { get; set; } = "";
    public string Gati { get; set; } = "";
    public string RetroDisplay { get; set; } = "";
    public string CombustionFlag { get; set; } = "";
}
