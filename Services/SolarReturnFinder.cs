using System;
using System.Collections.Generic;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// When the Sun comes back to the degree it held at birth.
///
/// A "year of age" measured this way is the Sun's own circuit rather than a count of calendar
/// days, which is what a dasha reckoned in years of life means. The two differ: a Gregorian year
/// of 365.2425 days runs short against the Sun's actual circuit, so a nominal count drifts about
/// a day across a 144-year span - and lands on the wrong side of a boundary for anyone born
/// within a day of one.
///
/// The search is a bisection on the Sun's distance from its natal longitude. Its apparent motion
/// varies through the year - faster at perihelion in January than at aphelion in July - so a
/// boundary cannot be found by scaling; it has to be solved for.
/// </summary>
public class SolarReturnFinder
{
    /// <summary>
    /// How far past a return to start looking for the next. The Sun's circuit varies by a few
    /// hours either side of 365.25 days, so a fortnight of clearance cannot skip a boundary.
    /// </summary>
    private const double MinYearDays = 350.0;

    /// <summary>Half a day. The Sun moves about a degree a day, so this cannot step over a crossing.</summary>
    private const double ScanStepDays = 0.5;

    /// <summary>Enough to cover the spread of a solar year from the starting point.</summary>
    private const int MaxScanSteps = 60;

    private const int Bisections = 32;

    /// <summary>Used only when the ephemeris cannot answer, so a date still comes back.</summary>
    private const double NominalYearDays = 365.25;

    private readonly Func<double, double> _sunLongitude;

    /// <summary>
    /// Returns already solved, in order, counted from the birth they were solved against.
    ///
    /// A dasha table asks for boundaries at rising ages - 1, 12, 13, 24 - and each answer is the
    /// one before it plus a few more returns. Without this the search restarts at birth every
    /// time and re-solves the same early returns over and over. Held per birth, since a different
    /// birth has a different natal degree.
    /// </summary>
    private double? _cachedBirthJd;
    private readonly List<double> _returnsFromBirth = new();

    public SolarReturnFinder(Func<double, double> sunLongitude)
    {
        _sunLongitude = sunLongitude;
    }

    /// <summary>
    /// Read the Sun from the app's ephemeris at the configured ayanamsa.
    /// </summary>
    public static SolarReturnFinder FromEphemeris(EphemerisService ephemeris, int ayanamshaId, double ayanamshaOffset = 0.0)
        => new(jd => ephemeris.GetPlanetPosition(jd, (int)Planet.Sun, ayanamshaId, ayanamshaOffset).longitude);

    /// <summary>
    /// The instant, as a Julian Day, when the Sun has made <paramref name="years"/> complete
    /// returns from <paramref name="birthJd"/>.
    ///
    /// Fractional values are interpolated within the year they fall in, which is what a
    /// sub-period boundary needs - those land at arbitrary fractions of a period rather than on
    /// whole returns.
    /// </summary>
    public double JdAfterYears(double birthJd, double years)
    {
        if (years <= 0.0) return birthJd;

        int whole = (int)Math.Floor(years);
        double fraction = years - whole;

        double jd = ReturnNumber(birthJd, whole);
        if (fraction <= 0.0) return jd;

        // The remaining fraction is taken within the year it falls in rather than as a fixed
        // number of days, so a part-year keeps its share of that particular circuit.
        double nextBoundary = ReturnNumber(birthJd, whole + 1);
        return jd + (nextBoundary - jd) * fraction;
    }

    /// <summary>
    /// The instant of the <paramref name="count"/>th return, extending the cache as needed.
    /// Only returns past the end of the cache are solved, so a table of rising ages costs one
    /// pass through the span rather than one pass per row.
    /// </summary>
    private double ReturnNumber(double birthJd, int count)
    {
        if (count <= 0) return birthJd;

        if (_cachedBirthJd != birthJd)
        {
            _cachedBirthJd = birthJd;
            _returnsFromBirth.Clear();
        }

        double jd = _returnsFromBirth.Count > 0 ? _returnsFromBirth[^1] : birthJd;
        while (_returnsFromBirth.Count < count)
        {
            jd = NextReturn(jd, birthJd);
            _returnsFromBirth.Add(jd);
        }
        return _returnsFromBirth[count - 1];
    }

    /// <summary>
    /// The next return to the natal longitude, strictly after <paramref name="fromJd"/>.
    /// Stepping past the boundary before bracketing it stops the search re-finding the return it
    /// is standing on.
    /// </summary>
    private double NextReturn(double fromJd, double birthJd)
    {
        double target = _sunLongitude(birthJd);

        // A return is about a year away; start looking a fortnight short of that and step in.
        double low = fromJd + MinYearDays;

        for (int i = 0; i < MaxScanSteps; i++)
        {
            double high = low + ScanStepDays;
            if (CrossesTarget(low, high, target))
            {
                return Bisect(low, high, target);
            }
            low = high;
        }

        // No crossing found - outside the ephemeris span, most likely. Fall back to a nominal
        // year so a date is still produced rather than the dasha collapsing.
        return fromJd + NominalYearDays;
    }

    /// <summary>Whether the Sun passes the target between two instants.</summary>
    private bool CrossesTarget(double from, double to, double target)
    {
        // The gap falls steadily to zero and jumps to nearly 360 just past the crossing, so the
        // one step where it rises is the boundary.
        return GapBefore(to, target) > GapBefore(from, target);
    }

    /// <summary>Degrees the Sun still has to travel before reaching the target; 0 to 360.</summary>
    private double GapBefore(double jd, double target)
    {
        double diff = target - _sunLongitude(jd);
        return ((diff % 360.0) + 360.0) % 360.0;
    }

    private double Bisect(double from, double to, double target)
    {
        double low = from;
        double high = to;
        for (int i = 0; i < Bisections; i++)
        {
            double middle = (low + high) / 2.0;
            if (GapBefore(middle, target) > 180.0) high = middle; else low = middle;
        }
        return (low + high) / 2.0;
    }
}
