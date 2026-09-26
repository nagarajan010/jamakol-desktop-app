using System;
using System.Collections.Generic;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Periods for any nakshatra-based dasha system.
///
/// The machinery is identical across the family - starting lord, balance of the first period, a
/// fixed cycle of lords, and sub-periods dividing their parent in the same proportions - so it
/// lives here once and <see cref="NakshatraDashaSystem"/> supplies the tables that differ.
///
/// Produces the same <see cref="DashaResult"/> as the Vimshottari calculator, so the existing tree,
/// the current-period summary and the timezone handling all apply unchanged.
/// </summary>
public class UduDashaCalculator
{
    /// <summary>
    /// A dasha year is the true sidereal solar year. The Gregorian 365.2425 made every boundary
    /// fall a day or two early across a full cycle.
    /// </summary>
    public const double DaysPerYear = 365.256363;

    /// <summary>Deepest level the family is reckoned to: Maha, Antar, Pratyantar, Sookshma, Prana, Deha.</summary>
    public const int MaxLevel = 6;

    private readonly NakshatraDashaSystem _system;
    private double _timeZoneOffset;

    public UduDashaCalculator(NakshatraDashaSystem system)
    {
        _system = system;
    }

    public DashaResult Calculate(
        double moonLongitude,
        double birthJulianDay,
        double currentJulianDay,
        int levels = MaxLevel,
        double timeZoneOffset = 0.0)
    {
        _timeZoneOffset = timeZoneOffset;
        levels = Math.Clamp(levels, 1, MaxLevel);

        var result = new DashaResult();

        double moon = ((moonLongitude % 360.0) + 360.0) % 360.0;
        int nakshatra = Math.Clamp((int)(moon / NakshatraDashaSystem.NakshatraSpan), 0, 26);
        double degreeIn = moon % NakshatraDashaSystem.NakshatraSpan;

        result.MoonNakshatra = ZodiacUtils.NakshatraNames[nakshatra + 1];
        result.MoonNakshatraPada = Math.Min(4, (int)(degreeIn / (NakshatraDashaSystem.NakshatraSpan / 4)) + 1);

        var lords = _system.Sequence;
        int startIndex = Array.IndexOf(lords, _system.StartingLord(nakshatra));
        if (startIndex < 0) return result;

        double startJd = birthJulianDay;

        // One full cycle: the lords sum to the system's whole span. The first period is only the
        // unspent remainder of the starting lord's span.
        for (int i = 0; i < lords.Length; i++)
        {
            Planet lord = lords[(startIndex + i) % lords.Length];
            double years = _system.Years[lord];
            if (i == 0)
            {
                years *= _system.BalanceFraction(nakshatra, degreeIn);
                result.BalanceAtBirthDays = years * DaysPerYear;
            }

            double endJd = startJd + years * DaysPerYear;
            var period = MakePeriod(lord, 1, startJd, endJd, years, currentJulianDay);

            if (levels >= 2)
            {
                period.SubPeriods = SubPeriods(lord, startJd, endJd, years, 2, levels, currentJulianDay);
            }

            result.MahaDashas.Add(period);
            startJd = endJd;
        }

        SetCurrentChain(result);
        return result;
    }

    /// <summary>
    /// The sub-periods of one parent, recursing to <paramref name="maxLevel"/>.
    ///
    /// A sub-period takes the same share of its parent as its lord takes of the cycle, and the
    /// parent's MEASURED span is what is shared out, with the last child given the parent's exact
    /// end - so the tiling is exact at every depth instead of drifting by accumulated rounding.
    /// </summary>
    private List<DashaPeriod> SubPeriods(
        Planet parentLord,
        double parentStartJd,
        double parentEndJd,
        double parentYears,
        int level,
        int maxLevel,
        double currentJd)
    {
        var lords = _system.Sequence;
        int startIndex = Array.IndexOf(lords, parentLord);
        var children = new List<DashaPeriod>();
        if (startIndex < 0) return children;

        double parentSpan = parentEndJd - parentStartJd;
        double startJd = parentStartJd;
        int n = lords.Length;

        for (int i = 0; i < n; i++)
        {
            // Forward from the parent, or - for Ashtottari - backward from the lord BEFORE it,
            // which lands the parent's own sub-period last.
            Planet lord = _system.SubPeriodsReversed
                ? lords[((startIndex - 1 - i) % n + n) % n]
                : lords[(startIndex + i) % n];

            double proportion = _system.Years[lord] / _system.TotalYears;
            double endJd = i == n - 1 ? parentEndJd : startJd + parentSpan * proportion;
            double years = parentYears * proportion;

            var child = MakePeriod(lord, level, startJd, endJd, years, currentJd);
            if (level < maxLevel)
            {
                child.SubPeriods = SubPeriods(lord, startJd, endJd, years, level + 1, maxLevel, currentJd);
            }

            children.Add(child);
            startJd = endJd;
        }

        return children;
    }

    private DashaPeriod MakePeriod(Planet lord, int level, double startJd, double endJd, double years, double currentJd)
    {
        return new DashaPeriod
        {
            Planet = lord.ToString(),
            Symbol = ZodiacUtils.PlanetSymbols.TryGetValue(lord, out var sym) ? sym : lord.ToString(),
            LordLabel = _system.LordLabel(lord),
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

    /// <summary>
    /// Walk the running period down every level and record each one. Shared by every calculator
    /// that produces a <see cref="DashaResult"/>.
    /// </summary>
    public static void SetCurrentChain(DashaResult result)
    {
        var level = result.MahaDashas;
        for (int depth = 1; depth <= MaxLevel; depth++)
        {
            DashaPeriod? active = level.Find(p => p.IsActive);
            if (active == null) return;

            switch (depth)
            {
                case 1: result.CurrentMahaDasha = active; break;
                case 2: result.CurrentAntarDasha = active; break;
                case 3: result.CurrentPratyantaraDasha = active; break;
                case 4: result.CurrentSookshmaDasha = active; break;
                case 5: result.CurrentPranaDasha = active; break;
                case 6: result.CurrentDehaDasha = active; break;
            }

            level = active.SubPeriods;
        }
    }

    private DateTime JdToLocal(double jd)
    {
        if (jd < 1721426) return DateTime.MinValue;
        return new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Unspecified)
            .AddDays(jd - 2451545.0)
            .AddHours(_timeZoneOffset);
    }
}
