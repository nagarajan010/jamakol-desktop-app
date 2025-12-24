namespace JamakolAstrology.Models;

/// <summary>
/// Model for Panchanga (Tamil almanac) details
/// </summary>
public class PanchangaDetails
{
    // Nakshatra (Moon's star)
    public string NakshatraName { get; set; } = "";
    public string NakshatraTamil { get; set; } = "";
    public int NakshatraPada { get; set; }
    public string NakshatraDisplay => $"{NakshatraTamil} ({NakshatraPada})";

    // Tithi (Lunar day)
    public string TithiName { get; set; } = "";
    public string TithiTamil { get; set; } = "";
    public string Paksha { get; set; } = ""; // Shukla/Krishna
    public string PakshaTamil { get; set; } = "";
    public string TithiDisplay => $"{PakshaTamil} / {TithiTamil}";

    // Yoga (27 yogas)
    public string YogaName { get; set; } = "";
    public string YogaTamil { get; set; } = "";

    // Karana (Half-tithi)
    public string KaranaName { get; set; } = "";
    public string KaranaTamil { get; set; } = "";

    // Day
    public string DayName { get; set; } = "";
    public string DayTamil { get; set; } = "";

    // Sun times
    public string Sunrise { get; set; } = "";
    public string Sunset { get; set; } = "";

    // Ayanamsa
    public double AyanamsaValue { get; set; }
    public string AyanamsaDisplay => $"{(int)AyanamsaValue}°{(int)((AyanamsaValue % 1) * 60)}'{(int)(((AyanamsaValue % 1) * 60 % 1) * 60)}\"";

    // Udayadi Nazhikai (time from sunrise in Nazhikai)
    public string UdayadiNazhikai { get; set; } = "";

    // Rasi (Sun and Moon signs)
    public string SunRasi { get; set; } = "";
    public string SunRasiTamil { get; set; } = "";
    public string MoonRasi { get; set; } = "";
    public string MoonRasiTamil { get; set; } = "";
    public string RasiDisplay => $"{MoonRasiTamil}, {SunRasiTamil}";

    // Hora (current hora lord)
    public string HoraLord { get; set; } = "";
    public string HoraLordTamil { get; set; } = "";

    // Year (Tamil year)
    public string TamilYear { get; set; } = "";
    public string TamilMonth { get; set; } = "";
}
