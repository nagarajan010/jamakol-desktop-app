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
    private readonly Dictionary<int, TextBlock> _aprakashTextBlocks;
    private readonly DivisionalChartService _divisionalChartService;
    
    // Store chart data for division switching
    private ChartData? _currentChartData;
    private double _currentFontSize = 12;
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
        
        // Map sign numbers to their Aprakash TextBlock controls (bottom-left)
        _aprakashTextBlocks = new Dictionary<int, TextBlock>
        {
            { 1, Aprakash1 }, { 2, Aprakash2 }, { 3, Aprakash3 }, { 4, Aprakash4 },
            { 5, Aprakash5 }, { 6, Aprakash6 }, { 7, Aprakash7 }, { 8, Aprakash8 },
            { 9, Aprakash9 }, { 10, Aprakash10 }, { 11, Aprakash11 }, { 12, Aprakash12 }
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
        // Update chart title - show chart type with person name
        string personName = !string.IsNullOrEmpty(chartData.BirthData.Name) ? chartData.BirthData.Name : "";
        string rasiTitle = GetLocalizedChartTitle(1);
        ChartTitle.Text = string.IsNullOrEmpty(personName) ? rasiTitle : $"{personName}\n{rasiTitle}";
        AscendantLabel.Text = "";

        // Prepare content for each sign - separate regular planets from Aprakash graha
        var displayBySign = new Dictionary<int, List<(string text, string type)>>();
        var aprakashBySign = new Dictionary<int, List<string>>(); // Aprakash graha per sign
        for (int i = 1; i <= 12; i++) 
        {
            displayBySign[i] = new List<(string text, string type)>();
            aprakashBySign[i] = new List<string>();
        }

        // Add "As" to ascendant sign
        displayBySign[chartData.AscendantSign].Add(("As", "lagna"));

        // Add planets - separate Aprakash graha
        foreach (var p in chartData.Planets)
        {
             // Check if this is a standard planet (Name matches expected) or Aprakash graha
             bool isStandardPlanet = ZodiacUtils.PlanetNames.ContainsKey(p.Planet) && 
                                    p.Name == ZodiacUtils.PlanetNames[p.Planet];
             
             if (isStandardPlanet)
             {
                 // Regular planet - display in main area
                 string abbr = GetLocalizedAbbr(p.Planet);
                 string planetDisplay = p.IsRetrograde ? $"({abbr})" : abbr;
                 string text = _hideDegrees ? planetDisplay : $"{(int)p.DegreeInSign}° {planetDisplay}";
                 
                 string type = (p.Planet == Models.Planet.Rahu || p.Planet == Models.Planet.Ketu) 
                    ? "rahuKetu" : "planet";

                 displayBySign[p.Sign].Add((text, type));
             }
             else
             {
                 // Aprakash graha - collect for bottom-left display
                 string abbr = p.Symbol;
                 string text = _hideDegrees ? abbr : $"{(int)p.DegreeInSign}°{abbr}";
                 aprakashBySign[p.Sign].Add(text);
             }
        }

        // Render to TextBlocks - use dynamic column layout for better space usage
        foreach (var kvp in displayBySign)
        {
            int sign = kvp.Key;
            var items = kvp.Value;
            var aprakashItems = aprakashBySign[sign];

            if (_signTextBlocks.TryGetValue(sign, out var textBlock))
            {
                textBlock.Inlines.Clear();
                textBlock.FontSize = fontSize;
                textBlock.TextWrapping = TextWrapping.Wrap;
                
                // Display regular planets
                if (items.Count > 0)
                {
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
            
            // Populate Aprakash graha in dedicated bottom-left TextBlock
            if (_aprakashTextBlocks.TryGetValue(sign, out var aprakashBlock))
            {
                aprakashBlock.Text = aprakashItems.Count > 0 ? string.Join(" ", aprakashItems) : "";
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

        // Update chart title
        string localizedTitle = GetLocalizedChartTitle(divisionalData.Division);
        if (localizedTitle.Contains(" ("))
        {
            ChartTitle.Text = localizedTitle.Replace(" (", "\n(");
        }
        else
        {
            ChartTitle.Text = localizedTitle;
        }
        AscendantLabel.Text = "";

        // Prepare content for each sign - separate regular planets from Aprakash graha
        var displayBySign = new Dictionary<int, List<(string text, string type)>>();
        var aprakashBySign = new Dictionary<int, List<string>>();
        for (int i = 1; i <= 12; i++) 
        {
            displayBySign[i] = new List<(string text, string type)>();
            aprakashBySign[i] = new List<string>();
        }

        // Add "As" to ascendant sign
        displayBySign[divisionalData.AscendantSign].Add(("As", "lagna"));

        // Add planets at their divisional positions - separate Aprakash graha
        foreach (var p in divisionalData.Planets)
        {
            bool isStandardPlanet = ZodiacUtils.PlanetNames.ContainsKey(p.Planet) && 
                                   p.Name == ZodiacUtils.PlanetNames[p.Planet];
            
            if (isStandardPlanet)
            {
                string abbr = GetLocalizedAbbr(p.Planet);
                string planetDisplay = p.IsRetrograde ? $"({abbr})" : abbr;
                string text = _hideDegrees ? planetDisplay : $"{(int)p.DivisionalDegree}° {planetDisplay}";
                
                string type = (p.Planet == Models.Planet.Rahu || p.Planet == Models.Planet.Ketu) 
                    ? "rahuKetu" : "planet";

                displayBySign[p.DivisionalSign].Add((text, type));
            }
            else
            {
                // Aprakash graha - collect for bottom display
                string abbr = p.Symbol;
                string text = _hideDegrees ? abbr : $"{(int)p.DivisionalDegree}°{abbr}";
                aprakashBySign[p.DivisionalSign].Add(text);
            }
        }

        // Render to TextBlocks
        foreach (var kvp in displayBySign)
        {
            int sign = kvp.Key;
            var items = kvp.Value;
            var aprakashItems = aprakashBySign[sign];

            if (_signTextBlocks.TryGetValue(sign, out var textBlock))
            {
                textBlock.Inlines.Clear();
                textBlock.FontSize = fontSize;
                textBlock.TextWrapping = TextWrapping.Wrap;
                
                // Display regular planets
                if (items.Count > 0)
                {
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
            
            // Populate Aprakash graha in dedicated bottom-left TextBlock
            if (_aprakashTextBlocks.TryGetValue(sign, out var aprakashBlock))
            {
                aprakashBlock.Text = aprakashItems.Count > 0 ? string.Join(" ", aprakashItems) : "";
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
        
        // Clear Aprakash TextBlocks
        foreach (var tb in _aprakashTextBlocks.Values)
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

    /// <summary>
    /// Handle mouse wheel to allow bubbling if inner scrollviewer cannot scroll.
    /// </summary>
    /// <summary>
    /// Handle mouse wheel to allow bubbling if inner scrollviewer cannot scroll or is at boundary.
    /// </summary>
    private void Control_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        var element = e.OriginalSource as DependencyObject;
        ScrollViewer? scroller = null;
        
        // Walk up to find ScrollViewer within this control
        while (element != null && element != this)
        {
            if (element is ScrollViewer sv)
            {
                scroller = sv;
                break;
            }
            
            // Handle logical parent for non-visual elements like Run
            if (element is FrameworkContentElement fce)
            {
                element = fce.Parent;
            }
            else if (element is Visual || element is System.Windows.Media.Media3D.Visual3D)
            {
                element = VisualTreeHelper.GetParent(element);
            }
            else
            {
                element = null;
            }
        }

        // If over a scrollviewer
        if (scroller != null)
        {
            // Check if we should pass the scroll to parent
            bool shouldPassToParent = false;

            // 1. If content fits entirely, always pass
            if (scroller.ExtentHeight <= scroller.ViewportHeight)
            {
                shouldPassToParent = true;
            }
            // 2. If scrolling UP (Delta > 0) and at TOP
            else if (e.Delta > 0 && scroller.VerticalOffset <= 0)
            {
                shouldPassToParent = true;
            }
            // 3. If scrolling DOWN (Delta < 0) and at BOTTOM
            else if (e.Delta < 0 && scroller.VerticalOffset >= scroller.ScrollableHeight)
            {
                shouldPassToParent = true;
            }

            if (shouldPassToParent)
            {
                // Can't scroll internally, so let parent scroll.
                // We mark the preview event as handled so the inner scrollviewer doesn't catch it
                e.Handled = true;

                // Raise a new MouseWheel event that bubbles up from this control
                var eventArg = new System.Windows.Input.MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = this;
                
                this.RaiseEvent(eventArg);
            }
        }
    }
    private string GetLocalizedAbbr(Planet p)
    {
        if (!ZodiacUtils.IsTamil) return ZodiacUtils.PlanetAbbreviations[p];

        return p switch
        {
            Planet.Sun => "சூரி",
            Planet.Moon => "சந்",
            Planet.Mars => "செவ்",
            Planet.Mercury => "புத",
            Planet.Jupiter => "குரு",
            Planet.Venus => "சுக்",
            Planet.Saturn => "சனி",
            Planet.Rahu => "ராகு",
            Planet.Ketu => "கேது",
            _ => ZodiacUtils.PlanetAbbreviations[p]
        };
    }

    private string GetLocalizedChartTitle(int div)
    {
        string baseName = div switch {
            1 => "Rasi", 2 => "Hora", 3 => "Drekkana", 4 => "Chaturthamsa", 7 => "Saptamsa", 9 => "Navamsa",
            10 => "Dasamsa", 12 => "Dwadasamsa", 16 => "Shodasamsa", 20 => "Vimsamsa",
            24 => "Siddhamsa", 27 => "Nakshatramsa", 30 => "Trimsamsa", 40 => "Khavedamsa",
            45 => "Akshavedamsa", 60 => "Shashtiamsa", _ => "Varga"
        };
        
        string name = baseName;
        if (ZodiacUtils.IsTamil)
        {
             name = baseName switch {
                 "Rasi" => "ராசி",
                 "Hora" => "ஹோரா", "Drekkana" => "திரேக்காணம்", "Chaturthamsa" => "சதுர்த்தாம்சம்",
                 "Saptamsa" => "சப்தாம்சம்", "Navamsa" => "நவாம்சம்", "Dasamsa" => "தசாம்சம்",
                 "Dwadasamsa" => "துவாதாம்சம்", "Shodasamsa" => "ஷோடசாம்சம்", "Vimsamsa" => "விம்சாம்சம்",
                 "Siddhamsa" => "சித்தாம்சம்", "Nakshatramsa" => "நட்சத்திராம்சம்", "Trimsamsa" => "திரிம்சாம்சம்",
                 "Khavedamsa" => "கவேதாம்சம்", "Akshavedamsa" => "அட்சவேதாம்சம்", "Shashtiamsa" => "சஷ்டியாம்சம்",
                 _ => name
             };
        }
        
        return $"{name} (D-{div})";
    }
}
