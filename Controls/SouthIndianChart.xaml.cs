using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology.Controls;

/// <summary>
/// South Indian Chart Control - displays planets in fixed sign positions
/// </summary>
public partial class SouthIndianChart : UserControl
{
    private readonly Dictionary<int, TextBlock> _signTextBlocks;
    private readonly Dictionary<int, Border> _signBorders;

    public SouthIndianChart()
    {
        InitializeComponent();
        
        // Map sign numbers to their TextBlock controls
        _signTextBlocks = new Dictionary<int, TextBlock>
        {
            { 1, Planets1 }, { 2, Planets2 }, { 3, Planets3 }, { 4, Planets4 },
            { 5, Planets5 }, { 6, Planets6 }, { 7, Planets7 }, { 8, Planets8 },
            { 9, Planets9 }, { 10, Planets10 }, { 11, Planets11 }, { 12, Planets12 }
        };

        // Map sign numbers to their Border controls
        _signBorders = new Dictionary<int, Border>
        {
            { 1, Cell1 }, { 2, Cell2 }, { 3, Cell3 }, { 4, Cell4 },
            { 5, Cell5 }, { 6, Cell6 }, { 7, Cell7 }, { 8, Cell8 },
            { 9, Cell9 }, { 10, Cell10 }, { 11, Cell11 }, { 12, Cell12 }
        };
    }

    /// <summary>
    /// Update chart display with calculated chart data
    /// </summary>
    public void UpdateChart(ChartData chartData, double fontSize = 12)
    {
        // Clear all cells
        foreach (var tb in _signTextBlocks.Values)
        {
            tb.Inlines.Clear();
            tb.Text = string.Empty;
        }

        // Reset all borders to default style
        foreach (var border in _signBorders.Values)
        {
            border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fffff8"));
            border.BorderThickness = new Thickness(1);
        }

        // Update chart title
        ChartTitle.Text = !string.IsNullOrEmpty(chartData.BirthData.Name) ? chartData.BirthData.Name : "Birth Chart";
        AscendantLabel.Text = $"Asc: {chartData.AscendantSignName}\n{ZodiacUtils.FormatDegreeInSign(chartData.AscendantDegree)}";

        // Highlight ascendant cell
        if (_signBorders.TryGetValue(chartData.AscendantSign, out var ascBorder))
        {
            // Use a slightly different background for ascendant (e.g. Cornsilk #fff8dc)
            ascBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fff8dc"));
            ascBorder.BorderThickness = new Thickness(2);
        }

        // Prepare content for each sign
        var displayBySign = new Dictionary<int, List<(string text, string type)>>();
        for (int i = 1; i <= 12; i++) displayBySign[i] = new List<(string text, string type)>();

        // Add "Lagna" to ascendant sign
        displayBySign[chartData.AscendantSign].Add(("Lagna", "lagna"));

        // Add planets
        foreach (var p in chartData.Planets)
        {
             // Format: "12° Su"
             string retro = p.IsRetrograde ? " R" : "";
             string deg = $"{(int)p.DegreeInSign}°";
             string abbr = ZodiacUtils.PlanetAbbreviations[p.Planet];
             string text = $"{deg} {abbr}{retro}";
             
             string type = (p.Planet == Models.Planet.Rahu || p.Planet == Models.Planet.Ketu) 
                ? "rahuKetu" : "planet";

             displayBySign[p.Sign].Add((text, type));
        }

        // Render to TextBlocks
        foreach (var kvp in displayBySign)
        {
            int sign = kvp.Key;
            var items = kvp.Value;

            if (_signTextBlocks.TryGetValue(sign, out var textBlock) && items.Count > 0)
            {
                textBlock.Inlines.Clear();
                textBlock.FontSize = fontSize;
                
                for (int i = 0; i < items.Count; i++)
                {
                    var (text, type) = items[i];
                    
                    var run = new System.Windows.Documents.Run(text);
                    run.Foreground = type switch
                    {
                        "planet" => new SolidColorBrush(Color.FromRgb(204, 0, 0)),     // Red #cc0000
                        "rahuKetu" => new SolidColorBrush(Color.FromRgb(204, 0, 0)),   // Red #cc0000 
                        "lagna" => new SolidColorBrush(Color.FromRgb(0, 153, 0)),      // Green #009900
                        _ => new SolidColorBrush(Colors.Black)
                    };

                    if (type == "lagna") run.FontWeight = FontWeights.Bold;

                    textBlock.Inlines.Add(run);
                    
                    if (i < items.Count - 1)
                    {
                        textBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                    }
                }
            }
        }
    }

    /// <summary>
    /// Clear the chart display
    /// </summary>
    public void ClearChart()
    {
        foreach (var tb in _signTextBlocks.Values)
        {
            tb.Text = string.Empty;
        }

        ChartTitle.Text = "Birth Chart";
        AscendantLabel.Text = "Asc: ";

        foreach (var border in _signBorders.Values)
        {
            border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fffff8"));
            border.BorderThickness = new Thickness(1);
        }
    }
}
