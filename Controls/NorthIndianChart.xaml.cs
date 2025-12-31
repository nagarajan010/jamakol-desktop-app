using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology.Controls;

/// <summary>
/// North Indian Chart Control - displays planets in fixed house positions (Diamond/Diamond Chart)
/// </summary>
public partial class NorthIndianChart : UserControl
{
    private readonly Dictionary<int, Grid> _houseGrids;
    private readonly Dictionary<int, TextBlock> _signTextBlocks;
    private readonly Dictionary<int, TextBlock> _planetTextBlocks;
    private readonly Dictionary<int, TextBlock> _aprakashTextBlocks;
    private readonly DivisionalChartService _divisionalChartService;
    
    // Store chart data for division switching
    private ChartData? _currentChartData;
    private double _currentFontSize = 12;
    private bool _hideDegrees = false;
    private int _currentDivision = 1;

    public NorthIndianChart()
    {
        InitializeComponent();
        
        // Map house numbers (1-12) to their Grid controls
        _houseGrids = new Dictionary<int, Grid>
        {
            { 1, H1 }, { 2, H2 }, { 3, H3 }, { 4, H4 },
            { 5, H5 }, { 6, H6 }, { 7, H7 }, { 8, H8 },
            { 9, H9 }, { 10, H10 }, { 11, H11 }, { 12, H12 }
        };

        // Map house numbers to Sign TextBlocks
        _signTextBlocks = new Dictionary<int, TextBlock>
        {
            { 1, H1Sign }, { 2, H2Sign }, { 3, H3Sign }, { 4, H4Sign },
            { 5, H5Sign }, { 6, H6Sign }, { 7, H7Sign }, { 8, H8Sign },
            { 9, H9Sign }, { 10, H10Sign }, { 11, H11Sign }, { 12, H12Sign }
        };

        // Map house numbers to Planet TextBlocks
        _planetTextBlocks = new Dictionary<int, TextBlock>
        {
            { 1, H1Planets }, { 2, H2Planets }, { 3, H3Planets }, { 4, H4Planets },
            { 5, H5Planets }, { 6, H6Planets }, { 7, H7Planets }, { 8, H8Planets },
            { 9, H9Planets }, { 10, H10Planets }, { 11, H11Planets }, { 12, H12Planets }
        };
        
        // Map house numbers to Aprakash TextBlocks
        _aprakashTextBlocks = new Dictionary<int, TextBlock>
        {
            { 1, H1Aprakash }, { 2, H2Aprakash }, { 3, H3Aprakash }, { 4, H4Aprakash },
            { 5, H5Aprakash }, { 6, H6Aprakash }, { 7, H7Aprakash }, { 8, H8Aprakash },
            { 9, H9Aprakash }, { 10, H10Aprakash }, { 11, H11Aprakash }, { 12, H12Aprakash }
        };
        
        _divisionalChartService = new DivisionalChartService();
        LocalizeContextMenu();
    }

    /// <summary>
    /// Gets or sets whether to hide degrees in planet display
    /// </summary>
    public bool HideDegrees 
    { 
        get => _hideDegrees; 
        set 
        { 
            _hideDegrees = value;
            if (_currentChartData != null)
            {
                DisplayDivision(_currentDivision);
            }
        }
    }
    
    private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Redraw lines if size changes (for responsiveness)
        // Currently utilizing fixed Path Data in XAML for simplicity as it's in a Viewbox.
        // Viewbox handles scaling, so manual redraw is not strictly needed unless aspect ratio breaks.
    }

    private void LocalizeContextMenu()
    {
        if (DivisionContextMenu == null) return;
        
        foreach (var item in DivisionContextMenu.Items)
        {
            if (item is MenuItem menuItem && menuItem.Tag != null)
            {
                if (int.TryParse(menuItem.Tag.ToString(), out int div))
                {
                    menuItem.Header = GetLocalizedChartTitle(div);
                }
            }
        }
    }
    
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
    
    public void UpdateChart(ChartData chartData, double fontSize = 12, bool hideDegrees = false)
    {
        _currentChartData = chartData;
        _currentFontSize = fontSize;
        _hideDegrees = hideDegrees;
        
        SetDivision(1);
        DisplayNatalChart(chartData, fontSize);
    }
    
    public void UpdateDivisionalChart(DivisionalChartData divisionalData, ChartData? chartData = null, string personName = "", double fontSize = 12)
    {
        if (chartData != null) _currentChartData = chartData;
        _currentFontSize = fontSize;
        
        SetDivision(divisionalData.Division);
        DisplayDivisionalChartInternal(divisionalData, personName, fontSize);
    }
    
    public void SetDivision(int division)
    {
        _currentDivision = division;
    }

    private void DisplayDivision(int division)
    {
        if (_currentChartData == null) return;
        
        _currentDivision = division;
        
        if (division == 1)
        {
            DisplayNatalChart(_currentChartData, _currentFontSize);
        }
        else
        {
            var divisionalData = _divisionalChartService.CalculateDivisionalChart(_currentChartData, division);
            DisplayDivisionalChartInternal(divisionalData, _currentChartData.BirthData.Name, _currentFontSize);
        }
    }

    private void DisplayNatalChart(ChartData chartData, double fontSize)
    {
        ClearChart();
        
        string rasiTitle = GetLocalizedChartTitle(1);
        ChartTitle.Text = rasiTitle;

        // 1. Calculate Ascendant Sign
        int ascSign = chartData.AscendantSign;
        
        // 2. Set Signs for each House (House 1 is fixed at top, Sign = Ascendant)
        // North Indian Chart: Houses are FIXED. Signs move.
        // House 1 (Top) has Sign = Ascendant Sign.
        // House 2 (Top Left) has Sign = Ascendant + 1.
        // ...
        for (int house = 1; house <= 12; house++)
        {
            int sign = (ascSign + house - 1 - 1) % 12 + 1; // 1-based math
            if (_signTextBlocks.TryGetValue(house, out var tb))
            {
                tb.Text = sign.ToString();
            }
        }

        // 3. Place Planets in Houses
        // Organize planets by House number (not Sign, but we derive House from Planet Sign)
        var planetsByHouse = new Dictionary<int, List<(string text, string type)>>();
        var aprakashByHouse = new Dictionary<int, List<string>>();
        for (int i = 1; i <= 12; i++) 
        {
            planetsByHouse[i] = new List<(string text, string type)>();
            aprakashByHouse[i] = new List<string>();
        }

        // Add "Lagna" label to House 1? Not necessary, House 1 is always Lagna in North Indian chart.
        // But maybe users want to see "As" or "La"? Usually "L" is written in House 1.
        planetsByHouse[1].Add(("Lagna", "lagna"));

        foreach (var p in chartData.Planets)
        {
            // Determine House Logic:
            // House = (PlanetSign - AscendantSign + 1)
            // Handle wrap around 12
            int house = (p.Sign - ascSign) + 1;
            if (house <= 0) house += 12;

            bool isStandardPlanet = ZodiacUtils.PlanetNames.ContainsKey(p.Planet) && 
                                   p.Name == ZodiacUtils.PlanetNames[p.Planet];
             
            if (isStandardPlanet)
            {
                 string abbr = GetLocalizedAbbr(p.Planet);
                 string planetDisplay = p.IsRetrograde ? $"({abbr})" : abbr;
                 string text = _hideDegrees ? planetDisplay : $"{planetDisplay} {(int)p.DegreeInSign}°"; // Degree usually after name in NI style? Or before? Doesn't matter.
                 
                 string type = (p.Planet == Models.Planet.Rahu || p.Planet == Models.Planet.Ketu) 
                    ? "rahuKetu" : "planet";

                 planetsByHouse[house].Add((text, type));
            }
            else
            {
                 string abbr = GetLocalizedAprakashAbbr(p.Name, p.Symbol);
                 string text = _hideDegrees ? abbr : $"{abbr} {(int)p.DegreeInSign}°";
                 aprakashByHouse[house].Add(text);
            }
        }

        RenderPlanets(planetsByHouse, aprakashByHouse, fontSize);
    }

    private void DisplayDivisionalChartInternal(DivisionalChartData divisionalData, string personName, double fontSize)
    {
        ClearChart();
        ChartTitle.Text = GetLocalizedChartTitle(divisionalData.Division);

        int ascSign = divisionalData.AscendantSign;

        // Set Signs
        for (int house = 1; house <= 12; house++)
        {
            int sign = (ascSign + house - 1 - 1) % 12 + 1;
            if (_signTextBlocks.TryGetValue(house, out var tb))
            {
                tb.Text = sign.ToString();
            }
        }

        var planetsByHouse = new Dictionary<int, List<(string text, string type)>>();
        var aprakashByHouse = new Dictionary<int, List<string>>();
        for (int i = 1; i <= 12; i++) 
        {
            planetsByHouse[i] = new List<(string text, string type)>();
            aprakashByHouse[i] = new List<string>();
        }

        planetsByHouse[1].Add(("Lagna", "lagna"));

        foreach (var p in divisionalData.Planets)
        {
            int house = (p.DivisionalSign - ascSign) + 1;
            if (house <= 0) house += 12;

            bool isStandardPlanet = ZodiacUtils.PlanetNames.ContainsKey(p.Planet) && 
                                   p.Name == ZodiacUtils.PlanetNames[p.Planet];
            
            if (isStandardPlanet)
            {
                string abbr = GetLocalizedAbbr(p.Planet);
                string planetDisplay = p.IsRetrograde ? $"({abbr})" : abbr;
                string text = _hideDegrees ? planetDisplay : $"{planetDisplay} {(int)p.DivisionalDegree}°";
                
                string type = (p.Planet == Models.Planet.Rahu || p.Planet == Models.Planet.Ketu) 
                    ? "rahuKetu" : "planet";

                planetsByHouse[house].Add((text, type));
            }
            else
            {
                string abbr = GetLocalizedAprakashAbbr(p.Name, p.Symbol);
                string text = _hideDegrees ? abbr : $"{abbr} {(int)p.DivisionalDegree}°";
                aprakashByHouse[house].Add(text);
            }
        }

        RenderPlanets(planetsByHouse, aprakashByHouse, fontSize);
    }

    private void RenderPlanets(Dictionary<int, List<(string text, string type)>> planetsByHouse, Dictionary<int, List<string>> aprakashByHouse, double fontSize)
    {
        foreach (var kvp in planetsByHouse)
        {
            int house = kvp.Key;
            var items = kvp.Value;
            var aprakashItems = aprakashByHouse[house];

            if (_planetTextBlocks.TryGetValue(house, out var textBlock))
            {
                textBlock.Inlines.Clear();
                textBlock.FontSize = fontSize;

                foreach (var (text, type) in items)
                {
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
                    textBlock.Inlines.Add(new System.Windows.Documents.Run(" "));
                }
            }

            if (_aprakashTextBlocks.TryGetValue(house, out var aprakashBlock))
            {
                aprakashBlock.Text = aprakashItems.Count > 0 ? string.Join(" ", aprakashItems) : "";
            }
        }
    }

    public void ClearChart()
    {
        foreach (var tb in _signTextBlocks.Values) tb.Text = "";
        foreach (var tb in _planetTextBlocks.Values) { tb.Inlines.Clear(); tb.Text = ""; }
        foreach (var tb in _aprakashTextBlocks.Values) tb.Text = "";
    }

    private void Control_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        // Bubble scroll wheel if needed
        e.Handled = false;
    }

    // Localization Helpers (Duplicated from SouthIndianChart - consider moving to Utils if frequent)
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
    
    private string GetLocalizedAprakashAbbr(string name, string symbol)
    {
        if (!ZodiacUtils.IsTamil) return symbol;
        
        return name switch
        {
            "Dhooma" => "தூம்",
            "Vyatipata" => "வியதி",
            "Parivesha" => "பரி",
            "Indrachapa" => "இந்",
            "Upaketu" => "உப",
            _ => symbol
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
