using System.Windows.Controls;
using System.Windows.Media;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology.Controls;

/// <summary>
/// Jamakol Chart Control - displays transit planets, special points, and Jama Graha corner boxes
/// </summary>
public partial class JamakolChart : UserControl
{
    private readonly Dictionary<int, TextBlock> _planetTextBlocks;

    public JamakolChart()
    {
        InitializeComponent();

        // Map sign numbers to their planet TextBlock controls (transit planets in center)
        _planetTextBlocks = new Dictionary<int, TextBlock>
        {
            { 1, Planets1 }, { 2, Planets2 }, { 3, Planets3 }, { 4, Planets4 },
            { 5, Planets5 }, { 6, Planets6 }, { 7, Planets7 }, { 8, Planets8 },
            { 9, Planets9 }, { 10, Planets10 }, { 11, Planets11 }, { 12, Planets12 }
        };
    }

    /// <summary>
    /// Update chart display with Jamakol data, Jama Grahas, and Special Points
    /// </summary>
    public void UpdateChart(JamakolData jamakolData, List<JamaGrahaPosition>? jamaGrahas = null, List<SpecialPoint>? specialPoints = null, double chartFontSize = 14, double jamaGrahaFontSize = 10, string? vedicDayLord = null)
    {
        // Clear all cells
        ClearAllCells();

        // Update center info
        var birthData = jamakolData.ChartData.BirthData;
        ChartTitle.Text = !string.IsNullOrEmpty(birthData.Name) ? birthData.Name : "Jamakol Chart";
        DateTimeLabel.Text = $"{birthData.BirthDateTime:dd-MM-yyyy HH:mm}";
        
        // Show real ascendant from chart data
        int ascSign = jamakolData.ChartData.AscendantSign;
        string ascName = ZodiacUtils.SignNames[ascSign];
        AscendantLabel.Text = $"Ascendant: {ascName}";

        // Show day lord (use passed Vedic day lord or calculate from civil date as fallback)
        string dayLord = vedicDayLord ?? JamaGrahaCalculator.GetDayLord(birthData.BirthDateTime.DayOfWeek);
        DayLordLabel.Text = $"Day Lord: {dayLord}";

        // Build display content for each sign: planets + special points with colors
        // We'll store structured tuples instead of strings
        var displayBySign = new Dictionary<int, List<(string text, string type)>>();
        for (int i = 1; i <= 12; i++)
        {
            displayBySign[i] = new List<(string text, string type)>();
        }

        // Show "Lagna" marker at the real ascendant sign
        displayBySign[ascSign].Insert(0, ("Lagna", "lagna"));

        // Add regular planets (type = "planet" or "rahuKetu")
        foreach (var planet in jamakolData.PlanetPositions)
        {
            int degree = (int)Math.Floor(planet.DegreeInSign);
            string abbr = GetPlanetAbbreviation(planet.Planet);
            string retro = planet.IsRetrograde ? " R" : "";
            
            // Rahu and Ketu get special orange color with parentheses
            string type = (planet.Planet == Models.Planet.Rahu || planet.Planet == Models.Planet.Ketu) 
                ? "rahuKetu" : "planet";
            
            // Format: "12° Su" for all planets
            string displayText = $"{degree}° {abbr}{retro}";
            
            displayBySign[planet.Sign].Add((displayText, type));
        }

        // Add special points (AR, UD, KV) - type = "special"
        if (specialPoints != null)
        {
            foreach (var sp in specialPoints)
            {
                int house = sp.SignIndex + 1; // SignIndex is 0-based
                // Format: "12° UD" - just degree and symbol
                int degree = (int)Math.Floor(sp.AbsoluteLongitude % 30);
                string displayText = $"{degree}° {sp.Symbol}";
                displayBySign[house].Add((displayText, "special"));
            }
        }

        // Update TextBlocks with colored content
        foreach (var kvp in displayBySign)
        {
            int sign = kvp.Key;
            var items = kvp.Value;

            if (_planetTextBlocks.TryGetValue(sign, out var textBlock) && items.Count > 0)
            {
                textBlock.Inlines.Clear();
                textBlock.FontSize = chartFontSize;
                
                for (int i = 0; i < items.Count; i++)
                {
                    var (text, type) = items[i];
                    
                    var run = new System.Windows.Documents.Run(text);
                    run.Foreground = type switch
                    {
                        "planet" => new SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 0, 0)),     // Red #cc0000
                        "rahuKetu" => new SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 0, 0)), // Orange #ff9900
                        "special" => new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 153, 0)),  // Blue #0066cc
                        "lagna" => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 153, 0)),      // Green #009900
                        _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0))
                    };
                    
                    // Make Lagna bold
                    if (type == "lagna")
                    {
                        run.FontWeight = System.Windows.FontWeights.Bold;
                    }
                    
                    textBlock.Inlines.Add(run);
                    
                    if (i < items.Count - 1)
                    {
                        textBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                    }
                }
            }
        }

        // Jama Grahas are displayed ONLY in corner boxes (OUTSIDE the chart)
        if (jamaGrahas != null && jamaGrahas.Count > 0)
        {
            // Update corner boxes with Jama Graha planet positions
            UpdateCornerBoxes(jamaGrahas, jamaGrahaFontSize);
        }
    }


    private void UpdateCornerBoxes(List<JamaGrahaPosition> jamaGrahas, double jamaGrahaFontSize)
    {
        // Define the mapping: Label (e.g. "Ve") -> (House Index, Default Symbol, TextBlock Control)
        // Ve box -> Pisces (12)
        // Me box -> Aries (1)
        // Ju box -> Gemini (3)
        // Sa box -> Capricorn (10)
        // Ma box -> Cancer (4)
        // Mo box -> Sagittarius (9)
        // Sn box -> Libra (7)
        // Su box -> Virgo (6)
        
        var boxMap = new List<(int House, string DefaultSymbol, TextBlock TextBlock)>
        {
            (12, "Ve", VeText),
            (1, "Me", MeText),
            (3, "Ju", JuText),
            (10, "Sa", SaText),
            (4, "Ma", MaText),
            (9, "Mo", MoText),
            (7, "Sn", SnText),
            (6, "Su", SuText)
        };

        foreach (var (house, defaultSymbol, tb) in boxMap)
        {
            tb.Inlines.Clear();
            tb.FontSize = jamaGrahaFontSize;

            var graha = jamaGrahas.FirstOrDefault(g => g.House == house);
            
            // 1. Symbol (Bold, Black)
            var symbolRun = new System.Windows.Documents.Run(graha?.Symbol ?? defaultSymbol);
            symbolRun.Foreground = new SolidColorBrush(Colors.Black);
            symbolRun.FontWeight = System.Windows.FontWeights.Bold;
            tb.Inlines.Add(symbolRun);

            if (graha != null)
            {
                tb.Inlines.Add(new System.Windows.Documents.LineBreak());

                // 2. Degree (Normal, Red)
                // Format: 16°16'
                string degreeText = $"{(int)graha.Degree}°{(int)graha.DegreeInSign:D2}'";
                var degreeRun = new System.Windows.Documents.Run(degreeText);
                degreeRun.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 0, 0)); // Red
                tb.Inlines.Add(degreeRun);

                tb.Inlines.Add(new System.Windows.Documents.LineBreak());

                // 3. Sign Name in Parentheses (Normal, Red)
                // Format: (Aries)
                var signRun = new System.Windows.Documents.Run($"({graha.SignName})");
                signRun.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 0, 0)); // Red
                tb.Inlines.Add(signRun);
            }
        }
    }

    private void ClearAllCells()
    {
        // Clear planet textblocks
        foreach (var tb in _planetTextBlocks.Values)
        {
            tb.Text = string.Empty;
        }
        
        // Clear corner boxes
        var cornerBoxes = new[] { VeText, MeText, JuText, SaText, MaText, MoText, SnText, SuText };
        var defaults = new[] { "Ve", "Me", "Ju", "Sa", "Ma", "Mo", "Sn", "Su" };
        
        for (int i = 0; i < cornerBoxes.Length; i++)
        {
            cornerBoxes[i].Inlines.Clear();
            var run = new System.Windows.Documents.Run(defaults[i]);
            run.FontWeight = System.Windows.FontWeights.Bold;
            cornerBoxes[i].Inlines.Add(run);
        }
    }

    private string GetPlanetAbbreviation(Models.Planet planet)
    {
        return planet switch
        {
            Models.Planet.Sun => "Su",
            Models.Planet.Moon => "Mo",
            Models.Planet.Mars => "Ma",
            Models.Planet.Mercury => "Me",
            Models.Planet.Jupiter => "Ju",
            Models.Planet.Venus => "Ve",
            Models.Planet.Saturn => "Sa",
            Models.Planet.Rahu => "Ra",
            Models.Planet.Ketu => "Ke",
            _ => ""
        };
    }

    /// <summary>
    /// Clear the chart display
    /// </summary>
    public void ClearChart()
    {
        ClearAllCells();
        ChartTitle.Text = "Jamakol Chart";
        DateTimeLabel.Text = "";
        AscendantLabel.Text = "Ascendant:";
        DayLordLabel.Text = "Day Lord:";
    }
}
