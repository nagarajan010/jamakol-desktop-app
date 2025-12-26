using System;
using System.Collections.Generic;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Calculator for supplementary astrological points: Rahu Kalam, Yemakandam, Mrithyu, Mandhi
/// - Rahu Kalam & Yemakandam: portion-based sign + Sun's degree
/// - Mrithyu & Mandhi: offset-based calculation from Sun's position
/// </summary>
public class SupplementaryPlanetsCalculator
{
    private static readonly string[] SignNames = 
    {
        "Aries", "Taurus", "Gemini", "Cancer", "Leo", "Virgo",
        "Libra", "Scorpio", "Sagittarius", "Capricorn", "Aquarius", "Pisces"
    };

    private static readonly string[] NakshatraNames =
    {
        "Ashwini", "Bharani", "Krittika", "Rohini", "Mrigashira", "Ardra",
        "Punarvasu", "Pushya", "Ashlesha", "Magha", "Purva Phalguni", "Uttara Phalguni",
        "Hasta", "Chitra", "Swati", "Vishakha", "Anuradha", "Jyeshtha",
        "Mula", "Purva Ashadha", "Uttara Ashadha", "Shravana", "Dhanishta", "Shatabhisha",
        "Purva Bhadrapada", "Uttara Bhadrapada", "Revati"
    };

    // The 8 signs used in Jamakol system for Rahu Kalam & Yemakandam
    private static readonly int[] PortionToSign = { 12, 10, 9, 7, 6, 4, 3, 1 }; // Pisces, Cap, Sag, Lib, Vir, Can, Gem, Ari

    // Portion indices for Rahu Kalam by weekday (0=Sun, 1=Mon, ..., 6=Sat)
    private static readonly int[] RahuKalamPortions = { 8, 2, 7, 5, 6, 4, 3 };

    // Portion indices for Yemakandam by weekday
    private static readonly int[] YemakandamPortions = { 5, 4, 3, 2, 1, 7, 6 };

    // Mrithyu degree offsets by day lord for day/night
    // கதிரவன்=Sun, மதி=Moon, சேய்=Mars, மால்=Mercury, பொன்=Jupiter, புகர்=Venus, மந்தன்=Saturn
    private static readonly Dictionary<string, (double day, double night)> MrithyuOffsets = new()
    {
        { "Sun",     (60, 312) },
        { "Moon",    (36, 288) },
        { "Mars",    (12, 264) },
        { "Mercury", (156, 240) },
        { "Jupiter", (132, 216) },
        { "Venus",   (108, 192) },
        { "Saturn",  (84, 336) }
    };

    // Mandhi degree offsets by day lord for day/night
    private static readonly Dictionary<string, (double day, double night)> MandhiOffsets = new()
    {
        { "Sun",     (156, 240) },
        { "Moon",    (132, 216) },
        { "Mars",    (108, 192) },
        { "Mercury", (84, 336) },
        { "Jupiter", (60, 312) },
        { "Venus",   (36, 288) },
        { "Saturn",  (12, 264) }
    };

    /// <summary>
    /// Calculate all supplementary planet points
    /// </summary>
    public List<SpecialPoint> Calculate(double sunLongitude, string dayLord, bool isDay, DayOfWeek dayOfWeek)
    {
        var result = new List<SpecialPoint>();
        int vara = (int)dayOfWeek;
        double sunDegreeInSign = sunLongitude % 30;

        // Rahu Kalam: portion-based sign + Sun's degree
        int rahuPortion = RahuKalamPortions[vara];
        result.Add(CreatePortionBasedPoint("Rahu Kalam", "RK", rahuPortion, sunDegreeInSign));

        // Yemakandam: portion-based sign + Sun's degree
        int yemaPortion = YemakandamPortions[vara];
        result.Add(CreatePortionBasedPoint("Yemakandam", "YK", yemaPortion, sunDegreeInSign));

        // Mrithyu: offset-based calculation from Sun's position
        result.Add(CalculateOffsetPoint("Mrithyu", "MR", sunLongitude, dayLord, isDay, MrithyuOffsets));

        // Mandhi: offset-based calculation from Sun's position
        result.Add(CalculateOffsetPoint("Mandhi", "MA", sunLongitude, dayLord, isDay, MandhiOffsets));

        return result;
    }

    /// <summary>
    /// Create point using portion-based sign + Sun's degree
    /// </summary>
    private SpecialPoint CreatePortionBasedPoint(string name, string symbol, int portionIndex, double degreeInSign)
    {
        int signNumber = PortionToSign[portionIndex - 1]; // 1-12
        int signIndex = signNumber - 1; // 0-11
        double position = (signIndex * 30) + degreeInSign;

        var (nakshatra, pada) = CalculateNakshatra(position);

        return new SpecialPoint
        {
            Name = name,
            Symbol = symbol,
            Sign = SignNames[signIndex],
            SignIndex = signIndex,
            DegreeInSign = degreeInSign,
            AbsoluteLongitude = position,
            NakshatraName = nakshatra,
            Pada = pada
        };
    }

    /// <summary>
    /// Calculate offset-based point: (sunPos + offset[day][dayType]) % 360
    /// For night, add 180° adjustment
    /// </summary>
    private SpecialPoint CalculateOffsetPoint(
        string name, 
        string symbol, 
        double sunLongitude, 
        string dayLord, 
        bool isDay,
        Dictionary<string, (double day, double night)> offsets)
    {
        // Get offset from lookup table
        if (!offsets.TryGetValue(dayLord, out var offset))
        {
            offset = offsets["Sun"]; // Default to Sun if not found
        }

        double offsetValue = isDay ? offset.day : offset.night;

        // Calculate position: (sunPos + offset) % 360
        double position = (sunLongitude + offsetValue) % 360;

        // For night, add 180° adjustment
        if (!isDay)
        {
            position = (position + 180) % 360;
        }

        // Calculate sign and degree
        int signIndex = (int)(position / 30);
        if (signIndex >= 12) signIndex = 11;
        if (signIndex < 0) signIndex = 0;
        double degreeInSign = position % 30;

        var (nakshatra, pada) = CalculateNakshatra(position);

        return new SpecialPoint
        {
            Name = name,
            Symbol = symbol,
            Sign = SignNames[signIndex],
            SignIndex = signIndex,
            DegreeInSign = degreeInSign,
            AbsoluteLongitude = position,
            NakshatraName = nakshatra,
            Pada = pada
        };
    }

    private (string nakshatra, int pada) CalculateNakshatra(double longitude)
    {
        double nakshatraSize = 360.0 / 27.0;
        int nakshatraIndex = (int)(longitude / nakshatraSize);
        if (nakshatraIndex >= 27) nakshatraIndex = 26;
        if (nakshatraIndex < 0) nakshatraIndex = 0;

        double offsetInNak = longitude - (nakshatraIndex * nakshatraSize);
        int pada = (int)(offsetInNak / (nakshatraSize / 4)) + 1;
        if (pada > 4) pada = 4;
        if (pada < 1) pada = 1;

        return (NakshatraNames[nakshatraIndex], pada);
    }
}
