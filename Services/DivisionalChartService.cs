using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Service for calculating Divisional Charts (Varga Charts) in Vedic Astrology
/// Based on traditional Parasara formulas
/// </summary>
public class DivisionalChartService
{
    /// <summary>
    /// Sign names in order (1-indexed, index 0 is empty)
    /// </summary>
    private static readonly string[] SignNames = ZodiacUtils.SignNames;

    /// <summary>
    /// Calculate divisional chart position for a given longitude and division
    /// </summary>
    /// <param name="longitude">Absolute longitude (0-360)</param>
    /// <param name="division">Division number (1-12, 16, 20, 24, 27, 30, 40, 45, 60)</param>
    /// <returns>DivisionalPosition with sign index, sign name, and degree</returns>
    public DivisionalPosition CalculateDivisionalPosition(double longitude, int division)
    {
        // Normalize longitude to 0-360
        longitude = ZodiacUtils.NormalizeDegree(longitude);

        // Get the base rasi (sign) index (0-11 for calculation, we'll convert to 1-12)
        int rasiIndex = (int)Math.Floor(longitude / 30);
        
        // Get degree within the rasi (0-30)
        double degreeInRasi = longitude - (rasiIndex * 30);

        // Calculate divisional position based on the division
        int divisionalSignIndex = division switch
        {
            1 => CalculateD1(rasiIndex, degreeInRasi),
            2 => CalculateD2(rasiIndex, degreeInRasi),
            3 => CalculateD3(rasiIndex, degreeInRasi),
            4 => CalculateD4(rasiIndex, degreeInRasi),
            5 => CalculateD5(rasiIndex, degreeInRasi),
            6 => CalculateD6(rasiIndex, degreeInRasi),
            7 => CalculateD7(rasiIndex, degreeInRasi),
            8 => CalculateD8(rasiIndex, degreeInRasi),
            9 => CalculateD9(rasiIndex, degreeInRasi),
            10 => CalculateD10(rasiIndex, degreeInRasi),
            11 => CalculateD11(rasiIndex, degreeInRasi),
            12 => CalculateD12(rasiIndex, degreeInRasi),
            16 => CalculateD16(rasiIndex, degreeInRasi),
            20 => CalculateD20(rasiIndex, degreeInRasi),
            24 => CalculateD24(rasiIndex, degreeInRasi),
            27 => CalculateD27(rasiIndex, degreeInRasi),
            30 => CalculateD30(rasiIndex, degreeInRasi),
            40 => CalculateD40(rasiIndex, degreeInRasi),
            45 => CalculateD45(rasiIndex, degreeInRasi),
            60 => CalculateD60(rasiIndex, degreeInRasi),
            _ => rasiIndex, // Default to D1 (Rasi chart)
        };

        // Calculate degree within the divisional sign
        double partSize = 30.0 / division;
        int partNumber = (int)Math.Floor(degreeInRasi / partSize);
        double degreeWithinPart = degreeInRasi - (partNumber * partSize);
        double divisionalDegree = degreeWithinPart * division; // Scale back to 30 degrees

        // Convert from 0-indexed to 1-indexed sign
        int signIndex = (divisionalSignIndex % 12) + 1;

        return new DivisionalPosition
        {
            Sign = signIndex,
            SignName = SignNames[signIndex],
            DegreeInSign = divisionalDegree
        };
    }

    /// <summary>
    /// Calculate divisional chart for all planets and ascendant
    /// </summary>
    public DivisionalChartData CalculateDivisionalChart(ChartData chartData, int division)
    {
        var result = new DivisionalChartData
        {
            Division = division,
            Name = GetDivisionalChartName(division)
        };

        // Calculate divisional ascendant
        var ascPosition = CalculateDivisionalPosition(chartData.AscendantDegree, division);
        result.AscendantSign = ascPosition.Sign;
        result.AscendantSignName = ascPosition.SignName;
        result.AscendantDegree = ascPosition.DegreeInSign;

        // Calculate divisional positions for all planets
        foreach (var planet in chartData.Planets)
        {
            var divPosition = CalculateDivisionalPosition(planet.Longitude, division);
            result.Planets.Add(new DivisionalPlanetPosition
            {
                Planet = planet.Planet,
                Name = planet.Name,
                Symbol = planet.Symbol,
                NatalLongitude = planet.Longitude,
                NatalSign = planet.Sign,
                NatalSignName = planet.SignName,
                DivisionalSign = divPosition.Sign,
                DivisionalSignName = divPosition.SignName,
                DivisionalDegree = divPosition.DegreeInSign,
                IsRetrograde = planet.IsRetrograde
            });
        }

        return result;
    }

    /// <summary>
    /// Get divisional chart name
    /// </summary>
    public static string GetDivisionalChartName(int division)
    {
        return division switch
        {
            1 => "Rasi (D-1)",
            2 => "Hora (D-2)",
            3 => "Drekkana (D-3)",
            4 => "Chaturthamsa (D-4)",
            5 => "Panchamsa (D-5)",
            6 => "Shashthamsa (D-6)",
            7 => "Saptamsa (D-7)",
            8 => "Ashtamsa (D-8)",
            9 => "Navamsa (D-9)",
            10 => "Dasamsa (D-10)",
            11 => "Rudramsa (D-11)",
            12 => "Dwadasamsa (D-12)",
            16 => "Shodasamsa (D-16)",
            20 => "Vimsamsa (D-20)",
            24 => "Siddhamsa (D-24)",
            27 => "Nakshatramsa (D-27)",
            30 => "Trimsamsa (D-30)",
            40 => "Khavedamsa (D-40)",
            45 => "Akshavedamsa (D-45)",
            60 => "Shashtiamsa (D-60)",
            _ => $"D-{division}",
        };
    }

    #region Divisional Chart Calculations

    /// <summary>
    /// D-1: Rasi Chart (Birth Chart) - Simply returns the natal rasi
    /// </summary>
    private int CalculateD1(int rasiIndex, double degreeInRasi) => rasiIndex;

    /// <summary>
    /// D-2: Hora Chart
    /// Odd signs: 0-15° = Sun (Leo), 15-30° = Moon (Cancer)
    /// Even signs: 0-15° = Moon (Cancer), 15-30° = Sun (Leo)
    /// </summary>
    private int CalculateD2(int rasiIndex, double degreeInRasi)
    {
        bool isOddSign = (rasiIndex % 2 == 0); // 0,2,4,6,8,10 are odd signs (Aries, Gemini, etc.)
        bool isFirstHalf = degreeInRasi < 15;

        if (isOddSign)
            return isFirstHalf ? 4 : 3; // Sun (Leo=4) or Moon (Cancer=3)
        else
            return isFirstHalf ? 3 : 4; // Moon (Cancer=3) or Sun (Leo=4)
    }

    /// <summary>
    /// D-3: Drekkana Chart
    /// Each sign divided into 3 parts of 10° each
    /// 1st part: Same sign, 2nd part: 5th from sign, 3rd part: 9th from sign
    /// </summary>
    private int CalculateD3(int rasiIndex, double degreeInRasi)
    {
        int part = (int)Math.Floor(degreeInRasi / 10); // 0, 1, or 2
        
        return part switch
        {
            0 => rasiIndex,
            1 => (rasiIndex + 4) % 12,
            2 => (rasiIndex + 8) % 12,
            _ => rasiIndex,
        };
    }

    /// <summary>
    /// D-4: Chaturthamsa Chart
    /// Each sign divided into 4 parts of 7.5° each
    /// Goes into 1st, 4th, 7th, 10th from the sign
    /// </summary>
    private int CalculateD4(int rasiIndex, double degreeInRasi)
    {
        int part = (int)Math.Floor(degreeInRasi / 7.5); // 0, 1, 2, or 3
        
        return part switch
        {
            0 => rasiIndex,
            1 => (rasiIndex + 3) % 12,
            2 => (rasiIndex + 6) % 12,
            3 => (rasiIndex + 9) % 12,
            _ => rasiIndex,
        };
    }

    /// <summary>
    /// D-5: Panchamsa Chart
    /// Each sign divided into 5 parts of 6° each
    /// Odd signs: Ar, Aq, Sg, Ge, Li (0, 10, 8, 2, 6)
    /// Even signs: Ta, Vi, Pi, Cp, Sc (1, 5, 11, 9, 7)
    /// </summary>
    private int CalculateD5(int rasiIndex, double degreeInRasi)
    {
        int part = (int)Math.Floor(degreeInRasi / 6); // 0-4
        bool isOddSign = (rasiIndex % 2 == 0);

        int[] oddSequence = { 0, 10, 8, 2, 6 };  // Ar, Aq, Sg, Ge, Li
        int[] evenSequence = { 1, 5, 11, 9, 7 }; // Ta, Vi, Pi, Cp, Sc

        return isOddSign ? oddSequence[part] : evenSequence[part];
    }

    /// <summary>
    /// D-6: Shashthamsa Chart
    /// Each sign divided into 6 parts of 5° each
    /// Odd signs: Start from Aries (0)
    /// Even signs: Start from Libra (6)
    /// </summary>
    private int CalculateD6(int rasiIndex, double degreeInRasi)
    {
        int part = (int)Math.Floor(degreeInRasi / 5); // 0-5
        bool isOddSign = (rasiIndex % 2 == 0);

        if (isOddSign)
            return part; // Start from Aries
        else
            return (part + 6) % 12; // Start from Libra
    }

    /// <summary>
    /// D-7: Saptamsa Chart
    /// Each sign divided into 7 parts of ~4.2857° each
    /// Odd signs: Start from the sign itself
    /// Even signs: Start from 7th from the sign
    /// </summary>
    private int CalculateD7(int rasiIndex, double degreeInRasi)
    {
        double partSize = 30.0 / 7; // ≈4.2857°
        int part = (int)Math.Floor(degreeInRasi / partSize); // 0-6
        bool isOddSign = (rasiIndex % 2 == 0);

        if (isOddSign)
            return (rasiIndex + part) % 12; // Start from sign itself
        else
            return (rasiIndex + 6 + part) % 12; // Start from 7th sign
    }

    /// <summary>
    /// D-8: Ashtamsa Chart
    /// Each sign divided into 8 parts of 3.75° each
    /// Movable signs: Start from Aries (0)
    /// Fixed signs: Start from Sagittarius (8)
    /// Dual signs: Start from Leo (4)
    /// </summary>
    private int CalculateD8(int rasiIndex, double degreeInRasi)
    {
        int part = (int)Math.Floor(degreeInRasi / 3.75); // 0-7
        
        int[] movableSigns = { 0, 3, 6, 9 };  // Ar, Cn, Li, Cp
        int[] fixedSigns = { 1, 4, 7, 10 };   // Ta, Le, Sc, Aq

        if (movableSigns.Contains(rasiIndex))
            return part; // Start from Aries
        else if (fixedSigns.Contains(rasiIndex))
            return (part + 8) % 12; // Start from Sagittarius
        else // Dual signs
            return (part + 4) % 12; // Start from Leo
    }

    /// <summary>
    /// D-9: Navamsa Chart (Dharmamsa)
    /// Each sign divided into 9 parts of ~3.3333° each
    /// Fire signs: Start from Aries, Earth: Capricorn, Air: Libra, Water: Cancer
    /// </summary>
    private int CalculateD9(int rasiIndex, double degreeInRasi)
    {
        double partSize = 30.0 / 9; // ≈3.3333°
        int part = (int)Math.Floor(degreeInRasi / partSize); // 0-8
        
        int[] fireSigns = { 0, 4, 8 };   // Ar, Le, Sg
        int[] earthSigns = { 1, 5, 9 };  // Ta, Vi, Cp
        int[] airSigns = { 2, 6, 10 };   // Ge, Li, Aq

        if (fireSigns.Contains(rasiIndex))
            return part; // Start from Aries
        else if (earthSigns.Contains(rasiIndex))
            return (part + 9) % 12; // Start from Capricorn
        else if (airSigns.Contains(rasiIndex))
            return (part + 6) % 12; // Start from Libra
        else // Water signs
            return (part + 3) % 12; // Start from Cancer
    }

    /// <summary>
    /// D-10: Dasamsa Chart
    /// Each sign divided into 10 parts of 3° each
    /// Odd signs: Start from the sign itself
    /// Even signs: Start from 9th from the sign
    /// </summary>
    private int CalculateD10(int rasiIndex, double degreeInRasi)
    {
        int part = (int)Math.Floor(degreeInRasi / 3); // 0-9
        bool isOddSign = (rasiIndex % 2 == 0);

        if (isOddSign)
            return (rasiIndex + part) % 12;
        else
            return (rasiIndex + 8 + part) % 12;
    }

    /// <summary>
    /// D-11: Rudramsa Chart (Ekadasamsa)
    /// Each sign divided into 11 parts
    /// Count anti-zodiacally from Aries based on sign number
    /// </summary>
    private int CalculateD11(int rasiIndex, double degreeInRasi)
    {
        double partSize = 30.0 / 11; // ≈2.7273°
        int part = (int)Math.Floor(degreeInRasi / partSize); // 0-10
        
        // Count sign position from Aries (1-12)
        int signNumber = rasiIndex + 1;
        
        // Count anti-zodiacally from Aries
        int antiZodiacalIndex = (12 - signNumber + 1) % 12;
        
        return (antiZodiacalIndex + part) % 12;
    }

    /// <summary>
    /// D-12: Dwadasamsa Chart
    /// Each sign divided into 12 parts of 2.5° each
    /// Start from the sign itself
    /// </summary>
    private int CalculateD12(int rasiIndex, double degreeInRasi)
    {
        int part = (int)Math.Floor(degreeInRasi / 2.5); // 0-11
        return (rasiIndex + part) % 12;
    }

    /// <summary>
    /// D-16: Shodasamsa Chart
    /// Movable: Aries, Fixed: Leo, Dual: Sagittarius
    /// </summary>
    private int CalculateD16(int rasiIndex, double degreeInRasi)
    {
        double partSize = 30.0 / 16;
        int part = (int)Math.Floor(degreeInRasi / partSize);
        
        int[] movableSigns = { 0, 3, 6, 9 };
        int[] fixedSigns = { 1, 4, 7, 10 };

        if (movableSigns.Contains(rasiIndex))
            return part % 12;
        else if (fixedSigns.Contains(rasiIndex))
            return (part + 4) % 12;
        else
            return (part + 8) % 12;
    }

    /// <summary>
    /// D-20: Vimsamsa Chart
    /// Movable: Aries, Fixed: Sagittarius, Dual: Leo
    /// </summary>
    private int CalculateD20(int rasiIndex, double degreeInRasi)
    {
        int part = (int)Math.Floor(degreeInRasi / 1.5);
        
        int[] movableSigns = { 0, 3, 6, 9 };
        int[] fixedSigns = { 1, 4, 7, 10 };

        if (movableSigns.Contains(rasiIndex))
            return part % 12;
        else if (fixedSigns.Contains(rasiIndex))
            return (part + 8) % 12;
        else
            return (part + 4) % 12;
    }

    /// <summary>
    /// D-24: Siddhamsa Chart
    /// Odd signs: Start from Leo
    /// Even signs: Start from Cancer
    /// </summary>
    private int CalculateD24(int rasiIndex, double degreeInRasi)
    {
        int part = (int)Math.Floor(degreeInRasi / 1.25);
        bool isOddSign = (rasiIndex % 2 == 0);

        if (isOddSign)
            return (part + 4) % 12; // Start from Leo
        else
            return (part + 3) % 12; // Start from Cancer
    }

    /// <summary>
    /// D-27: Nakshatramsa/Bhamsa Chart
    /// Fire: Aries, Earth: Cancer, Air: Libra, Water: Capricorn
    /// </summary>
    private int CalculateD27(int rasiIndex, double degreeInRasi)
    {
        double partSize = 30.0 / 27; // ≈1.1111°
        int part = (int)Math.Floor(degreeInRasi / partSize); // 0-26
        
        int[] fireSigns = { 0, 4, 8 };
        int[] earthSigns = { 1, 5, 9 };
        int[] airSigns = { 2, 6, 10 };

        if (fireSigns.Contains(rasiIndex))
            return part % 12; // Start from Aries
        else if (earthSigns.Contains(rasiIndex))
            return (part + 3) % 12; // Start from Cancer
        else if (airSigns.Contains(rasiIndex))
            return (part + 6) % 12; // Start from Libra
        else // Water signs
            return (part + 9) % 12; // Start from Capricorn
    }

    /// <summary>
    /// D-30: Trimsamsa Chart
    /// Complex formula with specific degree ranges
    /// </summary>
    private int CalculateD30(int rasiIndex, double degreeInRasi)
    {
        bool isOddSign = (rasiIndex % 2 == 0);

        if (isOddSign)
        {
            // Odd signs: Mars(5°), Saturn(5°), Jupiter(8°), Mercury(7°), Venus(5°)
            if (degreeInRasi < 5) return 0;  // Mars (Aries)
            if (degreeInRasi < 10) return 6; // Saturn (Aquarius - index 10, but using Libra=6 per formula)
            if (degreeInRasi < 18) return 8; // Jupiter (Sagittarius)
            if (degreeInRasi < 25) return 2; // Mercury (Gemini)
            return 4; // Venus (Leo)
        }
        else
        {
            // Even signs: Venus(5°), Mercury(7°), Jupiter(8°), Saturn(5°), Mars(5°)
            if (degreeInRasi < 5) return 4;  // Venus (Leo)
            if (degreeInRasi < 12) return 2; // Mercury (Gemini)
            if (degreeInRasi < 20) return 8; // Jupiter (Sagittarius)
            if (degreeInRasi < 25) return 10; // Saturn (Aquarius)
            return 0; // Mars (Aries)
        }
    }

    /// <summary>
    /// D-40: Khavedamsa Chart
    /// Odd signs: Start from Aries
    /// Even signs: Start from Libra
    /// </summary>
    private int CalculateD40(int rasiIndex, double degreeInRasi)
    {
        double partSize = 30.0 / 40; // 0.75°
        int part = (int)Math.Floor(degreeInRasi / partSize); // 0-39
        bool isOddSign = (rasiIndex % 2 == 0);

        if (isOddSign)
            return part % 12; // Start from Aries
        else
            return (part + 6) % 12; // Start from Libra
    }

    /// <summary>
    /// D-45: Akshavedamsa Chart
    /// Movable: Aries, Fixed: Leo, Dual: Sagittarius
    /// </summary>
    private int CalculateD45(int rasiIndex, double degreeInRasi)
    {
        double partSize = 30.0 / 45;
        int part = (int)Math.Floor(degreeInRasi / partSize);
        
        int[] movableSigns = { 0, 3, 6, 9 };
        int[] fixedSigns = { 1, 4, 7, 10 };

        if (movableSigns.Contains(rasiIndex))
            return part % 12;
        else if (fixedSigns.Contains(rasiIndex))
            return (part + 4) % 12;
        else
            return (part + 8) % 12;
    }

    /// <summary>
    /// D-60: Shashtiamsa Chart
    /// Each sign divided into 60 parts of 0.5° each
    /// Start counting from the sign itself
    /// </summary>
    private int CalculateD60(int rasiIndex, double degreeInRasi)
    {
        double partSize = 0.5; // 30 minutes = 0.5 degrees
        int part = (int)Math.Floor(degreeInRasi / partSize); // 0-59
        
        return (rasiIndex + part) % 12;
    }

    #endregion
}

/// <summary>
/// Basic divisional position result
/// </summary>
public class DivisionalPosition
{
    public int Sign { get; set; }           // 1-12 (Aries to Pisces)
    public string SignName { get; set; } = string.Empty;
    public double DegreeInSign { get; set; }
}

/// <summary>
/// Complete divisional chart data
/// </summary>
public class DivisionalChartData
{
    public int Division { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AscendantSign { get; set; }
    public string AscendantSignName { get; set; } = string.Empty;
    public double AscendantDegree { get; set; }
    public List<DivisionalPlanetPosition> Planets { get; set; } = new();
    
    /// <summary>
    /// Gets planets in a specific sign (1-12)
    /// </summary>
    public IEnumerable<DivisionalPlanetPosition> GetPlanetsInSign(int sign)
    {
        return Planets.Where(p => p.DivisionalSign == sign);
    }
}

/// <summary>
/// Planet position in a divisional chart
/// </summary>
public class DivisionalPlanetPosition
{
    public Planet Planet { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public double NatalLongitude { get; set; }
    public int NatalSign { get; set; }
    public string NatalSignName { get; set; } = string.Empty;
    public int DivisionalSign { get; set; }
    public string DivisionalSignName { get; set; } = string.Empty;
    public double DivisionalDegree { get; set; }
    public bool IsRetrograde { get; set; }
}
