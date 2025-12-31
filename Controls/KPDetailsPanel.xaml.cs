using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology.Controls;

public partial class KPDetailsPanel : UserControl
{
    // Event raised when a transit event row is selected
    public event EventHandler<DateTime>? TransitSelected;

    public KPDetailsPanel()
    {
        InitializeComponent();
        
        // Apply font size from settings
        var settings = AppSettings.Load();
        if (settings != null)
        {
            KPGrid.FontSize = settings.TableFontSize;
        }

        // Initialize Body Selector
        var bodies = new List<BodySelectorItem>
        {
            new BodySelectorItem { Name = "All / அனைத்தும்", Id = null },
            new BodySelectorItem { Name = "Lagna (Asc)", Id = -1 },
            new BodySelectorItem { Name = "Sun", Id = SwissEphNet.SwissEph.SE_SUN },
            new BodySelectorItem { Name = "Moon", Id = SwissEphNet.SwissEph.SE_MOON },
            new BodySelectorItem { Name = "Mars", Id = SwissEphNet.SwissEph.SE_MARS },
            new BodySelectorItem { Name = "Mercury", Id = SwissEphNet.SwissEph.SE_MERCURY },
            new BodySelectorItem { Name = "Jupiter", Id = SwissEphNet.SwissEph.SE_JUPITER },
            new BodySelectorItem { Name = "Venus", Id = SwissEphNet.SwissEph.SE_VENUS },
            new BodySelectorItem { Name = "Saturn", Id = SwissEphNet.SwissEph.SE_SATURN },
            new BodySelectorItem { Name = "Rahu", Id = SwissEphNet.SwissEph.SE_MEAN_NODE },
            new BodySelectorItem { Name = "Ketu", Id = SwissEphNet.SwissEph.SE_TRUE_NODE }
        };
        
        BodySelector.ItemsSource = bodies;
        BodySelector.DisplayMemberPath = "Name";
        BodySelector.SelectedValuePath = "Id";
        BodySelector.SelectedIndex = 0;
    }

    private class BodySelectorItem
    {
        public string Name { get; set; } = "";
        public int? Id { get; set; }
    }

    private ChartData? _currentChart;

    public void UpdateChart(ChartData? chart)
    {
        _currentChart = chart;
        
        if (chart == null)
        {
            KPGrid.ItemsSource = null;
            return;
        }
        
        // Ensure DatePickers have default values if not set
        if (!StartDatePicker.SelectedDate.HasValue)
        {
            StartDatePicker.SelectedDate = chart.BirthData.BirthDateTime.Date;
            EndDatePicker.SelectedDate = chart.BirthData.BirthDateTime.Date.AddDays(1);
        }
        
        var items = new List<KPViewItem>();
        // ... (lines 26-38 assumed unchanged logic)
        
        // Add Lagna (Ascendant)
        if (chart.HouseCusps != null && chart.HouseCusps.Count > 0)
        {
            var lagna = chart.HouseCusps[0]; // 1st House Cusp is Lagna
            items.Add(new KPViewItem
            {
                Name = ZodiacUtils.IsTamil ? "லக்னம் (As)" : "Lagna (As)",
                DegreeDisplay = lagna.DegreeDisplay,
                SignLord = lagna.KpDetails.SignLord,
                StarLord = lagna.KpDetails.StarLord,
                SubLord = lagna.KpDetails.SubLord,
                SubSubLord = lagna.KpDetails.SubSubLord,
                SookshmaLord = lagna.KpDetails.SookshmaLord,
                PranaLord = lagna.KpDetails.PranaLord,
                DehaLord = lagna.KpDetails.DehaLord
            });
        }
        
        // Add Planets
        foreach (var p in chart.Planets)
        {
            items.Add(new KPViewItem
            {
                Name = $"{(ZodiacUtils.IsTamil ? ZodiacUtils.GetPlanetName(p.Planet) : p.Name)} ({p.Symbol})",
                DegreeDisplay = ZodiacUtils.FormatDegreeInSign(p.Longitude),
                SignLord = p.KpDetails.SignLord,
                StarLord = p.KpDetails.StarLord,
                SubLord = p.KpDetails.SubLord,
                SubSubLord = p.KpDetails.SubSubLord,
                SookshmaLord = p.KpDetails.SookshmaLord,
                PranaLord = p.KpDetails.PranaLord,
                DehaLord = p.KpDetails.DehaLord
            });
        }
        
        // Add House Cusps
        if (chart.HouseCusps != null)
        {
            foreach (var cusp in chart.HouseCusps)
            {
                items.Add(new KPViewItem
                {
                    Name = FormatHouseName(cusp.HouseNumber),
                    DegreeDisplay = cusp.DegreeDisplay,
                    SignLord = cusp.KpDetails.SignLord,
                    StarLord = cusp.KpDetails.StarLord,
                    SubLord = cusp.KpDetails.SubLord,
                    SubSubLord = cusp.KpDetails.SubSubLord,
                    SookshmaLord = cusp.KpDetails.SookshmaLord,
                    PranaLord = cusp.KpDetails.PranaLord,
                    DehaLord = cusp.KpDetails.DehaLord
                });
            }
        }
        
        KPGrid.ItemsSource = items;
        LocalizeHeaders();
    }

    private void LocalizeHeaders()
    {
        if (KPGrid.Columns.Count < 7) return;
        
        bool isTa = ZodiacUtils.IsTamil;
        KPGrid.Columns[0].Header = isTa ? "கிரகம்/முனை" : "Body/Cusp";
        KPGrid.Columns[1].Header = isTa ? "நட்சத்திர அதிபதி" : "Nakshatra Lord";
        KPGrid.Columns[2].Header = isTa ? "உப அதிபதி" : "Sub Lord";
        KPGrid.Columns[3].Header = isTa ? "உப-உப அதிபதி" : "Prati-Sub"; // Or Prati-Sub transliterated? "பிரதி-உப"
        KPGrid.Columns[4].Header = isTa ? "சூட்சுமம்" : "Sookshma";
        KPGrid.Columns[5].Header = isTa ? "பிராணன்" : "Prana";
        KPGrid.Columns[6].Header = isTa ? "தேகம்" : "Deha";
    }
    
    public void ClearChart()
    {
        KPGrid.ItemsSource = null;
    }
    
    // Helper to format ordinal
    private string FormatHouseName(int number)
    {
        if (ZodiacUtils.IsTamil)
            return $"{number}ம் பாவம்";
            
        string suffix = (number % 100 >= 11 && number % 100 <= 13) ? "th"
            : (number % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return $"{number}{suffix} House";
    }
    // Keep track of current chart to get location data (Moved to top)


    private async void CalculateTransitBtn_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_currentChart == null) return;
        if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue) return;

        CalculateTransitBtn.IsEnabled = false;
        TransitStatusText.Text = "Calculating...";
        TransitGrid.ItemsSource = null;

        try
        {
            // Create service (should ideally be DI or singleton, but new instance is fine here)
            // We need EphemerisService instance. We can create one or pass existing.
            // Since EphemerisService is disposable, using block desirable, or rely on internal management.
            // The service design requires us to pass one.
            using var ephemeris = new EphemerisService(); 
            var service = new KPTransitService(ephemeris);

            var start = StartDatePicker.SelectedDate.Value.ToUniversalTime();
            var end = EndDatePicker.SelectedDate.Value.ToUniversalTime();
            // Add end of day time
            end = end.AddHours(23).AddMinutes(59);

            var progress = new Progress<string>(msg => TransitStatusText.Text = msg);

            // Get selected body
            int? targetBodyId = (int?)BodySelector.SelectedValue;
            
            // Get Ayanamsha Settings
            var settings = AppSettings.Load();
            int ayanamshaId = (int)settings.Ayanamsha;
            double ayanamshaOffset = settings.AyanamshaOffset;

            var results = await service.CalculateTransitsAsync(start, end, _currentChart, ayanamshaId, ayanamshaOffset, progress, targetBodyId);

            TransitGrid.ItemsSource = results;
            TransitStatusText.Text = $"Found {results.Count} events.";
        }
        catch (System.Exception ex)
        {
             TransitStatusText.Text = "Error: " + ex.Message;
        }
        finally
        {
            CalculateTransitBtn.IsEnabled = true;
        }
    }

    private void LoadChartButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.DataContext is TransitEvent transitEvent)
        {
            // Raise event with the local time of the transit
            TransitSelected?.Invoke(this, transitEvent.TimeUtc.ToLocalTime());
        }
    }
}

public class KPViewItem
{
    public string Name { get; set; } = "";
    public string DegreeDisplay { get; set; } = "";
    public string SignLord { get; set; } = "";
    public string StarLord { get; set; } = "";
    public string SubLord { get; set; } = "";
    public string SubSubLord { get; set; } = "";
    public string SookshmaLord { get; set; } = "";
    public string PranaLord { get; set; } = "";
    public string DehaLord { get; set; } = "";
}
