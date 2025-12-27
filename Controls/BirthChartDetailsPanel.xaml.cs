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
        AddLine("Natal Chart", "", "", true);

        // Basic Info - use GetDisplayDate/GetDisplayTime for BC date support
        AddLine("Date:          ", bd.GetDisplayDate());
        AddLine("Time:          ", bd.GetDisplayTime());
        
        var tzSpan = TimeSpan.FromHours(bd.TimeZoneOffset);
        AddLine("Time Zone:     ", $"{tzSpan.Hours}:{tzSpan.Minutes:00}:{tzSpan.Seconds:00} (East of GMT)");
        
        string latStr = ConvertToDms(bd.Latitude, true);
        string longStr = ConvertToDms(bd.Longitude, false);
        AddLine("Place:         ", $"{longStr}, {latStr}");
        AddLine("               ", $"{bd.Location}");
        AddLine("Altitude:      ", "0.00 meters");

        AddLine("", "");

        // Panchanga - only show if NOT a BC date (sunrise/panchanga calculations not available for BC dates)
        if (!bd.IsBCDate)
        {
            AddLine("Lunar Yr-Mo:   ", $"{pd.TamilYear} - {pd.TamilMonth}");
            AddLine("Tithi:         ", $"{pd.Paksha} {pd.TithiName} ({pd.TithiLord}) ", $"({pd.TithiPercentLeft:F2}% left)");
            AddLine("Vedic Weekday: ", $"{pd.DayName} ({pd.DayLordAbbr})");
            AddLine("Nakshatra:     ", $"{pd.NakshatraName} ({pd.NakshatraLord}) ", $"({pd.NakshatraPercentLeft:F2}% left)");
            AddLine("Yoga:          ", $"{pd.YogaName} ", $"({pd.YogaPercentLeft:F2}% left)");
            AddLine("Karana:        ", $"{pd.KaranaName} ", $"({pd.KaranaPercentLeft:F2}% left)");
            
            string horaSignAbbr = "-";
            var horaLordPlanet = result.ChartData.Planets.FirstOrDefault(p => p.Name.Equals(pd.HoraLord, StringComparison.OrdinalIgnoreCase));
            if (horaLordPlanet != null)
            {
                 int sign = horaLordPlanet.Sign;
                 if (sign >= 1 && sign <= 12) 
                 {
                     string sName = JamakolAstrology.Services.ZodiacUtils.SignNames[sign];
                     if (sName.Length >= 2) horaSignAbbr = sName.Substring(0, 2);
                 }
            }
            AddLine("Hora Lord:     ", $"{pd.HoraLord} (5 min sign: {horaSignAbbr})");
            AddLine("Mahakala Hora: ", $"{pd.HoraLord}"); 
            AddLine("Kaala Lord:    ", "-");

            AddLine("", "");

            // Sun & Time
            AddLine("Sunrise:       ", $"{pd.Sunrise}");
            AddLine("Sunset:        ", $"{pd.Sunset}");
            AddLine("Janma Ghatis:  ", $"{pd.JanmaGhatis:F4}");

            AddLine("", "");
            AddLine("Ayanamsa:      ", $"{FormatDegree(pd.AyanamsaValue, true)}");;
            AddLine("Sidereal Time: ", $"{pd.SiderealTime}");
        }
        else
        {
            // BC date - show basic info from chartData instead
            AddLine("Note:          ", "BC date - Panchanga details not available");
            AddLine("", "");
            AddLine("Ayanamsa:      ", $"{FormatDegree(result.ChartData.AyanamsaValue, true)}");;
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
