using System.Windows;
using System.Windows.Controls;
using JamakolAstrology.Models;

namespace JamakolAstrology;

/// <summary>
/// Settings window for configuring ayanamsha, location defaults, etc.
/// </summary>
public partial class SettingsWindow : Window
{
    public AppSettings Settings { get; private set; }
    public bool IsSaved { get; private set; }

    public SettingsWindow(AppSettings currentSettings)
    {
        InitializeComponent();
        Settings = currentSettings ?? new AppSettings();
        LoadSettings();
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
        LatitudeInput.Text = Settings.DefaultLatitude.ToString("F4");
        LongitudeInput.Text = Settings.DefaultLongitude.ToString("F4");
        TimezoneInput.Text = Settings.DefaultTimezone.ToString("F1");
        ChartFontSizeInput.Text = Settings.ChartFontSize.ToString();
        JamaGrahaFontSizeInput.Text = Settings.JamaGrahaFontSize.ToString();
        TableFontSizeInput.Text = Settings.TableFontSize.ToString();
        InputFontSizeInput.Text = Settings.InputFontSize.ToString();

        // Select current Prasanna mode
        foreach (ComboBoxItem item in PrasannaModeCombo.Items)
        {
            if (item.Tag is PrasannaCalcMode prasannaMode && prasannaMode == Settings.PrasannaMode)
            {
                PrasannaModeCombo.SelectedItem = item;
                break;
            }
        }

        // Select Default Tab
        DefaultTabCombo.SelectedIndex = Settings.DefaultTabIndex;
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
            Settings.DefaultLatitude = double.Parse(LatitudeInput.Text);
            Settings.DefaultLongitude = double.Parse(LongitudeInput.Text);
            Settings.DefaultTimezone = double.Parse(TimezoneInput.Text);
            Settings.ChartFontSize = double.Parse(ChartFontSizeInput.Text);
            Settings.JamaGrahaFontSize = double.Parse(JamaGrahaFontSizeInput.Text);
            Settings.TableFontSize = double.Parse(TableFontSizeInput.Text);
            Settings.InputFontSize = double.Parse(InputFontSizeInput.Text);

            // Save Prasanna mode
            if (PrasannaModeCombo.SelectedItem is ComboBoxItem prasannaItem && prasannaItem.Tag is PrasannaCalcMode prasannaMode)
            {
                Settings.PrasannaMode = prasannaMode;
            }

            // Save Default Tab
            Settings.DefaultTabIndex = DefaultTabCombo.SelectedIndex;

            IsSaved = true;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Invalid input: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        IsSaved = false;
        DialogResult = false;
        Close();
    }
}

