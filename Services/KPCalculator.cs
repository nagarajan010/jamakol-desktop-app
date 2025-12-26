using System;
using System.Collections.Generic;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Calculator for Krishnamurti Paddhati (KP) Lords
/// Calculates hierarchy: Sign Lord -> Star Lord -> Sub Lord -> Sub-Sub Lord -> ...
/// </summary>
public class KPCalculator
{
    // Planet sequence in Vimshottari system
    private static readonly string[] DashaSequence = 
    { "Ketu", "Venus", "Sun", "Moon", "Mars", "Rahu", "Jupiter", "Saturn", "Mercury" };

    // Years for each planet's Maha Dasa (total = 120 years)
    private static readonly Dictionary<string, double> DashaYears = new()
    {
        { "Ketu", 7 }, { "Venus", 20 }, { "Sun", 6 }, { "Moon", 10 },
        { "Mars", 7 }, { "Rahu", 18 }, { "Jupiter", 16 }, { "Saturn", 19 }, { "Mercury", 17 }
    };
    
    // Sign Lords (0=Aries ... 11=Pisces)
    private static readonly string[] SignLords =
    {
        "Mars", "Venus", "Mercury", "Moon", "Sun", "Mercury", "Venus", 
        "Mars", "Jupiter", "Saturn", "Saturn", "Jupiter"
    };

    // Nakshatra Rulers
    private static readonly string[] NakshatraRulers =
    {
        "Ketu", "Venus", "Sun", "Moon", "Mars", "Rahu", "Jupiter", "Saturn", "Mercury",  // 0-8
        "Ketu", "Venus", "Sun", "Moon", "Mars", "Rahu", "Jupiter", "Saturn", "Mercury",  // 9-17
        "Ketu", "Venus", "Sun", "Moon", "Mars", "Rahu", "Jupiter", "Saturn", "Mercury"   // 18-26
    };

    /// <summary>
    /// Calculate KP Lords for a given longitude
    /// </summary>
    /// <param name="longitude">Absolute longitude (0-360)</param>
    public KPLords Calculate(double longitude)
    {
        var lords = new KPLords();
        longitude = ZodiacUtils.NormalizeDegree(longitude);

        // 1. Sign Lord
        int signIndex = (int)(longitude / 30.0);
        lords.SignLord = SignLords[signIndex];

        // 2. Star Lord (Nakshatra)
        double nakshatraSize = 360.0 / 27.0; // 13.3333... deg
        int nakshatraIndex = (int)(longitude / nakshatraSize);
        if (nakshatraIndex >= 27) nakshatraIndex = 26;
        
        string starLord = NakshatraRulers[nakshatraIndex];
        lords.StarLord = starLord;

        // Position within Nakshatra
        double posInNak = longitude - (nakshatraIndex * nakshatraSize);
        
        // 3. Sub Lord
        // Start from Nakshatra Lord, divide Nakshatra Size proportional to years
        lords.SubLord = FindSubLevelLord(starLord, nakshatraSize, posInNak, out double remAfterSub, out double subSpan, out string subLordName);

        // 4. Sub-Sub Lord (Pratyantar)
        // Start from Sub Lord, divide Sub Span proportional to years
        lords.SubSubLord = FindSubLevelLord(subLordName, subSpan, remAfterSub, out double remAfterSubSub, out double subSubSpan, out string subSubLordName);

        // 5. Sookshma Lord
        lords.SookshmaLord = FindSubLevelLord(subSubLordName, subSubSpan, remAfterSubSub, out double remAfterSookshma, out double sookshmaSpan, out string sookshmaLordName);

        // 6. Prana Lord
        lords.PranaLord = FindSubLevelLord(sookshmaLordName, sookshmaSpan, remAfterSookshma, out double remAfterPrana, out double pranaSpan, out string pranaLordName);

        // 7. Deha Lord
        lords.DehaLord = FindSubLevelLord(pranaLordName, pranaSpan, remAfterPrana, out _, out _, out _);

        return lords;
    }

    /// <summary>
    /// Finds the lord of a specific subdivision
    /// </summary>
    /// <param name="rulerOfParent">The ruler of the parent level (start point of cycle)</param>
    /// <param name="parentSpan">The full span of the parent level in degrees</param>
    /// <param name="positionInParent">Current position within the parent span</param>
    /// <param name="remaining">Output: Remaining position within THIS found level</param>
    /// <param name="span">Output: The span of THIS found level</param>
    /// <param name="lordName">Output: Name of the found lord</param>
    /// <returns>The found lord name</returns>
    private string FindSubLevelLord(string rulerOfParent, double parentSpan, double positionInParent, 
                                    out double remaining, out double span, out string lordName)
    {
        int startIndex = Array.IndexOf(DashaSequence, rulerOfParent);
        double currentPos = 0;

        for (int i = 0; i < 9; i++)
        {
            int planetIndex = (startIndex + i) % 9;
            string planet = DashaSequence[planetIndex];
            
            // Calculate span of this planet in this level
            // Span = (PlanetYears / 120) * ParentSpan
            double planetSpan = (DashaYears[planet] / 120.0) * parentSpan;

            if (positionInParent < currentPos + planetSpan)
            {
                // Found it
                remaining = positionInParent - currentPos;
                span = planetSpan;
                lordName = planet;
                return planet;
            }

            currentPos += planetSpan;
        }

        // Fallback (should not happen due to precision, but assume last planet)
        remaining = 0;
        span = 0;
        
        // Find last one
        int lastIndex = (startIndex + 8) % 9;
        lordName = DashaSequence[lastIndex];
        return lordName;
    }
}
