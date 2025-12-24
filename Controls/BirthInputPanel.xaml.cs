using System;
using System.Windows;
using System.Windows.Controls;

namespace JamakolAstrology.Controls;

/// <summary>
/// Interaction logic for BirthInputPanel.xaml
/// </summary>
public partial class BirthInputPanel : UserControl
{
    public event EventHandler? CalculateRequested;

    public BirthInputPanel()
    {
        InitializeComponent();
    }

    // Properties to access input values
    public string PersonName => NameInput.Text;
    public DateTime? SelectedDate => DateInput.SelectedDate;
    public string TimeText => TimeInput.Text;
    public string Location => LocationInput.Text;
    public string Latitude => LatitudeInput.Text;
    public string Longitude => LongitudeInput.Text;
    public string Timezone => TimezoneInput.Text;

    private void CalculateButton_Click(object sender, RoutedEventArgs e)
    {
        CalculateRequested?.Invoke(this, EventArgs.Empty);
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
}
