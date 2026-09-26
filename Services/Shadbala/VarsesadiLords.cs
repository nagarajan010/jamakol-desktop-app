using System;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services.Shadbala;

/// <summary>Which hora the varsesadi bala's sixty-virupa share reads.</summary>
public enum VarsesadiHora
{
    /// <summary>From real sunrise, day and night each divided into twelve. The classical natal hora.</summary>
    Kalahora,

    /// <summary>Equal sixty-minute hours counted from sunrise.</summary>
    EqualFromSunrise,

    /// <summary>Equal sixty-minute hours from 6 AM local mean time, from the 6 AM that precedes the birth.</summary>
    MahakalahoraLmt
}

/// <summary>
/// The four varsesadi lords. The year and month lords need a moment located by root-solving; the
/// day and hora lords are read from the vedic day. Ported from the Android app, whose notes on
/// each rule record what it matched against the deva.guru reference set.
/// </summary>
public class VarsesadiLords
{
    /// <summary>Hora order: each hour's lord is the next in this sequence.</summary>
    public static readonly Planet[] HoraOrder =
        { Planet.Sun, Planet.Venus, Planet.Mercury, Planet.Moon, Planet.Saturn, Planet.Jupiter, Planet.Mars };

    /// <summary>Weekday lords, Sunday first.</summary>
    public static readonly Planet[] WeekdayLord =
        { Planet.Sun, Planet.Moon, Planet.Mars, Planet.Mercury, Planet.Jupiter, Planet.Venus, Planet.Saturn };

    private const int Bisections = 40;

    private readonly EphemerisService _eph;
    private readonly SunriseCalculator _sunrise;
    private readonly int _ayanamshaId;
    private readonly double _ayanamshaOffset;

    public VarsesadiLords(EphemerisService eph, SunriseCalculator sunrise, int ayanamshaId, double ayanamshaOffset = 0)
    {
        _eph = eph;
        _sunrise = sunrise;
        _ayanamshaId = ayanamshaId;
        _ayanamshaOffset = ayanamshaOffset;
    }

    private double Sun(double jd) => _eph.GetPlanetPosition(jd, (int)Planet.Sun, _ayanamshaId, _ayanamshaOffset).longitude;
    private double Moon(double jd) => _eph.GetPlanetPosition(jd, (int)Planet.Moon, _ayanamshaId, _ayanamshaOffset).longitude;

    private static double Wrap180(double x)
    {
        while (x < -180) x += 360;
        while (x > 180) x -= 360;
        return x;
    }

    /// <summary>The new-moon instant at or before a moment, by bisection on the elongation.</summary>
    private double NewMoonAtOrBefore(double from)
    {
        double Elong(double jd) => Wrap180(Moon(jd) - Sun(jd));
        double hi = from;
        int guard = 0;
        while (Elong(hi) < 0 && guard++ < 400) hi -= 0.5;
        double lo = hi - 0.5;
        while (Elong(lo) > 0 && guard++ < 400) lo -= 0.5;
        for (int i = 0; i < Bisections; i++)
        {
            double mid = (lo + hi) / 2;
            if (Elong(mid) <= 0) lo = mid; else hi = mid;
        }
        return (lo + hi) / 2;
    }

    /// <summary>
    /// Chaitra Sukla Pratipada: the new moon falling with the Sun in PISCES. Anchoring instead on
    /// the last new moon before the Mesha ingress picks a different lunation whenever the syzygy
    /// falls in the gap, and that alone accounted for every year-lord mismatch in the Android
    /// app's comparison.
    /// </summary>
    public double? ChaitraSuklaPratipada(double birthJd)
    {
        double t = birthJd;
        for (int i = 0; i < 14; i++)
        {
            double syzygy = NewMoonAtOrBefore(t);
            double sun = Sun(syzygy);
            if (sun >= 330.0 && sun < 360.0) return syzygy;
            t = syzygy - 1.0;
        }
        return null;
    }

    /// <summary>The sankranti that began the solar month holding the Sun at birth.</summary>
    public double SolarMasaStart(double birthJd)
    {
        double cusp = Math.Floor(Sun(birthJd) / 30.0) * 30.0;
        double Dist(double jd) => Wrap180(Sun(jd) - cusp);
        double hi = birthJd;
        int guard = 0;
        while (Dist(hi) < 0 && guard++ < 400) hi -= 1;
        double lo = hi - 1;
        while (Dist(lo) > 0 && guard++ < 400) lo -= 1;
        for (int i = 0; i < Bisections; i++)
        {
            double mid = (lo + hi) / 2;
            if (Dist(mid) <= 0) lo = mid; else hi = mid;
        }
        return (lo + hi) / 2;
    }

    /// <summary>
    /// The vara lord running at an instant, AT A PLACE: the conjunction is global, but the vedic
    /// day turns at the local sunrise, so the same syzygy can fall on different weekdays in
    /// different places. The Android app measured the birth place at 100/100 for the year lord,
    /// against 98 for the capital and 73 for UT midnight.
    /// </summary>
    public Planet LordAtInstant(double instantJd, double latitude, double longitude)
    {
        double lmtOffset = longitude / 15.0;
        double local = instantJd + lmtOffset / 24.0;
        long jdn = (long)Math.Floor(local + 0.5);
        var localDate = EphemerisService.JulianDateToDateTime(instantJd).AddHours(lmtOffset).Date;
        var sunriseLocal = _sunrise.CalculateSunrise(localDate, latitude, longitude, lmtOffset);
        double sunriseJd = ToJd(sunriseLocal, lmtOffset);
        if (instantJd < sunriseJd) jdn -= 1;
        return WeekdayLord[(int)(((jdn + 1) % 7 + 7) % 7)];
    }

    /// <summary>The year lord: the vara at the Chaitra Sukla Pratipada syzygy, at the birth place.</summary>
    public Planet? YearLord(double birthJd, double latitude, double longitude)
    {
        double? syzygy = ChaitraSuklaPratipada(birthJd);
        return syzygy == null ? null : LordAtInstant(syzygy.Value, latitude, longitude);
    }

    /// <summary>The month lord: the vara of the sankranti that began the solar month, at the birth place.</summary>
    public Planet MonthLord(double birthJd, double latitude, double longitude)
        => LordAtInstant(SolarMasaStart(birthJd), latitude, longitude);

    /// <summary>
    /// The hora lord under a convention.
    /// <paramref name="vedicDay"/> is the weekday of the vedic day (turning at sunrise);
    /// the three instants bound it.
    /// </summary>
    public static Planet HoraLord(
        VarsesadiHora hora, double birthJd, DayOfWeek vedicDay,
        double sunriseJd, double sunsetJd, double nextSunriseJd, double longitude)
    {
        int start = Array.IndexOf(HoraOrder, WeekdayLord[(int)vedicDay]);
        switch (hora)
        {
            case VarsesadiHora.Kalahora:
            {
                bool day = birthJd >= sunriseJd && birthJd < sunsetJd;
                double len = day ? (sunsetJd - sunriseJd) / 12.0 : (nextSunriseJd - sunsetJd) / 12.0;
                int i = (int)((birthJd - (day ? sunriseJd : sunsetJd)) / len);
                i = Math.Clamp(i, 0, 11) + (day ? 0 : 12);
                return HoraOrder[(start + i) % 7];
            }
            case VarsesadiHora.EqualFromSunrise:
            {
                int i = (int)Math.Floor((birthJd - sunriseJd) * 24.0);
                return HoraOrder[((start + i) % 7 + 7) % 7];
            }
            default:
            {
                // 6 AM local mean time of the LMT date, stepped back a day when the birth is
                // before it: a birth at 4 AM is the 23rd hora of the cycle that began the
                // previous 6 AM, not outside any hora.
                double local = birthJd + longitude / 360.0;
                double sixAm = Math.Floor(local + 0.5) - 0.5 + 6.0 / 24.0 - longitude / 360.0;
                if (birthJd < sixAm) sixAm -= 1.0;
                // The 6 AM cycle is named by the weekday of that 6 AM's calendar day.
                int dow = (int)(((long)Math.Floor(sixAm + longitude / 360.0 + 0.5) + 1) % 7);
                int s = Array.IndexOf(HoraOrder, WeekdayLord[dow]);
                int i = (int)Math.Floor((birthJd - sixAm) * 24.0);
                return HoraOrder[(s + i) % 7];
            }
        }
    }

    private double ToJd(DateTime local, double offsetHours)
    {
        var u = local.AddHours(-offsetHours);
        return _eph.GetJulianDay(u.Year, u.Month, u.Day, u.Hour + u.Minute / 60.0 + u.Second / 3600.0 + u.Millisecond / 3.6e6);
    }
}
