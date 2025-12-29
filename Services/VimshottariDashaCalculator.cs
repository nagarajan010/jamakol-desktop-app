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
    public DashaResult Calculate(double moonLongitude, double birthJulianDay, double currentJulianDay, int calculateLevels = 3)
    {
        var result = new DashaResult();

        // Calculate Moon's nakshatra and position within it
        double nakshatraSize = 360.0 / 27.0; // 13.333... degrees
        int nakshatraIndex = (int)(moonLongitude / nakshatraSize);
        if (nakshatraIndex >= 27) nakshatraIndex = 26;
        if (nakshatraIndex < 0) nakshatraIndex = 0;

        double positionInNakshatra = moonLongitude - (nakshatraIndex * nakshatraSize);
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
        
        // Days per sidereal year is approx 365.25636, Dasha usually uses Savana (360) or Sidereal.
        // Standard practice often uses Gregorian 365.2425 or 365.25. 
        // JHora default is 365.2425 (Gregorian year).
        double daysPerYear = 365.2425;
        
        result.BalanceAtBirthDays = balanceYears * daysPerYear;

        // Calculate all Maha Dasas
        double dashaStartJd = birthJulianDay;
        
        // First Maha Dasa starts with remaining balance
        for (int cycle = 0; cycle < 2; cycle++) // 2 cycles = 240 years (more than enough)
        {
            for (int i = 0; i < 9; i++)
            {
                int planetIndex = (startIndex + i) % 9;
                string planet = DashaSequence[planetIndex];
                
                double years;
                if (cycle == 0 && i == 0)
                {
                    years = balanceYears; // First dasa uses balance
                }
                else
                {
                    years = DashaYears[planet];
                }

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
                        planet, dashaStartJd, years, 2, calculateLevels, currentJulianDay, daysPerYear);
                }

                result.MahaDashas.Add(mahaDasha);

                // Track current dasha
                if (mahaDasha.IsActive)
                {
                    result.CurrentMahaDasha = mahaDasha;
                    FindCurrentSubDashas(mahaDasha, currentJulianDay, result);
                }

                dashaStartJd = dashaEndJd;

                // Stop if we've gone past 150 years from birth (approx 54786 days)
                if (dashaStartJd > birthJulianDay + 54786) break;
            }
            if (dashaStartJd > birthJulianDay + 54786) break;
        }

        return result;
    }

    /// <summary>
    /// Calculate sub-periods recursively using Julian Days
    /// </summary>
    private List<DashaPeriod> CalculateSubPeriods(
        string mahaPlanet, 
        double startJd, 
        double totalYears, 
        int level, 
        int maxLevel,
        double currentJd,
        double daysPerYear)
    {
        var subPeriods = new List<DashaPeriod>();
        int startIndex = Array.IndexOf(DashaSequence, mahaPlanet);
        double subStartJd = startJd;

        for (int i = 0; i < 9; i++)
        {
            int planetIndex = (startIndex + i) % 9;
            string planet = DashaSequence[planetIndex];

            // Sub-period duration = (mahaDuration * planet's proportion) / 120
            double proportion = DashaYears[planet] / 120.0;
            double subYears = totalYears * proportion;
            double subDurationDays = subYears * daysPerYear;
            double subEndJd = subStartJd + subDurationDays;

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
                    planet, subStartJd, subYears, level + 1, maxLevel, currentJd, daysPerYear);
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
            return new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddDays(delta);
        }
        catch {
            return DateTime.MinValue;
        }
    }
}
