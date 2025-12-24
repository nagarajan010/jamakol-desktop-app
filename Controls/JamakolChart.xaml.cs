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
        // Corner boxes show the Jama Graha planet that is IN that position/house
        // Each corner box is positioned near a specific house:
        // - Ve box (top-left) → Pisces (12) position
        // - Me box (top-2) → Aries (1) position  
        // - Ju box (top-right) → Gemini (3) position
        // - Sa box (left) → Capricorn (10) position
        // - Ma box (right) → Cancer (4) position
        // - Mo box (bottom-left) → Sagittarius (9) position
        // - Sn box (bottom-center) → Libra (7) position
        // - Su box (bottom-right) → Virgo (6) position

        // Find planet for each house and display in corresponding corner box
        var piscesGraha = jamaGrahas.FirstOrDefault(g => g.House == 12);   // Pisces
        var ariesGraha = jamaGrahas.FirstOrDefault(g => g.House == 1);     // Aries
        var geminiGraha = jamaGrahas.FirstOrDefault(g => g.House == 3);    // Gemini
        var capGraha = jamaGrahas.FirstOrDefault(g => g.House == 10);      // Capricorn
        var cancerGraha = jamaGrahas.FirstOrDefault(g => g.House == 4);    // Cancer
        var sagGraha = jamaGrahas.FirstOrDefault(g => g.House == 9);       // Sagittarius
        var libraGraha = jamaGrahas.FirstOrDefault(g => g.House == 7);     // Libra
        var virgoGraha = jamaGrahas.FirstOrDefault(g => g.House == 6);     // Virgo

        // Apply font size to all corner box elements
        VeLabel.FontSize = jamaGrahaFontSize; VeValue.FontSize = jamaGrahaFontSize;
        MeLabel.FontSize = jamaGrahaFontSize; MeValue.FontSize = jamaGrahaFontSize;
        JuLabel.FontSize = jamaGrahaFontSize; JuValue.FontSize = jamaGrahaFontSize;
        SaLabel.FontSize = jamaGrahaFontSize; SaValue.FontSize = jamaGrahaFontSize;
        MaLabel.FontSize = jamaGrahaFontSize; MaValue.FontSize = jamaGrahaFontSize;
        MoLabel.FontSize = jamaGrahaFontSize; MoValue.FontSize = jamaGrahaFontSize;
        SnLabel.FontSize = jamaGrahaFontSize; SnValue.FontSize = jamaGrahaFontSize;
        SuLabel.FontSize = jamaGrahaFontSize; SuValue.FontSize = jamaGrahaFontSize;

        // Update corner box labels and values based on which planet is there
        // Pisces corner (Ve box position)
        VeLabel.Text = piscesGraha?.Symbol ?? "Ve";
        VeValue.Text = piscesGraha != null ? $"{(int)piscesGraha.Degree}°{(int)piscesGraha.DegreeInSign:D2}'\n({piscesGraha.SignName}°)" : "";


        // Aries corner (Me box position)
        MeLabel.Text = ariesGraha?.Symbol ?? "Me";
        MeValue.Text = ariesGraha != null ? $"{(int)ariesGraha.Degree}°{(int)ariesGraha.DegreeInSign:D2}'\n({ariesGraha.SignName}°)" : "";

        // Gemini corner (Ju box position)
        JuLabel.Text = geminiGraha?.Symbol ?? "Ju";
        JuValue.Text = geminiGraha != null ? $"{(int)geminiGraha.Degree}°{(int)geminiGraha.DegreeInSign:D2}'\n({geminiGraha.SignName}°)" : "";

        // Capricorn side (Sa box position)
        SaLabel.Text = capGraha?.Symbol ?? "Sa";
        SaValue.Text = capGraha != null ? $"{(int)capGraha.Degree}°{(int)capGraha.DegreeInSign:D2}'\n({capGraha.SignName}°)" : "";

        // Cancer side (Ma box position)
        MaLabel.Text = cancerGraha?.Symbol ?? "Ma";
        MaValue.Text = cancerGraha != null ? $"{(int)cancerGraha.Degree}°{(int)cancerGraha.DegreeInSign:D2}'\n({cancerGraha.SignName}°)" : "";

        // Sagittarius corner (Mo box position)
        MoLabel.Text = sagGraha?.Symbol ?? "Mo";
        MoValue.Text = sagGraha != null ? $"{(int)sagGraha.Degree}°{(int)sagGraha.DegreeInSign:D2}'\n({sagGraha.SignName}°)" : "";

        // Libra corner (Sn box position)
        SnLabel.Text = libraGraha?.Symbol ?? "Sn";
        SnValue.Text = libraGraha != null ? $"{(int)libraGraha.Degree}°{(int)libraGraha.DegreeInSign:D2}'\n({libraGraha.SignName}°)" : "";

        // Virgo corner (Su box position)
        SuLabel.Text = virgoGraha?.Symbol ?? "Su";
        SuValue.Text = virgoGraha != null ? $"{(int)virgoGraha.Degree}°{(int)virgoGraha.DegreeInSign:D2}'\n({virgoGraha.SignName}°)" : "";    }

    private void ClearAllCells()
    {
        // Clear planet textblocks
        foreach (var tb in _planetTextBlocks.Values)
        {
            tb.Text = string.Empty;
        }
        
        // Clear corner values and reset labels
        VeLabel.Text = "Ve"; VeValue.Text = "";
        MeLabel.Text = "Me"; MeValue.Text = "";
        JuLabel.Text = "Ju"; JuValue.Text = "";
        SaLabel.Text = "Sa"; SaValue.Text = "";
        MaLabel.Text = "Ma"; MaValue.Text = "";
        MoLabel.Text = "Mo"; MoValue.Text = "";
        SnLabel.Text = "Sn"; SnValue.Text = "";
        SuLabel.Text = "Su"; SuValue.Text = "";
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
