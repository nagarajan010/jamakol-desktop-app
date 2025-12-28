using System;
using System.Windows;
using System.Windows.Controls;
using JamakolAstrology.Models;
using System.Linq;

namespace JamakolAstrology.Controls;

public partial class BirthChartDetailsPanel : UserControl
{
    public BirthChartDetailsPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Update the planetary positions grid using ChartData
    /// </summary>
    public void UpdatePlanetaryPositions(ChartData chartData)
    {
        PlanetGridControl.UpdateGrid(chartData);
    }

    /// <summary>
    /// Update all details including text box
    /// </summary>
    public void UpdateDetails(CompositeChartResult result)
    {
        UpdatePlanetaryPositions(result.ChartData);

        var bd = result.ChartData.BirthData;
        var pd = result.PanchangaDetails;

        NatalDetailsText.Inlines.Clear();
        
        var labelBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#990000")); // Maroon
        // var valueBrush = System.Windows.Media.Brushes.Black;
        var valueBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333333"));
        var dimBrush = System.Windows.Media.Brushes.Gray;
        var percentBrush = System.Windows.Media.Brushes.DimGray;

        void AddLine(string label, string value, string extra = "", bool isHeader = false)
        {
            if (isHeader)
            {
                NatalDetailsText.Inlines.Add(new System.Windows.Documents.Run(label + "\n") 
                { 
                    FontWeight = FontWeights.Bold, 
                    Foreground = labelBrush,
                    FontSize = 14
                });
                NatalDetailsText.Inlines.Add(new System.Windows.Documents.Run("\n"));
                return;
            }

            if (string.IsNullOrEmpty(label) && string.IsNullOrEmpty(value))
            {
                NatalDetailsText.Inlines.Add(new System.Windows.Documents.Run("\n"));
                return;
            }

            NatalDetailsText.Inlines.Add(new System.Windows.Documents.Run(label) { Foreground = labelBrush, FontWeight = FontWeights.SemiBold });
            NatalDetailsText.Inlines.Add(new System.Windows.Documents.Run(value) { Foreground = valueBrush });
            if (!string.IsNullOrEmpty(extra))
            {
                NatalDetailsText.Inlines.Add(new System.Windows.Documents.Run(extra) { Foreground = percentBrush, FontSize = 11 });
            }
            NatalDetailsText.Inlines.Add(new System.Windows.Documents.Run("\n"));
        }

        // Header
        AddLine(JamakolAstrology.Resources.Strings.NatalChartDetails, "", "", true);

        // Basic Info - use GetDisplayDate/GetDisplayTime for BC date support
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

        // Helper for culture-aware string selection
        string GetVal(string en, string ta) => JamakolAstrology.Services.ZodiacUtils.IsTamil ? ta : en;
        
        // Panchanga - only show if NOT a BC date (sunrise/panchanga calculations not available for BC dates)
        if (!bd.IsBCDate)
        {
            AddLine(JamakolAstrology.Resources.Strings.LabelLunarYearMonth, $"{GetVal(pd.EnglishYear, pd.TamilYear)} - {GetVal(pd.EnglishMonth, pd.TamilMonth)}");
            
            string tithiName = GetVal(pd.TithiName, pd.TithiTamil);
            string paksha = GetVal(pd.Paksha, pd.PakshaTamil);
            AddLine(JamakolAstrology.Resources.Strings.LabelTithi, $"{paksha} {tithiName} ({pd.TithiLord}) ", $"({pd.TithiPercentLeft:F2}% left)");
            
            AddLine(JamakolAstrology.Resources.Strings.LabelVedicWeekday, $"{GetVal(pd.DayName, pd.DayTamil)} ({pd.DayLordAbbr})");
            AddLine(JamakolAstrology.Resources.Strings.LabelNakshatra, $"{GetVal(pd.NakshatraName, pd.NakshatraTamil)} ({pd.NakshatraLord}) ", $"({pd.NakshatraPercentLeft:F2}% left)");
            AddLine(JamakolAstrology.Resources.Strings.LabelYoga, $"{GetVal(pd.YogaName, pd.YogaTamil)} ", $"({pd.YogaPercentLeft:F2}% left)");
            AddLine(JamakolAstrology.Resources.Strings.LabelKarana, $"{GetVal(pd.KaranaName, pd.KaranaTamil)} ", $"({pd.KaranaPercentLeft:F2}% left)");
            
            string horaSignAbbr = "-";
            var horaLordPlanet = result.ChartData.Planets.FirstOrDefault(p => p.Name.Equals(pd.HoraLord, StringComparison.OrdinalIgnoreCase));
            if (horaLordPlanet != null)
            {
                 int sign = horaLordPlanet.Sign;
                 if (sign >= 1 && sign <= 12) 
                 {
                     string sName = JamakolAstrology.Services.ZodiacUtils.GetSignName(sign);
                     if (sName.Length >= 2) horaSignAbbr = sName.Substring(0, 2);
                     else horaSignAbbr = sName;
                 }
            }
            
            // Localize Hora Lord Name if possible
            string horaLordName = pd.HoraLord;
            if (Enum.TryParse<JamakolAstrology.Models.Planet>(pd.HoraLord, true, out var hPlanet))
            {
                horaLordName = JamakolAstrology.Services.ZodiacUtils.GetPlanetName(hPlanet);
            }
            
            AddLine(JamakolAstrology.Resources.Strings.LabelHoraLord, $"{horaLordName} (5 min sign: {horaSignAbbr})");
            AddLine(JamakolAstrology.Resources.Strings.LabelMahakalaHora, $"{horaLordName}"); 
            AddLine(JamakolAstrology.Resources.Strings.LabelKaalaLord, "-");

            AddLine("", "");

            // Sun & Time
            AddLine(JamakolAstrology.Resources.Strings.LabelSunrise, $"{pd.Sunrise}");
            AddLine(JamakolAstrology.Resources.Strings.LabelSunset, $"{pd.Sunset}");
            AddLine(JamakolAstrology.Resources.Strings.LabelJanmaGhatis, $"{pd.JanmaGhatis:F4}");

            AddLine("", "");
            AddLine(JamakolAstrology.Resources.Strings.LabelAyanamsa, $"{FormatDegree(pd.AyanamsaValue, true)}");;
            AddLine(JamakolAstrology.Resources.Strings.LabelSiderealTime, $"{pd.SiderealTime}");
        }
        else
        {
            // BC date - show basic info from chartData instead
            AddLine("Note:          ", "BC date - Panchanga details not available");
            AddLine("", "");
            AddLine(JamakolAstrology.Resources.Strings.LabelAyanamsa, $"{FormatDegree(result.ChartData.AyanamsaValue, true)}");;
        }
    }

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
        // For ayanamsa, values > 180° should be displayed as negative (common for BC dates)
        // e.g., 311.68° → -48.32° (360 - 311.68 = 48.32)
        if (normalizeForAyanamsa && val > 180)
        {
            val = val - 360;
        }
        
        bool isNegative = val < 0;
        double absVal = Math.Abs(val);
        int d = (int)absVal;
        double rem = (absVal - d) * 60;
        int m = (int)rem;
        double s = (rem - m) * 60;
        
        string sign = isNegative ? "-" : "";
        return $"{sign}{d}-{m:00}-{s:00.00}";
    }

    /// <summary>
    /// Clear all data
    /// </summary>
    public void Clear()
    {
        PlanetGridControl.DataGridControl.ItemsSource = null;
        NatalDetailsText.Text = "No content available";
        AshtakavargaText.Text = "No content available";
    }
}
