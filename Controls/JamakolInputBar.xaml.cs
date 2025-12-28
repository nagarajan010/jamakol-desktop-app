using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology.Controls;

/// <summary>
/// Jamakol Input Bar UserControl - contains all input fields and action buttons
/// </summary>
public partial class JamakolInputBar : UserControl
{
    private readonly DispatcherTimer _liveTimer;
    private bool _isLiveUpdateRunning;
    private readonly GeoNamesService _geoService;
    private bool _isUpdatingText = false;

    // Events for MainWindow to subscribe to
    public event EventHandler? CalculateRequested;
    public event EventHandler? SaveRequested;
    public event EventHandler? LoadRequested;
    public event EventHandler? LiveTimerTick;
    public event EventHandler? ExportRequested;
    public event EventHandler? ImportRequested;

    // Public access to input values
    public string ChartName => NameInput.Text;
    public DateTime? SelectedDate => DateInput.SelectedDate;
    public string TimeText => TimeInput.Text;
    public string LatitudeText => LatInput.Text;
    public string LongitudeText => LongInput.Text;
    public string TimezoneText => TzInput.Text;
    public string LocationText => LocationInput.Text;

    public JamakolInputBar()
    {
        _geoService = new GeoNamesService();
        _isUpdatingText = true;
        InitializeComponent();
        _isUpdatingText = false;

        // Setup live timer
        _liveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _liveTimer.Tick += LiveTimer_Tick_Internal;
    }

    /// <summary>
    /// Apply settings defaults to input fields
    /// </summary>
    public void ApplySettings(AppSettings settings)
    {
        _isUpdatingText = true;
        LocationInput.Text = settings.DefaultLocationName;
        _isUpdatingText = false;
        LatInput.Text = settings.DefaultLatitude.ToString("F4");
        LongInput.Text = settings.DefaultLongitude.ToString("F4");
        TzInput.Text = settings.DefaultTimezone.ToString("F1");
    }

    private async void LocationInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_isUpdatingText) return;

        string query = LocationInput.Text;
        if (query.Length < 3)
        {
            SuggestionsPopup.IsOpen = false;
            return;
        }

        try
        {
            var results = await _geoService.SearchPlaceAsync(query);
            if (results.Count > 0)
            {
                SuggestionsList.ItemsSource = results;
                SuggestionsPopup.IsOpen = true;
            }
            else
            {
                SuggestionsPopup.IsOpen = false;
            }
        }
        catch
        {
            SuggestionsPopup.IsOpen = false;
        }
    }

    private void LocationInput_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (SuggestionsPopup.IsOpen)
        {
            if (e.Key == System.Windows.Input.Key.Down)
            {
                SuggestionsList.SelectedIndex = (SuggestionsList.SelectedIndex + 1) % SuggestionsList.Items.Count;
                SuggestionsList.ScrollIntoView(SuggestionsList.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Up)
            {
                if (SuggestionsList.SelectedIndex > 0)
                    SuggestionsList.SelectedIndex--;
                else
                    SuggestionsList.SelectedIndex = SuggestionsList.Items.Count - 1;
                
                SuggestionsList.ScrollIntoView(SuggestionsList.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Tab)
            {
                if (SuggestionsList.SelectedItem is GeoLocation loc)
                {
                    ApplyLocation(loc);
                    SuggestionsPopup.IsOpen = false;
                    e.Handled = true;
                }
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                SuggestionsPopup.IsOpen = false;
            }
        }
    }

    private void SuggestionsList_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            if (SuggestionsList.SelectedItem is GeoLocation loc)
            {
                ApplyLocation(loc);
                SuggestionsPopup.IsOpen = false;
                e.Handled = true;
            }
        }
    }

    private void SuggestionsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (SuggestionsList.SelectedItem is GeoLocation loc)
        {
            ApplyLocation(loc);
            SuggestionsPopup.IsOpen = false;
        }
    }

    private void ApplyLocation(GeoLocation loc)
    {
        _isUpdatingText = true;
        LocationInput.Text = loc.Name;
        LocationInput.Select(LocationInput.Text.Length, 0);
        _isUpdatingText = false;

        LatInput.Text = loc.Lat;
        LongInput.Text = loc.Lng;
        
        if (loc.Timezone != null && !string.IsNullOrEmpty(loc.Timezone.TimeZoneId))
        {
            var offset = _geoService.GetTimezoneOffset(loc.Timezone.TimeZoneId);
            TzInput.Text = offset.ToString("0.##");
        }
    }

    private void LiveTimer_Tick_Internal(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        // Update the UI fields
        DateInput.SelectedDate = now;
        TimeInput.Text = now.ToString("HH:mm:ss");
        
        // Notify parent to calculate
        LiveTimerTick?.Invoke(this, EventArgs.Empty);
    }

    // Allow external setting of input values (for loading saved charts)
    public void SetInputs(string name, DateTime date, string time, double lat, double lng, double tz)
    {
        NameInput.Text = name;
        DateInput.SelectedDate = date;
        TimeInput.Text = time;
        LatInput.Text = lat.ToString();
        LongInput.Text = lng.ToString();
        TzInput.Text = tz.ToString();
    }

    // Overload for BC date support (Year/Month/Day as integers)
    public void SetInputs(string name, int year, int month, int day, string time, double lat, double lng, double tz)
    {
        NameInput.Text = name;
        // For AD dates, set DatePicker; for BC dates, it won't display properly but we store the values
        if (year > 0 && year <= 9999)
        {
            try { DateInput.SelectedDate = new DateTime(year, month, day); }
            catch { DateInput.SelectedDate = null; }
        }
        else
        {
            DateInput.SelectedDate = null; // BC dates can't be displayed in DatePicker
        }
        TimeInput.Text = time;
        LatInput.Text = lat.ToString();
        LongInput.Text = lng.ToString();
        TzInput.Text = tz.ToString();
    }

    public void SetStatus(string message) => StatusText.Text = message;

    public void SetStartStopButtonText(string text) => StartStopButton.Content = text;

    private void NowButton_Click(object sender, RoutedEventArgs e)
    {
        var now = DateTime.Now;
        NameInput.Text = "Query";
        DateInput.SelectedDate = now;
        TimeInput.Text = now.ToString("HH:mm:ss");
        
        // Trigger calculation
        CalculateRequested?.Invoke(this, EventArgs.Empty);
    }

    private void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isLiveUpdateRunning)
        {
            _liveTimer.Stop();
            _isLiveUpdateRunning = false;
            StartStopButton.Content = JamakolAstrology.Resources.Strings.BtnStart;
            // Revert to Blue
            StartStopButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 150, 243)); 
            StatusText.Text = "Live update stopped";
        }
        else
        {
            _isLiveUpdateRunning = true;
            StartStopButton.Content = JamakolAstrology.Resources.Strings.BtnStop;
            // Change to Red
            StartStopButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54));
            StatusText.Text = "Live update running...";
            _liveTimer.Start();
            
            // Trigger immediate update
            LiveTimer_Tick_Internal(this, EventArgs.Empty);
        }
    }

    public void UpdateToCurrentTime()
    {
        var now = DateTime.Now;
        DateInput.SelectedDate = now;
        TimeInput.Text = now.ToString("HH:mm:ss");
    }

    private void CalculateButton_Click(object sender, RoutedEventArgs e)
    {
        CalculateRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveRequested?.Invoke(this, EventArgs.Empty);
    }

    private void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        LoadRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PlaceSearchDialog();
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() == true && dialog.SelectedLocation != null)
        {
            var loc = dialog.SelectedLocation;
            LocationInput.Text = loc.Name;
            LatInput.Text = loc.Lat;
            LongInput.Text = loc.Lng;
            
            if (loc.Timezone != null && !string.IsNullOrEmpty(loc.Timezone.TimeZoneId))
            {
                var offset = _geoService.GetTimezoneOffset(loc.Timezone.TimeZoneId);
                TzInput.Text = offset.ToString("0.##");
            }
        }
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        ExportRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        ImportRequested?.Invoke(this, EventArgs.Empty);
    }
}
