using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology.Controls;

/// <summary>
/// Interaction logic for BirthInputPanel.xaml
/// </summary>
public partial class BirthInputPanel : UserControl
{
    public event EventHandler? CalculateRequested;
    public event EventHandler? SaveRequested;
    public event EventHandler? LoadRequested;
    public event EventHandler<bool>? HideDegreesChanged;
    private readonly GeoNamesService _geoService;
    private bool _isUpdatingText = false; // Prevent circular events
    private AppSettings? _cachedSettings;

    public BirthInputPanel()
    {
        _geoService = new GeoNamesService();
        _isUpdatingText = true; // Prevent initial search/popup on startup
        InitializeComponent();
        _isUpdatingText = false;
    }

    // Properties to access input values
    public string PersonName => NameInput.Text;
    public DateTime? SelectedDate => DateInput.SelectedDate;
    public string TimeText => TimeInput.Text;
    public string Location => LocationInput.Text;
    public string Latitude => LatitudeInput.Text;
    public string Longitude => LongitudeInput.Text;
    public string Timezone => TimezoneInput.Text;
    public bool HideDegrees => HideDegreesCheckBox.IsChecked == true;

    /// <summary>
    /// Apply settings defaults to input fields
    /// </summary>
    public void ApplySettings(AppSettings settings)
    {
        _cachedSettings = settings;
        _isUpdatingText = true;
        LocationInput.Text = settings.DefaultLocationName;
        _isUpdatingText = false;
        LatitudeInput.Text = settings.DefaultLatitude.ToString("F4");
        LongitudeInput.Text = settings.DefaultLongitude.ToString("F4");
        TimezoneInput.Text = settings.DefaultTimezone.ToString("F1");
    }

    private void CalculateButton_Click(object sender, RoutedEventArgs e)
    {
        CalculateRequested?.Invoke(this, EventArgs.Empty);
    }

    private void NowButton_Click(object sender, RoutedEventArgs e)
    {
        // Reset Name
        NameInput.Text = "Person";

        // Reset Date/Time
        var now = DateTime.Now;
        DateInput.SelectedDate = now;
        TimeInput.Text = now.ToString("HH:mm:ss");

        // Reset Location if settings available
        if (_cachedSettings != null)
        {
            _isUpdatingText = true;
            LocationInput.Text = _cachedSettings.DefaultLocationName;
            _isUpdatingText = false;
            LatitudeInput.Text = _cachedSettings.DefaultLatitude.ToString("F4");
            LongitudeInput.Text = _cachedSettings.DefaultLongitude.ToString("F4");
            TimezoneInput.Text = _cachedSettings.DefaultTimezone.ToString("F1");
        }

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
    
    private void HideDegreesCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        HideDegreesChanged?.Invoke(this, HideDegreesCheckBox.IsChecked == true);
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PlaceSearchDialog();
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() == true && dialog.SelectedLocation != null)
        {
            ApplyLocation(dialog.SelectedLocation);
        }
    }

    private async void LocationInput_TextChanged(object sender, TextChangedEventArgs e)
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
            // Fail silently for autocomplete
            SuggestionsPopup.IsOpen = false;
        }
    }

    private void SuggestionsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SuggestionsList.SelectedItem is GeoLocation loc)
        {
            ApplyLocation(loc);
            SuggestionsPopup.IsOpen = false;
        }
    }

    private void LocationInput_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (SuggestionsPopup.IsOpen)
        {
            if (e.Key == Key.Down)
            {
                SuggestionsList.SelectedIndex = (SuggestionsList.SelectedIndex + 1) % SuggestionsList.Items.Count;
                SuggestionsList.ScrollIntoView(SuggestionsList.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                if (SuggestionsList.SelectedIndex > 0)
                    SuggestionsList.SelectedIndex--;
                else
                    SuggestionsList.SelectedIndex = SuggestionsList.Items.Count - 1;
                
                SuggestionsList.ScrollIntoView(SuggestionsList.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                if (SuggestionsList.SelectedItem is GeoLocation loc)
                {
                    ApplyLocation(loc);
                    SuggestionsPopup.IsOpen = false;
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Escape)
            {
                SuggestionsPopup.IsOpen = false;
            }
        }
    }

    private void SuggestionsList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (SuggestionsList.SelectedItem is GeoLocation loc)
            {
                ApplyLocation(loc);
                SuggestionsPopup.IsOpen = false;
                e.Handled = true;
            }
        }
    }

    private void ApplyLocation(GeoLocation loc)
    {
        _isUpdatingText = true;
        LocationInput.Text = loc.Name;
        // Move cursor to end
        LocationInput.Select(LocationInput.Text.Length, 0); 
        _isUpdatingText = false;

        LatitudeInput.Text = loc.Lat;
        LongitudeInput.Text = loc.Lng;
        
        // Timezone logic if available
        if (loc.Timezone != null && !string.IsNullOrEmpty(loc.Timezone.TimeZoneId))
        {
             var offset = _geoService.GetTimezoneOffset(loc.Timezone.TimeZoneId);
             TimezoneInput.Text = offset.ToString("0.##");
        }
    }

    public void SetStatus(string message)
    {
        StatusText.Text = message;
    }

    public void SetDateTime(DateTime dt)
    {
        DateInput.SelectedDate = dt;
        TimeInput.Text = dt.ToString("HH:mm:ss");
    }

    public void SetInputs(string name, DateTime date, string time, double lat, double lng, double tz, string location)
    {
        NameInput.Text = name;
        DateInput.SelectedDate = date;
        TimeInput.Text = time;
        LatitudeInput.Text = lat.ToString("F4");
        LongitudeInput.Text = lng.ToString("F4");
        TimezoneInput.Text = tz.ToString("F1");
        _isUpdatingText = true;
        LocationInput.Text = location;
        _isUpdatingText = false;
    }
}
