using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;
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
    /// <summary>
    /// Update the panel with Panchanga details
    /// </summary>
    public void UpdateDetails(PanchangaDetails details)
    {
        bool isTamil = ZodiacUtils.IsTamil;

        // Nakshatra
        string nakName = isTamil ? details.NakshatraTamil : details.NakshatraName;
        NakshatraText.Text = !string.IsNullOrEmpty(nakName) 
            ? $"{nakName} ({details.NakshatraPada})" 
            : "-";
        
        // Tithi
        string tithiName = isTamil ? details.TithiTamil : details.TithiName;
        string paksha = isTamil ? details.PakshaTamil : details.Paksha;
        TithiText.Text = !string.IsNullOrEmpty(tithiName) 
            ? $"{paksha} / {tithiName}" 
            : "-";
        
        // Yoga
        string yogaName = isTamil ? details.YogaTamil : details.YogaName;
        YogaText.Text = !string.IsNullOrEmpty(yogaName) 
            ? yogaName 
            : "-";
        
        // Karana
        string karanaName = isTamil ? details.KaranaTamil : details.KaranaName;
        KaranaText.Text = !string.IsNullOrEmpty(karanaName) 
            ? karanaName 
            : "-";
        
        // Day
        string dayName = isTamil ? details.DayTamil : details.DayName;
        DayText.Text = !string.IsNullOrEmpty(dayName) 
            ? dayName 
            : "-";
        
        // Sunrise/Sunset - Time is culture invariant mostly (digits), but maybe "AM/PM" vs "மு/பி"?
        // DateTime.ToString() handles it if culture set. detailed.Sunrise is string. 
        // Keeping as is for now as it's time string.
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
        
        // Hora
        // Use Pre-calculated Tamil property if available, else standard
        string hora = isTamil ? details.HoraLordTamil : details.HoraLord;
        HoraText.Text = !string.IsNullOrEmpty(hora) 
            ? hora 
            : "-";
            
        // Kala Hora
        string kalaHora = isTamil ? details.KalaHoraLordTamil : details.KalaHoraLord;
        KalaHoraText.Text = !string.IsNullOrEmpty(kalaHora) 
            ? kalaHora 
            : "-";
        
        // Rasi (Moon)
        string rasi = isTamil ? details.MoonRasiTamil : details.MoonRasi;
        RasiText.Text = !string.IsNullOrEmpty(rasi) 
            ? rasi 
            : "-";
        
        // Year/Month
        // Use localized strings
        string year = isTamil ? details.TamilYear : details.EnglishYear;
        string month = isTamil ? details.TamilMonth : details.EnglishMonth;
        YearMonthText.Text = !string.IsNullOrEmpty(year) 
            ? $"{year} / {month}" 
            : "-";
    }
}
