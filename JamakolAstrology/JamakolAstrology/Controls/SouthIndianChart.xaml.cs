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
    public void UpdateChart(ChartData chartData)
    {
        // Clear all cells
        foreach (var tb in _signTextBlocks.Values)
        {
            tb.Text = string.Empty;
        }

        // Reset all borders to default style
        foreach (var border in _signBorders.Values)
        {
            border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16213e"));
            border.BorderThickness = new Thickness(1);
        }

        // Update chart title
        if (!string.IsNullOrEmpty(chartData.BirthData.Name))
        {
            ChartTitle.Text = chartData.BirthData.Name;
        }
        else
        {
            ChartTitle.Text = "Birth Chart";
        }

        // Update ascendant label
        AscendantLabel.Text = $"Asc: {chartData.AscendantSignName}\n{ZodiacUtils.FormatDegreeInSign(chartData.AscendantDegree)}";

        // Highlight ascendant cell
        if (_signBorders.TryGetValue(chartData.AscendantSign, out var ascBorder))
        {
            ascBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1f4068"));
            ascBorder.BorderThickness = new Thickness(2);
        }

        // Group planets by sign
        var planetsBySign = chartData.Planets
            .GroupBy(p => p.Sign)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Display planets in their respective signs
        foreach (var kvp in planetsBySign)
        {
            int sign = kvp.Key;
            var planets = kvp.Value;

            if (_signTextBlocks.TryGetValue(sign, out var textBlock))
            {
                var planetTexts = planets.Select(p =>
                {
                    string retro = p.IsRetrograde ? "(R)" : "";
                    string deg = $"{(int)p.DegreeInSign}°";
                    return $"{ZodiacUtils.PlanetAbbreviations[p.Planet]}{retro}\n{deg}";
                });

                textBlock.Text = string.Join("\n", planetTexts);
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
            border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16213e"));
            border.BorderThickness = new Thickness(1);
        }
    }
}
