using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace JamakolAstrology.Controls;

/// <summary>
/// Jamakol Input Bar UserControl - contains all input fields and action buttons
/// </summary>
public partial class JamakolInputBar : UserControl
{
    private readonly DispatcherTimer _liveTimer;
    private bool _isLiveUpdateRunning;

    // Events for MainWindow to subscribe to
    public event EventHandler? CalculateRequested;
    public event EventHandler? SaveRequested;
    public event EventHandler? LoadRequested;
    public event EventHandler? LiveTimerTick;

    // Public access to input values
    public string ChartName => NameInput.Text;
    public DateTime? SelectedDate => DateInput.SelectedDate;
    public string TimeText => TimeInput.Text;
    public string LatitudeText => LatInput.Text;
    public string LongitudeText => LongInput.Text;
    public string TimezoneText => TzInput.Text;

    public JamakolInputBar()
    {
        InitializeComponent();

        // Setup live timer
        _liveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _liveTimer.Tick += LiveTimer_Tick_Internal;
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
            StartStopButton.Content = "Start";
            // Revert to Blue
            StartStopButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 150, 243)); 
            StatusText.Text = "Live update stopped";
        }
        else
        {
            _isLiveUpdateRunning = true;
            StartStopButton.Content = "Stop";
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
}
