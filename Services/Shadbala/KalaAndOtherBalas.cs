using System;
using System.Collections.Generic;
using System.Linq;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services.Shadbala;

/// <summary>
/// Dig, kala (natonnata, paksha, tribhaga, varsesadi), ayana, chesta, naisargika and drg bala.
/// Ported from the Android app; the notes on each record what it was measured against there.
/// </summary>
public static class OtherBalas
{
    private static double Norm(double d) => ((d % 360.0) + 360.0) % 360.0;

    private static double Separation(double a, double b)
    {
        double diff = Math.Abs(Norm(a) - Norm(b)) % 360.0;
        return diff > 180.0 ? 360.0 - diff : diff;
    }

    // ---------------------------------------------------------------- Dig

    /// <summary>The angle each graha is strongest at, as a house: east 1, north 4, west 7, south 10.</summary>
    private static readonly Dictionary<Planet, int> DigHouse = new()
    {
        { Planet.Sun, 10 }, { Planet.Mars, 10 }, { Planet.Moon, 4 }, { Planet.Venus, 4 },
        { Planet.Saturn, 7 }, { Planet.Jupiter, 1 }, { Planet.Mercury, 1 }
    };

    /// <summary>
    /// DIG: 60 at the graha's own angle, none at the opposite one, linear between - the arc from
    /// the weak point over three. The angles are equal houses from the lagna. Worst disagreement
    /// with the reference over 700 cells is its one-decimal rounding.
    /// </summary>
    public static double Dig(Planet graha, double longitude, double ascendant)
    {
        double strong = Norm(ascendant + (DigHouse[graha] - 1) * 30.0);
        return Separation(longitude, Norm(strong + 180.0)) / 3.0;
    }

    // ---------------------------------------------------------------- Natonnata

    private static readonly HashSet<Planet> Diurnal = new() { Planet.Sun, Planet.Jupiter, Planet.Venus };

    /// <summary>
    /// The diurnal share: 0 at midnight, 30 at the horizons, 60 at midday - scaled to the REAL
    /// day, so the four segments are unequal in time. Against the reference, real sunrise scores
    /// 604/700 and a plain clock 112/700; the rest are pre-dawn births where the fixture holds
    /// only one day's sunrise pair.
    /// </summary>
    public static double DiurnalShare(double birthHour, double sunriseHour, double sunsetHour)
    {
        double midday = (sunriseHour + sunsetHour) / 2.0;
        double nightLength = (24.0 - sunsetHour) + sunriseHour;
        double midnight = (sunsetHour + nightLength / 2.0) % 24.0;

        static double Fraction(double from, double to, double at)
        {
            double span = ((to - from) % 24.0 + 24.0) % 24.0;
            return span <= 0.0 ? 0.0 : (((at - from) % 24.0 + 24.0) % 24.0) / span;
        }

        if (birthHour >= sunriseHour && birthHour < midday)
            return 30.0 + 30.0 * Fraction(sunriseHour, midday, birthHour);
        if (birthHour >= midday && birthHour < sunsetHour)
            return 60.0 - 30.0 * Fraction(midday, sunsetHour, birthHour);
        if (((birthHour - sunsetHour) % 24.0 + 24.0) % 24.0 < ((midnight - sunsetHour) % 24.0 + 24.0) % 24.0)
            return 30.0 - 30.0 * Fraction(sunsetHour, midnight, birthHour);
        return 30.0 * Fraction(midnight, sunriseHour, birthHour);
    }

    /// <summary>
    /// NATONNATA: diurnal grahas (Sun, Jupiter, Venus) take the share, nocturnal ones (Moon,
    /// Mars, Saturn) its complement, and Mercury - karaka of the measure - is full at all hours.
    /// </summary>
    public static double Natonnata(Planet graha, double birthHour, double sunriseHour, double sunsetHour)
    {
        if (graha == Planet.Mercury) return 60.0;
        double share = DiurnalShare(birthHour, sunriseHour, sunsetHour);
        return Diurnal.Contains(graha) ? share : 60.0 - share;
    }

    // ---------------------------------------------------------------- Paksha

    private static readonly HashSet<Planet> PakshaBenefics = new() { Planet.Moon, Planet.Mercury, Planet.Jupiter, Planet.Venus };

    /// <summary>
    /// PAKSHA: the Moon's elongation from the Sun, folded to 0-180, over three for the benefics
    /// and its complement for the malefics. Mercury is treated as always benefic, as the
    /// reference does. The Moon's share is DOUBLED, and rounded to one decimal BEFORE doubling -
    /// the reference rounds there, and doubling first left 38 of 700 cells a tenth out.
    /// </summary>
    public static double Paksha(Planet graha, double sunLongitude, double moonLongitude)
    {
        double diff = Norm(moonLongitude - sunLongitude);
        double share = (diff <= 180.0 ? diff : 360.0 - diff) / 3.0;
        if (graha == Planet.Moon) return Math.Round(share * 10.0, MidpointRounding.AwayFromZero) / 10.0 * 2.0;
        return PakshaBenefics.Contains(graha) ? share : 60.0 - share;
    }

    // ---------------------------------------------------------------- Tribhaga

    private static readonly Planet[] DayThirds = { Planet.Mercury, Planet.Sun, Planet.Saturn };
    private static readonly Planet[] NightThirds = { Planet.Moon, Planet.Venus, Planet.Mars };

    /// <summary>The lord of the third of the day or night a birth falls in.</summary>
    public static Planet TribhagaLord(double birthHour, double sunriseHour, double sunsetHour)
    {
        if (birthHour >= sunriseHour && birthHour < sunsetHour)
        {
            double len = (sunsetHour - sunriseHour) / 3.0;
            int i = len <= 0 ? 0 : Math.Clamp((int)((birthHour - sunriseHour) / len), 0, 2);
            return DayThirds[i];
        }
        // Night runs sunset -> next sunrise, so a pre-dawn birth wraps back to the previous sunset.
        double nightLength = (24.0 - sunsetHour) + sunriseHour;
        double offset = birthHour >= sunsetHour ? birthHour - sunsetHour : birthHour + 24.0 - sunsetHour;
        double part = nightLength / 3.0;
        int n = part <= 0 ? 0 : Math.Clamp((int)(offset / part), 0, 2);
        return NightThirds[n];
    }

    /// <summary>
    /// TRIBHAGA: 60 to the lord of the third the birth falls in, and 60 to Jupiter always as
    /// karaka - not stacked when Jupiter also rules. Real sunrise, not the lecture's 6am/6pm:
    /// 598/600 against 568/600.
    /// </summary>
    public static double Tribhaga(Planet graha, double birthHour, double sunriseHour, double sunsetHour)
        => graha == Planet.Jupiter || graha == TribhagaLord(birthHour, sunriseHour, sunsetHour) ? 60.0 : 0.0;

    // ---------------------------------------------------------------- Varsesadi

    /// <summary>VARSESADI: year lord 15, month lord 30, day lord 45, hora lord 60; they add.</summary>
    public static double Varsesadi(Planet graha, Planet? year, Planet? month, Planet? day, Planet? hora)
        => (graha == year ? 15.0 : 0) + (graha == month ? 30.0 : 0) + (graha == day ? 45.0 : 0) + (graha == hora ? 60.0 : 0);

    // ---------------------------------------------------------------- Ayana

    private static readonly HashSet<Planet> SouthStrong = new() { Planet.Moon, Planet.Saturn };

    /// <summary>Mean obliquity of the ecliptic, good to arcseconds over the app's range.</summary>
    public static double Obliquity(double julianDay) => 23.439291 - 0.0130042 * ((julianDay - 2451545.0) / 36525.0);

    /// <summary>Declination from the TROPICAL longitude alone; latitude is deliberately excluded.</summary>
    public static double Declination(double tropicalLongitude, double obliquity)
        => Math.Asin(Math.Sin(obliquity * Math.PI / 180) * Math.Sin(tropicalLongitude * Math.PI / 180)) * 180 / Math.PI;

    /// <summary>
    /// AYANA: (24 + kranti) / 48 * 60, the kranti counted north-positive for Sun, Mars, Jupiter
    /// and Venus, south-positive for Moon and Saturn, and always positive for Mercury; the Sun
    /// DOUBLED last. Follows Raman, who reproduces his Example 33 on 7 of 7, and Shri Jyoti Star
    /// - not deva.guru, which differs by a mean of 3.49 virupa here and is not followed.
    /// </summary>
    public static double Ayana(Planet graha, double declination)
    {
        double kranti = graha == Planet.Mercury ? Math.Abs(declination)
                      : SouthStrong.Contains(graha) ? -declination
                      : declination;
        double v = (24.0 + kranti) / 48.0 * 60.0;
        return graha == Planet.Sun ? v * 2.0 : v;
    }

    // ---------------------------------------------------------------- Chesta

    /// <summary>The grahas with a chesta of their own; the luminaries borrow, the nodes have none.</summary>
    public static readonly Planet[] ChestaComputed = { Planet.Mars, Planet.Mercury, Planet.Jupiter, Planet.Venus, Planet.Saturn };

    /// <summary>
    /// CHESTA for the five non-luminaries: the kendra between the true Sun and the graha's TRUE
    /// HELIOCENTRIC longitude (its sighrocca), reduced to 0-180, over three. Reproduces the
    /// reference on 500 of 500 cells, the mean residual exactly half its display rounding.
    /// </summary>
    public static double Chesta(double sunLongitude, double heliocentricLongitude)
    {
        double k = Norm(sunLongitude - heliocentricLongitude);
        return (k > 180.0 ? 360.0 - k : k) / 3.0;
    }

    /// <summary>
    /// The luminaries never retrograde and BORROW: the Sun half its (doubled) ayana, the Moon half
    /// its paksha. That double-counts both on purpose - it is the classical treatment, and the
    /// halving is what puts a 0-120 component onto chesta's 0-60 scale.
    /// </summary>
    public static double ChestaBorrowed(double fullComponent) => fullComponent * 0.5;

    // ---------------------------------------------------------------- Naisargika

    private static readonly Dictionary<Planet, int> NaisargikaRank = new()
    {
        { Planet.Saturn, 1 }, { Planet.Mars, 2 }, { Planet.Mercury, 3 }, { Planet.Jupiter, 4 },
        { Planet.Venus, 5 }, { Planet.Moon, 6 }, { Planet.Sun, 7 }
    };

    /// <summary>NAISARGIKA: one rupa divided by seven, times 1..7 from Saturn to the Sun. A constant.</summary>
    public static double Naisargika(Planet graha) => Math.Round(60.0 / 7.0 * NaisargikaRank[graha], 2, MidpointRounding.AwayFromZero);

    // ---------------------------------------------------------------- Drg

    private static readonly HashSet<Planet> Subha = new() { Planet.Jupiter, Planet.Venus, Planet.Moon, Planet.Mercury };

    /// <summary>The raw drishti for an angle FROM the aspecting graha TO the aspected one.</summary>
    public static double Drishti(double angle)
    {
        double a = Norm(angle);
        if (a < 30) return 0;
        if (a < 60) return (a - 30) / 30 * 15;
        if (a < 90) return 15 + (a - 60) / 30 * 30;
        if (a < 120) return 45 - (a - 90) / 30 * 15;
        if (a < 150) return 30 - (a - 120) / 30 * 30;
        if (a < 180) return 0;
        if (a < 300) return 60 - (a - 180) / 120 * 60;
        return 0;
    }

    /// <summary>Visesa drishti at the true angle: Mars +15, Jupiter +30, Saturn +45, half-open ranges.</summary>
    public static double Visesa(Planet from, double angle)
    {
        double a = Norm(angle);
        bool In(double lo, double hi) => a >= lo && a < hi;
        return from switch
        {
            Planet.Mars => In(90, 120) || In(210, 240) ? 15 : 0,
            Planet.Jupiter => In(120, 150) || In(240, 270) ? 30 : 0,
            Planet.Saturn => In(60, 90) || In(270, 300) ? 45 : 0,
            _ => 0
        };
    }

    /// <summary>
    /// DRG, the PARASARA system: every aspect ADDS, a subha aspect at five quarters of its value
    /// and a papa aspect at three - never negative. (The Sripati system subtracts papa aspects
    /// and gives small signed numbers; it is the other column of the same page, not an error.)
    /// Agrees with the reference on all 700 cells.
    /// </summary>
    public static double Drg(Planet drsya, IReadOnlyDictionary<Planet, double> longitudes)
    {
        double total = 0;
        foreach (var (drsta, lon) in longitudes)
        {
            if (drsta == drsya) continue;
            double angle = Norm(longitudes[drsya] - lon);
            double v = (Drishti(angle) + Visesa(drsta, angle)) * (Subha.Contains(drsta) ? 1.25 : 0.75);
            if (v > 0) total += v;
        }
        return total;
    }
}
