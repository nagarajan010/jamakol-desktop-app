using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology.Controls;

public partial class AmshaDevataPanel : UserControl
{
    private ChartData? _currentChart;
    private int _currentDivision = 9; // Default to D-9 Navamsa

    public AmshaDevataPanel()
    {
        InitializeComponent();
        InitializeContextMenu();
        
        // Apply font size from settings
        var settings = AppSettings.Load();
        if (settings != null)
        {
            DevataGrid.FontSize = settings.TableFontSize;
        }
    }

    private void InitializeContextMenu()
    {
        // Define divisions suitable for localization
        // Key is D-Num, Value is English base name
        var divisions = new Dictionary<int, string>
        {
            { 2, "Hora" }, { 3, "Drekkana" }, { 4, "Chaturthamsa" },
            { 7, "Saptamsa" }, { 9, "Navamsa" }, { 10, "Dasamsa" },
            { 12, "Dwadasamsa" }, { 16, "Shodasamsa" }, { 20, "Vimsamsa" },
            { 24, "Siddhamsa" }, { 27, "Nakshatramsa" }, { 30, "Trimsamsa" },
            { 40, "Khavedamsa" }, { 45, "Akshavedamsa" }, { 60, "Shashtiamsa" }
        };
        
        VargaContextMenu.Items.Clear();
        foreach (var div in divisions)
        {
            string name = div.Value; 
            // Localize known names
            if (ZodiacUtils.IsTamil)
            {
                 name = name switch {
                     "Hora" => "ஹோரா", "Drekkana" => "திரேக்காணம்", "Chaturthamsa" => "சதுர்த்தாம்சம்",
                     "Saptamsa" => "சப்தாம்சம்", "Navamsa" => "நவாம்சம்", "Dasamsa" => "தசாம்சம்",
                     "Dwadasamsa" => "துவாதாம்சம்", "Shodasamsa" => "ஷோடசாம்சம்", "Vimsamsa" => "விம்சாம்சம்",
                     "Siddhamsa" => "சித்தாம்சம்", "Nakshatramsa" => "நட்சத்திராம்சம்", "Trimsamsa" => "திரிம்சாம்சம்",
                     "Khavedamsa" => "கவேதாம்சம்", "Akshavedamsa" => "அட்சவேதாம்சம்", "Shashtiamsa" => "சஷ்டியாம்சம்",
                     _ => name
                 };
            }
            
            var item = new MenuItem { Header = $"{name} (D-{div.Key})", Tag = div.Key };
            item.Click += VargaMenuItem_Click;
            VargaContextMenu.Items.Add(item);
        }
    }

    private void VargaMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem item && item.Tag is int div)
        {
            _currentDivision = div;
            VargaNameText.Text = item.Header.ToString();
            RefreshGrid();
        }
    }

    public void UpdateChart(ChartData? chart, double fontSize = 11)
    {
        _currentChart = chart;
        
        // Enforce settings
        var settings = AppSettings.Load();
        if (settings != null) fontSize = settings.TableFontSize;
        
        DevataGrid.FontSize = fontSize;
        RefreshGrid();
    }

    private readonly DivisionalChartService _divService = new();

    private void RefreshGrid()
    {
        if (_currentChart == null)
        {
            DevataGrid.ItemsSource = null;
            return;
        }
        
        // Update Deity Column Header
        // Update Deity Column Header (Done in LocalizeHeaders at end or here if dynamic)
        // Ensure localize headers call uses current division
        // DeityColumn.Header = $"In whose amsa in D-{_currentDivision}";

        // Update Varga name with localized version
        VargaNameText.Text = GetLocalizedVargaName(_currentDivision);
        
        var items = new List<DevataViewItem>();
        
        // Calculate the specific divisional chart to get accurate division signs
        var divChartData = _divService.CalculateDivisionalChart(_currentChart, _currentDivision);

        // 1. Add Lagna (Ascendant)
        var lagnaDeityInfo = AmshaDevataCalculator.GetDeity(
            _currentDivision, 
            _currentChart.AscendantDegree, 
            _currentChart.AscendantSign
        );
        
        items.Add(new DevataViewItem
        {
            PlanetName = ZodiacUtils.IsTamil ? "லக்னம்" : "Lagna",
            DivisionSign = lagnaDeityInfo.PartNumber.ToString(), 
            Index = lagnaDeityInfo.DeityIndex.ToString(),
            Deity = lagnaDeityInfo.Deity
        });

        // 2. Add Planets
        // Define sort order
        var sortOrder = new List<string> { "Sun", "Moon", "Mars", "Mercury", "Jupiter", "Venus", "Saturn", "Rahu", "Ketu" };
        
        // Sort planets: defined ones first in order, then others (Upagrahas)
        var sortedPlanets = _currentChart.Planets
            .OrderBy(p => {
                int index = sortOrder.IndexOf(p.Name);
                return index == -1 ? 999 : index;
            })
            .ToList();

        foreach (var p in sortedPlanets)
        {
            var deityInfo = AmshaDevataCalculator.GetDeity(
                _currentDivision, 
                p.Longitude, 
                p.Sign
            );
            
            // Note: User wants "Division" column to show the Part Number (e.g. 1st Navamsa), not the Sign Number.
            // And "Index" column to show the Deity Index.
            
            items.Add(new DevataViewItem
            {
                PlanetName = ZodiacUtils.IsTamil ? ZodiacUtils.GetPlanetName(p.Planet) : p.Name,
                DivisionSign = deityInfo.PartNumber.ToString(),
                Index = deityInfo.DeityIndex.ToString(),
                Deity = deityInfo.Deity
            });
        }

        DevataGrid.ItemsSource = items;
        LocalizeHeaders();
    }
    
    private void LocalizeHeaders()
    {
        bool isTa = ZodiacUtils.IsTamil;
        if (DevataGrid.Columns.Count >= 4)
        {
            DevataGrid.Columns[0].Header = isTa ? "கிரகம்" : "Body";
            DevataGrid.Columns[1].Header = isTa ? "வர்க்கம்" : "Division"; // Or Part
            DevataGrid.Columns[2].Header = isTa ? "எண்" : "Index";
            // Column 3 header is dynamic, handled in RefreshGrid (DeityColumn)
            DeityColumn.Header = isTa 
                ? $"யாருடைய அம்சம் (D-{_currentDivision})" 
                : $"In whose amsa in D-{_currentDivision}";
        }
    }
    private string GetLocalizedVargaName(int div)
    {
        string baseName = div switch {
            2 => "Hora", 3 => "Drekkana", 4 => "Chaturthamsa", 7 => "Saptamsa", 9 => "Navamsa",
            10 => "Dasamsa", 12 => "Dwadasamsa", 16 => "Shodasamsa", 20 => "Vimsamsa",
            24 => "Siddhamsa", 27 => "Nakshatramsa", 30 => "Trimsamsa", 40 => "Khavedamsa",
            45 => "Akshavedamsa", 60 => "Shashtiamsa", _ => "Varga"
        };
        
        string name = baseName;
        if (ZodiacUtils.IsTamil)
        {
             name = baseName switch {
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

public class DevataViewItem
{
    public string PlanetName { get; set; } = "";
    public string DivisionSign { get; set; } = ""; // The sign number in the Varga
    public string Index { get; set; } = "";        // The part number
    public string Deity { get; set; } = "";
}
