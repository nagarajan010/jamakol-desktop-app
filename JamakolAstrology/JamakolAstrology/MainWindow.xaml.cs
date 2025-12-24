using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology;

/// <summary>
/// Main window for Jamakol Astrology application
/// </summary>
public partial class MainWindow : Window
{
    private readonly ChartCalculator _chartCalculator;
    private readonly JamakolCalculator _jamakolCalculator;
    private readonly JamaGrahaCalculator _jamaGrahaCalculator;
    private readonly SpecialPointsCalculator _specialPointsCalculator;
    private readonly PrasannaCalculator _prasannaCalculator;
    private readonly PanchangaCalculator _panchangaCalculator;
    private SunriseCalculator _sunriseCalculator;
    private readonly ChartStorageService _chartStorageService;
    private ChartData? _currentChartData;
    private AppSettings _appSettings;
    private DispatcherTimer? _liveTimer;

    public MainWindow()
    {
        InitializeComponent();
        _chartCalculator = new ChartCalculator();
        _jamakolCalculator = new JamakolCalculator();
        _jamaGrahaCalculator = new JamaGrahaCalculator();
        _specialPointsCalculator = new SpecialPointsCalculator();
        _prasannaCalculator = new PrasannaCalculator();
        _panchangaCalculator = new PanchangaCalculator();
        _sunriseCalculator = new SunriseCalculator();
        _chartStorageService = new ChartStorageService();
        _appSettings = AppSettings.Load();
        
        // Set default date and time to NOW
        UpdateToCurrentTime();
        
        // Auto-calculate on window load
        Loaded += MainWindow_Loaded;
    }

    private void UpdateToCurrentTime()
    {
        var now = DateTime.Now;
        
        // Birth Chart tab
        BirthInputControl.SetDateTime(now);
        
        // Jamakol tab - also set to current time
        JamakolInputControl.UpdateToCurrentTime();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Auto-calculate charts on startup with current time
        try
        {
            CalculateChart();
        }
        catch
        {
            // Ignore errors on startup
        }

        // Also calculate Jamakol chart on startup
        try
        {
            CalculateJamakolChart();
        }
        catch
        {
            // Ignore errors on startup
        }
        
        // Force initial state
        MainTabControl.SelectedIndex = 0;
        UpdateTabButtons(0);
        
        // Apply initial font settings
        ApplyFontSizes();
    }

    private void CalculateChart()
    {

        BirthInputControl.SetStatus("Calculating...");

        // Parse input data
        var birthData = new BirthData
        {
            Name = BirthInputControl.PersonName.Trim(),
            BirthDateTime = ParseDateTime(),
            Latitude = double.Parse(BirthInputControl.Latitude),
            Longitude = double.Parse(BirthInputControl.Longitude),
            Location = BirthInputControl.Location.Trim(),
            TimeZoneOffset = double.Parse(BirthInputControl.Timezone)
        };

        // Calculate chart
        try
        {
            _currentChartData = _chartCalculator.CalculateChart(birthData, _appSettings.Ayanamsha);
        }
        catch (Exception ex) when (ex.Message.Contains("sefstars.txt") || ex.Message.Contains("SwissEph file"))
        {
            MessageBox.Show($"Ayanamsha Error:\n\n{ex.Message}\n\nReverting to Lahiri Ayanamsha.", "Missing Files", MessageBoxButton.OK, MessageBoxImage.Warning);
            _appSettings.Ayanamsha = AyanamshaType.Lahiri;
            _appSettings.Save();
            _currentChartData = _chartCalculator.CalculateChart(birthData, _appSettings.Ayanamsha);
        }

        // Update South Indian chart display
        ChartControl.UpdateChart(_currentChartData);

        // Update planetary positions grid
        UpdatePlanetGrid(_currentChartData);

        // Determine Vedic Day and Sunrise/Sunset logic
        // Calculate sunrise for the civil date
        var civilDate = birthData.BirthDateTime.Date;
        var civilSunrise = _sunriseCalculator.CalculateSunrise(
            civilDate, 
            birthData.Latitude, 
            birthData.Longitude, 
            birthData.TimeZoneOffset);

        DateTime vedicDate;
        DateTime todaySunrise, todaySunset, tomorrowSunrise;

        // "Sunrise to Sunrise" rule: 
        // If born before sunrise, it belongs to the previous Vedic day
        if (birthData.BirthDateTime < civilSunrise)
        {
            vedicDate = civilDate.AddDays(-1);
            todaySunrise = _sunriseCalculator.CalculateSunrise(
                vedicDate, 
                birthData.Latitude, 
                birthData.Longitude, 
                birthData.TimeZoneOffset);
            todaySunset = _sunriseCalculator.CalculateSunset(
                vedicDate, 
                birthData.Latitude, 
                birthData.Longitude, 
                birthData.TimeZoneOffset);
            tomorrowSunrise = civilSunrise; // The civil sunrise is effectively tomorrow's sunrise for the previous vedic day
        }
        else
        {
            vedicDate = civilDate;
            todaySunrise = civilSunrise;
            todaySunset = _sunriseCalculator.CalculateSunset(
                vedicDate, 
                birthData.Latitude, 
                birthData.Longitude, 
                birthData.TimeZoneOffset);
            tomorrowSunrise = _sunriseCalculator.CalculateSunrise(
                vedicDate.AddDays(1), 
                birthData.Latitude, 
                birthData.Longitude, 
                birthData.TimeZoneOffset);
        }

        string dayLord = JamaGrahaCalculator.GetDayLord(vedicDate.DayOfWeek);

        // Calculate Jamakol data
        var jamakolData = _jamakolCalculator.Calculate(_currentChartData);
        
        // Calculate Jama Grahas using the correct Day Lord
        var jamaGrahas = _jamaGrahaCalculator.Calculate(birthData.BirthDateTime, dayLord);

        // Calculate Special Points (Aarudam, Udayam, Kavippu)
        // Pass the pre-calculated sunrise/sunset times
        var specialPoints = CalculateSpecialPoints(birthData, _currentChartData, todaySunrise, todaySunset, tomorrowSunrise);
        
        // Update Jamakol chart with planets, Jama Grahas, special points, and font sizes
        JamakolChartControl.UpdateChart(jamakolData, jamaGrahas, specialPoints, _appSettings.ChartFontSize, _appSettings.JamaGrahaFontSize, dayLord);
        UpdateJamakolPlanetGrid(jamakolData);


        BirthInputControl.SetStatus($"Calculated. Day: {dayLord} (Vedic Date: {vedicDate:dd-MMM})");
    }

    private List<SpecialPoint> CalculateSpecialPoints(
        BirthData birthData, 
        ChartData chartData,
        DateTime todaySunrise,
        DateTime todaySunset,
        DateTime tomorrowSunrise)
    {
        var specialPoints = new List<SpecialPoint>();

        // Calculate Aarudam (based on birth minute)
        var aarudam = _specialPointsCalculator.CalculateAarudam(birthData.BirthDateTime);
        specialPoints.Add(aarudam);

        // Get Sun's longitude - Udayam rises with Sun at sunrise and returns to Sun at sunset
        var sun = chartData.Planets.FirstOrDefault(p => p.Name == "Sun");
        double sunLongitude = sun?.Longitude ?? 0;
        
        // Udayam at sunrise = Sun's position
        // Udayam at sunset = Sun's position (completes 360° during day and returns to Sun)
        // During day: Udayam travels 360° (full rotation)
        // During night: Udayam travels 360° (another full rotation)
        double sunriseUdayam = sunLongitude;
        double sunsetUdayam = sunLongitude;  // Same as sunrise - completes full cycle

        // Calculate Udayam (Udaya Lagna) using Sun's position
        var udayaLagnaCalc = new UdayaLagnaCalculator(
            todaySunrise, todaySunset, tomorrowSunrise, sunriseUdayam, sunsetUdayam);
        var udayam = udayaLagnaCalc.CalculateUdayam(birthData.BirthDateTime);
        specialPoints.Add(udayam);

        // Calculate Kavippu (Sun sign = Tamil month)
        int sunSign = sun?.Sign ?? 1; // 1-12
        var kavippu = _specialPointsCalculator.CalculateKavippu(
            sunSign, udayam.AbsoluteLongitude, aarudam.AbsoluteLongitude);
        specialPoints.Add(kavippu);

        return specialPoints;
    }

    private void OnBirthCalculateRequested(object sender, EventArgs e)
    {
        try
        {
            CalculateChart();
        }
        catch (Exception ex)
        {

            BirthInputControl.SetStatus($"Error: {ex.Message}");
            MessageBox.Show($"Error calculating chart:\n{ex.Message}", "Calculation Error", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }



    private void JamakolCalculateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            CalculateJamakolChart();
        }
        catch (Exception ex)
        {
            JamakolInputControl.SetStatus($"Error: {ex.Message}");
            MessageBox.Show($"Error calculating Jamakol chart:\n{ex.Message}", "Calculation Error", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnJamakolCalculateRequested(object sender, EventArgs e)
    {
        CalculateJamakolChart();
    }



    private void OnJamakolLiveTimerTick(object sender, EventArgs e)
    {
        try
        {
            // Control has already updated the time display internally
            CalculateJamakolChart();
        }
        catch
        {
            // Ignore errors during live updates
        }
    }


    private void CalculateJamakolChart()
    {
        // Parse Jamakol-specific input data
        var jamakolDate = JamakolInputControl.SelectedDate ?? DateTime.Now;
        var jamakolTimeParts = JamakolInputControl.TimeText.Split(':');
        int hour = jamakolTimeParts.Length > 0 ? int.Parse(jamakolTimeParts[0]) : 12;
        int minute = jamakolTimeParts.Length > 1 ? int.Parse(jamakolTimeParts[1]) : 0;
        int second = jamakolTimeParts.Length > 2 ? int.Parse(jamakolTimeParts[2]) : 0;
        var jamakolDateTime = new DateTime(jamakolDate.Year, jamakolDate.Month, jamakolDate.Day, hour, minute, second);

        var birthData = new BirthData
        {
            Name = JamakolInputControl.ChartName.Trim(),
            BirthDateTime = jamakolDateTime,
            Latitude = double.Parse(JamakolInputControl.LatitudeText),
            Longitude = double.Parse(JamakolInputControl.LongitudeText),
            Location = "Query",
            TimeZoneOffset = double.Parse(JamakolInputControl.TimezoneText)
        };

        // Calculate chart for Jamakol
        var chartData = _chartCalculator.CalculateChart(birthData, _appSettings.Ayanamsha);

        // Determine Vedic Day and Sunrise/Sunset logic
        var civilDate = birthData.BirthDateTime.Date;
        var civilSunrise = _sunriseCalculator.CalculateSunrise(
            civilDate, birthData.Latitude, birthData.Longitude, birthData.TimeZoneOffset);

        DateTime vedicDate;
        DateTime todaySunrise, todaySunset, tomorrowSunrise;

        if (birthData.BirthDateTime < civilSunrise)
        {
            vedicDate = civilDate.AddDays(-1);
            todaySunrise = _sunriseCalculator.CalculateSunrise(
                vedicDate, birthData.Latitude, birthData.Longitude, birthData.TimeZoneOffset);
            todaySunset = _sunriseCalculator.CalculateSunset(
                vedicDate, birthData.Latitude, birthData.Longitude, birthData.TimeZoneOffset);
            tomorrowSunrise = civilSunrise;
        }
        else
        {
            vedicDate = civilDate;
            todaySunrise = civilSunrise;
            todaySunset = _sunriseCalculator.CalculateSunset(
                vedicDate, birthData.Latitude, birthData.Longitude, birthData.TimeZoneOffset);
            tomorrowSunrise = _sunriseCalculator.CalculateSunrise(
                vedicDate.AddDays(1), birthData.Latitude, birthData.Longitude, birthData.TimeZoneOffset);
        }

        // For Jama Graha: Day is based on 6 AM to 6 AM (not sunrise)
        // If time is before 6 AM, it's still the previous day for Jama Graha purposes
        DateTime jamaGrahaDate = birthData.BirthDateTime.Hour < 6 
            ? birthData.BirthDateTime.Date.AddDays(-1) 
            : birthData.BirthDateTime.Date;
        string dayLord = JamaGrahaCalculator.GetDayLord(jamaGrahaDate.DayOfWeek);

        // Calculate Jamakol data
        var jamakolData = _jamakolCalculator.Calculate(chartData);
        
        // Calculate Jama Grahas (uses 6 AM to 6 PM periods internally)
        var jamaGrahas = _jamaGrahaCalculator.Calculate(birthData.BirthDateTime, dayLord);

        // Calculate Special Points
        var specialPoints = CalculateSpecialPoints(birthData, chartData, todaySunrise, todaySunset, tomorrowSunrise);
        
        // Update Jamakol chart only (pass vedicDayLord)
        JamakolChartControl.UpdateChart(jamakolData, jamaGrahas, specialPoints, _appSettings.ChartFontSize, _appSettings.JamaGrahaFontSize, dayLord);
        UpdateJamakolPlanetGrid(jamakolData);
        UpdateJamaGrahaGrid(jamaGrahas, specialPoints);

        // Calculate and display Prasanna Details (using Jama Graha positions)
        var prasannaDetails = _prasannaCalculator.Calculate(jamaGrahas, specialPoints, _appSettings.PrasannaMode);
        UpdatePrasannaDetailsUI(prasannaDetails);

        // Calculate and display Panchanga Details
        // Use the actual ayanamsa value from the chart calculation
        var panchangaDetails = _panchangaCalculator.Calculate(chartData, todaySunrise, todaySunset, chartData.AyanamsaValue);
        UpdatePanchangaDetailsUI(panchangaDetails);

        JamakolInputControl.SetStatus($"Calculated. Day: {dayLord} (Vedic Date: {vedicDate:dd-MMM})");
        
        // Hide saved chart info when manually calculating (since it's a new or modified chart)
        // Only keep it if we just loaded it, but usually a manual Calculate implies a change.
        // Determining if this calculation was triggered by Load or Manual is tricky,
        // but typically if we just Loaded, we populated the fields. 
        // However, if the user hits Calculate AGAIN, it's a fresh calculation.
        // So hiding it is safer to avoid misleading data.
        if (SavedChartInfoPanelControl != null)
        {
            SavedChartInfoPanelControl.Hide();
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var settingsWindow = new SettingsWindow(_appSettings);
            settingsWindow.Owner = this;
            
            if (settingsWindow.ShowDialog() == true && settingsWindow.IsSaved)
            {
                _appSettings = settingsWindow.Settings;
            _appSettings.Save();
                ApplyFontSizes();
                
                // Recreate SunriseCalculator with new settings
                _sunriseCalculator?.Dispose();
                _sunriseCalculator = new SunriseCalculator(_appSettings.SunriseMode);
                
                // Recalculate based on which tab is active
                if (MainTabControl.SelectedIndex == 0)
                {
                    // Birth Chart tab is active
                    if (_currentChartData != null)
                    {
                        CalculateChart();
                    }
                }
                else if (MainTabControl.SelectedIndex == 1)
                {
                    // Jamakol tab is active - recalculate Jamakol
                    CalculateJamakolChart();
                }
                
                if (BirthInputControl != null)
                    BirthInputControl.SetStatus("Settings saved and applied");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening settings: {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnBirthChart_Click(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 0;
        UpdateTabButtons(0);
    }

    private void BtnJamakol_Click(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 1;
        UpdateTabButtons(1);
    }

    private void UpdateTabButtons(int index)
    {
        var activeBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#990000"));
        var inactiveBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#cc0000"));

        if (index == 0)
        {
            BtnBirthChart.Background = activeBrush;
            BtnJamakol.Background = inactiveBrush;
        }
        else
        {
            BtnBirthChart.Background = inactiveBrush;
            BtnJamakol.Background = activeBrush;
        }
    }

    private void OnJamakolSaveRequested(object sender, EventArgs e)
    {
        try
        {
            // Parse current Jamakol inputs
            var jamakolDate = JamakolInputControl.SelectedDate ?? DateTime.Now;
            var jamakolTimeParts = JamakolInputControl.TimeText.Split(':');
            int hour = jamakolTimeParts.Length > 0 ? int.Parse(jamakolTimeParts[0]) : 12;
            int minute = jamakolTimeParts.Length > 1 ? int.Parse(jamakolTimeParts[1]) : 0;
            int second = jamakolTimeParts.Length > 2 ? int.Parse(jamakolTimeParts[2]) : 0;
            var jamakolDateTime = new DateTime(jamakolDate.Year, jamakolDate.Month, jamakolDate.Day, hour, minute, second);

            // Create saved chart model
            var savedChart = new SavedJamakolChart
            {
                Name = JamakolInputControl.ChartName.Trim(),
                QueryDateTime = jamakolDateTime,
                Latitude = double.Parse(JamakolInputControl.LatitudeText),
                Longitude = double.Parse(JamakolInputControl.LongitudeText),
                Timezone = double.Parse(JamakolInputControl.TimezoneText)
            };

            // Serialize current chart data if available
            if (_currentChartData != null)
            {
                savedChart.ChartDataJson = System.Text.Json.JsonSerializer.Serialize(_currentChartData);
            }

            // Open save dialog
            var saveDialog = new SaveChartDialog(_chartStorageService, savedChart);
            saveDialog.Owner = this;
            if (saveDialog.ShowDialog() == true && saveDialog.IsSaved)
            {
                JamakolInputControl.SetStatus("Chart saved successfully!");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving chart: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnJamakolLoadRequested(object sender, EventArgs e)
    {
        try
        {
            var dialog = new SavedChartsDialog(_chartStorageService);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true && dialog.ShouldLoadChart && dialog.ChartToLoad != null)
            {
                var chart = dialog.ChartToLoad;
                
                // Load the chart data into Jamakol inputs
                JamakolInputControl.SetInputs(
                    chart.Name,
                    chart.QueryDateTime.Date,
                    chart.QueryDateTime.ToString("HH:mm:ss"),
                    chart.Latitude,
                    chart.Longitude,
                    chart.Timezone
                );
                
                // Recalculate the chart
                CalculateJamakolChart();
                JamakolInputControl.SetStatus($"Loaded: {chart.Name}");
                
                // Show chart details dialog with category, tags, result, prediction
                // Resolve category and tags
                var categoryName = chart.CategoryId.HasValue ? _chartStorageService.GetCategory(chart.CategoryId.Value)?.Name : "";
                var tagNames = chart.TagIds.Select(id => _chartStorageService.GetTag(id)?.Name).Where(n => !string.IsNullOrEmpty(n)).Cast<string>().ToList();

                // Show chart details in the new modular panel
                SavedChartInfoPanelControl.SetChartInfo(
                    chart.Result.ToString(),
                    categoryName ?? "",
                    tagNames,
                    chart.Prediction
                );
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading charts: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private DateTime ParseDateTime()
    {
        var date = BirthInputControl.SelectedDate ?? DateTime.Now;
        var timeParts = BirthInputControl.TimeText.Split(':');
        
        int hour = timeParts.Length > 0 ? int.Parse(timeParts[0]) : 12;
        int minute = timeParts.Length > 1 ? int.Parse(timeParts[1]) : 0;
        int second = timeParts.Length > 2 ? int.Parse(timeParts[2]) : 0;

        return new DateTime(date.Year, date.Month, date.Day, hour, minute, second);
    }

    private void UpdatePlanetGrid(ChartData chartData)
    {
        BirthPlanetaryDetailsControl.UpdateDetails(chartData);
    }

    private void UpdateJamakolPlanetGrid(JamakolData jamakolData)
    {
        // Delegate to the modular DataGridsPanel control
        DataGridsPanelControl.UpdatePlanetGrid(jamakolData);
    }

    private void UpdateJamaGrahaGrid(List<JamaGrahaPosition> jamaGrahas, List<SpecialPoint> specialPoints)
    {
        // Delegate to the modular DataGridsPanel control
        DataGridsPanelControl.UpdateJamaGrahaGrid(jamaGrahas, specialPoints);
    }

    private void UpdatePrasannaDetailsUI(PrasannaDetails details)
    {
        // Delegate to the modular PrasannaPanel control
        PrasannaPanelControl.UpdateDetails(details);
    }

    private void UpdatePanchangaDetailsUI(PanchangaDetails details)
    {
        // Delegate to the modular PanchangaPanel control
        PanchangaPanelControl.UpdateDetails(details);
    }

    private void ApplyFontSizes()
    {
        // Apply Table Font Size to DataGrids
        if (DataGridsPanelControl != null)
            SetFontSizeRecursive(DataGridsPanelControl, _appSettings.TableFontSize);
        
        // Apply Input Font Size for Birth Chart Input Panel
        if (BirthInputControl != null)
        {
             SetFontSizeRecursive(BirthInputControl, _appSettings.InputFontSize);
        }

        // Apply Input Font Size for Jamakol Input Panel
        if (JamakolInputControl != null)
        {
             SetFontSizeRecursive(JamakolInputControl, _appSettings.InputFontSize);
        }

        // Apply Table Font Size to Prasanna and Panchanga Details
        if (PrasannaPanelControl != null)
             SetFontSizeRecursive(PrasannaPanelControl, _appSettings.TableFontSize);

        if (PanchangaPanelControl != null)
             SetFontSizeRecursive(PanchangaPanelControl, _appSettings.TableFontSize);

        if (SavedChartInfoPanelControl != null)
             SetFontSizeRecursive(SavedChartInfoPanelControl, _appSettings.InputFontSize);
    }
    
    private void SetFontSizeRecursive(System.Windows.FrameworkElement element, double size)
    {
        if (element is System.Windows.Controls.Control c) c.FontSize = size;
        if (element is System.Windows.Controls.TextBlock t) t.FontSize = size;
        
        if (element is System.Windows.Controls.Panel panel)
        {
            foreach (System.Windows.UIElement child in panel.Children)
            {
                if (child is System.Windows.FrameworkElement fe)
                    SetFontSizeRecursive(fe, size);
            }
        }
        else if (element is System.Windows.Controls.ContentControl cc && cc.Content is System.Windows.FrameworkElement contentParams)
        {
             SetFontSizeRecursive(contentParams, size);
        }
        else if (element is System.Windows.Controls.Border border && border.Child is System.Windows.FrameworkElement child)
        {
             SetFontSizeRecursive(child, size);
        }
    }
}


