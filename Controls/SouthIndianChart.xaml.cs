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
    private readonly DivisionalChartService _divisionalChartService;
    
    // Store chart data for division switching
    private ChartData? _currentChartData;
    private double _currentFontSize = 12;
    private bool _isUpdating = false;
    private bool _hideDegrees = false;
    private int _currentDivision = 1;
    
    /// <summary>
    /// Gets or sets whether to hide degrees in planet display
    /// </summary>
    public bool HideDegrees 
    { 
        get => _hideDegrees; 
        set 
        { 
            _hideDegrees = value;
            // Refresh display if chart data exists
            if (_currentChartData != null)
            {
                DisplayDivision(_currentDivision);
            }
        }
    }

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
        
        _divisionalChartService = new DivisionalChartService();
    }
    
    /// <summary>
    /// Handle division context menu item click
    /// </summary>
    private void DivisionMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (_currentChartData == null) return;
        
        if (sender is MenuItem menuItem && menuItem.Tag != null)
        {
            int division = int.Parse(menuItem.Tag.ToString()!);
            _currentDivision = division;
            DisplayDivision(division);
        }
    }
    
    /// <summary>
    /// Display a specific division of the current chart
    /// </summary>
    private void DisplayDivision(int division)
    {
        if (_currentChartData == null) return;
        
        _currentDivision = division;
        
        if (division == 1)
        {
            // D-1 is the natal chart
            DisplayNatalChart(_currentChartData, _currentFontSize);
        }
        else
        {
            // Calculate and display divisional chart
            var divisionalData = _divisionalChartService.CalculateDivisionalChart(_currentChartData, division);
            DisplayDivisionalChartInternal(divisionalData, _currentChartData.BirthData.Name, _currentFontSize);
        }
    }
    
    /// <summary>
    /// Set division to a specific value
    /// </summary>
    public void SetDivision(int division)
    {
        _currentDivision = division;
    }

    /// <summary>
    /// Update chart display with calculated chart data
    /// </summary>
    public void UpdateChart(ChartData chartData, double fontSize = 12, bool hideDegrees = false)
    {
        // Store for division switching
        _currentChartData = chartData;
        _currentFontSize = fontSize;
        _hideDegrees = hideDegrees;
        
        // Reset dropdown to D-1
        SetDivision(1);
        
        // Display the natal chart
        DisplayNatalChart(chartData, fontSize);
    }
    
    /// <summary>
    /// Internal method to display the natal (D-1) chart
    /// </summary>
    private void DisplayNatalChart(ChartData chartData, double fontSize)
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

        // Update chart title - show chart type with person name
        string personName = !string.IsNullOrEmpty(chartData.BirthData.Name) ? chartData.BirthData.Name : "";
        ChartTitle.Text = string.IsNullOrEmpty(personName) ? "Rasi (D-1)" : $"{personName}\nRasi (D-1)";
        AscendantLabel.Text = $"Asc: {chartData.AscendantSignName}\n{ZodiacUtils.FormatDegreeInSign(chartData.AscendantDegree)}";

        // Prepare content for each sign
        var displayBySign = new Dictionary<int, List<(string text, string type)>>();
        for (int i = 1; i <= 12; i++) displayBySign[i] = new List<(string text, string type)>();

        // Add "Lagna" to ascendant sign
        displayBySign[chartData.AscendantSign].Add(("Lagna", "lagna"));

        // Add planets
        foreach (var p in chartData.Planets)
        {
             // Format: "12° Su" or "12° (Su)" for retrograde, just "Su" or "(Su)" if hiding degrees
             string abbr = ZodiacUtils.PlanetAbbreviations[p.Planet];
             string planetDisplay = p.IsRetrograde ? $"({abbr})" : abbr;
             string text = _hideDegrees ? planetDisplay : $"{(int)p.DegreeInSign}° {planetDisplay}";
             
             string type = (p.Planet == Models.Planet.Rahu || p.Planet == Models.Planet.Ketu) 
                ? "rahuKetu" : "planet";

             displayBySign[p.Sign].Add((text, type));
        }

        // Render to TextBlocks - use dynamic column layout for better space usage
        foreach (var kvp in displayBySign)
        {
            int sign = kvp.Key;
            var items = kvp.Value;

            if (_signTextBlocks.TryGetValue(sign, out var textBlock) && items.Count > 0)
            {
                textBlock.Inlines.Clear();
                textBlock.FontSize = fontSize;
                textBlock.TextWrapping = TextWrapping.Wrap;
                
                // Use more columns when hiding degrees (items are shorter)
                // 3-4 columns when hiding degrees, 2-3 columns when showing degrees
                int columns = _hideDegrees ? 4 : 3;
                
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
                    
                    // Add separator: line break every N items (columns), space otherwise
                    if (i < items.Count - 1)
                    {
                        if ((i + 1) % columns == 0)
                        {
                            textBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                        }
                        else
                        {
                            textBlock.Inlines.Add(new System.Windows.Documents.Run(" "));
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Update chart display with divisional chart data
    /// </summary>
    public void UpdateDivisionalChart(DivisionalChartData divisionalData, ChartData? chartData = null, string personName = "", double fontSize = 12)
    {
        // Store chart data for division switching
        if (chartData != null)
        {
            _currentChartData = chartData;
        }
        _currentFontSize = fontSize;
        
        // Set dropdown to the correct division
        SetDivision(divisionalData.Division);
        
        // Display the divisional chart
        DisplayDivisionalChartInternal(divisionalData, personName, fontSize);
    }
    
    /// <summary>
    /// Internal method to display a divisional chart
    /// </summary>
    private void DisplayDivisionalChartInternal(DivisionalChartData divisionalData, string personName, double fontSize)
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

        // Update chart title - show division name only
        ChartTitle.Text = divisionalData.Name;
        AscendantLabel.Text = "";
        // AscendantLabel.Text = $"Asc: {divisionalData.AscendantSignName}\n{ZodiacUtils.FormatDegreeInSign(divisionalData.AscendantDegree)}";

        // Prepare content for each sign
        var displayBySign = new Dictionary<int, List<(string text, string type)>>();
        for (int i = 1; i <= 12; i++) displayBySign[i] = new List<(string text, string type)>();

        // Add "Lagna" to ascendant sign
        displayBySign[divisionalData.AscendantSign].Add(("Lagna", "lagna"));

        // Add planets at their divisional positions
        foreach (var p in divisionalData.Planets)
        {
            // Format: "12° Su" or "12° (Su)" for retrograde, just "Su" or "(Su)" if hiding degrees
            string abbr = ZodiacUtils.PlanetAbbreviations[p.Planet];
            string planetDisplay = p.IsRetrograde ? $"({abbr})" : abbr;
            string text = _hideDegrees ? planetDisplay : $"{(int)p.DivisionalDegree}° {planetDisplay}";
            
            string type = (p.Planet == Models.Planet.Rahu || p.Planet == Models.Planet.Ketu) 
                ? "rahuKetu" : "planet";

            displayBySign[p.DivisionalSign].Add((text, type));
        }

        // Render to TextBlocks - use dynamic column layout for better space usage
        foreach (var kvp in displayBySign)
        {
            int sign = kvp.Key;
            var items = kvp.Value;

            if (_signTextBlocks.TryGetValue(sign, out var textBlock) && items.Count > 0)
            {
                textBlock.Inlines.Clear();
                textBlock.FontSize = fontSize;
                textBlock.TextWrapping = TextWrapping.Wrap;
                
                // Use more columns when hiding degrees (items are shorter)
                int columns = _hideDegrees ? 4 : 3;
                
                for (int i = 0; i < items.Count; i++)
                {
                    var (text, type) = items[i];
                    
                    var run = new System.Windows.Documents.Run(text);
                    run.Foreground = type switch
                    {
                        "planet" => new SolidColorBrush(Color.FromRgb(204, 0, 0)),
                        "rahuKetu" => new SolidColorBrush(Color.FromRgb(204, 0, 0)),
                        "lagna" => new SolidColorBrush(Color.FromRgb(0, 153, 0)),
                        _ => new SolidColorBrush(Colors.Black)
                    };

                    if (type == "lagna") run.FontWeight = FontWeights.Bold;

                    textBlock.Inlines.Add(run);
                    
                    // Add separator: line break every N items (columns), space otherwise
                    if (i < items.Count - 1)
                    {
                        if ((i + 1) % columns == 0)
                        {
                            textBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                        }
                        else
                        {
                            textBlock.Inlines.Add(new System.Windows.Documents.Run(" "));
                        }
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
