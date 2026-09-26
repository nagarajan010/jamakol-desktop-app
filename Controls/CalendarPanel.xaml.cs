using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JamakolAstrology.Helpers;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology.Controls;

/// <summary>
/// Month calendar showing the full Panchanga for each day, with a detail pane
/// for the selected day.
/// </summary>
public partial class CalendarPanel : UserControl
{
    private readonly CalendarService _calendarService = new();

    private readonly GeoNamesService _geoService = new();

    private AppSettings _settings = AppSettings.Load();
    private DateTime _currentMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private List<CalendarDay> _days = new();
    private CalendarDayViewModel? _selectedCell;
    private bool _isCalculating;

    // Day view state
    private DateTime? _dayViewDate;
    private bool _suppressDatePickerEvent;
    private int _planetRequestToken;

    private bool IsDayViewOpen => DayView.Visibility == Visibility.Visible;

    /// <summary>Aprakash Grahas (shadow points), distinct from the nine classical grahas.</summary>
    private static readonly HashSet<string> AprakashNames = new()
    {
        "Dhooma", "Vyatipata", "Parivesha", "Indrachapa", "Upaketu"
    };

    // Per-tab location override. Null means "use the default from Settings".
    // Kept local so viewing another city here never rewrites the user's global default.
    private string? _overrideLocationName;
    private double _overrideLatitude;
    private double _overrideLongitude;
    private double? _overrideTimezone;

    private bool HasOverride => _overrideLocationName != null;

    private double ActiveLatitude => HasOverride ? _overrideLatitude : _settings.DefaultLatitude;
    private double ActiveLongitude => HasOverride ? _overrideLongitude : _settings.DefaultLongitude;
    private string ActiveLocationName => HasOverride ? _overrideLocationName! : _settings.DefaultLocationName;

    /// <summary>Raised when the user asks to open a day in the Birth Chart tab.</summary>
    public event EventHandler<DateTime>? OpenInBirthChartRequested;

    public CalendarPanel()
    {
        InitializeComponent();
        BuildWeekdayHeader();

        if (_settings != null)
            UiThemeHelper.SetFontSizeRecursive(this, _settings.TableFontSize);

        Loaded += OnLoaded;
    }

    private bool _loadedOnce;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Only calculate on the first reveal; later tab switches reuse the grid.
        if (_loadedOnce) return;
        _loadedOnce = true;
        await LoadMonthAsync();
    }

    /// <summary>
    /// Re-read settings and recalculate. Called by MainWindow after the settings
    /// dialog changes ayanamsa, sunrise mode or default location.
    /// </summary>
    public async Task RefreshAsync(AppSettings settings)
    {
        _settings = settings;
        UiThemeHelper.SetFontSizeRecursive(this, _settings.TableFontSize);
        BuildWeekdayHeader();   // header size follows TableFontSize too
        await LoadMonthAsync();
    }

    private void BuildWeekdayHeader()
    {
        string[] headers = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        WeekdayHeader.Children.Clear();

        double headerSize = (_settings?.TableFontSize > 0 ? _settings.TableFontSize : 11) + 1;

        for (int i = 0; i < 7; i++)
        {
            WeekdayHeader.Children.Add(new TextBlock
            {
                Text = headers[i],
                FontSize = headerSize,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
                TextAlignment = TextAlignment.Center
            });
        }
    }

    private async Task LoadMonthAsync()
    {
        if (_isCalculating) return;
        _isCalculating = true;
        BusyOverlay.Visibility = Visibility.Visible;

        try
        {
            int year = _currentMonth.Year;
            int month = _currentMonth.Month;
            double lat = ActiveLatitude;
            double lon = ActiveLongitude;
            double tz = GetTimezoneOffset();
            var settings = _settings;

            MonthTitleText.Text = _currentMonth.ToString("MMMM yyyy");
            // Both toolbars show the same location.
            LocationText.Text = ActiveLocationName;
            DayLocationText.Text = ActiveLocationName;
            var resetVisibility = HasOverride ? Visibility.Visible : Visibility.Collapsed;
            BtnResetLocation.Visibility = resetVisibility;
            BtnDayResetLocation.Visibility = resetVisibility;

            // The sweep is CPU-bound; keep the UI responsive.
            var days = await Task.Run(() =>
                _calendarService.CalculateMonth(year, month, lat, lon, tz, settings));

            _days = days;
            RenderDays();

            // Summarise the Tamil month span across the displayed month.
            var inMonth = _days.Where(d => d.IsInDisplayedMonth).ToList();
            if (inMonth.Count > 0)
            {
                bool isTamil = ZodiacUtils.IsTamil;
                var names = inMonth
                    .Select(d => isTamil ? d.TamilMonth : d.EnglishMonth)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .ToList();

                string yearName = isTamil ? inMonth[0].TamilYear : inMonth[0].EnglishYear;
                TamilMonthText.Text = names.Count > 0
                    ? $"{string.Join(" / ", names)}  •  {yearName}"
                    : "";
            }

            // Preselect today when it is in view, else the first day of the month.
            var preselect = _days.FirstOrDefault(d => d.IsToday && d.IsInDisplayedMonth)
                            ?? inMonth.FirstOrDefault();
            if (preselect != null)
                SelectDay(preselect);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error building calendar: {ex.Message}",
                "Calendar Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BusyOverlay.Visibility = Visibility.Collapsed;
            _isCalculating = false;
        }
    }

    /// <summary>
    /// Timezone offset for the calendar's location. A city picked here carries its
    /// own zone, so sunrise and every derived time are right for that place; with no
    /// override we fall back to the machine's offset (correct for the common case of
    /// a user viewing their own region).
    /// </summary>
    private double GetTimezoneOffset()
        => _overrideTimezone ?? TimeZoneInfo.Local.GetUtcOffset(_currentMonth).TotalHours;

    private void RenderDays()
    {
        double cellFontSize = _settings?.TableFontSize > 0 ? _settings.TableFontSize : 11;
        var cells = _days.Select(d => new CalendarDayViewModel(d, cellFontSize)).ToList();
        DaysItems.ItemsSource = cells;
        _selectedCell = null;
    }

    private void SelectDay(CalendarDay day)
    {
        var cells = DaysItems.ItemsSource as List<CalendarDayViewModel>;
        var cell = cells?.FirstOrDefault(c => c.Day.Date == day.Date);
        if (cell != null)
        {
            if (_selectedCell != null) _selectedCell.IsSelected = false;
            _selectedCell = cell;
            cell.IsSelected = true;
        }
    }

    // ---------------- Day view ----------------

    /// <summary>
    /// Open the full-screen day view for a date. The date need not be inside the
    /// currently loaded month — stepping past either edge loads what it needs.
    /// </summary>
    private async Task ShowDayViewAsync(DateTime date)
    {
        var day = _days.FirstOrDefault(d => d.Date == date.Date);

        if (day == null)
        {
            // Stepped outside the loaded grid: load that month, then retry.
            _currentMonth = new DateTime(date.Year, date.Month, 1);
            await LoadMonthAsync();
            day = _days.FirstOrDefault(d => d.Date == date.Date);
            if (day == null) return;
        }

        _dayViewDate = date.Date;

        MonthView.Visibility = Visibility.Collapsed;
        DayView.Visibility = Visibility.Visible;

        SelectDay(day);
        ShowDetail(day);

        // Keep the picker in step without re-entering the changed handler.
        _suppressDatePickerEvent = true;
        DayDatePicker.SelectedDate = date.Date;
        _suppressDatePickerEvent = false;

        await LoadDayPlanetsAsync(day);
    }

    /// <summary>
    /// Planetary positions are far heavier than the Panchanga sweep, so they are
    /// computed per day on a background thread rather than for the whole month.
    /// </summary>
    private async Task LoadDayPlanetsAsync(CalendarDay day)
    {
        double lat = ActiveLatitude;
        double lon = ActiveLongitude;
        double tz = GetTimezoneOffset();
        var settings = _settings;

        // Guard against a slower earlier request landing after a newer one.
        var token = ++_planetRequestToken;

        try
        {
            var chart = await Task.Run(() =>
                _calendarService.CalculateDayPlanets(day, lat, lon, tz, settings));

            if (token != _planetRequestToken) return;   // superseded

            // ChartCalculator appends the Aprakash Grahas (Dhooma, Vyatipata,
            // Parivesha, Indrachapa, Upaketu) after the nine classical grahas.
            // They are shown greyed so they read as the shadow points they are.
            DetailPlanets.ItemsSource = chart.Planets
                .Select(p => new PlanetRowViewModel(p, AprakashNames.Contains(p.Name)))
                .ToList();
        }
        catch (Exception ex)
        {
            if (token != _planetRequestToken) return;
            DetailPlanets.ItemsSource = null;
            System.Diagnostics.Debug.WriteLine($"Planet calculation failed: {ex.Message}");
        }
    }

    private async void BtnBackToMonth_Click(object sender, RoutedEventArgs e)
    {
        DayView.Visibility = Visibility.Collapsed;
        MonthView.Visibility = Visibility.Visible;

        // If the day view wandered into another month, bring the grid along.
        if (_dayViewDate.HasValue &&
            (_dayViewDate.Value.Year != _currentMonth.Year ||
             _dayViewDate.Value.Month != _currentMonth.Month))
        {
            _currentMonth = new DateTime(_dayViewDate.Value.Year, _dayViewDate.Value.Month, 1);
            await LoadMonthAsync();
        }
    }

    private async void BtnPrevDay_Click(object sender, RoutedEventArgs e)
    {
        if (_dayViewDate.HasValue)
            await ShowDayViewAsync(_dayViewDate.Value.AddDays(-1));
    }

    private async void BtnNextDay_Click(object sender, RoutedEventArgs e)
    {
        if (_dayViewDate.HasValue)
            await ShowDayViewAsync(_dayViewDate.Value.AddDays(1));
    }

    private async void DayDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressDatePickerEvent) return;
        if (DayDatePicker.SelectedDate is DateTime picked && picked.Date != _dayViewDate)
            await ShowDayViewAsync(picked.Date);
    }

    private void ShowDetail(CalendarDay day)
    {
        bool isTamil = ZodiacUtils.IsTamil;

        DayTitleText.Text = day.Date.ToString("dd MMM yyyy");
        DayHeadlineText.Text = day.Date.ToString("dd MMMM yyyy");

        string tamilMonth = isTamil ? day.TamilMonth : day.EnglishMonth;
        string tamilYear = isTamil ? day.TamilYear : day.EnglishYear;
        DaySubText.Text =
            $"{(isTamil ? day.DayTamil : day.DayName)}  •  {day.DayLord}  •  {tamilMonth}  •  {tamilYear}";

        // Full/new moon banner, anchored on the tithi in force at sunrise.
        var moonTithi = day.PrimaryTithi;
        if (moonTithi?.Name is "Purnima" or "Amavasya")
        {
            bool isFull = moonTithi.Name == "Purnima";
            string label = isFull ? "Purnima — full moon" : "Amavasya — new moon";
            string ends = moonTithi.End.HasValue
                ? $"\nends {moonTithi.EndDisplayRelativeTo(day.Date)}"
                : "";

            DetailMoonText.Text = label + ends;
            DetailMoonIcon.Fill = isFull
                ? new SolidColorBrush(Color.FromRgb(0xF5, 0xC2, 0x18))
                : new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x3D));
            DetailMoonIcon.Stroke = new SolidColorBrush(
                isFull ? Color.FromRgb(0x8A, 0x6D, 0x00) : Color.FromRgb(0x33, 0x33, 0x3D));
            DetailMoonBanner.Visibility = Visibility.Visible;
        }
        else
        {
            DetailMoonBanner.Visibility = Visibility.Collapsed;
        }

        DetailSunriseText.Text = day.SunriseDisplay;
        DetailSunsetText.Text = day.SunsetDisplay;

        DetailTithis.ItemsSource = day.Tithis.Select(s => new SegmentViewModel(s, isTamil, true, day.Date)).ToList();
        DetailNakshatras.ItemsSource = day.Nakshatras.Select(s => new SegmentViewModel(s, isTamil, false, day.Date)).ToList();
        DetailYogas.ItemsSource = day.Yogas.Select(s => new SegmentViewModel(s, isTamil, false, day.Date)).ToList();
        DetailKaranas.ItemsSource = day.Karanas.Select(s => new SegmentViewModel(s, isTamil, false, day.Date)).ToList();

        string sunRasi = isTamil ? day.SunRasiTamil : day.SunRasi;
        string moonRasi = isTamil ? day.MoonRasiTamil : day.MoonRasi;
        DetailRasiText.Text = $"Sun: {sunRasi}    Moon: {moonRasi}";

        DetailPeriods.ItemsSource = day.InauspiciousPeriods;

        // Day and night horas sit in their own columns.
        DetailHorasDay.ItemsSource = day.Horas
            .Where(h => h.IsDay)
            .Select(h => new HoraViewModel(h, isTamil, day.Date)).ToList();
        DetailHorasNight.ItemsSource = day.Horas
            .Where(h => !h.IsDay)
            .Select(h => new HoraViewModel(h, isTamil, day.Date)).ToList();
    }

    private async void DayCell_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ContentPresenter presenter && presenter.Content is CalendarDayViewModel cell)
            await ShowDayViewAsync(cell.Day.Date);
    }

    private async void BtnPrevMonth_Click(object sender, RoutedEventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(-1);
        await LoadMonthAsync();
    }

    private async void BtnNextMonth_Click(object sender, RoutedEventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(1);
        await LoadMonthAsync();
    }

    private async void BtnToday_Click(object sender, RoutedEventArgs e)
    {
        var thisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        if (_currentMonth == thisMonth)
        {
            var today = _days.FirstOrDefault(d => d.IsToday && d.IsInDisplayedMonth);
            if (today != null) SelectDay(today);
            return;
        }

        _currentMonth = thisMonth;
        await LoadMonthAsync();
    }

    private async void BtnChangeLocation_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PlaceSearchDialog { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true || dialog.SelectedLocation == null)
            return;

        var loc = dialog.SelectedLocation;

        if (!double.TryParse(loc.Lat, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double lat) ||
            !double.TryParse(loc.Lng, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double lon))
        {
            MessageBox.Show("That place has no usable coordinates.", "Location",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _overrideLocationName = loc.Name;
        _overrideLatitude = lat;
        _overrideLongitude = lon;

        // Use the city's own timezone so sunrise-derived times are correct there.
        _overrideTimezone = !string.IsNullOrEmpty(loc.Timezone?.TimeZoneId)
            ? _geoService.GetTimezoneOffset(loc.Timezone!.TimeZoneId)
            : null;

        await ReloadForLocationChangeAsync();
    }

    private async void BtnResetLocation_Click(object sender, RoutedEventArgs e)
    {
        _overrideLocationName = null;
        _overrideTimezone = null;
        await ReloadForLocationChangeAsync();
    }

    /// <summary>
    /// Recalculate after a location change, refreshing whichever view is open.
    /// </summary>
    private async Task ReloadForLocationChangeAsync()
    {
        await LoadMonthAsync();

        if (IsDayViewOpen && _dayViewDate.HasValue)
        {
            var day = _days.FirstOrDefault(d => d.Date == _dayViewDate.Value);
            if (day != null)
            {
                ShowDetail(day);
                await LoadDayPlanetsAsync(day);
            }
        }
    }

    private void BtnOpenInBirthChart_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCell != null)
            OpenInBirthChartRequested?.Invoke(this, _selectedCell.Day.Date);
    }
}

/// <summary>
/// View-model for one grid cell. Exposes the pre-formatted strings and brushes
/// the DataTemplate binds to, so the template stays free of converters.
/// </summary>
public class CalendarDayViewModel : System.ComponentModel.INotifyPropertyChanged
{
    public CalendarDay Day { get; }

    private static readonly Brush InMonthBg = new SolidColorBrush(Colors.White);
    private static readonly Brush OutMonthBg = new SolidColorBrush(Color.FromRgb(0xF7, 0xF7, 0xF7));
    private static readonly Brush TodayBg = new SolidColorBrush(Color.FromRgb(0xFF, 0xF6, 0xE0));
    private static readonly Brush SelectedBorder = new SolidColorBrush(Color.FromRgb(0xCC, 0x00, 0x00));
    private static readonly Brush NormalBorder = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD));
    private static readonly Brush InMonthFg = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22));
    private static readonly Brush OutMonthFg = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
    private static readonly Brush SundayFg = new SolidColorBrush(Color.FromRgb(0xCC, 0x00, 0x00));

    // Paksha badge: bright chip for the waxing half, dark chip for the waning half.
    private static readonly Brush ShuklaBg = new SolidColorBrush(Color.FromRgb(0xFF, 0xE8, 0xA3));
    private static readonly Brush ShuklaFg = new SolidColorBrush(Color.FromRgb(0x7A, 0x54, 0x00));
    private static readonly Brush KrishnaBg = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x55));
    private static readonly Brush KrishnaFg = new SolidColorBrush(Colors.White);
    private static readonly Brush Transparent = System.Windows.Media.Brushes.Transparent;

    // Moon icons: a filled gold disc for the full moon, a hollow dark-rimmed
    // disc for the new moon.
    private static readonly Brush FullMoonFill = new SolidColorBrush(Color.FromRgb(0xF5, 0xC2, 0x18));
    private static readonly Brush FullMoonStroke = new SolidColorBrush(Color.FromRgb(0x8A, 0x6D, 0x00));
    private static readonly Brush NewMoonFill = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x3D));
    private static readonly Brush NewMoonStroke = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x3D));

    private bool _isSelected;

    public CalendarDayViewModel(CalendarDay day, double baseFontSize)
    {
        Day = day;

        // Cell text scales with the app's table font setting. Sizes inside a
        // DataTemplate can't be reached by UiThemeHelper.SetFontSizeRecursive
        // (it walks the live visual tree, not templates), so they are bound here.
        BodyFontSize = baseFontSize;
        SmallFontSize = Math.Max(9, baseFontSize - 1);
        DateFontSize = baseFontSize + 3;
        BadgeFontSize = Math.Max(8, baseFontSize - 2);
        MoonIconSize = baseFontSize + 1;

        bool isTamil = ZodiacUtils.IsTamil;

        DayNumber = day.Date.Day.ToString();

        var tithi = day.PrimaryTithi;
        var nakshatra = day.PrimaryNakshatra;

        // Name and end-time are separate so the grid can column-align the times.
        TithiName = tithi == null ? "" : (isTamil ? tithi.NameTamil : tithi.Name);
        TithiEnd = tithi?.End.HasValue == true ? tithi.EndDisplayRelativeTo(day.Date) : "";
        NakshatraName = nakshatra == null ? "" : (isTamil ? nakshatra.NameTamil : nakshatra.Name);
        NakshatraEnd = nakshatra?.End.HasValue == true ? nakshatra.EndDisplayRelativeTo(day.Date) : "";

        // A day almost always carries a second tithi/nakshatra (the one that runs
        // on past the next sunrise), so listing it in the cell would be noise on
        // every single day. The transition time is the real signal and is already
        // shown as the end time above; the full list stays in the tooltip and the
        // detail pane.

        // Paksha badge. Waxing (Shukla) reads as a light "bright half" chip,
        // waning (Krishna) as a dark one, so the lunar half is legible at a glance
        // rather than being a faint letter lost next to the date.
        bool isShukla = tithi != null && tithi.Paksha == "Shukla";
        PakshaShort = tithi == null ? "" : (isShukla ? "SHU" : "KRI");
        PakshaTooltip = tithi == null
            ? ""
            : (isShukla ? "Shukla Paksha (waxing moon)" : "Krishna Paksha (waning moon)");
        PakshaBackground = tithi == null ? Transparent : (isShukla ? ShuklaBg : KrishnaBg);
        PakshaForeground = tithi == null ? Transparent : (isShukla ? ShuklaFg : KrishnaFg);

        // Full-moon / new-moon marker, the two days a panchangam always highlights.
        // Anchor on the tithi in force AT SUNRISE (the day a panchangam names by),
        // not any overlapping one — otherwise both the day it starts and the day
        // it ends would get an icon.
        string? special = tithi?.Name is "Purnima" or "Amavasya" ? tithi.Name : null;

        MoonVisibility = special == null ? Visibility.Collapsed : Visibility.Visible;
        MoonFill = special == "Purnima" ? FullMoonFill : NewMoonFill;
        MoonStroke = special == "Purnima" ? FullMoonStroke : NewMoonStroke;
        MoonPhaseTooltip = special switch
        {
            "Purnima" => "Purnima (full moon)",
            "Amavasya" => "Amavasya (new moon)",
            _ => ""
        };

        TithiTooltip = BuildTooltip("Tithi", day.Tithis, isTamil, day.Date);
        NakshatraTooltip = BuildTooltip("Nakshatra", day.Nakshatras, isTamil, day.Date);

        var rahu = day.InauspiciousPeriods.FirstOrDefault(p => p.Symbol == "RK");
        RahuKalamLine = rahu != null ? rahu.RangeDisplay : "";
    }

    private static string BuildTooltip(string label, List<PanchangaSegment> segments, bool isTamil, DateTime day)
    {
        if (segments.Count == 0) return "";
        var lines = segments.Select(s =>
        {
            string name = isTamil ? s.NameTamil : s.Name;
            return $"{name}: {s.RangeDisplayRelativeTo(day)}";
        });
        return $"{label}\n{string.Join("\n", lines)}";
    }

    public double BodyFontSize { get; }
    public double SmallFontSize { get; }
    public double DateFontSize { get; }
    public double BadgeFontSize { get; }
    public double MoonIconSize { get; }

    public string DayNumber { get; }
    public string TithiName { get; }
    public string TithiEnd { get; }
    public string NakshatraName { get; }
    public string NakshatraEnd { get; }
    public string PakshaShort { get; }
    public string PakshaTooltip { get; }
    public Brush PakshaBackground { get; }
    public Brush PakshaForeground { get; }
    public Visibility MoonVisibility { get; }
    public Brush MoonFill { get; }
    public Brush MoonStroke { get; }
    public string MoonPhaseTooltip { get; }
    public string RahuKalamLine { get; }
    public string TithiTooltip { get; }
    public string NakshatraTooltip { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged(nameof(CellBorder));
            OnPropertyChanged(nameof(CellBorderThickness));
        }
    }

    public Brush CellBackground => Day.IsToday ? TodayBg : (Day.IsInDisplayedMonth ? InMonthBg : OutMonthBg);

    public Brush DateForeground => !Day.IsInDisplayedMonth
        ? OutMonthFg
        : (Day.Date.DayOfWeek == DayOfWeek.Sunday ? SundayFg : InMonthFg);

    public Brush CellBorder => IsSelected ? SelectedBorder : NormalBorder;

    public Thickness CellBorderThickness => IsSelected ? new Thickness(2) : new Thickness(1);

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
}

/// <summary>Detail-pane row for one Panchanga segment.</summary>
public class SegmentViewModel
{
    public SegmentViewModel(PanchangaSegment segment, bool isTamil, bool showPaksha, DateTime day)
    {
        string name = isTamil ? segment.NameTamil : segment.Name;

        if (showPaksha && !string.IsNullOrEmpty(segment.Paksha))
        {
            string paksha = isTamil ? segment.PakshaTamil : segment.Paksha;
            name = $"{paksha} {name}";
        }

        if (!string.IsNullOrEmpty(segment.Lord))
            name = $"{name} ({segment.Lord})";

        DisplayName = name;
        RangeDisplay = segment.RangeDisplayRelativeTo(day);
    }

    public string DisplayName { get; }
    public string RangeDisplay { get; }
}

/// <summary>Day-view row for one planet.</summary>
public class PlanetRowViewModel
{
    private static readonly Brush ClassicalFg = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22));
    private static readonly Brush AprakashFg = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99));

    public PlanetRowViewModel(PlanetPosition p, bool isAprakash = false)
    {
        RowForeground = isAprakash ? AprakashFg : ClassicalFg;
        Name = p.Name;
        SignName = ZodiacUtils.GetSignName(p.Sign);
        NakshatraName = ZodiacUtils.IsTamil
            ? ZodiacUtils.GetNakshatraName(p.Nakshatra)
            : p.NakshatraName;
        NakshatraPada = p.NakshatraPada;
        RetroFlag = p.IsRetrograde ? "R" : "";

        int deg = (int)p.DegreeInSign;
        int min = (int)((p.DegreeInSign - deg) * 60);
        DegreeDisplay = $"{deg:00}°{min:00}'";
    }

    public Brush RowForeground { get; }
    public string Name { get; }
    public string SignName { get; }
    public string DegreeDisplay { get; }
    public string NakshatraName { get; }
    public int NakshatraPada { get; }
    public string RetroFlag { get; }
}

/// <summary>Detail-pane row for one hora slot.</summary>
public class HoraViewModel
{
    public HoraViewModel(HoraSlot slot, bool isTamil, DateTime day)
    {
        DisplayLord = isTamil ? slot.LordTamil : slot.Lord;
        RangeDisplay = slot.RangeDisplayRelativeTo(day);
    }

    public string DisplayLord { get; }
    public string RangeDisplay { get; }
}
