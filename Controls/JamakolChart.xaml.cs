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
    private readonly Dictionary<int, TextBlock> _aprakashTextBlocks;
    private readonly Dictionary<int, TextBlock> _cornerBoxTextBlocks;
    private readonly Dictionary<int, System.Windows.Controls.Border> _cornerBoxBorders;

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

        // Map sign numbers to Aprakash TextBlock controls (bottom-left)
        _aprakashTextBlocks = new Dictionary<int, TextBlock>
        {
            { 1, Aprakash1 }, { 2, Aprakash2 }, { 3, Aprakash3 }, { 4, Aprakash4 },
            { 5, Aprakash5 }, { 6, Aprakash6 }, { 7, Aprakash7 }, { 8, Aprakash8 },
            { 9, Aprakash9 }, { 10, Aprakash10 }, { 11, Aprakash11 }, { 12, Aprakash12 }
        };

        // Map sign numbers to corner box TextBlocks
        _cornerBoxTextBlocks = new Dictionary<int, TextBlock>
        {
            { 1, Box1 }, { 2, Box2 }, { 3, Box3 }, { 4, Box4 },
            { 5, Box5 }, { 6, Box6 }, { 7, Box7 }, { 8, Box8 },
            { 9, Box9 }, { 10, Box10 }, { 11, Box11 }, { 12, Box12 }
        };

        // Map sign numbers to corner box Borders (for visibility control)
        _cornerBoxBorders = new Dictionary<int, System.Windows.Controls.Border>
        {
            { 1, Border1 }, { 2, Border2 }, { 3, Border3 }, { 4, Border4 },
            { 5, Border5 }, { 6, Border6 }, { 7, Border7 }, { 8, Border8 },
            { 9, Border9 }, { 10, Border10 }, { 11, Border11 }, { 12, Border12 }
        };
    }

    /// <summary>
    /// Update chart display with Jamakol data, Jama Grahas, and Special Points
    /// </summary>
    public void UpdateChart(JamakolData jamakolData, List<JamaGrahaPosition>? jamaGrahas = null, List<SpecialPoint>? specialPoints = null, double chartFontSize = 14, double jamaGrahaFontSize = 10, string? vedicDayLord = null, bool useFixedSignBoxes = false)
    {
        // Clear all cells
        ClearAllCells();

        // Update center info
        var birthData = jamakolData.ChartData.BirthData;
        ChartTitle.Text = !string.IsNullOrEmpty(birthData.Name) ? birthData.Name : "Jamakol Chart";
        DateTimeLabel.Text = $"{birthData.BirthDateTime:dd-MM-yyyy HH:mm}";
        
        // Show real ascendant from chart data
        int ascSign = jamakolData.ChartData.AscendantSign;
        string ascName = ZodiacUtils.GetSignName(ascSign);
        AscendantLabel.Text = $"Ascendant: {ascName}";

        // Show day lord (use passed Vedic day lord or calculate from civil date as fallback)
        // Show day lord (use passed Vedic day lord or calculate from civil date as fallback)
        string dayLordEn = vedicDayLord ?? JamaGrahaCalculator.GetDayLord(birthData.BirthDateTime.DayOfWeek);
        string dayLord = dayLordEn;
        if (ZodiacUtils.IsTamil && Enum.TryParse<Models.Planet>(dayLordEn, true, out var dayLordPlanet))
        {
            dayLord = ZodiacUtils.GetPlanetName(dayLordPlanet);
        }
        
        string labelAsc = ZodiacUtils.IsTamil ? "லக்னம்" : "Ascendant";
        string labelDay = ZodiacUtils.IsTamil ? "நாள் அதிபதி" : "Day Lord";

        AscendantLabel.Text = $"{labelAsc}: {ascName}";
        DayLordLabel.Text = $"{labelDay}: {dayLord}";

        // Build display content for each sign: planets + special points with colors
        // We'll store structured tuples instead of strings
        var displayBySign = new Dictionary<int, List<(string text, string type)>>();
        for (int i = 1; i <= 12; i++)
        {
            displayBySign[i] = new List<(string text, string type)>();
        }

        // Show "Lagna" marker at the real ascendant sign
        string lagnaText = ZodiacUtils.IsTamil ? "லக்னம்" : "Lagna";
        displayBySign[ascSign].Insert(0, (lagnaText, "lagna"));

        // Prepare for aprakash grahas
        var aprakashBySign = new Dictionary<int, List<string>>();
        for (int i = 1; i <= 12; i++)
        {
            aprakashBySign[i] = new List<string>();
        }

        // Add regular planets (type = "planet" or "rahuKetu") - separate aprakash
        foreach (var planet in jamakolData.PlanetPositions)
        {
            int degree = (int)Math.Floor(planet.DegreeInSign);
            
            // Check if this is a standard planet (Name matches expected) or Aprakash graha
            bool isStandardPlanet = ZodiacUtils.PlanetNames.ContainsKey(planet.Planet) && 
                                   planet.EnglishName == ZodiacUtils.PlanetNames[planet.Planet];
            
            if (isStandardPlanet)
            {
                string abbr = GetPlanetAbbreviation(planet.Planet);
                string retro = planet.IsRetrograde ? " R" : "";
                
                // Rahu and Ketu get special orange color with parentheses
                string type = (planet.Planet == Models.Planet.Rahu || planet.Planet == Models.Planet.Ketu) 
                    ? "rahuKetu" : "planet";
                
                // Format: "12° Su" for all planets
                string displayText = $"{degree}° {abbr}{retro}";
                
                displayBySign[planet.Sign].Add((displayText, type));
            }
            else
            {
                // Aprakash graha - collect for bottom-left display
                string abbr = GetLocalizedSymbol(planet.Symbol); 
                string text = $"{abbr}";
                aprakashBySign[planet.Sign].Add(text);
            }
        }

        // Add special points (AR, UD, KV) - type = "special"
        if (specialPoints != null)
        {
            foreach (var sp in specialPoints)
            {
                int house = sp.SignIndex + 1; // SignIndex is 0-based
                // Format: "12° UD" - just degree and symbol
                // Format: "12° UD" - just degree and symbol
                int degree = (int)Math.Floor(sp.AbsoluteLongitude % 30);
                string localizedSymbol = GetLocalizedSymbol(sp.Symbol);
                string displayText = $"{degree}° {localizedSymbol}";
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

            // Populate Aprakash graha in dedicated bottom-left TextBlock
            var aprakashItems = aprakashBySign[sign];
            if (_aprakashTextBlocks.TryGetValue(sign, out var aprakashBlock))
            {
                aprakashBlock.Text = aprakashItems.Count > 0 ? string.Join(" ", aprakashItems) : "";
            }
        }

        // Jama Grahas are displayed ONLY in corner boxes (OUTSIDE the chart)
        if (jamaGrahas != null && jamaGrahas.Count > 0)
        {
            // Update corner boxes with Jama Graha planet positions
            UpdateCornerBoxes(jamaGrahas, jamaGrahaFontSize, useFixedSignBoxes);
        }
        else
        {
            // No jama grahas - hide all boxes if useFixedSignBoxes is true
            if (useFixedSignBoxes)
            {
                foreach (var border in _cornerBoxBorders.Values)
                {
                    border.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }
    }


    private void UpdateCornerBoxes(List<JamaGrahaPosition> jamaGrahas, double jamaGrahaFontSize, bool useFixedSignBoxes)
    {
        // Original 8 boxes that were always shown (positions: 1,3,4,6,7,9,10,12)
        var originalBoxes = new HashSet<int> { 1, 3, 4, 6, 7, 9, 10, 12 };

        // Loop through all 12 box positions
        for (int boxNum = 1; boxNum <= 12; boxNum++)
        {
            var border = _cornerBoxBorders[boxNum];
            var textBlock = _cornerBoxTextBlocks[boxNum];

            if (useFixedSignBoxes)
            {
                // Fixed sign mode: show planet in its SIGN position
                // Find jama graha with this sign
                var graha = jamaGrahas.FirstOrDefault(g => g.Sign == boxNum);
                
                if (graha != null)
                {
                    border.Visibility = System.Windows.Visibility.Visible;
                    PopulateCornerBox(textBlock, graha, jamaGrahaFontSize);
                }
                else
                {
                    border.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
            else
            {
                // Original mode: show planet in its HOUSE position (default boxes)
                // Only show the 8 original boxes (1,3,4,6,7,9,10,12)
                if (originalBoxes.Contains(boxNum))
                {
                    border.Visibility = System.Windows.Visibility.Visible;
                    
                    // Find graha by HOUSE position (not sign)
                    var graha = jamaGrahas.FirstOrDefault(g => g.House == boxNum);
                    
                    if (graha != null)
                    {
                        PopulateCornerBox(textBlock, graha, jamaGrahaFontSize);
                    }
                    else
                    {
                        // Show default box number when no graha at this house
                        textBlock.Inlines.Clear();
                        textBlock.FontSize = jamaGrahaFontSize;
                        var run = new System.Windows.Documents.Run(boxNum.ToString());
                        run.FontWeight = System.Windows.FontWeights.Bold;
                        textBlock.Inlines.Add(run);
                    }
                }
                else
                {
                    // Hide boxes 2, 5, 8, 11
                    border.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }
    }

    private void PopulateCornerBox(TextBlock tb, JamaGrahaPosition graha, double fontSize)
    {
        tb.Inlines.Clear();
        tb.FontSize = fontSize;

        // 1. Symbol (Bold, Black)
        string locSymbol = GetLocalizedSymbol(graha.Symbol); // Expects abbreviation like "Su", "Mo" or "Sn"
        // If symbol is full name "Sn" (Snake) or "Su" need to handle
        // Graha.Symbol comes from JamaGrahaCalculator which returns "Su", "Mo", "Sn"
        
        var symbolRun = new System.Windows.Documents.Run(locSymbol);
        symbolRun.Foreground = new SolidColorBrush(Colors.Black);
        symbolRun.FontWeight = System.Windows.FontWeights.Bold;
        tb.Inlines.Add(symbolRun);

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
        // 3. Sign Name in Parentheses (Normal, Red)
        // Format: (Aries)
        string signName = ZodiacUtils.GetSignName(graha.Sign); // Use sign index properly if available, or name
        // JamaGrahaPosition has Sign (int) and SignName (string). Use Sign (int) with utility.
        
        var signRun = new System.Windows.Documents.Run($"({signName})");
        signRun.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 0, 0)); // Red
        tb.Inlines.Add(signRun);
    }

    private void ClearAllCells()
    {
        // Clear planet textblocks
        foreach (var tb in _planetTextBlocks.Values)
        {
            tb.Text = string.Empty;
        }
        
        // Clear aprakash textblocks
        foreach (var tb in _aprakashTextBlocks.Values)
        {
            tb.Text = string.Empty;
        }
        
        // Clear all corner boxes
        foreach (var tb in _cornerBoxTextBlocks.Values)
        {
            tb.Inlines.Clear();
        }
    }

    private string GetPlanetAbbreviation(Models.Planet planet)
    {
        if (ZodiacUtils.IsTamil)
        {
             return planet switch
             {
                 Models.Planet.Sun => "சூரி",
                 Models.Planet.Moon => "சந்",
                 Models.Planet.Mars => "செவ்",
                 Models.Planet.Mercury => "புத",
                 Models.Planet.Jupiter => "குரு",
                 Models.Planet.Venus => "சுக்",
                 Models.Planet.Saturn => "சனி",
                 Models.Planet.Rahu => "ராகு",
                 Models.Planet.Ketu => "கேது",
                 _ => ""
             };
        }

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
    private string GetLocalizedSymbol(string symbol)
    {
        if (!ZodiacUtils.IsTamil) return symbol;
        
        // Handle Jama Graha Symbols: Su, Mo, Ma, Me, Ju, Ve, Sa, Sn (Snake)
        // Handle Special Points: UD, AR, KV, YK, MR, MA, RK
        // Handle Aprakash: Dh, Vy, Pa, In, Uk
        return symbol switch
        {
            "Su" => "சூரி",
            "Mo" => "சந்",
            "Ma" => "செவ்",
            "Me" => "புத",
            "Ju" => "குரு",
            "Ve" => "சுக்",
            "Sa" => "சனி",
            "Sn" => "பாம்பு", // Snake
            
            "UD" => "உத",
            "AR" => "ஆரு",
            "KV" => "கவி",
            "YK" => "எம",
            "MR" => "மிரு",
            "MA" => "மா",
            "RK" => "ராகு", // Rahu Kalam
            
            "Dh" => "தூ",
            "Vy" => "வ", // Vyatipat
            "Pa" => "ப", // Parivesh or Pata (check context)
            "In" => "இ",
            "Uk" => "உ",
            "Ka" => "கா",
            
            _ => symbol
        };
    }
}
