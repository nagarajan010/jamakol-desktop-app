using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using JamakolAstrology.Models;
using JamakolAstrology.Services;
using JamakolAstrology.Helpers;

namespace JamakolAstrology;

/// <summary>
/// Main window for Jamakol Astrology application
/// Refactored to use ChartOrchestratorService for logic.
/// </summary>
public partial class MainWindow : Window
{
    private readonly ChartOrchestratorService _chartOrchestrator;
    private readonly ChartStorageService _chartStorageService;
    private ChartData? _currentChartData;
    private AppSettings _appSettings;
    
    // Timer is managed within JamakolInputBar now, but we subscribe to it
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize services
        _chartOrchestrator = new ChartOrchestratorService();
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
        BirthInputControl.SetDateTime(now);
        JamakolInputControl.UpdateToCurrentTime();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Auto-calculate charts on startup with current time
        try
        {
            CalculateChart();
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

    #region Calculation Logic

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

        try
        {
            // Execute full chart calculation via orchestrator
            var result = _chartOrchestrator.CalculateFullChart(birthData, _appSettings);
            
            // Store current data for saving later
            _currentChartData = result.ChartData;

            // Update UI Components
            ChartControl.UpdateChart(result.ChartData, _appSettings.ChartFontSize);
            BirthPlanetaryDetailsControl.UpdateDetails(result.ChartData);
            
            // Update Jamakol & Associated Grids (Even on Birth Tab, we calc everything)
            JamakolChartControl.UpdateChart(
                result.JamakolData, 
                result.JamaGrahas, 
                result.SpecialPoints, 
                _appSettings.ChartFontSize, 
                _appSettings.JamaGrahaFontSize, 
                result.DayLord);
            
            DataGridsPanelControl.UpdatePlanetGrid(result.JamakolData);
            DataGridsPanelControl.UpdateJamaGrahaGrid(result.JamaGrahas, result.SpecialPoints);
            PrasannaPanelControl.UpdateDetails(result.PrasannaDetails);
            PanchangaPanelControl.UpdateDetails(result.PanchangaDetails);

            BirthInputControl.SetStatus($"Calculated. Day: {result.DayLord} (Vedic Date: {result.VedicDate:dd-MMM})");
        }
        catch (Exception ex) when (ex.Message.Contains("sefstars.txt") || ex.Message.Contains("SwissEph file"))
        {
            MessageBox.Show($"Ayanamsha Error:\n\n{ex.Message}\n\nReverting to Lahiri Ayanamsha.", "Missing Files", MessageBoxButton.OK, MessageBoxImage.Warning);
            _appSettings.Ayanamsha = AyanamshaType.Lahiri;
            _appSettings.Save();
            // Retry
            CalculateChart();
        }
        catch (Exception ex)
        {
            BirthInputControl.SetStatus($"Error: {ex.Message}");
            MessageBox.Show($"Error calculating chart:\n{ex.Message}", "Calculation Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        try
        {
            // Execute full calculation
            var result = _chartOrchestrator.CalculateFullChart(birthData, _appSettings);

            // Update UI Components
            // Note: Jamakol tab primarily cares about the Jamakol Chart and Grids
            JamakolChartControl.UpdateChart(
                result.JamakolData, 
                result.JamaGrahas, 
                result.SpecialPoints, 
                _appSettings.ChartFontSize, 
                _appSettings.JamaGrahaFontSize, 
                result.DayLord);

            DataGridsPanelControl.UpdatePlanetGrid(result.JamakolData);
            DataGridsPanelControl.UpdateJamaGrahaGrid(result.JamaGrahas, result.SpecialPoints);
            PrasannaPanelControl.UpdateDetails(result.PrasannaDetails);
            PanchangaPanelControl.UpdateDetails(result.PanchangaDetails);
            
            // Also store for saving
            _currentChartData = result.ChartData;

            JamakolInputControl.SetStatus($"Calculated. Day: {result.DayLord} (Vedic Date: {result.VedicDate:dd-MMM})");

            // Hide saved chart info when manually calculating
            if (SavedChartInfoPanelControl != null)
            {
                SavedChartInfoPanelControl.Hide();
            }
        }
        catch (Exception ex)
        {
            JamakolInputControl.SetStatus($"Error: {ex.Message}");
            // Suppress messagebox for timer/live events to avoid spam
            // MessageBox.Show(...) 
        }
    }

    #endregion

    #region Event Handlers

    private void OnBirthCalculateRequested(object sender, EventArgs e) => CalculateChart();

    private void OnJamakolCalculateRequested(object sender, EventArgs e) => CalculateJamakolChart();

    private void OnJamakolLiveTimerTick(object sender, EventArgs e) => CalculateJamakolChart(); // Live update

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
                
                // Update orchestrator with new settings
                _chartOrchestrator.UpdateSunriseMode(_appSettings.SunriseMode);
                
                // Recalculate active tab
                if (MainTabControl.SelectedIndex == 0) CalculateChart();
                else CalculateJamakolChart();
                
                if (BirthInputControl != null) BirthInputControl.SetStatus("Settings saved and applied");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening settings: {ex.Message}", "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnJamakolSaveRequested(object sender, EventArgs e)
    {
        try
        {
            var jamakolDate = JamakolInputControl.SelectedDate ?? DateTime.Now;
            var timeParts = JamakolInputControl.TimeText.Split(':');
            var jamakolDateTime = new DateTime(jamakolDate.Year, jamakolDate.Month, jamakolDate.Day, 
                                             int.Parse(timeParts[0]), int.Parse(timeParts[1]), int.Parse(timeParts[2]));

            var savedChart = new SavedJamakolChart
            {
                Name = JamakolInputControl.ChartName.Trim(),
                QueryDateTime = jamakolDateTime,
                Latitude = double.Parse(JamakolInputControl.LatitudeText),
                Longitude = double.Parse(JamakolInputControl.LongitudeText),
                Timezone = double.Parse(JamakolInputControl.TimezoneText),
                ChartDataJson = _currentChartData != null ? System.Text.Json.JsonSerializer.Serialize(_currentChartData) : ""
            };

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
                JamakolInputControl.SetInputs(chart.Name, chart.QueryDateTime.Date, chart.QueryDateTime.ToString("HH:mm:ss"), 
                                            chart.Latitude, chart.Longitude, chart.Timezone);
                
                CalculateJamakolChart();
                JamakolInputControl.SetStatus($"Loaded: {chart.Name}");
                
                var categoryName = chart.CategoryId.HasValue ? _chartStorageService.GetCategory(chart.CategoryId.Value)?.Name : "";
                var tagNames = chart.TagIds.Select(id => _chartStorageService.GetTag(id)?.Name).Where(n => !string.IsNullOrEmpty(n)).Cast<string>().ToList();

                SavedChartInfoPanelControl.SetChartInfo(chart.Result.ToString(), categoryName ?? "", tagNames, chart.Prediction);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading charts: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Helpers

    private DateTime ParseDateTime()
    {
        var date = BirthInputControl.SelectedDate ?? DateTime.Now;
        var timeParts = BirthInputControl.TimeText.Split(':');
        return new DateTime(date.Year, date.Month, date.Day, 
                          timeParts.Length > 0 ? int.Parse(timeParts[0]) : 12,
                          timeParts.Length > 1 ? int.Parse(timeParts[1]) : 0, 
                          timeParts.Length > 2 ? int.Parse(timeParts[2]) : 0);
    }

    private void UpdateTabButtons(int index)
    {
        var activeBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#990000"));
        var inactiveBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#cc0000"));
        BtnBirthChart.Background = index == 0 ? activeBrush : inactiveBrush;
        BtnJamakol.Background = index == 1 ? activeBrush : inactiveBrush;
    }

    private void ApplyFontSizes()
    {
        if (DataGridsPanelControl != null) UiThemeHelper.SetFontSizeRecursive(DataGridsPanelControl, _appSettings.TableFontSize);
        if (BirthInputControl != null) UiThemeHelper.SetFontSizeRecursive(BirthInputControl, _appSettings.InputFontSize);
        if (JamakolInputControl != null) UiThemeHelper.SetFontSizeRecursive(JamakolInputControl, _appSettings.InputFontSize);
        if (PrasannaPanelControl != null) UiThemeHelper.SetFontSizeRecursive(PrasannaPanelControl, _appSettings.TableFontSize);
        if (PanchangaPanelControl != null) UiThemeHelper.SetFontSizeRecursive(PanchangaPanelControl, _appSettings.TableFontSize);
        if (SavedChartInfoPanelControl != null) UiThemeHelper.SetFontSizeRecursive(SavedChartInfoPanelControl, _appSettings.InputFontSize);
    }

    #endregion
}


