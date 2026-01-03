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
            new BodySelectorItem { Name = "All", Id = null },
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

        // Initialize Filter Selectors (Planets)
        var filterPlanets = new List<BodySelectorItem>
        {
            new BodySelectorItem { Name = "All", Id = null },
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

        StarFilter.ItemsSource = filterPlanets;
        StarFilter.DisplayMemberPath = "Name";
        StarFilter.SelectedValuePath = "Id";
        StarFilter.SelectedIndex = 0;

        SubFilter.ItemsSource = filterPlanets; 
        SubFilter.DisplayMemberPath = "Name";
        SubFilter.SelectedValuePath = "Id";
        SubFilter.SelectedIndex = 0;
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
        
        // --- 1. POPULATE SIGNIFICATORS ---
        var sigCalculator = new KPSignificatorCalculator();
        var houseSigs = sigCalculator.CalculateHouseSignificators(chart);
        var planetSigs = sigCalculator.CalculatePlanetSignifications(chart);
        
        HouseSignificatorsGrid.ItemsSource = houseSigs;
        PlanetSignificationsGrid.ItemsSource = planetSigs;
        
        // --- 2. POPULATE RULING PLANETS (For Chart Time) ---
        // Note: RP tab has a "Calculate Now" button, but we can also show RP for the chart moment validation if desired.
        // For now, let's just clear the fields or show empty until user clicks "Calculate for NOW"
        // OR we can calculate RP for the chart moment itself:
        var rpCalculator = new RulingPlanetsCalculator();
        var chartRP = rpCalculator.Calculate(chart);
        
        // Bind to the grid's DataContext or set individual TextBoxes?
        // Since we bound TextBoxes to properties like {Binding LagnaSignLord}, we set DataContext of the Tab content or Grid.
        // Let's find the Grid inside the "Ruling Planets" tab. 
        // A cleaner way is to set the DataContext of the whole RulingPlanets content Grid.
        // But for simplicity in this code-behind, we can set it to the TabItem's content (Grid).
        
        // We'll set the DataContext of the Panel to itself or a VM, but here we can just set the RP grid's context.
        // However, XAML bindings {Binding ...} look for DataContext. 
        // Let's assume the element names. But we used Binding in XAML, so we need a DataContext.
        // We will set the DataContext of the specific Grid in the RP tab. 
        // We need to give that Grid a name in XAML or traverse.
        // Looking at XAML, the Grid inside Ruling Planets tab has no name, but the controls are bound.
        // We can set DataContext of the UserControl or specific Tab.
        // Let's name the Grid in XAML in next step? No, we can't edit XAML again in this turn easily.
        // Wait, I can access the TabItem if I check the visual tree or if I named the TabItem? I didn't name TabItem.
        // But I can access the named elements inside it like LagnaSignLordTextBox if I named them... I didn't name them, I bound them.
        // Ah, I see: <TextBox Text="{Binding LagnaSignLord}" ... />
        // So I must set DataContext.
        // I will set DataContext of the TabItem's content.
        
        if (FindName("StrongRulersList") is ListView lv)
        {
             // Find the parent Grid of the list view which is the main grid of the tab
             if (lv.Parent is Grid rpGrid)
             {
                 rpGrid.DataContext = chartRP;
             }
             
             // Also update the time text
             if (FindName("RPTimeText") is TextBlock tb)
             {
                 tb.Text = chartRP.JudgmentTime.ToString("dd-MMM-yyyy HH:mm:ss");
             }
             
             lv.ItemsSource = chartRP.CombinedRulers;
        }

        // --- 3. POPULATE CUSPAL INTERLINKS ---
        // We need to calculate what the Sub Lord signifies for each cusp
        // We already have house significators.
        if (chart.HouseCusps != null)
        {
            foreach (var cusp in chart.HouseCusps)
            {
                cusp.SubLordSignifies.Clear();
                string subLord = cusp.KpDetails.SubLord;
                
                // Find which houses this sub lord signifies using our calculated planetSigs
                var pSig = planetSigs.FirstOrDefault(p => p.PlanetName == subLord);
                if (pSig != null)
                {
                    cusp.SubLordSignifies.AddRange(pSig.SignifiesHouses);
                }
            }
            CuspalInterlinksGrid.ItemsSource = chart.HouseCusps;
        }

        LocalizeHeaders();
    }

    private void LocalizeHeaders()
    {
        bool isTa = ZodiacUtils.IsTamil;

        if (KPGrid.Columns.Count >= 7)
        {
            KPGrid.Columns[0].Header = isTa ? "கிரகம்/முனை" : "Body/Cusp";
            KPGrid.Columns[1].Header = isTa ? "நட்சத்திர அதிபதி" : "Nakshatra Lord";
            KPGrid.Columns[2].Header = isTa ? "உப அதிபதி" : "Sub Lord";
            KPGrid.Columns[3].Header = isTa ? "உப-உப அதிபதி" : "Prati-Sub";
            KPGrid.Columns[4].Header = isTa ? "சூட்சுமம்" : "Sookshma";
            KPGrid.Columns[5].Header = isTa ? "பிராணன்" : "Prana";
            KPGrid.Columns[6].Header = isTa ? "தேகம்" : "Deha";
        }
        
        // Significators Headers
        if (HouseSignificatorsGrid.Columns.Count >= 5)
        {
            HouseSignificatorsGrid.Columns[0].Header = isTa ? "பாவம்" : "House";
            HouseSignificatorsGrid.Columns[1].Header = isTa ? "நிலை 1 (நின். நட்ச.)" : "L1 (Occ. Star)";
            HouseSignificatorsGrid.Columns[2].Header = isTa ? "நிலை 2 (நின்றவர்)" : "L2 (Occupant)";
            HouseSignificatorsGrid.Columns[3].Header = isTa ? "நிலை 3 (அதி. நட்ச.)" : "L3 (Own. Star)";
            HouseSignificatorsGrid.Columns[4].Header = isTa ? "நிலை 4 (அதிபதி)" : "L4 (Owner)";
        }
        
        if (PlanetSignificationsGrid.Columns.Count >= 5)
        {
            PlanetSignificationsGrid.Columns[0].Header = isTa ? "கிரகம்" : "Planet";
            PlanetSignificationsGrid.Columns[1].Header = isTa ? "குறிக்காட்டுபவை" : "Signifies Houses";
            PlanetSignificationsGrid.Columns[2].Header = isTa ? "நின்றது" : "Occ";
            PlanetSignificationsGrid.Columns[3].Header = isTa ? "சாரம்" : "Star";
            PlanetSignificationsGrid.Columns[4].Header = isTa ? "ஆட்சி" : "Owns";
        }

        
        // Transit Headers
        // Note: Using x:Name objects directly
        if (TrColStart != null)
        {
            TrColStart.Header = isTa ? "ஆரம்ப நேரம்" : "Start Time";
            TrColEnd.Header = isTa ? "முடிவு நேரம்" : "End Time";
            TrColBody.Header = isTa ? "கிரகம்" : "Body";
            TrColSignName.Header = isTa ? "ராசி" : "Sign Name";
            TrColSign.Header = isTa ? "ராசி அதிபதி" : "Sign Lord";
            TrColStarName.Header = isTa ? "நட்சத்திரம்" : "Star Name";
            TrColStar.Header = isTa ? "நட்ச. அதிபதி" : "Star Lord";
            TrColOldSub.Header = isTa ? "பழைய உப" : "Old Sub";
            TrColNewSub.Header = isTa ? "புதிய உப" : "New Sub";
        }
    }
    
    public void ClearChart()
    {
        KPGrid.ItemsSource = null;
        HouseSignificatorsGrid.ItemsSource = null;
        PlanetSignificationsGrid.ItemsSource = null;
        CuspalInterlinksGrid.ItemsSource = null;
        if (FindName("StrongRulersList") is ListView lv && lv.Parent is Grid rpGrid)
        {
            rpGrid.DataContext = null;
            lv.ItemsSource = null;
        }
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

            var start = StartDatePicker.SelectedDate.Value.Date;
            var end = EndDatePicker.SelectedDate.Value.Date;

            // Parse Time Inputs
            if (TimeSpan.TryParse(StartTimeInput.Text, out var startTime))
                start = start.Add(startTime);
            
            if (TimeSpan.TryParse(EndTimeInput.Text, out var endTime))
                end = end.Add(endTime);
            else
                end = end.AddHours(23).AddMinutes(59).AddSeconds(59); // Default to end of day if invalid

            start = start.ToUniversalTime();
            end = end.ToUniversalTime();

            var progress = new Progress<string>(msg => TransitStatusText.Text = msg);

            // Get selected body
            int? targetBodyId = (int?)BodySelector.SelectedValue;
            
            // Get Ayanamsha Settings
            var settings = AppSettings.Load();
            int ayanamshaId = (int)settings.Ayanamsha;
            double ayanamshaOffset = settings.AyanamshaOffset;

            var results = await service.CalculateTransitsAsync(start, end, _currentChart, ayanamshaId, ayanamshaOffset, progress, targetBodyId);

            // Assign End Times based on next event's Start Time
            for (int i = 0; i < results.Count; i++)
            {
                if (i < results.Count - 1)
                {
                    results[i].EndTimeUtc = results[i + 1].TimeUtc;
                }
                else
                {
                    // For the last event, we can default to the user's selected End Date or leave null
                    results[i].EndTimeUtc = end; // Show up to the end of the requested range
                }
            }

            // Filter results
            int? starFilterId = (int?)StarFilter.SelectedValue;
            int? subFilterId = (int?)SubFilter.SelectedValue;
            
            if (starFilterId.HasValue || subFilterId.HasValue)
            {
                // We need to match the English planet name corresponding to the ID
                string? starName = starFilterId.HasValue ? ZodiacUtils.GetPlanetName((Planet)starFilterId.Value) : null;
                string? subName = subFilterId.HasValue ? ZodiacUtils.GetPlanetName((Planet)subFilterId.Value) : null;
                
                // Note: GetPlanetName might return Tamil if IsTamil is true, but we need English for matching if model is English.
                // Assuming model uses standard names or match logic. 
                // However, to be safe, we should match against the ID if the model carried IDs, but the model carries strings.
                // Let's assume the Model strings (TransitEvent.Star) are localized or consistent with GetPlanetName.
                // IF GetPlanetName returns localized string, and TransitEvent.Star is localized (via KPTransitService -> KPCalculator -> ZodiacUtils?)
                // KPCalculator returns English names usually. 
                // Let's force English for matching? No, let's rely on string comparison but ensure we get the right name.
                // If the App is in Tamil, GetPlanetName returns Tamil.
                // Does KPTransitService use Localized names? 
                // In KPTransitService.cs: line 62 `string pName = ZodiacUtils.GetPlanetName(planetEnum);` -> Body Name
                // line 175: `Star = calculatedLords.StarLord`. 
                // KPCalculator -> returns `KPLords` strings.
                // KPCalculator likely returns short English names "Mo", "Ju"? Or full names? 
                // If it returns short names, we are in trouble matching "Jupiter" or "குரு".
                // 
                // BUT: In the KPGrid (Birth Chart), we use a Converter `PlanetNameConverter`.
                // This implies the underlying strings are NOT localized.
                // KPCalculator usually returns English names "Sun", "Moon".
                
                // So, we need to compare against English names.
                // But GetPlanetName((Planet)id) will return Tamil if IsTamil is true.
                // We must use `GetPlanetName` but force English or use `Planet` enum string?
                // `Planet.Jupiter.ToString()` -> "Jupiter".
                // This is safer.
                
                string? starFilterName = starFilterId.HasValue ? ((Planet)starFilterId.Value).ToString() : null;
                string? subFilterName = subFilterId.HasValue ? ((Planet)subFilterId.Value).ToString() : null;
                
                // Handle Rahu/Ketu special naming if enum differs (MeanNode vs Rahu)
                if (starFilterId == SwissEphNet.SwissEph.SE_MEAN_NODE) starFilterName = "Rahu";
                if (starFilterId == SwissEphNet.SwissEph.SE_TRUE_NODE) starFilterName = "Ketu";
                if (subFilterId == SwissEphNet.SwissEph.SE_MEAN_NODE) subFilterName = "Rahu";
                if (subFilterId == SwissEphNet.SwissEph.SE_TRUE_NODE) subFilterName = "Ketu";

                if (starFilterId.HasValue) results = results.Where(r => r.Star.Equals(starFilterName, StringComparison.OrdinalIgnoreCase)).ToList();
                if (subFilterId.HasValue) results = results.Where(r => r.NewSubLord.Equals(subFilterName, StringComparison.OrdinalIgnoreCase)).ToList();
            }

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

    private void CalculateRPBtn_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            var settings = AppSettings.Load();
            var rpCalculator = new RulingPlanetsCalculator();
            
            // Use default location from settings for "NOW" calculation
            // Effectively calculating for the user's current location and time
            var rp = rpCalculator.CalculateForNow(
                settings.DefaultLatitude, 
                settings.DefaultLongitude, 
                settings.DefaultTimezone, 
                (int)settings.Ayanamsha, 
                settings.AyanamshaOffset
            );
            
            if (FindName("StrongRulersList") is ListView lv)
            {
                 if (lv.Parent is Grid rpGrid)
                 {
                     rpGrid.DataContext = rp;
                 }
                 if (FindName("RPTimeText") is TextBlock tb)
                 {
                     tb.Text = rp.JudgmentTime.ToString("dd-MMM-yyyy HH:mm:ss") + " (Now)";
                 }
                 lv.ItemsSource = rp.CombinedRulers;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Error calculating RP: " + ex.Message);
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
