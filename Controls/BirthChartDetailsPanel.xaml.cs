using System.Windows;
using System.Windows.Controls;
using JamakolAstrology.Models;

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
        KpDetailsControl.UpdateChart(chartData);
        AVDetailsControl.UpdateChart(chartData);
    }

    /// <summary>
    /// Update all details including text box
    /// </summary>
    public void UpdateDetails(CompositeChartResult result)
    {
        UpdatePlanetaryPositions(result.ChartData);
        UpdateDashas(result.DashaResult);

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

        // Basic Info
        AddLine("Date:          ", $"{bd.BirthDateTime:MMMM d, yyyy}");
        AddLine("Time:          ", $"{bd.BirthDateTime:h:mm:ss tt}".ToLower());
        
        var tzSpan = TimeSpan.FromHours(bd.TimeZoneOffset);
        AddLine("Time Zone:     ", $"{tzSpan.Hours}:{tzSpan.Minutes:00}:{tzSpan.Seconds:00} (East of GMT)");
        
        string latStr = ConvertToDms(bd.Latitude, true);
        string longStr = ConvertToDms(bd.Longitude, false);
        AddLine("Place:         ", $"{longStr}, {latStr}");
        AddLine("               ", $"{bd.Location}");
        AddLine("Altitude:      ", "0.00 meters");

        AddLine("", "");

        // Panchanga
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
        AddLine("Ayanamsa:      ", $"{FormatDegree(pd.AyanamsaValue)}");
        AddLine("Sidereal Time: ", $"{pd.SiderealTime}");
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

    private string FormatDegree(double val)
    {
        int d = (int)val;
        double rem = (val - d) * 60;
        int m = (int)rem;
        double s = (rem - m) * 60;
        return $"{d}-{m:00}-{s:00.00}";
    }

    /// <summary>
    /// Update Dasha details
    /// </summary>
    public void UpdateDashas(DashaResult? result)
    {
        if (result == null)
        {
            CurrentDashaText.Text = "-";
            CurrentDashaDates.Text = "-";
            CurrentDashaLevels.Text = "-";
            DashaTreeView.ItemsSource = null;
            return;
        }

        // Set TreeView source
        DashaTreeView.ItemsSource = result.MahaDashas;

        // Set Current Dasha Texts
        if (result.CurrentAntarDasha != null)
        {
            // Format: Jupiter / Saturn / Mercury
            CurrentDashaText.Text = result.CurrentDashaDisplay;
            
            // Format: 15-Oct-2023 to 22-Feb-2024 (showing range of deepest active level)
            var deepest = result.CurrentDehaDasha ?? 
                          result.CurrentPranaDasha ?? 
                          result.CurrentSookshmaDasha ?? 
                          result.CurrentPratyantaraDasha ?? 
                          result.CurrentAntarDasha;
                          
            CurrentDashaDates.Text = deepest != null 
                ? $"{deepest.DisplayName} ends on {deepest.EndDate:dd-MMM-yyyy HH:mm}" 
                : "";

            // Full chain
            string levels = $"{result.CurrentMahaDasha?.Planet} > {result.CurrentAntarDasha?.Planet} > {result.CurrentPratyantaraDasha?.Planet}";
            if (result.CurrentSookshmaDasha != null) levels += $" > {result.CurrentSookshmaDasha.Planet}";
            if (result.CurrentPranaDasha != null) levels += $" > {result.CurrentPranaDasha.Planet}";
            if (result.CurrentDehaDasha != null) levels += $" > {result.CurrentDehaDasha.Planet}";
            
            CurrentDashaLevels.Text = levels;
        }
    }

    /// <summary>
    /// Clear all data
    /// </summary>
    public void Clear()
    {
        PlanetGridControl.DataGridControl.ItemsSource = null;
        KpDetailsControl.ClearChart();
        AVDetailsControl.ClearChart();
        NatalDetailsText.Text = "No content available";
        AshtakavargaText.Text = "No content available";
        UpdateDashas(null);
    }
}
