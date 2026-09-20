using System;
using System.Collections.Generic;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Calculator for Vimshottari Dasa system (120-year cycle)
/// Calculates all 5 levels: Maha, Antar, Pratyantara, Sookshma, Prana
/// </summary>
public class VimshottariDashaCalculator
{
    /// <summary>
    /// Length of a dasha year in days: the true sidereal solar year.
    /// </summary>
    private const double DaysPerYear = 365.256363;

    /// <summary>Hours east of UTC used to print the periods; see Calculate.</summary>
    private double _timeZoneOffset;

    // Planet sequence in Vimshottari system
    private static readonly string[] DashaSequence = 
    { "Ketu", "Venus", "Sun", "Moon", "Mars", "Rahu", "Jupiter", "Saturn", "Mercury" };

    // Planet symbols
    private static readonly Dictionary<string, string> PlanetSymbols = new()
    {
        { "Ketu", "Ke" }, { "Venus", "Ve" }, { "Sun", "Su" }, { "Moon", "Mo" },
        { "Mars", "Ma" }, { "Rahu", "Ra" }, { "Jupiter", "Ju" }, { "Saturn", "Sa" }, { "Mercury", "Me" }
    };

    // Years for each planet's Maha Dasa (total = 120 years)
    private static readonly Dictionary<string, double> DashaYears = new()
    {
        { "Ketu", 7 }, { "Venus", 20 }, { "Sun", 6 }, { "Moon", 10 },
        { "Mars", 7 }, { "Rahu", 18 }, { "Jupiter", 16 }, { "Saturn", 19 }, { "Mercury", 17 }
    };

    // Nakshatra to ruling planet mapping (0-26 nakshatras)
    private static readonly string[] NakshatraRulers =
    {
        "Ketu", "Venus", "Sun", "Moon", "Mars", "Rahu", "Jupiter", "Saturn", "Mercury",  // 0-8
        "Ketu", "Venus", "Sun", "Moon", "Mars", "Rahu", "Jupiter", "Saturn", "Mercury",  // 9-17
        "Ketu", "Venus", "Sun", "Moon", "Mars", "Rahu", "Jupiter", "Saturn", "Mercury"   // 18-26
    };

    private static readonly string[] NakshatraNames =
    {
        "Ashwini", "Bharani", "Krittika", "Rohini", "Mrigashira", "Ardra",
        "Punarvasu", "Pushya", "Ashlesha", "Magha", "Purva Phalguni", "Uttara Phalguni",
        "Hasta", "Chitra", "Swati", "Vishakha", "Anuradha", "Jyeshtha",
        "Mula", "Purva Ashadha", "Uttara Ashadha", "Shravana", "Dhanishta", "Shatabhisha",
        "Purva Bhadrapada", "Uttara Bhadrapada", "Revati"
    };

    /// <summary>
    /// Calculate complete Vimshottari Dasa from Moon's position
    /// </summary>
    /// <param name="moonLongitude">Moon's absolute longitude (0-360)</param>
    /// <param name="birthJulianDay">Birth date in Julian Day</param>
    /// <param name="currentJulianDay">Target date in Julian Day for identifying "current" dasa</param>
    /// <param name="calculateLevels">Number of levels to calculate (1-5)</param>
    /// <param name="timeZoneOffset">
    /// Hours east of UTC for the chart's place, e.g. 5.5 for IST. A Julian Day is an instant with
    /// no zone, so a period converted straight back reads as UTC - the dates printed beside every
    /// period were hours off the clock the reader is holding, while the period marked current was
    /// right all along, since that test compares Julian Days.
    /// </param>
    public DashaResult Calculate(double moonLongitude, double birthJulianDay, double currentJulianDay, int calculateLevels = 3, double timeZoneOffset = 0.0)
    {
        var result = new DashaResult();
        _timeZoneOffset = timeZoneOffset;

        // Calculate Moon's nakshatra and position within it.
        // Normalize first: an out-of-range longitude would otherwise be papered over by the
        // clamps below while the in-nakshatra position stayed wrong, which can yield a negative
        // balance and hence a negative first mahadasha.
        moonLongitude = ((moonLongitude % 360.0) + 360.0) % 360.0;

        double nakshatraSize = 360.0 / 27.0; // 13.333... degrees
        int nakshatraIndex = (int)(moonLongitude / nakshatraSize);
        if (nakshatraIndex >= 27) nakshatraIndex = 26;
        if (nakshatraIndex < 0) nakshatraIndex = 0;

        double positionInNakshatra = moonLongitude % nakshatraSize;
        double proportionTraversed = positionInNakshatra / nakshatraSize;

        result.MoonNakshatra = NakshatraNames[nakshatraIndex];
        result.MoonNakshatraPada = (int)(proportionTraversed * 4) + 1;
        if (result.MoonNakshatraPada > 4) result.MoonNakshatraPada = 4;

        // Get the ruling planet for this nakshatra
        string birthDashaLord = NakshatraRulers[nakshatraIndex];
        int startIndex = Array.IndexOf(DashaSequence, birthDashaLord);

        // Calculate balance of birth dasa (remaining portion)
        double proportionRemaining = 1.0 - proportionTraversed;
        double birthDashaYears = DashaYears[birthDashaLord];
        double balanceYears = birthDashaYears * proportionRemaining;
        
        // Vimshottari periods are counted in SIDEREAL years - the Sun's return to the same fixed
        // star - which is what reference software means by "true sidereal solar years". Using the
        // Gregorian 365.2425 (or the Julian 365.25) made every boundary fall a day or two early
        // across the 120-year cycle.
        double daysPerYear = DaysPerYear;
        
        result.BalanceAtBirthDays = balanceYears * daysPerYear;

        // Calculate all Maha Dasas
        double dashaStartJd = birthJulianDay;
        
        // One full cycle only. The nine periods sum to exactly 120 years, which is the whole
        // Vimshottari span - continuing into a second cycle just repeats the same sequence at
        // ages nobody lives to. The old loop ran two cycles with a 150-year cut-off tested
        // AFTER appending, so charts ran out past 160 years.
        {
            for (int i = 0; i < 9; i++)
            {
                int planetIndex = (startIndex + i) % 9;
                string planet = DashaSequence[planetIndex];

                // First dasa uses the balance remaining at birth.
                double years = (i == 0) ? balanceYears : DashaYears[planet];

                double durationDays = years * daysPerYear;
                double dashaEndJd = dashaStartJd + durationDays;

                var mahaDasha = new DashaPeriod
                {
                    Planet = planet,
                    Symbol = PlanetSymbols[planet],
                    Level = 1,
                    StartJulianDay = dashaStartJd, // Use Julian Day
                    EndJulianDay = dashaEndJd, // Use Julian Day
                    // Keep DateTime for backward compatibility if possible, but map directly from JD helper
                    // StartDate = JulianDayToDateTime(dashaStartJd),
                    // EndDate = JulianDayToDateTime(dashaEndJd), 
                    // Let's populate StartDate/EndDate best effort for AD dates to avoid breaking other UI binding immediately
                    // For BC dates they might be MinValue or incorrect, but new UI uses Display properties.
                    StartDate = SafeJdToDateTime(dashaStartJd),
                    EndDate = SafeJdToDateTime(dashaEndJd),

                    DurationYears = years,
                    IsActive = currentJulianDay >= dashaStartJd && currentJulianDay < dashaEndJd
                };

                // Calculate sub-periods if needed
                if (calculateLevels >= 2)
                {
                    mahaDasha.SubPeriods = CalculateSubPeriods(
                        planet, dashaStartJd, dashaEndJd, years, 2, calculateLevels, currentJulianDay, daysPerYear);
                }

                result.MahaDashas.Add(mahaDasha);

                // Track current dasha
                if (mahaDasha.IsActive)
                {
                    result.CurrentMahaDasha = mahaDasha;
                    FindCurrentSubDashas(mahaDasha, currentJulianDay, result);
                }

                dashaStartJd = dashaEndJd;
            }
        }

        return result;
    }

    /// <summary>
    /// Calculate sub-periods recursively using Julian Days
    /// </summary>
    private List<DashaPeriod> CalculateSubPeriods(
        string mahaPlanet, 
        double startJd, 
        double endJd,
        double totalYears, 
        int level, 
        int maxLevel,
        double currentJd,
        double daysPerYear)
    {
        var subPeriods = new List<DashaPeriod>();
        int startIndex = Array.IndexOf(DashaSequence, mahaPlanet);
        double subStartJd = startJd;

        // Apportion the parent's MEASURED span rather than re-deriving it from years, and give
        // the final child the parent's exact end. Chaining nine computed spans per level over
        // six nested levels leaves floating-point cracks at the boundaries, and a query instant
        // landing in one makes FindCurrentSubDashas return no deeper period at all.
        double parentSpan = endJd - startJd;

        for (int i = 0; i < 9; i++)
        {
            int planetIndex = (startIndex + i) % 9;
            string planet = DashaSequence[planetIndex];

            // Sub-period takes the same share of its parent as its lord takes of the cycle.
            double proportion = DashaYears[planet] / 120.0;
            double subYears = totalYears * proportion;
            double subEndJd = (i == 8) ? endJd : subStartJd + (parentSpan * proportion);

            var subPeriod = new DashaPeriod
            {
                Planet = planet,
                Symbol = PlanetSymbols[planet],
                Level = level,
                StartJulianDay = subStartJd,
                EndJulianDay = subEndJd,
                StartDate = SafeJdToDateTime(subStartJd),
                EndDate = SafeJdToDateTime(subEndJd),
                DurationYears = subYears,
                IsActive = currentJd >= subStartJd && currentJd < subEndJd
            };

            // Recursively calculate deeper levels
            if (level < maxLevel)
            {
                subPeriod.SubPeriods = CalculateSubPeriods(
                    planet, subStartJd, subEndJd, subYears, level + 1, maxLevel, currentJd, daysPerYear);
            }

            subPeriods.Add(subPeriod);
            subStartJd = subEndJd;
        }

        return subPeriods;
    }

    /// <summary>
    /// Find and set current running dashas at all levels
    /// </summary>
    private void FindCurrentSubDashas(DashaPeriod mahaDasha, double currentJd, DashaResult result)
    {
        foreach (var antar in mahaDasha.SubPeriods)
        {
            if (antar.IsActive)
            {
                result.CurrentAntarDasha = antar;
                
                foreach (var pratyantar in antar.SubPeriods)
                {
                    if (pratyantar.IsActive)
                    {
                        result.CurrentPratyantaraDasha = pratyantar;
                        
                        foreach (var sookshma in pratyantar.SubPeriods)
                        {
                            if (sookshma.IsActive)
                            {
                                result.CurrentSookshmaDasha = sookshma;
                                
                                foreach (var prana in sookshma.SubPeriods)
                                {
                                    if (prana.IsActive)
                                    {
                                        result.CurrentPranaDasha = prana;
                                        
                                        foreach (var deha in prana.SubPeriods)
                                        {
                                            if (deha.IsActive)
                                            {
                                                result.CurrentDehaDasha = deha;
                                                return;
                                            }
                                        }
                                        return;
                                    }
                                }
                                return;
                            }
                        }
                        return;
                    }
                }
                return;
            }
        }
    }

    private DateTime SafeJdToDateTime(double jd)
    {
        try {
            // Very simplified conversion for compatibility
            // This will likely fail or wrap for BC dates, but we catch generic exception or clamp
            // .NET DateTime MinValue is 0001-01-01
            // JD for 0001-01-01 is roughly 1721425.5
            if (jd < 1721426) return DateTime.MinValue; // Treat BC as MinValue for Date operations
            
            // Just use a basic AddDays from a known epoch if within range
            // Epoch: 2000-01-01 12:00 UTC = JD 2451545.0
            double delta = jd - 2451545.0;
            // Shift into the chart's own zone: a Julian Day is an instant, and printing it raw
            // shows UTC beside a clock the reader reads as local.
            return new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Unspecified)
                .AddDays(delta)
                .AddHours(_timeZoneOffset);
        }
        catch {
            return DateTime.MinValue;
        }
    }
}
