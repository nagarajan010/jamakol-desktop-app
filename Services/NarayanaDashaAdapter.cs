using System;
using System.Collections.Generic;
using System.Linq;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Reads the rasi positions Narayana dasha needs out of a calculated chart.
/// </summary>
public class ChartDataRashiChart : IRashiChart
{
    private readonly Dictionary<Planet, int> _signs = new();
    private readonly Dictionary<Planet, double> _degrees = new();

    public int Lagna { get; }

    public ChartDataRashiChart(ChartData chart)
    {
        // ChartData carries signs 1-12; this interface is 0-based from Aries throughout.
        Lagna = ((chart.AscendantSign - 1) % 12 + 12) % 12;

        foreach (var planet in chart.Planets)
        {
            // Aprakash grahas reuse Planet.Sun as a placeholder, so take only the real graha rows
            // - otherwise a shadow point would overwrite the Sun's own position.
            if (!ZodiacUtils.PlanetNames.TryGetValue(planet.Planet, out var realName)) continue;
            if (planet.Name != realName) continue;

            _signs[planet.Planet] = ((planet.Sign - 1) % 12 + 12) % 12;
            _degrees[planet.Planet] = planet.DegreeInSign;
        }
    }

    public int SignOf(Planet planet) => _signs.TryGetValue(planet, out int s) ? s : 0;

    public double DegreeInSignOf(Planet planet) => _degrees.TryGetValue(planet, out double d) ? d : 0.0;
}

/// <summary>
/// Turns the Narayana reckoning, which is in years of age, into dated periods.
///
/// The ages are converted through SOLAR RETURNS for the same reason the Sudarshana dasha is: a
/// year of age is the Sun's circuit back to its natal longitude, not a count of calendar days.
/// </summary>
public class NarayanaDashaAdapter
{
    /// <summary>
    /// How deep to build eagerly. Rath names six levels; twelve to the sixth is over three
    /// million periods, so the rest is filled in as the reader opens it.
    /// </summary>
    public const int MaxLevel = 6;
    private const int DefaultDepth = 3;

    private readonly SolarReturnFinder? _solarReturns;

    public NarayanaDashaAdapter(SolarReturnFinder? solarReturns = null)
    {
        _solarReturns = solarReturns;
    }

    public NarayanaDashaResult Calculate(
        ChartData chartData,
        DateTime dateOfBirth,
        DateTime queryDate,
        double birthJulianDay,
        double timeZoneOffset = 0.0,
        int depth = DefaultDepth)
    {
        var rashiChart = new ChartDataRashiChart(chartData);
        var calculator = new NarayanaDashaCalculator(rashiChart);

        var result = new NarayanaDashaResult
        {
            StartingSign = calculator.StartingSign(),
            Order = calculator.DashaOrder()
        };

        int lagnaSign = rashiChart.Lagna + 1; // 1-based for display

        var periods = calculator.Calculate(withSubPeriods: false);

        foreach (var period in periods)
        {
            Materialise(period, calculator, rashiChart, lagnaSign, dateOfBirth, birthJulianDay,
                        queryDate, timeZoneOffset, depth);
            result.MahaDashas.Add(period);
        }

        BuildCurrentChain(result.MahaDashas, calculator, rashiChart, lagnaSign, dateOfBirth,
                          birthJulianDay, queryDate, timeZoneOffset, result.CurrentChain);

        return result;
    }

    /// <summary>Give a period its dates, then build its children down to the eager depth.</summary>
    private void Materialise(
        NarayanaPeriod period,
        NarayanaDashaCalculator calculator,
        IRashiChart chart,
        int lagnaSign,
        DateTime dateOfBirth,
        double birthJulianDay,
        DateTime queryDate,
        double timeZoneOffset,
        int depth)
    {
        period.LagnaSign = lagnaSign;
        period.StartDate = InstantAt(dateOfBirth, birthJulianDay, period.StartAge, timeZoneOffset);
        period.EndDate = InstantAt(dateOfBirth, birthJulianDay,
                                   period.StartAge + period.SpanYears, timeZoneOffset);
        period.IsActive = queryDate >= period.StartDate && queryDate < period.EndDate;

        if (period.Level >= depth || period.SpanYears <= 0.0) return;

        period.SubPeriods = calculator.SubPeriodsOf(
            period.Rashi, period.SpanYears, period.StartAge, period.Level + 1);

        // The twelve are equal, so tile the parent's own span exactly rather than converting
        // each age separately - twelve independent conversions do not close on the parent.
        DateSubPeriods(period, queryDate);

        foreach (var child in period.SubPeriods)
        {
            if (child.Level < depth && child.SpanYears > 0.0)
            {
                child.SubPeriods = calculator.SubPeriodsOf(
                    child.Rashi, child.SpanYears, child.StartAge, child.Level + 1);
                DateSubPeriods(child, queryDate);
                foreach (var grand in child.SubPeriods)
                {
                    Materialise(grand, calculator, chart, lagnaSign, dateOfBirth, birthJulianDay,
                                queryDate, timeZoneOffset, depth);
                }
            }
        }
    }

    /// <summary>
    /// Fill in one period's children on demand, for a level the tree was not built to.
    /// </summary>
    public List<NarayanaPeriod> Expand(
        NarayanaPeriod parent,
        ChartData chartData,
        DateTime dateOfBirth,
        double birthJulianDay,
        DateTime queryDate,
        double timeZoneOffset = 0.0)
    {
        if (parent.SubPeriods.Count > 0) return parent.SubPeriods;
        if (parent.Level >= MaxLevel || parent.SpanYears <= 0.0) return parent.SubPeriods;

        var rashiChart = new ChartDataRashiChart(chartData);
        var calculator = new NarayanaDashaCalculator(rashiChart);

        parent.SubPeriods = calculator.SubPeriodsOf(
            parent.Rashi, parent.SpanYears, parent.StartAge, parent.Level + 1);

        // Sub-periods are twelve EQUAL parts, so split the parent's measured span rather than
        // re-deriving each age through the solar returns. That keeps the children tiling the
        // parent exactly, and means an expander built without an ephemeris is still correct -
        // going back through nominal years would leave them drifting against their own parent.
        DateSubPeriods(parent, queryDate);
        return parent.SubPeriods;
    }

    /// <summary>The running period at each level, outermost first, expanding as it descends.</summary>
    private void BuildCurrentChain(
        List<NarayanaPeriod> roots,
        NarayanaDashaCalculator calculator,
        IRashiChart chart,
        int lagnaSign,
        DateTime dateOfBirth,
        double birthJulianDay,
        DateTime queryDate,
        double timeZoneOffset,
        List<NarayanaPeriod> chain)
    {
        IList<NarayanaPeriod> level = roots;

        while (true)
        {
            NarayanaPeriod? active = null;
            foreach (var p in level)
            {
                if (p.IsActive) { active = p; break; }
            }
            if (active == null) break;

            chain.Add(active);
            if (active.Level >= MaxLevel || active.SpanYears <= 0.0) break;

            if (active.SubPeriods.Count == 0)
            {
                active.SubPeriods = calculator.SubPeriodsOf(
                    active.Rashi, active.SpanYears, active.StartAge, active.Level + 1);
                DateSubPeriods(active, queryDate);
            }

            if (active.SubPeriods.Count == 0) break;
            level = active.SubPeriods;
        }
    }

    /// <summary>
    /// Date a period's twelve children by splitting its measured span, giving the remainder to
    /// the last so the tiling is exact to the tick.
    /// </summary>
    private static void DateSubPeriods(NarayanaPeriod parent, DateTime queryDate)
    {
        int count = parent.SubPeriods.Count;
        if (count == 0) return;

        long span = parent.EndDate.Ticks - parent.StartDate.Ticks;
        long each = span / count;

        for (int i = 0; i < count; i++)
        {
            var child = parent.SubPeriods[i];
            child.LagnaSign = parent.LagnaSign;
            child.StartDate = new DateTime(parent.StartDate.Ticks + i * each);
            child.EndDate = i == count - 1
                ? parent.EndDate
                : new DateTime(child.StartDate.Ticks + each);
            child.IsActive = queryDate >= child.StartDate && queryDate < child.EndDate;
        }
    }

    /// <summary>
    /// The instant a given age falls at, as a solar return where one can be solved.
    /// Without an ephemeris this falls back to a sidereal year so a date still comes back.
    /// </summary>
    private DateTime InstantAt(DateTime dateOfBirth, double birthJulianDay, double years, double timeZoneOffset)
    {
        if (years <= 0.0) return dateOfBirth;

        if (_solarReturns == null)
        {
            return dateOfBirth.AddDays(years * 365.256363);
        }

        double jd = _solarReturns.JdAfterYears(birthJulianDay, years);
        return dateOfBirth.AddDays(jd - birthJulianDay);
    }
}
