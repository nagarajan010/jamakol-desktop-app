using System.Windows;
using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology.Controls;

public partial class DashasPanel : UserControl
{
    private readonly SudarshanaDashaCalculator _sudarshanaExpander = new();

    // Narayana sub-periods are computed from the chart, so expanding a node needs it kept.
    private NarayanaDashaAdapter? _narayanaExpander;
    private ChartData? _narayanaChart;
    private DateTime _narayanaBirth;
    private double _narayanaBirthJd;
    private double _narayanaTimeZone;

    public DashasPanel()
    {
        InitializeComponent();
        NakshatraSystemPicker.ItemsSource = NakshatraDashaSystems.All;
        NakshatraSystemPicker.SelectedIndex = 0;
    }

    // Nakshatra dasha systems. Vimshottari comes from the chart calculation; the others are
    // computed when chosen, since building every system six levels deep up front would cost
    // several million periods for tables most charts never open.
    private DashaResult? _vimshottariResult;
    private double _moonLongitude;
    private double _birthJulianDay;
    private double _dashaTimeZone;
    private bool _hasDashaContext;

    /// <summary>What the non-Vimshottari systems need to compute their periods.</summary>
    public void SetNakshatraDashaContext(double moonLongitude, double birthJulianDay, double timeZoneOffset)
    {
        _moonLongitude = moonLongitude;
        _birthJulianDay = birthJulianDay;
        _dashaTimeZone = timeZoneOffset;
        _hasDashaContext = true;
    }

    private void NakshatraSystemChanged(object sender, SelectionChangedEventArgs e) => ShowSelectedNakshatraSystem();

    private void ShowSelectedNakshatraSystem()
    {
        var system = NakshatraSystemPicker.SelectedItem as NakshatraDashaSystem ?? NakshatraDashaSystems.Vimshottari;

        if (system.Condition != null)
        {
            NakshatraConditionNote.Text = $"Conditional dasha - applies when: {system.Condition}";
            NakshatraConditionNote.Visibility = Visibility.Visible;
        }
        else
        {
            NakshatraConditionNote.Visibility = Visibility.Collapsed;
        }

        if (system is VimshottariSystem || !_hasDashaContext || _vimshottariResult == null)
        {
            ShowNakshatraDasha(_vimshottariResult);
            return;
        }

        using var eph = new EphemerisService();
        double nowJd = eph.GetJulianDay(DateTime.UtcNow);
        var result = new UduDashaCalculator(system).Calculate(
            _moonLongitude, _birthJulianDay, nowJd, UduDashaCalculator.MaxLevel, _dashaTimeZone);
        ShowNakshatraDasha(result);
    }


    /// <summary>
    /// Fill in a Sudarshana level the moment the reader opens it.
    ///
    /// The tree is built a few levels deep because twelve to the power of six is over three
    /// million periods; the rest is arithmetic on the parent's own span, so it costs nothing
    /// until it is asked for.
    /// </summary>
    private void SudarshanaItemExpanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not TreeViewItem item) return;
        if (item.DataContext is not SudarshanaDashaPeriod period) return;

        if (period.SubPeriods.Count == 0 && period.CanExpand)
        {
            _sudarshanaExpander.Expand(period);
        }

        // Give the newly shown level its own children, so each one offers an expander in turn.
        foreach (var child in period.SubPeriods)
        {
            if (child.SubPeriods.Count == 0 && child.CanExpand)
            {
                _sudarshanaExpander.Expand(child);
            }
        }
    }

    /// <summary>
    /// Update Dasha details
    /// </summary>
    /// <summary>
    /// Fill in a Narayana level the moment the reader opens it.
    /// </summary>
    private void NarayanaItemExpanded(object sender, RoutedEventArgs e)
    {
        if (_narayanaExpander == null || _narayanaChart == null) return;
        if (e.OriginalSource is not TreeViewItem item) return;
        if (item.DataContext is not NarayanaPeriod period) return;

        void Fill(NarayanaPeriod p)
        {
            if (p.SubPeriods.Count > 0) return;
            _narayanaExpander.Expand(p, _narayanaChart, _narayanaBirth, _narayanaBirthJd,
                                     DateTime.Now, _narayanaTimeZone);
        }

        Fill(period);
        // Give the newly shown level its own children, so each offers an expander in turn.
        foreach (var child in period.SubPeriods) Fill(child);
    }

    public void UpdateNarayanaDashas(
        NarayanaDashaResult? result,
        ChartData? chart,
        DateTime birth,
        double birthJulianDay,
        double timeZoneOffset,
        NarayanaDashaAdapter? expander)
    {
        _narayanaExpander = expander;
        _narayanaChart = chart;
        _narayanaBirth = birth;
        _narayanaBirthJd = birthJulianDay;
        _narayanaTimeZone = timeZoneOffset;

        if (result == null)
        {
            CurrentNarayanaText.Text = "-";
            CurrentNarayanaDates.Text = "-";
            NarayanaStartInfo.Text = "";
            NarayanaTreeView.ItemsSource = null;
            return;
        }

        NarayanaTreeView.ItemsSource = result.MahaDashas;
        NarayanaStartInfo.Text = $"Starts in {result.StartingSignName} • order: "
            + string.Join(" ", result.Order.ConvertAll(s => ZodiacUtils.GetSignName(s + 1)));

        if (result.CurrentChain.Count == 0)
        {
            CurrentNarayanaText.Text = "No current period";
            CurrentNarayanaDates.Text = "";
            return;
        }

        CurrentNarayanaText.Text = result.CurrentDisplay;
        var deepest = result.CurrentChain[^1];
        CurrentNarayanaDates.Text = $"L{deepest.Level}: {deepest.DateRange}";
    }

    public void UpdateDashas(DashaResult? result, SudarshanaDashaResult? sudarshanaResult = null)
    {
        _vimshottariResult = result;
        ShowSelectedNakshatraSystem();

        UpdateSudarshanaDashas(sudarshanaResult);
    }

    /// <summary>Show one nakshatra system's periods in the tree and the running summary.</summary>
    private void ShowNakshatraDasha(DashaResult? result)
    {
        if (result == null)
        {
            CurrentDashaText.Text = "-";
            CurrentDashaDates.Text = "-";
            CurrentDashaLevels.Text = "-";
            DashaTreeView.ItemsSource = null;
        }
        else
        {
            // Set TreeView source
            DashaTreeView.ItemsSource = result.MahaDashas;

            // Set Current Dasha Texts
            if (result.CurrentAntarDasha != null)
            {
                // Format: Jupiter / Saturn / Mercury
                // Localize planets for display
                string GetLocPlanet(string p) => Services.ZodiacUtils.IsTamil && Enum.TryParse<Planet>(p, true, out var pl) 
                    ? Services.ZodiacUtils.GetPlanetName(pl) : p;
                // Yogini's lords are the yoginis, so name them rather than their graha.
                string Lord(DashaPeriod? d) => d == null ? "-" : d.LordLabel ?? GetLocPlanet(d.Planet);

                CurrentDashaText.Text = $"{Lord(result.CurrentMahaDasha)} / {Lord(result.CurrentAntarDasha)} / {Lord(result.CurrentPratyantaraDasha)}";
                
                // Format: 15-Oct-2023 to 22-Feb-2024 (showing range of deepest active level)
                var deepest = result.CurrentDehaDasha ?? 
                              result.CurrentPranaDasha ?? 
                              result.CurrentSookshmaDasha ?? 
                              result.CurrentPratyantaraDasha ?? 
                              result.CurrentAntarDasha;
                
                if (deepest != null)
                {
                    string endsOn = Services.ZodiacUtils.IsTamil ? "முடிவு" : "ends on";
                    string dateStr = deepest.LocalEndWithTime;
                    CurrentDashaDates.Text = $"{deepest.DisplayName} {endsOn} {dateStr}";
                }
                else
                {
                    CurrentDashaDates.Text = "";
                }

                // Full chain
                string p1 = Lord(result.CurrentMahaDasha);
                string p2 = Lord(result.CurrentAntarDasha);
                string p3 = Lord(result.CurrentPratyantaraDasha);
                
                string levels = $"{p1} > {p2} > {p3}";
                if (result.CurrentSookshmaDasha != null) levels += $" > {Lord(result.CurrentSookshmaDasha)}";
                if (result.CurrentPranaDasha != null) levels += $" > {Lord(result.CurrentPranaDasha)}";
                if (result.CurrentDehaDasha != null) levels += $" > {Lord(result.CurrentDehaDasha)}";
                
                CurrentDashaLevels.Text = levels;
            }
            else
            {
                CurrentDashaText.Text = "-";
                CurrentDashaDates.Text = "-";
                CurrentDashaLevels.Text = "-";
            }
        }
    }

    private void UpdateSudarshanaDashas(SudarshanaDashaResult? result)
    {
        if (result == null)
        {
            CurrentSudarshanaText.Text = "-";
            CurrentSudarshanaDates.Text = "-";
            SudarshanaTreeView.ItemsSource = null;
            return;
        }

        SudarshanaTreeView.ItemsSource = result.MainDashas;

        if (result.CurrentPeriod == null)
        {
            CurrentSudarshanaText.Text = "No current period within 144 years";
            CurrentSudarshanaDates.Text = "";
            return;
        }

        CurrentSudarshanaText.Text = result.CurrentDisplay;
        // The deepest link is the one actually running now, so its span is what to date.
        CurrentSudarshanaDates.Text =
            $"L{result.CurrentPeriod.Level}: {result.CurrentPeriod.DateRange}";
    }
}
