using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Helpers;

namespace JamakolAstrology.Controls;

/// <summary>
/// Panchanga Panel UserControl - displays Tamil almanac details
/// </summary>
public partial class PanchangaPanel : UserControl
{
    public PanchangaPanel()
    {
        InitializeComponent();
        
        // Apply font size from settings
        var settings = AppSettings.Load();
        if (settings != null)
        {
            UiThemeHelper.SetFontSizeRecursive(this, settings.TableFontSize);
        }
    }

    /// <summary>
    /// Update the panel with Panchanga details
    /// </summary>
    public void UpdateDetails(PanchangaDetails details)
    {
        // Nakshatra (English with pada)
        NakshatraText.Text = !string.IsNullOrEmpty(details.NakshatraName) 
            ? $"{details.NakshatraName} ({details.NakshatraPada})" 
            : "-";
        
        // Tithi (English with Paksha)
        TithiText.Text = !string.IsNullOrEmpty(details.TithiName) 
            ? $"{details.Paksha} / {details.TithiName}" 
            : "-";
        
        // Yoga (English)
        YogaText.Text = !string.IsNullOrEmpty(details.YogaName) 
            ? details.YogaName 
            : "-";
        
        // Karana (English)
        KaranaText.Text = !string.IsNullOrEmpty(details.KaranaName) 
            ? details.KaranaName 
            : "-";
        
        // Day (English)
        DayText.Text = !string.IsNullOrEmpty(details.DayName) 
            ? details.DayName 
            : "-";
        
        // Sunrise/Sunset
        SunriseText.Text = !string.IsNullOrEmpty(details.Sunrise) 
            ? details.Sunrise 
            : "-";
        SunsetText.Text = !string.IsNullOrEmpty(details.Sunset) 
            ? details.Sunset 
            : "-";
        
        // Ayanamsa
        AyanamsaText.Text = details.AyanamsaDisplay;
        
        // Nazhikai
        NazhikaiText.Text = !string.IsNullOrEmpty(details.UdayadiNazhikai) 
            ? details.UdayadiNazhikai 
            : "-";
        
        // Hora (English)
        HoraText.Text = !string.IsNullOrEmpty(details.HoraLord) 
            ? details.HoraLord 
            : "-";
            
        // Kala Hora
        KalaHoraText.Text = !string.IsNullOrEmpty(details.KalaHoraLord) 
            ? details.KalaHoraLord 
            : "-";
        
        // Rasi (English - Moon only)
        RasiText.Text = !string.IsNullOrEmpty(details.MoonRasi) 
            ? details.MoonRasi 
            : "-";
        
        // Year/Month (English names)
        YearMonthText.Text = !string.IsNullOrEmpty(details.EnglishYear) 
            ? $"{details.EnglishYear} / {details.EnglishMonth}" 
            : "-";
    }
}
