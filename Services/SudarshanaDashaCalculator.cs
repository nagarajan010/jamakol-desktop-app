using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Sudarshana Chakra Dasha - twelve years to a sign, one year to a sign inside that, and so on.
///
/// It reads nothing from the chart but the LAGNA: the first period belongs to the rising sign,
/// the next to the sign after it, and so on forward through the zodiac. Twelve signs at twelve
/// years each covers a hundred and forty-four.
///
/// ### Where the boundaries fall
///
/// The top two levels are SOLAR RETURNS - the Sun's circuit back to its natal longitude - and not
/// calendar years. Each return falls about six hours later than the last until it crosses midnight
/// and the date steps back a day, which is why the gaps read 365, 366, 365, 365 in a pattern the
/// calendar cannot explain. Using DateTime.AddYears reproduced the civil birthday instead, which
/// is a different quantity and lands on the wrong day for most charts.
///
/// Below the year it is a plain EQUAL SPLIT, a twelfth of the parent each. A true solar ingress
/// would be visibly unequal - the Sun takes about 31.5 days through Cancer near aphelion against
/// 29.5 through Capricorn near perihelion - and published tables show no such swing.
/// </summary>
public class SudarshanaDashaCalculator
{
    private const int Signs = 12;
    private const int MahadashaYears = 12;
    private const int TotalDashaYears = 144;

    /// <summary>
    /// How deep to build eagerly. Twelve to the power of six is over three million periods, so
    /// the tree is built to a usable depth rather than exhaustively; the running chain below
    /// that is still resolved on demand.
    /// </summary>
    private const int DefaultDepth = 3;

    private readonly SolarReturnFinder? _solarReturns;

    public SudarshanaDashaCalculator(SolarReturnFinder? solarReturns = null)
    {
        _solarReturns = solarReturns;
    }

    /// <summary>
    /// Build the dasha tree.
    /// </summary>
    /// <param name="dateOfBirth">Birth instant, used for the dates the periods carry.</param>
    /// <param name="queryDate">The moment to mark as running.</param>
    /// <param name="birthJulianDay">Birth instant as a Julian Day, for the solar-return search.</param>
    /// <param name="ascendantSign">Rising sign, 1-12. The sequence starts here.</param>
    /// <param name="depth">Levels to build; 1 = mahadashas only.</param>
    public SudarshanaDashaResult Calculate(
        DateTime dateOfBirth,
        DateTime queryDate,
        double birthJulianDay,
        int ascendantSign,
        int depth = DefaultDepth)
    {
        var result = new SudarshanaDashaResult();

        if (ascendantSign < 1 || ascendantSign > Signs) ascendantSign = 1;
        if (depth > SudarshanaDashaPeriod.MaxLevel) depth = SudarshanaDashaPeriod.MaxLevel;

        for (int index = 0; index < Signs; index++)
        {
            int sign = WrapSign(ascendantSign + index);
            double fromYears = index * MahadashaYears;
            double toYears = fromYears + MahadashaYears;

            var mainPeriod = BuildPeriod(
                sign: sign,
                mainSign: sign,
                lagnaSign: ascendantSign,
                fromYears: fromYears,
                toYears: toYears,
                dashaYear: (int)fromYears + 1,
                level: 1,
                dateOfBirth: dateOfBirth,
                birthJulianDay: birthJulianDay,
                queryDate: queryDate,
                depth: depth);

            result.MainDashas.Add(mainPeriod);

            if (mainPeriod.IsActive)
            {
                result.CurrentMainDasha = mainPeriod;
            }

            // The flat annual list the summary view reads.
            foreach (var sub in mainPeriod.SubPeriods)
            {
                result.Years.Add(sub);
                if (sub.IsActive) result.CurrentPeriod = sub;
            }
        }

        BuildCurrentChain(result.MainDashas, queryDate, result.CurrentChain);

        // The deepest resolved period is the one worth calling "current".
        if (result.CurrentChain.Count > 0)
        {
            result.CurrentPeriod = result.CurrentChain[^1];
        }

        return result;
    }

    private SudarshanaDashaPeriod BuildPeriod(
        int sign,
        int mainSign,
        int lagnaSign,
        double fromYears,
        double toYears,
        int dashaYear,
        int level,
        DateTime dateOfBirth,
        double birthJulianDay,
        DateTime queryDate,
        int depth)
    {
        DateTime start = InstantAt(dateOfBirth, birthJulianDay, fromYears);
        DateTime end = InstantAt(dateOfBirth, birthJulianDay, toYears);

        var period = new SudarshanaDashaPeriod
        {
            DashaYear = dashaYear,
            MainDashaSign = mainSign,
            LagnaSign = lagnaSign,
            Sign = sign,
            Level = level,
            StartDate = start,
            EndDate = end,
            IsActive = queryDate >= start && queryDate < end
        };

        if (level < depth)
        {
            period.SubPeriods = BuildSubPeriods(
                period, mainSign, lagnaSign, fromYears, toYears, level + 1,
                dateOfBirth, birthJulianDay, queryDate, depth);
        }

        return period;
    }

    /// <summary>
    /// Twelve equal parts of the parent, starting at the parent's own sign and running forward.
    ///
    /// Level 2 is still reckoned in years from birth, so its boundaries go through the same
    /// solar-return solve as the level above and a period's end is the next one's start exactly.
    /// Below that the split is equal, so the parent's measured SPAN is the right currency -
    /// going back through years would not close the round trip, since converting a date back
    /// with a nominal year lands a minute or two off.
    /// </summary>
    private ObservableCollection<SudarshanaDashaPeriod> BuildSubPeriods(
        SudarshanaDashaPeriod parent,
        int mainSign,
        int lagnaSign,
        double fromYears,
        double toYears,
        int level,
        DateTime dateOfBirth,
        double birthJulianDay,
        DateTime queryDate,
        int depth)
    {
        var children = new ObservableCollection<SudarshanaDashaPeriod>();

        // Below the year the split is equal and works from the parent's measured span, so it
        // carries no years at all - only the solar-return levels need them. Guarding on
        // eachYears for BOTH branches collapsed every level past the second, because the
        // equal-split recursion is handed no year range to divide.
        bool useSolarReturns = level <= 2;
        double eachYears = (toYears - fromYears) / Signs;
        if (useSolarReturns && eachYears <= 0.0) return children;

        long parentTicks = parent.EndDate.Ticks - parent.StartDate.Ticks;
        if (!useSolarReturns && parentTicks <= 0L) return children;

        for (int index = 0; index < Signs; index++)
        {
            int sign = WrapSign(parent.Sign + index);

            SudarshanaDashaPeriod child;

            if (useSolarReturns)
            {
                child = BuildPeriod(
                    sign: sign,
                    mainSign: mainSign,
                    lagnaSign: lagnaSign,
                    fromYears: fromYears + index * eachYears,
                    toYears: fromYears + (index + 1) * eachYears,
                    dashaYear: (int)Math.Floor(fromYears + index * eachYears) + 1,
                    level: level,
                    dateOfBirth: dateOfBirth,
                    birthJulianDay: birthJulianDay,
                    queryDate: queryDate,
                    depth: depth);
            }
            else
            {
                // Give the remainder to the last child so the tiling is exact to the tick
                // rather than to whatever twelve integer divisions happen to sum to.
                long each = parentTicks / Signs;
                DateTime start = new(parent.StartDate.Ticks + index * each);
                DateTime end = index == Signs - 1
                    ? parent.EndDate
                    : new DateTime(start.Ticks + each);

                child = new SudarshanaDashaPeriod
                {
                    DashaYear = parent.DashaYear,
                    MainDashaSign = mainSign,
                    LagnaSign = lagnaSign,
                    Sign = sign,
                    Level = level,
                    StartDate = start,
                    EndDate = end,
                    IsActive = queryDate >= start && queryDate < end
                };

                if (level < depth)
                {
                    child.SubPeriods = BuildSubPeriods(
                        child, mainSign, lagnaSign, 0, 0, level + 1,
                        dateOfBirth, birthJulianDay, queryDate, depth);
                }
            }

            children.Add(child);
        }

        return children;
    }

    /// <summary>
    /// Expand one period's children on demand, for a level the tree was not built to.
    /// Below the year the split is equal, so the parent's span is all this needs.
    /// </summary>
    public ObservableCollection<SudarshanaDashaPeriod> Expand(SudarshanaDashaPeriod parent)
    {
        if (parent.SubPeriods.Count > 0) return parent.SubPeriods;
        if (!parent.CanExpand) return parent.SubPeriods;

        long span = parent.EndDate.Ticks - parent.StartDate.Ticks;
        if (span <= 0) return parent.SubPeriods;

        long each = span / Signs;
        var children = new ObservableCollection<SudarshanaDashaPeriod>();

        for (int index = 0; index < Signs; index++)
        {
            DateTime start = new(parent.StartDate.Ticks + index * each);
            DateTime end = index == Signs - 1 ? parent.EndDate : new DateTime(start.Ticks + each);

            children.Add(new SudarshanaDashaPeriod
            {
                DashaYear = parent.DashaYear,
                MainDashaSign = parent.MainDashaSign,
                LagnaSign = parent.LagnaSign,
                Sign = WrapSign(parent.Sign + index),
                Level = parent.Level + 1,
                StartDate = start,
                EndDate = end
            });
        }

        parent.SubPeriods = children;
        return children;
    }

    /// <summary>
    /// The running period at each level, outermost first, all the way to the deepest.
    ///
    /// The chain is followed past the eagerly built levels by expanding as it goes: the reader
    /// wants to know what is running NOW at every level, and stopping whereever the eager build
    /// happened to stop showed three links where the dasha has six. Only the periods actually on
    /// the chain are expanded - one per level, not the whole tier.
    /// </summary>
    private void BuildCurrentChain(
        List<SudarshanaDashaPeriod> roots,
        DateTime queryDate,
        List<SudarshanaDashaPeriod> chain)
    {
        IList<SudarshanaDashaPeriod> level = roots;

        while (true)
        {
            SudarshanaDashaPeriod? active = null;
            foreach (var p in level)
            {
                if (p.IsActive) { active = p; break; }
            }
            if (active == null) break;

            chain.Add(active);
            if (!active.CanExpand) break;

            if (active.SubPeriods.Count == 0)
            {
                Expand(active);
                // A period built on demand carries no running flag, so set it here.
                foreach (var child in active.SubPeriods)
                {
                    child.IsActive = queryDate >= child.StartDate && queryDate < child.EndDate;
                }
            }

            if (active.SubPeriods.Count == 0) break;
            level = active.SubPeriods;
        }
    }

    /// <summary>
    /// The instant a given age falls at, as a solar return where one can be solved.
    /// Without an ephemeris this falls back to a sidereal year so a date still comes back.
    /// </summary>
    private DateTime InstantAt(DateTime dateOfBirth, double birthJulianDay, double years)
    {
        if (_solarReturns == null)
        {
            return dateOfBirth.AddDays(years * 365.256363);
        }

        double jd = _solarReturns.JdAfterYears(birthJulianDay, years);
        double daysFromBirth = jd - birthJulianDay;
        return dateOfBirth.AddDays(daysFromBirth);
    }

    private static int WrapSign(int sign) => ((sign - 1) % Signs + Signs) % Signs + 1;

    public static int GetMainDashaHouse(int dashaYear)
    {
        ValidateDashaYear(dashaYear);
        return ((dashaYear - 1) / Signs) + 1;
    }

    public static int GetSubDashaHouse(int dashaYear)
    {
        ValidateDashaYear(dashaYear);

        int mainHouse = GetMainDashaHouse(dashaYear);
        int offset = (dashaYear - 1) % Signs;
        return ((mainHouse - 1 + offset) % Signs) + 1;
    }

    private static void ValidateDashaYear(int dashaYear)
    {
        if (dashaYear < 1 || dashaYear > TotalDashaYears)
        {
            throw new ArgumentOutOfRangeException(nameof(dashaYear), "Sudarshana Dasha year must be between 1 and 144.");
        }
    }
}
