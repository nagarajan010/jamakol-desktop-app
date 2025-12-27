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
        var divisions = new Dictionary<int, string>
        {
            { 2, "Hora (D-2)" },
            { 3, "Drekkana (D-3)" },
            { 4, "Chaturthamsa (D-4)" },
            { 7, "Saptamsa (D-7)" },
            { 9, "Navamsa (D-9)" },
            { 10, "Dasamsa (D-10)" },
            { 12, "Dwadasamsa (D-12)" },
            { 16, "Shodasamsa (D-16)" },
            { 20, "Vimsamsa (D-20)" },
            { 24, "Siddhamsa (D-24)" },
            { 27, "Nakshatramsa (D-27)" },
            { 30, "Trimsamsa (D-30)" },
            { 40, "Khavedamsa (D-40)" },
            { 45, "Akshavedamsa (D-45)" },
            { 60, "Shashtiamsa (D-60)" }
        };

        foreach (var div in divisions)
        {
            var item = new MenuItem { Header = div.Value, Tag = div.Key };
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
        DeityColumn.Header = $"In whose amsa in D-{_currentDivision}";

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
            PlanetName = "Lagna",
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
                PlanetName = p.Name,
                DivisionSign = deityInfo.PartNumber.ToString(),
                Index = deityInfo.DeityIndex.ToString(),
                Deity = deityInfo.Deity
            });
        }

        DevataGrid.ItemsSource = items;
    }
}

public class DevataViewItem
{
    public string PlanetName { get; set; } = "";
    public string DivisionSign { get; set; } = ""; // The sign number in the Varga
    public string Index { get; set; } = "";        // The part number
    public string Deity { get; set; } = "";
}
