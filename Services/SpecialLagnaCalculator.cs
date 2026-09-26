using System;
using System.Collections.Generic;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>One special lagna or upagraha: a chart point with a position but no motion.</summary>
public class SpecialLagna
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public double Longitude { get; set; }

    /// <summary>Why this point is less certain than the others, or null.</summary>
    public string? Note { get; set; }

    public int Sign => (int)(Longitude / 30.0) % 12 + 1;
    public string SignName => ZodiacUtils.GetSignName(Sign);
    public double DegreeInSign => Longitude % 30.0;
    public string Degree => ZodiacUtils.FormatDegree(DegreeInSign);
    public string NakshatraName => ZodiacUtils.GetNakshatraName(ZodiacUtils.DegreeToNakshatra(Longitude));
    public int Pada => ZodiacUtils.GetNakshatraPada(Longitude);
}

/// <summary>
/// The special lagnas (Bhava, Hora, Ghatika, Vighatika, Sri, Pranapada) and the Parashari
/// upagrahas Gulika and Mandi. Ported from the Android app.
///
/// The special lagnas are built from two inputs: the Sun's sidereal longitude AT SUNRISE, and the
/// ISHTA KAALA - the time elapsed from sunrise to birth. Each advances from the sunrise Sun at its
/// own fixed rate. Gulika and Mandi instead take the ascendant rising at a particular moment in
/// Saturn's eighth of the day (or night).
///
/// These are the classical points, and are NOT the Jamakol system's Mandhi, which is a different
/// quantity sharing the name and is left untouched in <see cref="SupplementaryPlanetsCalculator"/>.
/// </summary>
public class SpecialLagnaCalculator
{
    /// <summary>A ghatika is 24 minutes; sixty make a day.</summary>
    public const double GhatikaHours = 24.0 / 60.0;

    /// <summary>A vighatika is a sixtieth of a ghatika - 24 seconds.</summary>
    public const double VighatikaHours = GhatikaHours / 60.0;

    /// <summary>A prana is 4 seconds; six make a vighatika.</summary>
    public const double PranasPerVighatika = 6.0;

    private readonly EphemerisService _ephemeris;
    private readonly int _ayanamshaId;
    private readonly double _ayanamshaOffset;

    public SpecialLagnaCalculator(EphemerisService ephemeris, int ayanamshaId, double ayanamshaOffset = 0.0)
    {
        _ephemeris = ephemeris;
        _ayanamshaId = ayanamshaId;
        _ayanamshaOffset = ayanamshaOffset;
    }

    /// <param name="birthJd">Birth instant, UT Julian Day.</param>
    /// <param name="sunriseJd">Sunrise that began this VEDIC day.</param>
    /// <param name="vara">Weekday of that vedic day, which fixes the vela rulership.</param>
    public List<SpecialLagna> Calculate(
        double birthJd, double sunriseJd, double sunsetJd, double nextSunriseJd,
        DayOfWeek vara, double latitude, double longitude)
    {
        double ishtaHours = (birthJd - sunriseJd) * 24.0;
        var points = Lagnas(SunAt(sunriseJd), ishtaHours, MoonAt(birthJd));

        // Gulika and Mandi: the ascendant at points in Saturn's vela.
        bool isDay = birthJd >= sunriseJd && birthJd < sunsetJd;
        double periodStart = isDay ? sunriseJd : sunsetJd;
        double periodEnd = isDay ? sunsetJd : nextSunriseJd;
        double part = (periodEnd - periodStart) / 8.0;
        int saturnPart = isDay
            ? InauspiciousPeriodsCalculator.GulikaDayPart(vara)
            : InauspiciousPeriodsCalculator.GulikaNightPart(vara);

        // Gulika rises at the START of Saturn's vela, Mandi at its MIDDLE. Which point each takes
        // is genuinely contested (Rath: "Opinions Galore: Upagraha rises at the 1. Start of Vela
        // 2. Middle time of vela 3. End of Vela"). Settled in the Android app against Jagannatha
        // Hora for 12 Sep 2026 21:07:11 Chennai: Gulika Ta 11-36-41, Maandi Ta 22-20-12 - the
        // ascendants at the vela's start and middle. Taking the END put Gulika a whole vela late
        // and in a different sign.
        double gulikaJd = periodStart + part * saturnPart;
        double mandiJd = periodStart + part * (saturnPart + 0.5);
        points.Add(Point("Gk", "Gulika", AscendantAt(gulikaJd, latitude, longitude)));
        points.Add(Point("Md", "Mandi", AscendantAt(mandiJd, latitude, longitude)));

        return points;
    }

    /// <summary>
    /// The six special lagnas from the Sun at sunrise, the ishta kaala in hours, and the Moon.
    /// Pure arithmetic, so it can be checked against worked figures directly.
    /// </summary>
    public static List<SpecialLagna> Lagnas(double sunAtSunrise, double ishtaHours, double moonLongitude)
    {
        var points = new List<SpecialLagna>();

        // Bhava Lagna: one sign per 5 ghatikas (2 hours) of ishta kaala.
        points.Add(Point("BL", "Bhava Lagna", sunAtSunrise + ishtaHours / 2.0 * 30.0));

        // Hora Lagna: one sign per 2.5 ghatikas (1 hour) - twice Bhava Lagna's rate.
        points.Add(Point("HL", "Hora Lagna", sunAtSunrise + ishtaHours * 30.0));

        // Ghatika Lagna: one sign per ghatika (24 minutes) - five times Bhava Lagna's rate.
        double gl = Norm(sunAtSunrise + ishtaHours / GhatikaHours * 30.0);
        points.Add(Point("GL", "Ghatika Lagna", gl));

        // Vighatika Lagna: one sign per vighatika - the same step down the scale Ghatika takes
        // from Hora. Not among Parasara's three and without a published worked example, so it is
        // the consistent extension of the scheme rather than a verified figure; it circles the
        // zodiac 300 times a day, so it is extremely sensitive to birth-time accuracy.
        points.Add(Point("ViL", "Vighatika Lagna", sunAtSunrise + ishtaHours / VighatikaHours * 30.0,
            "Extension of the scheme; no published example. Very sensitive to birth time."));

        // Sri Lagna: Ghatika Lagna advanced by the Moon's progress through its nakshatra,
        // measured in signs.
        double span = 360.0 / 27.0;
        double moonNakFraction = (Norm(moonLongitude) % span) / span;
        points.Add(Point("SL", "Sri Lagna", gl + moonNakFraction * 12.0 * 30.0));

        // Pranapada: one degree of arc per PRANA (4 seconds) of ishta kaala, then referred to the
        // Sun's sign type - movable as calculated, fixed from the 9th, dual from the 5th.
        double pranas = ishtaHours / VighatikaHours * PranasPerVighatika;
        int sunSign = (int)(sunAtSunrise / 30.0);
        double shift = (sunSign % 3) switch { 0 => 0.0, 1 => 240.0, _ => 120.0 };
        points.Add(Point("PP", "Pranapada", sunAtSunrise + pranas + shift));

        return points;
    }

    private double SunAt(double jd) => _ephemeris.GetPlanetPosition(jd, (int)Planet.Sun, _ayanamshaId, _ayanamshaOffset).longitude;
    private double MoonAt(double jd) => _ephemeris.GetPlanetPosition(jd, (int)Planet.Moon, _ayanamshaId, _ayanamshaOffset).longitude;
    private double AscendantAt(double jd, double lat, double lon) => _ephemeris.GetAscendant(jd, lat, lon, _ayanamshaId, _ayanamshaOffset);

    private static SpecialLagna Point(string code, string name, double longitude, string? note = null)
        => new() { Code = code, Name = name, Longitude = Norm(longitude), Note = note };

    private static double Norm(double degree)
    {
        double d = degree % 360.0;
        return d < 0 ? d + 360.0 : d;
    }
}
