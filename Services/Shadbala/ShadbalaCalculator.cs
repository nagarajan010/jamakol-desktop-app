using System;
using System.Collections.Generic;
using System.Linq;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services.Shadbala;

/// <summary>One graha's shadbala, component by component, against its requirement.</summary>
public class ShadbalaResult
{
    public Planet Graha { get; set; }

    public double Uccha { get; set; }
    public double Saptavargiya { get; set; }
    public double OjaYugma { get; set; }
    public double Kendradi { get; set; }
    public double Drekkana { get; set; }
    public double Sthana => Uccha + Saptavargiya + OjaYugma + Kendradi + Drekkana;

    public double Dig { get; set; }

    public double Natonnata { get; set; }
    public double Paksha { get; set; }
    public double Tribhaga { get; set; }
    public double Varsesadi { get; set; }
    /// <summary>Natonnata + paksha + tribhaga + varsesadi. Ayana is NOT inside it here.</summary>
    public double Kala => Natonnata + Paksha + Tribhaga + Varsesadi;

    public double Ayana { get; set; }
    public double Chesta { get; set; }
    public double Naisargika { get; set; }
    public double Drg { get; set; }

    /// <summary>The seven terms, each at full weight.</summary>
    public double Total => Sthana + Dig + Kala + Ayana + Chesta + Naisargika + Drg;

    /// <summary>The virupa this graha must reach to count as strong - Parasara's figures.</summary>
    public double Requirement { get; set; }
    public double Ratio => Requirement > 0 ? Total / Requirement : 0;
    public bool IsStrong => Ratio >= 1.0;
    public double Rupa => Total / 60.0;

    public string GrahaName => ZodiacUtils.GetPlanetName(Graha);
    public string Verdict => IsStrong ? "Strong" : "Weak";
}

/// <summary>
/// Shadbala - the six-fold strength, and the only number that combines the components. Ported
/// from the Android app, whose grouping was measured against the 100-chart reference set: sthana
/// is exactly its five parts, kala exactly its four, and the total the seven terms below with
/// every coefficient solving to 1.
///
///     sthana     uccha + saptavargiya + oja-yugma + kendradi + drekkana
///     dig
///     kala       natonnata + paksha + tribhaga + varsesadi
///     ayana
///     chesta
///     naisargika
///     drg
///
/// The Sun's ayana and the Moon's paksha each appear twice - in their own right, and halved as
/// the chesta the luminaries borrow. That is the classical treatment, not an oversight.
///
/// Yuddha bala is not a term: planetary war needs two grahas within about a degree, the reference
/// carries no column for it, and its total is the seven terms with nothing left over. A chart with
/// a genuine graha yuddha will read very slightly high.
/// </summary>
public class ShadbalaCalculator
{
    /// <summary>The virupa each graha must reach. Parasara's figures, constant for every chart.</summary>
    public static readonly Dictionary<Planet, double> Requirements = new()
    {
        { Planet.Sun, 390 }, { Planet.Moon, 360 }, { Planet.Mars, 300 }, { Planet.Mercury, 420 },
        { Planet.Jupiter, 390 }, { Planet.Venus, 330 }, { Planet.Saturn, 300 }
    };

    private readonly EphemerisService _eph;
    private readonly SunriseCalculator _sunrise;
    private readonly DivisionalChartService _vargas = new();
    private readonly int _ayanamshaId;
    private readonly double _ayanamshaOffset;

    public ShadbalaCalculator(EphemerisService eph, SunriseCalculator sunrise, int ayanamshaId, double ayanamshaOffset = 0)
    {
        _eph = eph;
        _sunrise = sunrise;
        _ayanamshaId = ayanamshaId;
        _ayanamshaOffset = ayanamshaOffset;
    }

    /// <param name="birthLocal">Birth moment on the local civil clock.</param>
    /// <param name="sunriseJd">Sunrise that began the VEDIC day (UT); a pre-dawn birth's is the previous day's.</param>
    /// <param name="vedicDay">Weekday of that vedic day.</param>
    public List<ShadbalaResult> Calculate(
        ChartData chart, DateTime birthLocal, double timeZoneOffset,
        double sunriseJd, double sunsetJd, double nextSunriseJd, DayOfWeek vedicDay,
        double latitude, double longitude,
        VarsesadiHora hora = VarsesadiHora.EqualFromSunrise)
    {
        // Only the seven real graha rows - the Aprakash grahas reuse Planet.Sun as a placeholder.
        var lon = new Dictionary<Planet, double>();
        foreach (var p in chart.Planets)
        {
            if (!SthanaBala.Scorable.Contains(p.Planet)) continue;
            if (!ZodiacUtils.PlanetNames.TryGetValue(p.Planet, out var realName) || p.Name != realName) continue;
            lon[p.Planet] = p.Longitude;
        }
        if (lon.Count < SthanaBala.Scorable.Length) return new List<ShadbalaResult>();

        double birthJd = chart.JulianDay;
        double asc = chart.AscendantDegree;

        // Hours from the birth's local civil midnight. A pre-dawn birth's vedic sunrise and sunset
        // are the previous day's and come out negative, which the day/night arithmetic expects.
        double midnightJd = birthJd - birthLocal.TimeOfDay.TotalHours / 24.0;
        double H(double jd) => (jd - midnightJd) * 24.0;
        double birthH = H(birthJd), riseH = H(sunriseJd), setH = H(sunsetJd);

        var lords = new VarsesadiLords(_eph, _sunrise, _ayanamshaId, _ayanamshaOffset);
        Planet? yearLord = lords.YearLord(birthJd, latitude, longitude);
        Planet monthLord = lords.MonthLord(birthJd, latitude, longitude);
        Planet dayLord = VarsesadiLords.WeekdayLord[(int)vedicDay];
        Planet horaLord = VarsesadiLords.HoraLord(hora, birthJd, vedicDay, sunriseJd, sunsetJd, nextSunriseJd, longitude);

        // Ayana reads the SAYANA longitude, so the ayanamsa goes back on.
        double obliquity = OtherBalas.Obliquity(birthJd);
        double Declination(Planet g) => OtherBalas.Declination(SthanaBala.Norm(lon[g] + chart.AyanamsaValue), obliquity);

        var results = new List<ShadbalaResult>();
        foreach (var g in SthanaBala.Scorable)
        {
            results.Add(new ShadbalaResult
            {
                Graha = g,
                Uccha = SthanaBala.Uccha(g, lon[g]),
                Saptavargiya = SthanaBala.Saptavargiya(g, lon, _vargas),
                OjaYugma = SthanaBala.OjaYugma(g, lon[g]),
                Kendradi = SthanaBala.Kendradi(lon[g], asc),
                Drekkana = SthanaBala.Drekkana(g, lon[g]),
                Dig = OtherBalas.Dig(g, lon[g], asc),
                Natonnata = OtherBalas.Natonnata(g, birthH, riseH, setH),
                Paksha = OtherBalas.Paksha(g, lon[Planet.Sun], lon[Planet.Moon]),
                Tribhaga = OtherBalas.Tribhaga(g, birthH, riseH, setH),
                Varsesadi = OtherBalas.Varsesadi(g, yearLord, monthLord, dayLord, horaLord),
                Ayana = OtherBalas.Ayana(g, Declination(g)),
                Naisargika = OtherBalas.Naisargika(g),
                Drg = OtherBalas.Drg(g, lon),
                Requirement = Requirements[g]
            });
        }

        // Chesta: the five compute theirs from the heliocentric sighrocca; the luminaries borrow
        // half of the Sun's ayana and the Moon's paksha, passed at full value.
        foreach (var r in results)
        {
            if (r.Graha == Planet.Sun) r.Chesta = OtherBalas.ChestaBorrowed(r.Ayana);
            else if (r.Graha == Planet.Moon) r.Chesta = OtherBalas.ChestaBorrowed(r.Paksha);
            else
            {
                double? helio = _eph.GetHeliocentricLongitude(birthJd, (int)r.Graha, _ayanamshaId, _ayanamshaOffset);
                r.Chesta = helio == null ? 0 : OtherBalas.Chesta(lon[Planet.Sun], helio.Value);
            }
        }

        return results;
    }
}

/// <summary>
/// Everything the shadbala needs beyond the chart, gathered once by the orchestrator so the panel
/// can recompute when the reader switches the varsesadi hora.
/// </summary>
public class ShadbalaContext
{
    public ChartData Chart { get; set; } = null!;
    public DateTime BirthLocal { get; set; }
    public double TimeZoneOffset { get; set; }
    public double SunriseJd { get; set; }
    public double SunsetJd { get; set; }
    public double NextSunriseJd { get; set; }
    public DayOfWeek VedicDay { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int AyanamshaId { get; set; }
    public double AyanamshaOffset { get; set; }

    public List<ShadbalaResult> Compute(VarsesadiHora hora)
    {
        using var eph = new EphemerisService();
        return new ShadbalaCalculator(eph, new SunriseCalculator(), AyanamshaId, AyanamshaOffset)
            .Calculate(Chart, BirthLocal, TimeZoneOffset, SunriseJd, SunsetJd, NextSunriseJd,
                       VedicDay, Latitude, Longitude, hora);
    }
}
