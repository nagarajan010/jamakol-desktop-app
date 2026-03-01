using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using JamakolAstrology.Models;
using JamakolAstrology.Services;
using SwissEphNet;

namespace JamakolAstrology.Controls;

public partial class TithiPraveshaPanel : UserControl
{
    private ChartData? _natalChart;
    private ChartData? _tithiPraveshaChart;
    private readonly EphemerisService _ephemeris = new();
    private readonly ChartOrchestratorService _orchestrator = new();
    private int _natalSunSign;
    private double _natalTithiDiff;
    private int _natalTithiNumber;
    private int _birthYear;

    public event EventHandler<TithiPraveshaCalculatedEventArgs>? ChartCalculated;

    private static readonly SolidColorBrush LabelBrush = new((Color)ColorConverter.ConvertFromString("#990000"));
    private static readonly SolidColorBrush ValueBrush = new((Color)ColorConverter.ConvertFromString("#333333"));
    private static readonly SolidColorBrush PercentBrush = Brushes.DimGray;

    public TithiPraveshaPanel()
    {
        InitializeComponent();
        AshtakavargaControl.ShowGridView();
    }

    public void UpdateChart(ChartData? chart)
    {
        _natalChart = chart;

        if (_natalChart != null)
        {
            InitializeYearComboBox();
            CalculateNatalParameters();
            CalculateButton.IsEnabled = true;
            
            if (YearComboBox.SelectedItem is int selectedYear)
                CalculateTithiPravesha(selectedYear);
        }
        else
        {
            YearComboBox.ItemsSource = null;
            CalculateButton.IsEnabled = false;
            PlanetGridControl.DataGridControl.ItemsSource = null;
            NatalDetailsText.Text = "No content available";
            AshtakavargaControl.ClearChart();
            StatusText.Text = "";
            _tithiPraveshaChart = null;
        }
    }

    private void InitializeYearComboBox()
    {
        if (_natalChart == null) return;

        _birthYear = _natalChart.BirthData.Year;
        var currentYear = DateTime.Now.Year;
        var years = new List<int>();

        for (int year = _birthYear; year <= currentYear + 10; year++)
            years.Add(year);

        YearComboBox.ItemsSource = years;
        YearComboBox.SelectedItem = currentYear;
    }

    private void CalculateNatalParameters()
    {
        if (_natalChart == null) return;

        var natalSun = _natalChart.Planets.FirstOrDefault(p => p.Planet == Planet.Sun);
        var natalMoon = _natalChart.Planets.FirstOrDefault(p => p.Planet == Planet.Moon);

        if (natalSun == null || natalMoon == null)
        {
            StatusText.Text = "Error: Sun or Moon not found";
            return;
        }

        _natalSunSign = natalSun.Sign;
        _natalTithiDiff = ZodiacUtils.NormalizeDegree(natalMoon.Longitude - natalSun.Longitude);
        _natalTithiNumber = (int)(_natalTithiDiff / 12) + 1;
        if (_natalTithiNumber > 30) _natalTithiNumber = 30;
    }

    private void CalculateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_natalChart == null || YearComboBox.SelectedItem is not int selectedYear) return;
        CalculateTithiPravesha(selectedYear);
    }

    private void CalculateTithiPravesha(int targetYear)
    {
        if (_natalChart == null) return;

        StatusText.Text = "Calculating...";

        try
        {
            var settings = AppSettings.Load();
            int ayanId = (int)settings.Ayanamsha;
            double ayanOffset = settings.AyanamshaOffset;
            double lat = _natalChart.BirthData.Latitude;
            double lon = _natalChart.BirthData.Longitude;
            double tzOffset = _natalChart.BirthData.TimeZoneOffset;

            var tithiPraveshaMoment = FindTithiPraveshaMoment(targetYear, ayanId, ayanOffset);

            if (tithiPraveshaMoment == null)
            {
                StatusText.Text = "Could not find Tithi Pravesha moment";
                return;
            }

            var localTime = tithiPraveshaMoment.Value.AddHours(tzOffset);

            var birthData = new BirthData
            {
                Year = localTime.Year,
                Month = localTime.Month,
                Day = localTime.Day,
                Hour = localTime.Hour,
                Minute = localTime.Minute,
                Second = localTime.Second,
                Latitude = lat,
                Longitude = lon,
                TimeZoneOffset = tzOffset,
                Location = _natalChart.BirthData.Location,
                Name = $"Tithi Pravesha {targetYear}"
            };

            var result = _orchestrator.CalculateFullChart(birthData, settings);
            _tithiPraveshaChart = result.ChartData;

            DisplayChart(result);

            ChartCalculated?.Invoke(this, new TithiPraveshaCalculatedEventArgs(_tithiPraveshaChart));

            StatusText.Text = "Calculated";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private void DisplayChart(CompositeChartResult result)
    {
        var chart = result.ChartData;
        if (chart == null) return;

        PlanetGridControl.UpdateGrid(chart);
        AshtakavargaControl.UpdateChart(chart);

        var bd = chart.BirthData;
        var pd = result.PanchangaDetails;

        NatalDetailsText.Inlines.Clear();

        void AddLine(string label, string value, string extra = "", bool isHeader = false)
        {
            if (isHeader)
            {
                NatalDetailsText.Inlines.Add(new Run(label + "\n")
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = LabelBrush,
                    FontSize = 14
                });
                NatalDetailsText.Inlines.Add(new Run("\n"));
                return;
            }

            if (string.IsNullOrEmpty(label) && string.IsNullOrEmpty(value))
            {
                NatalDetailsText.Inlines.Add(new Run("\n"));
                return;
            }

            NatalDetailsText.Inlines.Add(new Run(label) { Foreground = LabelBrush, FontWeight = FontWeights.SemiBold });
            NatalDetailsText.Inlines.Add(new Run(value) { Foreground = ValueBrush });
            if (!string.IsNullOrEmpty(extra))
                NatalDetailsText.Inlines.Add(new Run(extra) { Foreground = PercentBrush, FontSize = 11 });
            NatalDetailsText.Inlines.Add(new Run("\n"));
        }

        string GetVal(string en, string ta) => ZodiacUtils.IsTamil ? ta : en;

        AddLine("Tithi Pravesha Chart Details", "", "", true);

        AddLine(JamakolAstrology.Resources.Strings.LabelDate, bd.GetDisplayDate());
        AddLine(JamakolAstrology.Resources.Strings.LabelTime, bd.GetDisplayTime());

        var tzSpan = TimeSpan.FromHours(bd.TimeZoneOffset);
        AddLine(JamakolAstrology.Resources.Strings.LabelTimeZone, $"{tzSpan.Hours}:{tzSpan.Minutes:00}:{tzSpan.Seconds:00} (East of GMT)");

        string latStr = ConvertToDms(bd.Latitude, true);
        string longStr = ConvertToDms(bd.Longitude, false);
        AddLine(JamakolAstrology.Resources.Strings.LabelPlace, $"{longStr}, {latStr}");
        AddLine("               ", $"{bd.Location}");
        AddLine(JamakolAstrology.Resources.Strings.LabelAltitude, "0.00 meters");

        AddLine("", "");

        if (pd != null)
        {
            AddLine(JamakolAstrology.Resources.Strings.LabelLunarYearMonth, $"{GetVal(pd.EnglishYear, pd.TamilYear)} - {GetVal(pd.EnglishMonth, pd.TamilMonth)}");

            string tithiName = GetVal(pd.TithiName, pd.TithiTamil);
            string paksha = GetVal(pd.Paksha, pd.PakshaTamil);
            AddLine(JamakolAstrology.Resources.Strings.LabelTithi, $"{paksha} {tithiName} ({pd.TithiLord}) ", $"({pd.TithiPercentLeft:F2}% left)");

            AddLine(JamakolAstrology.Resources.Strings.LabelVedicWeekday, $"{GetVal(pd.DayName, pd.DayTamil)} ({pd.DayLordAbbr})");
            AddLine(JamakolAstrology.Resources.Strings.LabelNakshatra, $"{GetVal(pd.NakshatraName, pd.NakshatraTamil)} ({pd.NakshatraLord}) ", $"({pd.NakshatraPercentLeft:F2}% left)");
            AddLine(JamakolAstrology.Resources.Strings.LabelYoga, $"{GetVal(pd.YogaName, pd.YogaTamil)} ", $"({pd.YogaPercentLeft:F2}% left)");
            AddLine(JamakolAstrology.Resources.Strings.LabelKarana, $"{GetVal(pd.KaranaName, pd.KaranaTamil)} ", $"({pd.KaranaPercentLeft:F2}% left)");

            string horaLordName = pd.HoraLord;
            if (Enum.TryParse<Planet>(pd.HoraLord, true, out var hPlanet))
                horaLordName = ZodiacUtils.GetPlanetName(hPlanet);

            string horaSignAbbr = "-";
            var horaLordPlanet = chart.Planets.FirstOrDefault(p => p.Name.Equals(pd.HoraLord, StringComparison.OrdinalIgnoreCase));
            if (horaLordPlanet != null)
            {
                int sign = horaLordPlanet.Sign;
                if (sign >= 1 && sign <= 12)
                {
                    string sName = ZodiacUtils.GetSignName(sign);
                    horaSignAbbr = sName.Length >= 2 ? sName.Substring(0, 2) : sName;
                }
            }

            AddLine(JamakolAstrology.Resources.Strings.LabelHoraLord, $"{horaLordName} (5 min sign: {horaSignAbbr})");
            AddLine(JamakolAstrology.Resources.Strings.LabelMahakalaHora, $"{horaLordName}");
            AddLine(JamakolAstrology.Resources.Strings.LabelKaalaLord, "-");

            AddLine("", "");

            AddLine(JamakolAstrology.Resources.Strings.LabelSunrise, $"{pd.Sunrise}");
            AddLine(JamakolAstrology.Resources.Strings.LabelSunset, $"{pd.Sunset}");
            AddLine(JamakolAstrology.Resources.Strings.LabelJanmaGhatis, $"{pd.JanmaGhatis:F4}");

            AddLine("", "");
            AddLine(JamakolAstrology.Resources.Strings.LabelAyanamsa, $"{FormatDegree(pd.AyanamsaValue, true)}");
            AddLine(JamakolAstrology.Resources.Strings.LabelSiderealTime, $"{pd.SiderealTime}");
        }
        else
        {
            AddLine(JamakolAstrology.Resources.Strings.LabelAyanamsa, $"{FormatDegree(chart.AyanamsaValue, true)}");
        }
    }

    // ==================== Search Algorithm ====================

    private DateTime? FindTithiPraveshaMoment(int targetYear, int ayanId, double ayanOffset)
    {
        int month = _natalSunSign switch
        {
            1 => 4, 2 => 5, 3 => 6, 4 => 7, 5 => 8, 6 => 9,
            7 => 10, 8 => 11, 9 => 12, 10 => 1, 11 => 2, 12 => 3,
            _ => 4
        };

        int year = targetYear;
        if (_natalSunSign >= 10 && month <= 3)
            year = targetYear + 1;

        var searchStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);

        for (int day = 0; day < 45; day++)
        {
            var testDate = searchStart.AddDays(day);
            double jdNoon = _ephemeris.GetJulianDay(testDate.Year, testDate.Month, testDate.Day, 12.0);
            var sunPos = _ephemeris.GetPlanetPosition(jdNoon, SwissEph.SE_SUN, ayanId, ayanOffset);
            int currentSunSign = ZodiacUtils.DegreeToSign(sunPos.longitude);

            if (currentSunSign != _natalSunSign) continue;

            var moment = FindCrossingInDay(testDate, ayanId, ayanOffset);
            if (moment != null) return moment;
        }

        return null;
    }

    private double GetTithiDiff(double jd, int ayanId, double ayanOffset)
    {
        var sunPos = _ephemeris.GetPlanetPosition(jd, SwissEph.SE_SUN, ayanId, ayanOffset);
        var moonPos = _ephemeris.GetPlanetPosition(jd, SwissEph.SE_MOON, ayanId, ayanOffset);
        return ZodiacUtils.NormalizeDegree(moonPos.longitude - sunPos.longitude);
    }

    private DateTime? FindCrossingInDay(DateTime date, int ayanId, double ayanOffset)
    {
        for (int hour = 0; hour < 24; hour++)
        {
            double jd1 = _ephemeris.GetJulianDay(date.Year, date.Month, date.Day, hour);
            double jd2 = _ephemeris.GetJulianDay(date.Year, date.Month, date.Day, hour + 1);

            double diff1 = GetTithiDiff(jd1, ayanId, ayanOffset);
            double diff2 = GetTithiDiff(jd2, ayanId, ayanOffset);

            if (DoesCrossTarget(diff1, diff2, _natalTithiDiff))
            {
                double exactJd = BinarySearchJd(jd1, jd2, _natalTithiDiff, ayanId, ayanOffset, 30);

                var sunPos = _ephemeris.GetPlanetPosition(exactJd, SwissEph.SE_SUN, ayanId, ayanOffset);
                int sunSign = ZodiacUtils.DegreeToSign(sunPos.longitude);
                if (sunSign != _natalSunSign) continue;

                return JdToDateTime(exactJd);
            }
        }
        return null;
    }

    private bool DoesCrossTarget(double diff1, double diff2, double target)
    {
        if (diff2 < diff1 - 180) diff2 += 360;
        if (diff2 > diff1 + 180) diff2 -= 360;

        double t = target;
        if (t < Math.Min(diff1, diff2) - 10) t += 360;
        if (t > Math.Max(diff1, diff2) + 10) t -= 360;

        double lo = Math.Min(diff1, diff2);
        double hi = Math.Max(diff1, diff2);
        return t >= lo && t <= hi;
    }

    private double BinarySearchJd(double jdLow, double jdHigh, double target, int ayanId, double ayanOffset, int maxIter)
    {
        for (int i = 0; i < maxIter; i++)
        {
            double jdMid = (jdLow + jdHigh) / 2.0;
            double diffMid = GetTithiDiff(jdMid, ayanId, ayanOffset);
            double diffLow = GetTithiDiff(jdLow, ayanId, ayanOffset);

            double errorMid = NormalizeAngleDiff(diffMid - target);
            double errorLow = NormalizeAngleDiff(diffLow - target);

            if (Math.Abs(errorMid) < 0.001) return jdMid;

            if (errorLow * errorMid < 0)
                jdHigh = jdMid;
            else
                jdLow = jdMid;
        }
        return (jdLow + jdHigh) / 2.0;
    }

    private double NormalizeAngleDiff(double diff)
    {
        while (diff > 180) diff -= 360;
        while (diff < -180) diff += 360;
        return diff;
    }

    private DateTime JdToDateTime(double jd)
    {
        double jd0 = jd + 0.5;
        int z = (int)jd0;
        double f = jd0 - z;

        int a;
        if (z < 2299161) a = z;
        else
        {
            int alpha = (int)((z - 1867216.25) / 36524.25);
            a = z + 1 + alpha - alpha / 4;
        }

        int b = a + 1524;
        int c = (int)((b - 122.1) / 365.25);
        int d = (int)(365.25 * c);
        int e = (int)((b - d) / 30.6001);

        double dayFrac = b - d - (int)(30.6001 * e) + f;
        int day = (int)dayFrac;
        double timeFrac = (dayFrac - day) * 24.0;
        int hour = (int)timeFrac;
        double minFrac = (timeFrac - hour) * 60.0;
        int minute = (int)minFrac;
        int second = (int)((minFrac - minute) * 60.0);

        int month = e < 14 ? e - 1 : e - 13;
        int year = month > 2 ? c - 4716 : c - 4715;

        return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
    }

    // ==================== Helpers ====================

    private string ConvertToDms(double val, bool isLat)
    {
        double d = Math.Abs(val);
        int deg = (int)d;
        double rem = (d - deg) * 60;
        int min = (int)rem;
        double sec = (rem - min) * 60;
        string dir = isLat ? (val >= 0 ? "N" : "S") : (val >= 0 ? "E" : "W");
        return $"{deg} {dir} {min:00}' {sec:00}\"";
    }

    private string FormatDegree(double val, bool normalizeForAyanamsa = false)
    {
        if (normalizeForAyanamsa && val > 180) val = val - 360;
        bool isNegative = val < 0;
        double absVal = Math.Abs(val);
        int dg = (int)absVal;
        double rem = (absVal - dg) * 60;
        int m = (int)rem;
        double s = (rem - m) * 60;
        string sign = isNegative ? "-" : "";
        return $"{sign}{dg}-{m:00}-{s:00.00}";
    }

    private static readonly string[] TithiNames = {
        "Pratipada", "Dwitiya", "Tritiya", "Chaturthi", "Panchami",
        "Shashthi", "Saptami", "Ashtami", "Navami", "Dashami",
        "Ekadashi", "Dwadashi", "Trayodashi", "Chaturdashi", "Purnima", "Amavasya"
    };

    private string GetTithiName(int tithiNum)
    {
        if (tithiNum == 30) return "Amavasya";
        if (tithiNum == 15) return "Purnima";
        int idx = (tithiNum - 1) % 15;
        return TithiNames[idx];
    }

    public ChartData? GetCurrentChart() => _tithiPraveshaChart;
}

public class TithiPraveshaCalculatedEventArgs : EventArgs
{
    public ChartData ChartData { get; }

    public TithiPraveshaCalculatedEventArgs(ChartData chartData)
    {
        ChartData = chartData;
    }
}
