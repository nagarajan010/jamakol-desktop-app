using System;
using System.Collections.Generic;
using System.Linq;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Naisargika dasha - the "natural" periods of a life.
///
/// The mahadashas take nothing from the chart: every native gets the same seven periods in the
/// same order, because the system describes the natural ages of a human life. The Moon rules
/// infancy, Mercury the years of learning, Venus youth, Jupiter maturity, Saturn old age.
///
/// The sub-periods ARE chart-dependent, and that is their whole point: each is weighted by where
/// its planet stands from the lord of the period above it, so the same eighteen-year Jupiter
/// period is divided differently for every native. Ported from the Android app.
/// </summary>
public class NaisargikaDashaCalculator
{
    public const int MaxLevel = 6;

    /// <summary>
    /// The mahadashas, in order, with their length in years. Saturn's fifty carry the sequence
    /// to 120, the full lifespan the system assumes.
    /// </summary>
    public static readonly (Planet Lord, double Years)[] Periods =
    {
        (Planet.Moon, 1), (Planet.Mars, 2), (Planet.Mercury, 9), (Planet.Venus, 20),
        (Planet.Jupiter, 18), (Planet.Sun, 20), (Planet.Saturn, 50)
    };

    /// <summary>
    /// Sub-period weight by house position from the parent's lord - Parashara's
    /// Antardasha-adhyaya as set out in the Saravali. Not a strength scale and not in order: the
    /// 7th takes the smallest share at 1/7 while the 2nd, 6th, 11th and 12th take a full 1.
    /// The 1/2 at house 1 is for a DIFFERENT planet sharing the lord's sign; the lord itself
    /// takes a full 1.
    /// </summary>
    public static readonly Dictionary<int, double> HouseWeights = new()
    {
        { 1, 0.5 }, { 2, 1.0 }, { 3, 0.75 }, { 4, 0.25 }, { 5, 1.0 / 3.0 }, { 6, 1.0 },
        { 7, 1.0 / 7.0 }, { 8, 0.25 }, { 9, 1.0 / 3.0 }, { 10, 0.75 }, { 11, 1.0 }, { 12, 1.0 }
    };

    public const double LordWeight = 1.0;

    /// <summary>
    /// The order sub-periods run in, as houses from the parent's lord. It opens with the kendras
    /// in reverse. A kendra-then-panapara grouping is the natural guess and is wrong - it gave the
    /// same order for every lord and disagreed with a published implementation at every level.
    /// </summary>
    public static readonly int[] HouseOrder = { 10, 7, 4, 1, 2, 11, 8, 5, 6, 3, 12, 9 };

    /// <summary>The seven bodies that furnish sub-periods. The nodes have no house lordship.</summary>
    public static readonly Planet[] SubPeriodPlanets =
    {
        Planet.Sun, Planet.Moon, Planet.Mars, Planet.Mercury, Planet.Jupiter, Planet.Venus, Planet.Saturn
    };

    private readonly SolarReturnFinder? _solarReturns;
    private readonly Dictionary<Planet, (int Sign, double Degree)> _positions = new();
    private double _timeZoneOffset;

    /// <param name="positions">0-based sign and degree of each of the seven planets.</param>
    public NaisargikaDashaCalculator(
        IDictionary<Planet, (int Sign, double Degree)> positions,
        SolarReturnFinder? solarReturns = null)
    {
        foreach (var kv in positions) _positions[kv.Key] = kv.Value;
        _solarReturns = solarReturns;
    }

    /// <summary>Read the seven planets out of a calculated chart.</summary>
    public static NaisargikaDashaCalculator FromChart(ChartData chart, SolarReturnFinder? solarReturns = null)
    {
        var positions = new Dictionary<Planet, (int, double)>();
        foreach (var p in chart.Planets)
        {
            // Aprakash grahas reuse Planet.Sun as a placeholder; take only the real graha rows.
            if (!SubPeriodPlanets.Contains(p.Planet)) continue;
            if (!ZodiacUtils.PlanetNames.TryGetValue(p.Planet, out var realName) || p.Name != realName) continue;
            positions[p.Planet] = (((p.Sign - 1) % 12 + 12) % 12, p.DegreeInSign);
        }
        return new NaisargikaDashaCalculator(positions, solarReturns);
    }

    public DashaResult Calculate(
        double birthJulianDay,
        double currentJulianDay,
        int levels = MaxLevel,
        double timeZoneOffset = 0.0)
    {
        _timeZoneOffset = timeZoneOffset;
        levels = Math.Clamp(levels, 1, MaxLevel);

        var result = new DashaResult();
        double startJd = birthJulianDay;

        // Each boundary is counted from birth rather than stepped, so a rounding at one does not
        // carry into every later period.
        double elapsed = 0.0;
        foreach (var (lord, years) in Periods)
        {
            elapsed += years;
            double endJd = BoundaryAt(birthJulianDay, elapsed);
            var period = MakePeriod(lord, 1, startJd, endJd, years, currentJulianDay);
            if (levels >= 2)
            {
                period.SubPeriods = SubPeriods(lord, startJd, endJd, years, 2, levels, currentJulianDay);
            }
            result.MahaDashas.Add(period);
            startJd = endJd;
        }

        UduDashaCalculator.SetCurrentChain(result);
        return result;
    }

    /// <summary>
    /// The sub-periods inside any period, at any depth.
    ///
    /// Houses are counted from the IMMEDIATE parent's lord, not the mahadasha lord. The Android
    /// app checked this against a published implementation to five decimal places: inside
    /// Moon > Mercury > Mars the shares are those of houses counted from Mars, and one level
    /// deeper, inside that period's Moon, they are counted from the Moon again.
    /// </summary>
    public List<DashaPeriod> SubPeriods(
        Planet parentLord, double parentStartJd, double parentEndJd, double parentYears,
        int level, int maxLevel, double currentJd)
    {
        var children = new List<DashaPeriod>();
        if (!_positions.TryGetValue(parentLord, out var lordPos)) return children;

        var weighted = new List<(Planet Planet, int House, double Degree, double Weight)>();
        foreach (var planet in SubPeriodPlanets)
        {
            if (!_positions.TryGetValue(planet, out var pos)) continue;
            int house = HouseFrom(lordPos.Sign, pos.Sign);
            double weight = planet == parentLord ? LordWeight : HouseWeights[house];
            weighted.Add((planet, house, pos.Degree, weight));
        }
        if (weighted.Count == 0) return children;

        double total = weighted.Sum(w => w.Weight);

        // A fixed sequence of houses from the parent's lord, which acts as a temporary lagna.
        // Two grahas in one house go lower degree first - the reference implementation's
        // default, and unverified: nothing observed exercises it.
        var ordered = weighted
            .OrderBy(w => Array.IndexOf(HouseOrder, w.House))
            .ThenBy(w => w.Degree)
            .ToList();

        double parentSpan = parentEndJd - parentStartJd;
        double startJd = parentStartJd;
        for (int i = 0; i < ordered.Count; i++)
        {
            double share = ordered[i].Weight / total;
            double endJd = i == ordered.Count - 1 ? parentEndJd : startJd + parentSpan * share;
            double years = parentYears * share;

            var child = MakePeriod(ordered[i].Planet, level, startJd, endJd, years, currentJd);
            if (level < maxLevel)
            {
                child.SubPeriods = SubPeriods(ordered[i].Planet, startJd, endJd, years, level + 1, maxLevel, currentJd);
            }
            children.Add(child);
            startJd = endJd;
        }
        return children;
    }

    /// <summary>Which house a sign falls in, counting the lord's own sign as the first.</summary>
    private static int HouseFrom(int lordSign, int sign) => ((sign - lordSign + 12) % 12) + 1;

    /// <summary>
    /// The instant a given age falls at: a solar return where one can be solved, since a year of
    /// age is the Sun's circuit back to its natal degree, not a count of calendar days.
    /// </summary>
    private double BoundaryAt(double birthJd, double years)
        => _solarReturns?.JdAfterYears(birthJd, years) ?? birthJd + years * UduDashaCalculator.DaysPerYear;

    private DashaPeriod MakePeriod(Planet lord, int level, double startJd, double endJd, double years, double currentJd)
    {
        return new DashaPeriod
        {
            Planet = lord.ToString(),
            Symbol = ZodiacUtils.PlanetSymbols.TryGetValue(lord, out var sym) ? sym : lord.ToString(),
            Level = level,
            StartJulianDay = startJd,
            EndJulianDay = endJd,
            StartDate = JdToLocal(startJd),
            EndDate = JdToLocal(endJd),
            DurationYears = years,
            IsActive = currentJd >= startJd && currentJd < endJd,
            TimeZoneOffset = _timeZoneOffset
        };
    }

    private DateTime JdToLocal(double jd)
    {
        if (jd < 1721426) return DateTime.MinValue;
        return new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Unspecified)
            .AddDays(jd - 2451545.0)
            .AddHours(_timeZoneOffset);
    }
}
