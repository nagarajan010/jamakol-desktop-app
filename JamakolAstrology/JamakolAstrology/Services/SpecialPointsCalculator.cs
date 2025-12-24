using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Calculator for special points: Aarudam (AR), Kavippu (KV)
/// </summary>
public class SpecialPointsCalculator
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

    // Aarudam timing map: minute range -> sign index (0-11)
    private static readonly (int min, int max, int signIndex)[] AarudamMap =
    {
        (55, 59, 11), // Pisces
        (0, 4, 0),    // Aries
        (5, 9, 1),    // Taurus
        (10, 14, 2),  // Gemini
        (15, 19, 3),  // Cancer
        (20, 24, 4),  // Leo
        (25, 29, 5),  // Virgo
        (30, 34, 6),  // Libra
        (35, 39, 7),  // Scorpio
        (40, 44, 8),  // Sagittarius
        (45, 49, 9),  // Capricorn
        (50, 54, 10), // Aquarius
    };

    /// <summary>
    /// Calculate Aarudam (AR) based on birth minute
    /// Each 5-minute range maps to a zodiac sign
    /// </summary>
    public SpecialPoint CalculateAarudam(DateTime birthTime)
    {
        int minute = birthTime.Minute;
        int second = birthTime.Second;

        foreach (var (min, max, signIndex) in AarudamMap)
        {
            if (minute >= min && minute <= max)
            {
                // Calculate degree within the sign based on minute and second within the 5-minute range
                int minuteInRange = minute - min;
                int rangeSize = max - min + 1; // 5 minutes

                // Convert to total seconds in range
                int totalSecondsInRange = rangeSize * 60; // 300 seconds
                int currentSecondInRange = (minuteInRange * 60) + second;

                // Distribute across 30 degrees
                double degreeInSign = (currentSecondInRange / (double)totalSecondsInRange) * 30;

                // Calculate absolute longitude
                double absoluteLongitude = (signIndex * 30) + degreeInSign;

                // Calculate nakshatra
                var (nakshatra, pada) = CalculateNakshatra(absoluteLongitude);

                return new SpecialPoint
                {
                    Name = "Aarudam",
                    Symbol = "AR",
                    Sign = SignNames[signIndex],
                    SignIndex = signIndex,
                    DegreeInSign = degreeInSign,
                    AbsoluteLongitude = absoluteLongitude,
                    NakshatraName = nakshatra,
                    Pada = pada
                };
            }
        }

        // Default fallback (should not reach here)
        return new SpecialPoint
        {
            Name = "Aarudam",
            Symbol = "AR",
            Sign = "Aries",
            SignIndex = 0,
            DegreeInSign = 0,
            AbsoluteLongitude = 0,
            NakshatraName = "Ashwini",
            Pada = 1
        };
    }

    /// <summary>
    /// Calculate Kavippu (KV) based on Tamil month, Udayam, and Aarudam
    /// Tamil month is same as Sun sign (1=Aries, 2=Taurus, etc.)
    /// </summary>
    public SpecialPoint CalculateKavippu(int sunSign, double udayamLongitude, double aarudamLongitude)
    {
        // Calculate houses from longitudes
        int udayamHouse = GetHouseFromLongitude(udayamLongitude);
        int aarudamHouse = GetHouseFromLongitude(aarudamLongitude);

        // Determine Veethi based on Tamil month (Sun sign)
        // Tamil months: Chithirai (Aries=1), Vaikasi (Taurus=2), etc.
        int veethi;
        if (sunSign >= 2 && sunSign <= 5) // Vaikasi to Aavani (Taurus to Leo)
        {
            veethi = 1; // Mesham (Aries)
        }
        else if (sunSign >= 8 && sunSign <= 11) // Aippasi to Thai (Scorpio to Aquarius)
        {
            veethi = 3; // Mithunam (Gemini)
        }
        else // Panguni, Chithirai, Purattasi, Masi (Pisces, Aries, Virgo, Aquarius months)
        {
            veethi = 2; // Rishabam (Taurus)
        }

        // Calculate Kavippu house
        int housesFromAarudamToVeethi = CountHouses(aarudamHouse, veethi);
        int kavippuHouse = CountToHouse(udayamHouse, housesFromAarudamToVeethi);

        // Calculate Kavippu longitude
        double aarudamDegreeInSign = aarudamLongitude % 30;
        double kavippuLongitude = (kavippuHouse * 30) - aarudamDegreeInSign;

        // Normalize to 0-360
        if (kavippuLongitude < 0) kavippuLongitude += 360;
        kavippuLongitude %= 360;

        int signIndex = (int)(kavippuLongitude / 30);
        double degreeInSign = kavippuLongitude - (signIndex * 30);

        var (nakshatra, pada) = CalculateNakshatra(kavippuLongitude);

        return new SpecialPoint
        {
            Name = "Kavippu",
            Symbol = "KV",
            Sign = SignNames[signIndex],
            SignIndex = signIndex,
            DegreeInSign = degreeInSign,
            AbsoluteLongitude = kavippuLongitude,
            NakshatraName = nakshatra,
            Pada = pada
        };
    }

    /// <summary>
    /// Get house number (1-12) from absolute longitude (0-360)
    /// </summary>
    private int GetHouseFromLongitude(double longitude)
    {
        if (longitude % 30 == 0)
        {
            int edgeIndex = (int)Math.Ceiling(longitude / 30) + 1;
            return edgeIndex > 12 ? edgeIndex - 12 : edgeIndex;
        }
        return (int)Math.Ceiling(longitude / 30);
    }

    /// <summary>
    /// Count houses from start to end
    /// </summary>
    private int CountHouses(int start, int end)
    {
        if (end >= start)
        {
            return end - start + 1;
        }
        return (12 - start + 1) + end;
    }

    /// <summary>
    /// Count forward from start by count houses
    /// </summary>
    private int CountToHouse(int start, int count)
    {
        int result = (start + count - 1) % 12;
        return result == 0 ? 12 : result;
    }

    /// <summary>
    /// Calculate nakshatra and pada from absolute longitude
    /// </summary>
    private (string nakshatra, int pada) CalculateNakshatra(double longitude)
    {
        double nakshatraSize = 360.0 / 27.0; // 13.333... degrees
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
