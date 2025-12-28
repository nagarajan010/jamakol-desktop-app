using System.Windows;
using System.Windows.Controls;
using JamakolAstrology.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology;

/// <summary>
/// Settings window for configuring ayanamsha, location defaults, etc.
/// </summary>
public partial class SettingsWindow : Window
{
    public AppSettings Settings { get; private set; }
    public bool IsSaved { get; private set; }
    public bool LanguageChanged { get; private set; }
    private readonly GeoNamesService _geoService;
    private bool _isUpdatingText = false;
    private string _initialLanguage;

    public SettingsWindow(AppSettings currentSettings)
    {
        _geoService = new GeoNamesService();
        _isUpdatingText = true; // Prevent TextChanged during init
        InitializeComponent();
        Settings = currentSettings ?? new AppSettings();
        _initialLanguage = Settings.Language;
        LoadSettings();
        _isUpdatingText = false;
    }

    private void LoadSettings()
    {
        // Populate ayanamsha dropdown
        AyanamshaCombo.Items.Clear();
        foreach (AyanamshaType ayanamsha in Enum.GetValues(typeof(AyanamshaType)))
        {
            AyanamshaCombo.Items.Add(new ComboBoxItem 
            { 
                Content = ayanamsha.GetDisplayName(), 
                Tag = ayanamsha 
            });
        }

        // Select current ayanamsha
        foreach (ComboBoxItem item in AyanamshaCombo.Items)
        {
            if (item.Tag is AyanamshaType type && type == Settings.Ayanamsha)
            {
                AyanamshaCombo.SelectedItem = item;
                break;
            }
        }

        // Select current sunrise mode
        foreach (ComboBoxItem item in SunriseModeCombo.Items)
        {
            if (item.Tag is SunriseCalculationMode mode && mode == Settings.SunriseMode)
            {
                SunriseModeCombo.SelectedItem = item;
                break;
            }
        }

        // Load location defaults
        DefaultLocationInput.Text = Settings.DefaultLocationName;
        LatitudeInput.Text = Settings.DefaultLatitude.ToString("F4");
        LongitudeInput.Text = Settings.DefaultLongitude.ToString("F4");
        TimezoneInput.Text = Settings.DefaultTimezone.ToString("F1");
        ChartFontSizeInput.Text = Settings.ChartFontSize.ToString();
        JamaGrahaFontSizeInput.Text = Settings.JamaGrahaFontSize.ToString();
        TableFontSizeInput.Text = Settings.TableFontSize.ToString();
        InputFontSizeInput.Text = Settings.InputFontSize.ToString();

        // Select Default Tab
        DefaultTabCombo.SelectedIndex = Settings.DefaultTabIndex;

        // Load Fixed Sign Boxes setting
        UseFixedSignBoxesCheckbox.IsChecked = Settings.UseFixedSignBoxes;

        // Load Language setting
        foreach (ComboBoxItem item in LanguageCombo.Items)
        {
            if (item.Tag is string lang && lang == Settings.Language)
            {
                LanguageCombo.SelectedItem = item;
                break;
            }
        }
        if (LanguageCombo.SelectedItem == null)
            LanguageCombo.SelectedIndex = 0; // Default to English
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Save ayanamsha
            if (AyanamshaCombo.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is AyanamshaType type)
            {
                Settings.Ayanamsha = type;
            }

            // Save sunrise mode
            if (SunriseModeCombo.SelectedItem is ComboBoxItem sunriseItem && sunriseItem.Tag is SunriseCalculationMode mode)
            {
                Settings.SunriseMode = mode;
            }

            // Save location defaults
            Settings.DefaultLocationName = DefaultLocationInput.Text;
            Settings.DefaultLatitude = double.Parse(LatitudeInput.Text);
            Settings.DefaultLongitude = double.Parse(LongitudeInput.Text);
            Settings.DefaultTimezone = double.Parse(TimezoneInput.Text);
            Settings.ChartFontSize = double.Parse(ChartFontSizeInput.Text);
            Settings.JamaGrahaFontSize = double.Parse(JamaGrahaFontSizeInput.Text);
            Settings.TableFontSize = double.Parse(TableFontSizeInput.Text);
            Settings.InputFontSize = double.Parse(InputFontSizeInput.Text);

            // Save Default Tab
            Settings.DefaultTabIndex = DefaultTabCombo.SelectedIndex;

            // Save Fixed Sign Boxes setting
            Settings.UseFixedSignBoxes = UseFixedSignBoxesCheckbox.IsChecked == true;

            // Save Language setting
            if (LanguageCombo.SelectedItem is ComboBoxItem langItem && langItem.Tag is string langCode)
            {
                Settings.Language = langCode;
            }

            LanguageChanged = (Settings.Language != _initialLanguage);
            IsSaved = true;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Invalid input: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PlaceSearchDialog();
        dialog.Owner = this;
        if (dialog.ShowDialog() == true && dialog.SelectedLocation != null)
        {
            ApplyLocation(dialog.SelectedLocation);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        IsSaved = false;
        DialogResult = false;
        Close();
    }

    private async void DefaultLocationInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_isUpdatingText) return;

        string query = DefaultLocationInput.Text;
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

    private void DefaultLocationInput_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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
        DefaultLocationInput.Text = loc.Name;
        DefaultLocationInput.Select(DefaultLocationInput.Text.Length, 0);
        _isUpdatingText = false;

        LatitudeInput.Text = loc.Lat;
        LongitudeInput.Text = loc.Lng;
        
        if (loc.Timezone != null && !string.IsNullOrEmpty(loc.Timezone.TimeZoneId))
        {
            var offset = _geoService.GetTimezoneOffset(loc.Timezone.TimeZoneId);
            TimezoneInput.Text = offset.ToString("0.##");
        }
    }
}

