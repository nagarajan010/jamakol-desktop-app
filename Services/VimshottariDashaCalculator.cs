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
    /// <param name="birthDateTime">Birth date and time</param>
    /// <param name="currentDate">Date to check for current running dasa (usually today)</param>
    /// <param name="calculateLevels">Number of levels to calculate (1-5)</param>
    public DashaResult Calculate(double moonLongitude, DateTime birthDateTime, DateTime currentDate, int calculateLevels = 3)
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
        result.BalanceAtBirthDays = balanceYears * 365.2425;

        // Calculate all Maha Dasas
        DateTime dashaStart = birthDateTime;
        
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

                DateTime dashaEnd = dashaStart.AddDays(years * 365.2425);

                var mahaDasha = new DashaPeriod
                {
                    Planet = planet,
                    Symbol = PlanetSymbols[planet],
                    Level = 1,
                    StartDate = dashaStart,
                    EndDate = dashaEnd,
                    DurationYears = years,
                    IsActive = currentDate >= dashaStart && currentDate < dashaEnd
                };

                // Calculate sub-periods if needed
                if (calculateLevels >= 2)
                {
                    mahaDasha.SubPeriods = CalculateSubPeriods(
                        planet, dashaStart, years, 2, calculateLevels, currentDate);
                }

                result.MahaDashas.Add(mahaDasha);

                // Track current dasha
                if (mahaDasha.IsActive)
                {
                    result.CurrentMahaDasha = mahaDasha;
                    FindCurrentSubDashas(mahaDasha, currentDate, result);
                }

                dashaStart = dashaEnd;

                // Stop if we've gone past 150 years from birth
                if (dashaStart > birthDateTime.AddYears(150)) break;
            }
            if (dashaStart > birthDateTime.AddYears(150)) break;
        }

        return result;
    }

    /// <summary>
    /// Calculate sub-periods recursively
    /// </summary>
    private List<DashaPeriod> CalculateSubPeriods(
        string mahaPlanet, 
        DateTime startDate, 
        double totalYears, 
        int level, 
        int maxLevel,
        DateTime currentDate)
    {
        var subPeriods = new List<DashaPeriod>();
        int startIndex = Array.IndexOf(DashaSequence, mahaPlanet);
        DateTime subStart = startDate;

        for (int i = 0; i < 9; i++)
        {
            int planetIndex = (startIndex + i) % 9;
            string planet = DashaSequence[planetIndex];

            // Sub-period duration = (mahaDuration * planet's proportion) / 120
            double proportion = DashaYears[planet] / 120.0;
            double subYears = totalYears * proportion;
            DateTime subEnd = subStart.AddDays(subYears * 365.2425);

            var subPeriod = new DashaPeriod
            {
                Planet = planet,
                Symbol = PlanetSymbols[planet],
                Level = level,
                StartDate = subStart,
                EndDate = subEnd,
                DurationYears = subYears,
                IsActive = currentDate >= subStart && currentDate < subEnd
            };

            // Recursively calculate deeper levels
            if (level < maxLevel)
            {
                subPeriod.SubPeriods = CalculateSubPeriods(
                    planet, subStart, subYears, level + 1, maxLevel, currentDate);
            }

            subPeriods.Add(subPeriod);
            subStart = subEnd;
        }

        return subPeriods;
    }

    /// <summary>
    /// Find and set current running dashas at all levels
    /// </summary>
    private void FindCurrentSubDashas(DashaPeriod mahaDasha, DateTime currentDate, DashaResult result)
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
}
