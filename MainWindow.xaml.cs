using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Controls;
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
        
        // Subscribe to Jamakol input events
        JamakolInputControl.ExportRequested += JamakolInputControl_ExportRequested;
        JamakolInputControl.ImportRequested += JamakolInputControl_ImportRequested;
        
        // Subscribe to Birth Chart input events
        BirthInputControl.SaveRequested += BirthInputControl_SaveRequested;
        BirthInputControl.LoadRequested += BirthInputControl_LoadRequested;
        BirthInputControl.HideDegreesChanged += BirthInputControl_HideDegreesChanged;
    }

    private void UpdateToCurrentTime()
    {
        var now = DateTime.Now;
        BirthInputControl.SetDateTime(now);
        JamakolInputControl.UpdateToCurrentTime();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialize SideChartsPanel first
        _sharedSideCharts = new Controls.SideChartsPanel();
        
        // Initial Reparenting: Do NOT force to Basic if another tab (like Chakras) is default.
        // However, since we don't know for sure which inner tab is selected yet, we'll let the SelectionChanged event handle it.
        // But SelectionChanged might have fired before _sharedSideCharts was created.
        // So we manually check active tab and assign.
        
        if (BirthContentTabs.SelectedItem is TabItem initialTab)
        {
             // Simulate selection logic
             var header = initialTab.Header as string;
             if (header != "Chakras" && BasicChartPlaceholder != null && _sharedSideCharts != null)
             {
                 // Default fallback if logic fails or just assign to Basic if Basic is active or other
                 if (header == "Basic") BasicChartPlaceholder.Child = _sharedSideCharts;
                 // Add others if needed, but Basic is the main fallback
             }
        }
        else if (BasicChartPlaceholder != null && _sharedSideCharts != null)
        {
             // Fallback if no selection (unlikely)
             // But since Chakras is now first, we should probably NOT default to Basic visually if Chakras is open.
             // Safest is to do nothing and rely on SelectionChanged/User Action unless we want to force start on Basic?
             // User just moved Chakras to first, so likely wants Chakras first.
             // So we remove the forced Basic assignment.
        }

        // Apply settings defaults to input panels
        BirthInputControl.ApplySettings(_appSettings);
        JamakolInputControl.ApplySettings(_appSettings);
        
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
        
        // Apply initial state based on settings
        int defaultTab = _appSettings.DefaultTabIndex;
        MainTabControl.SelectedIndex = defaultTab;
        UpdateTabButtons(defaultTab);
        
        // Apply initial font settings
        ApplyFontSizes();
        
        // DEFAULT: Select the "Basic" tab (Index 1) instead of the first one (Chakras)
        // This triggers SelectionChanged which handles the side charts reparenting.
        if (BirthContentTabs.Items.Count > 1)
        {
            BirthContentTabs.SelectedIndex = 1; 
        }
    }
    
    // Global shared side charts instance
    private Controls.SideChartsPanel? _sharedSideCharts;

    private void BirthContentTabs_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (e.Source is TabControl && _sharedSideCharts != null)
        {
             var tabItem = BirthContentTabs.SelectedItem as TabItem;
             if (tabItem == null) return;
             
             // Detach from current parent
             var parent = VisualTreeHelper.GetParent(_sharedSideCharts) as Border;
             if (parent != null)
             {
                 parent.Child = null;
             }
             
             // Attach to new parent based on Header
             string header = tabItem.Header as string;
             
             try 
             {
                 if (header == "Basic" && BasicChartPlaceholder != null) BasicChartPlaceholder.Child = _sharedSideCharts;
                 else if (header == "Dashas" && DashasChartPlaceholder != null) DashasChartPlaceholder.Child = _sharedSideCharts;
                 else if (header == "Ashtakavarga" && AVChartPlaceholder != null) AVChartPlaceholder.Child = _sharedSideCharts;
                 else if (header == "KP" && KPChartPlaceholder != null) KPChartPlaceholder.Child = _sharedSideCharts;
                 else if (header == "Amsha Devata" && AmshaDevataChartPlaceholder != null) AmshaDevataChartPlaceholder.Child = _sharedSideCharts;
                 // Chakras: Do nothing (Side charts remain detached/hidden)
             }
             catch (Exception)
             {
                 // Ignore visual tree errors during initialization
             }
        }
    }

    #region Calculation Logic

    private void CalculateChart()
    {
        BirthInputControl.SetStatus("Calculating...");

        // Parse input data with BC date support
        var timeParts = BirthInputControl.TimeText.Split(':');
        int hour = timeParts.Length > 0 ? int.Parse(timeParts[0]) : 12;
        int minute = timeParts.Length > 1 ? int.Parse(timeParts[1]) : 0;
        int second = timeParts.Length > 2 ? int.Parse(timeParts[2]) : 0;
        
        var birthData = new BirthData
        {
            Name = BirthInputControl.PersonName.Trim(),
            Year = BirthInputControl.SelectedYear,
            Month = BirthInputControl.SelectedMonth,
            Day = BirthInputControl.SelectedDay,
            Hour = hour,
            Minute = minute,
            Second = second,
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

            // Update UI Components - pass current hide degrees setting
            bool hideDegrees = BirthInputControl.HideDegrees;
            // Update Side Charts Panel
            if (_sharedSideCharts != null)
            {
                _sharedSideCharts.UpdateCharts(result.ChartData, _appSettings.ChartFontSize, BirthInputControl.HideDegrees);
            }
            // Removed old individual chart updates as they are now in SideChartsPanel
            
            BirthDetailsPanel.UpdateDetails(result);
            
            // NEW: Update other top-level tabs content
            if (DashasPanelControl != null) DashasPanelControl.UpdateDashas(result.DashaResult);
            if (AVDetailsPanelControl != null) AVDetailsPanelControl.UpdateChart(result.ChartData);
            if (KpDetailsPanelControl != null) KpDetailsPanelControl.UpdateChart(result.ChartData);
            if (AmshaDevataPanelControl != null) AmshaDevataPanelControl.UpdateChart(result.ChartData, _appSettings.ChartFontSize);
            
            // Update Chakras panel
            ChakrasPanelControl.UpdateChart(result.ChartData, _appSettings.ChartFontSize);
            
            // Note: We DO NOT update Jamakol controls here anymore, to keep tabs independent.
            // If user wants Jamakol, they switch to Jamakol tab which has its own calculation.

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
            Year = jamakolDate.Year,
            Month = jamakolDate.Month,
            Day = jamakolDate.Day,
            Hour = hour,
            Minute = minute,
            Second = second,
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
                result.DayLord,
                _appSettings.UseFixedSignBoxes);

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
    
    private void BirthInputControl_HideDegreesChanged(object? sender, bool hideDegrees)
    {
        // Update both charts with the new hide degrees setting via Shared Panel
        if (_sharedSideCharts != null)
        {
            _sharedSideCharts.HideDegrees = hideDegrees;
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
            var dialog = new SavedChartsDialog(_chartStorageService, "Jamakol");
            dialog.Owner = this;
            if (dialog.ShowDialog() == true && dialog.ShouldLoadChart && dialog.ChartToLoad != null)
            {
                var chart = dialog.ChartToLoad;
                // Use Year/Month/Day for BC date support
                string timeStr = $"{chart.Hour:D2}:{chart.Minute:D2}:{chart.Second:D2}";
                JamakolInputControl.SetInputs(chart.Name, chart.Year, chart.Month, chart.Day, timeStr, 
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
        int year = BirthInputControl.SelectedYear;
        // For BC dates, this will return DateTime.MinValue
        if (year <= 0) return DateTime.MinValue;
        
        var timeParts = BirthInputControl.TimeText.Split(':');
        return new DateTime(year, BirthInputControl.SelectedMonth, BirthInputControl.SelectedDay, 
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
        if (BirthDetailsPanel != null) UiThemeHelper.SetFontSizeRecursive(BirthDetailsPanel, _appSettings.TableFontSize);
        
        // NEW: apply fonts to new panels
        if (DashasPanelControl != null) UiThemeHelper.SetFontSizeRecursive(DashasPanelControl, _appSettings.TableFontSize);
        if (AVDetailsPanelControl != null) UiThemeHelper.SetFontSizeRecursive(AVDetailsPanelControl, _appSettings.TableFontSize);
        if (KpDetailsPanelControl != null) UiThemeHelper.SetFontSizeRecursive(KpDetailsPanelControl, _appSettings.TableFontSize);
        
        // Chart controls are now in SideChartsPanel, need to trigger update if needed or just handle via layout refresh
        // Note: ChartControl font size is usually passed during UpdateChart, but we can try to apply recursive if it helps static text
    }

    #endregion

    #region Import/Export

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        JamakolInputControl_ExportRequested(sender, e);
    }

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        JamakolInputControl_ImportRequested(sender, e);
    }

    private void JamakolInputControl_ExportRequested(object? sender, EventArgs e)
    {
        var charts = _chartStorageService.GetAllCharts();
        if (charts.Count == 0)
        {
            MessageBox.Show("No charts to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = ".json",
            FileName = $"JamakolCharts_{DateTime.Now:yyyyMMdd}"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _chartStorageService.ExportToFile(dialog.FileName);
                MessageBox.Show($"Exported {charts.Count} charts successfully!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void JamakolInputControl_ImportRequested(object? sender, EventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                int count = _chartStorageService.ImportFromFile(dialog.FileName);
                MessageBox.Show($"Imported {count} new charts successfully!", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    #endregion

    #region Birth Chart Save/Load

    private void BirthInputControl_SaveRequested(object? sender, EventArgs e)
    {
        if (_currentChartData == null)
        {
            MessageBox.Show("Please calculate a chart first.", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            // Create a SavedJamakolChart from current birth chart data with BC date support
            var bd = _currentChartData.BirthData;
            var savedChart = new SavedJamakolChart
            {
                Name = BirthInputControl.PersonName,
                Year = bd.Year,
                Month = bd.Month,
                Day = bd.Day,
                Hour = bd.Hour,
                Minute = bd.Minute,
                Second = bd.Second,
                Latitude = bd.Latitude,
                Longitude = bd.Longitude,
                Location = bd.Location,
                Timezone = bd.TimeZoneOffset,
                ChartType = "BirthChart",
                ChartDataJson = System.Text.Json.JsonSerializer.Serialize(_currentChartData)
            };

            // Use the save dialog for category, tags, notes
            var saveDialog = new SaveChartDialog(_chartStorageService, savedChart);
            saveDialog.Owner = this;
            if (saveDialog.ShowDialog() == true && saveDialog.IsSaved)
            {
                BirthInputControl.SetStatus("Chart saved successfully!");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving chart: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BirthInputControl_LoadRequested(object? sender, EventArgs e)
    {
        var charts = _chartStorageService.GetAllCharts()
            .Where(c => c.ChartType == "BirthChart")
            .ToList();

        if (charts.Count == 0)
        {
            MessageBox.Show("No saved birth charts found.", "Load", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SavedChartsDialog(_chartStorageService, "BirthChart");
        dialog.Owner = this;
        if (dialog.ShowDialog() == true && dialog.ChartToLoad != null)
        {
            var chart = dialog.ChartToLoad;
            // Use Year/Month/Day for BC date support
            string timeStr = $"{chart.Hour:D2}:{chart.Minute:D2}:{chart.Second:D2}";
            BirthInputControl.SetInputs(
                chart.Name,
                chart.Year,
                chart.Month,
                chart.Day,
                timeStr,
                chart.Latitude,
                chart.Longitude,
                chart.Timezone,
                chart.Location
            );
            CalculateChart();
        }
    }

    #endregion
}


