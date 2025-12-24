namespace JamakolAstrology.Services;

/// <summary>
/// Jama Graha Calculator - calculates 8 secondary planets for the Jamakol system
/// Based on day lord and time period
/// </summary>
public class JamaGrahaCalculator
{
    // The 8 Jama Graha planets in order
    private static readonly string[] JamaGrahaNames = { "Sun", "Moon", "Mars", "Mercury", "Jupiter", "Venus", "Saturn", "Snake" };
    
    private static readonly string[] JamaGrahaSymbols = { "Su", "Mo", "Ma", "Me", "Ju", "Ve", "Sa", "Sn" };

    // Sign name to number mapping (only the 8 signs used in Jama Graha)
    private static readonly Dictionary<string, int> SignToNumber = new()
    {
        { "Aries", 1 }, { "Gemini", 3 }, { "Cancer", 4 }, { "Virgo", 6 },
        { "Libra", 7 }, { "Sagittarius", 9 }, { "Capricorn", 10 }, { "Pisces", 12 }
    };

    // The signs in the order they appear in the table
    private static readonly string[] TableSigns = { "Pisces", "Capricorn", "Sagittarius", "Libra", "Virgo", "Cancer", "Gemini", "Aries" };

    // Jama Graha lookup table - [DayLord][Period (0-7)][SignIndex] -> Planet name
    // Period times: J1=6:00-7:30, J2=7:30-9:00, J3=9:00-10:30, J4=10:30-12:00,
    //               J5=12:00-1:30, J6=1:30-3:00, J7=3:00-4:30, J8=4:30-6:00
    private static readonly Dictionary<string, string[,]> JamaGrahaTable = new()
    {
        { "Sun", new string[,] {
            { "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake" },
            { "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon" },
            { "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn" },
            { "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus" },
            { "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury" },
            { "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter" },
            { "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars" },
            { "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun" }
        }},
        { "Moon", new string[,] {
            { "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn" },
            { "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus" },
            { "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury" },
            { "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter" },
            { "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars" },
            { "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun" },
            { "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake" },
            { "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon" }
        }},
        { "Mars", new string[,] {
            { "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun" },
            { "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake" },
            { "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon" },
            { "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn" },
            { "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus" },
            { "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury" },
            { "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter" },
            { "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars" }
        }},
        { "Mercury", new string[,] {
            { "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter" },
            { "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars" },
            { "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun" },
            { "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake" },
            { "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon" },
            { "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn" },
            { "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus" },
            { "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury" }
        }},
        { "Jupiter", new string[,] {
            { "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars" },
            { "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun" },
            { "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake" },
            { "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon" },
            { "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn" },
            { "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus" },
            { "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury" },
            { "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter" }
        }},
        { "Venus", new string[,] {
            { "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury" },
            { "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter" },
            { "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars" },
            { "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun" },
            { "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake" },
            { "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon" },
            { "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn" },
            { "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus" }
        }},
        { "Saturn", new string[,] {
            { "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus" },
            { "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury" },
            { "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter" },
            { "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars" },
            { "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun" },
            { "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake" },
            { "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon" },
            { "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn" }
        }}
    };

    /// <summary>
    /// Get the day lord based on day of week
    /// </summary>
    public static string GetDayLord(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Sunday => "Sun",
            DayOfWeek.Monday => "Moon",
            DayOfWeek.Tuesday => "Mars",
            DayOfWeek.Wednesday => "Mercury",
            DayOfWeek.Thursday => "Jupiter",
            DayOfWeek.Friday => "Venus",
            DayOfWeek.Saturday => "Saturn",
            _ => "Sun"
        };
    }

    /// <summary>
    /// Calculate Jama Grahas based on birth time and day lord
    /// </summary>
    public List<JamaGrahaPosition> Calculate(DateTime birthDateTime, string? dayLord = null)
    {
        // Get day lord if not specified
        dayLord ??= GetDayLord(birthDateTime.DayOfWeek);

        // Calculate time in minutes from midnight
        double timeInMinutes = birthDateTime.Hour * 60 + birthDateTime.Minute + birthDateTime.Second / 60.0;

        // Define Jamam periods (each 90 minutes starting from 6 AM)
        // J1=6:00-7:30, J2=7:30-9:00, J3=9:00-10:30, J4=10:30-12:00
        // J5=12:00-1:30, J6=1:30-3:00, J7=3:00-4:30, J8=4:30-6:00
        var jamamPeriods = new[]
        {
            (start: 360.0, end: 450.0),   // J1: 6:00-7:30 AM (index 0)
            (start: 450.0, end: 540.0),   // J2: 7:30-9:00 AM (index 1)
            (start: 540.0, end: 630.0),   // J3: 9:00-10:30 AM (index 2)
            (start: 630.0, end: 720.0),   // J4: 10:30-12:00 PM (index 3)
            (start: 720.0, end: 810.0),   // J5: 12:00-1:30 PM (index 4)
            (start: 810.0, end: 900.0),   // J6: 1:30-3:00 PM (index 5)
            (start: 900.0, end: 990.0),   // J7: 3:00-4:30 PM (index 6)
            (start: 990.0, end: 1080.0),  // J8: 4:30-6:00 PM (index 7)
        };

        // Wrap time to 12-hour cycle (6 AM to 6 PM = 360 to 1080 minutes)
        double wrappedTime = timeInMinutes;
        if (wrappedTime < 360)
        {
            // Before 6 AM: add 12 hours
            wrappedTime += 720;
        }
        else if (wrappedTime >= 1080)
        {
            // After 6 PM: subtract 12 hours
            wrappedTime -= 720;
        }

        // Find current Jamam period
        int currentPeriod = 0;
        double baseDegree = 360.0;

        for (int i = 0; i < jamamPeriods.Length; i++)
        {
            if (wrappedTime >= jamamPeriods[i].start && wrappedTime < jamamPeriods[i].end)
            {
                currentPeriod = i;
                double duration = jamamPeriods[i].end - jamamPeriods[i].start; // 90 minutes
                double timePassed = wrappedTime - jamamPeriods[i].start;
                // Degree travels from 360° to 315° (45 degrees in 90 minutes)
                baseDegree = 360 - ((timePassed / duration) * 45);
                break;
            }
        }

        // Get the table for this day lord
        if (!JamaGrahaTable.TryGetValue(dayLord, out var table))
        {
            table = JamaGrahaTable["Sun"];
        }

        // Build result
        var result = new List<JamaGrahaPosition>();

        for (int signIndex = 0; signIndex < 8; signIndex++)
        {
            string signName = TableSigns[signIndex];
            string planetName = table[currentPeriod, signIndex];
            int houseNumber = SignToNumber[signName];

            // Each planet is 45 degrees behind the previous one
            double absoluteDegree = baseDegree - (signIndex * 45);
            if (absoluteDegree < 0)
            {
                absoluteDegree += 360;
            }

            // Calculate degree within sign (0-30°)
            double degreeInSign = absoluteDegree % 30;
            if (degreeInSign == 0)
            {
                degreeInSign = 30.0;
            }

            // Calculate actual sign from absolute degree
            int actualSign = (int)(absoluteDegree / 30) + 1;
            if (actualSign > 12) actualSign = 12;
            if (actualSign < 1) actualSign = 1;

            // Calculate nakshatra
            double nakshatraSize = 360.0 / 27.0; // 13.333... degrees
            int nakshatraIndex = (int)(absoluteDegree / nakshatraSize);
            // Clamp to valid range (0-26)
            if (nakshatraIndex >= 27) nakshatraIndex = 26;
            if (nakshatraIndex < 0) nakshatraIndex = 0;
            
            double offsetInNak = absoluteDegree - (nakshatraIndex * nakshatraSize);
            int pada = (int)(offsetInNak / (nakshatraSize / 4)) + 1;
            if (pada > 4) pada = 4;
            if (pada < 1) pada = 1;

            string symbol = GetPlanetSymbol(planetName);
            
            // Get nakshatra name safely
            int nakIndex = nakshatraIndex + 1;
            if (nakIndex > 27) nakIndex = 27;
            if (nakIndex < 1) nakIndex = 1;
            string nakName = nakIndex <= 27 ? ZodiacUtils.NakshatraNames[nakIndex] : "Revati";

            result.Add(new JamaGrahaPosition
            {
                Name = planetName,
                Symbol = symbol,
                House = houseNumber,
                Sign = actualSign,
                SignName = ZodiacUtils.SignNames[actualSign],
                Degree = absoluteDegree,
                DegreeInSign = degreeInSign,
                Nakshatra = nakIndex,
                NakshatraName = nakName,
                Pada = pada
            });
        }

        return result;
    }

    private string GetPlanetSymbol(string planetName)
    {
        return planetName switch
        {
            "Sun" => "Su",
            "Moon" => "Mo",
            "Mars" => "Ma",
            "Mercury" => "Me",
            "Jupiter" => "Ju",
            "Venus" => "Ve",
            "Saturn" => "Sa",
            "Snake" => "Sn",
            _ => planetName.Substring(0, 2)
        };
    }
}

/// <summary>
/// Jama Graha position data
/// </summary>
public class JamaGrahaPosition
{
    public string Name { get; set; } = "";
    public string Symbol { get; set; } = "";
    public int House { get; set; }
    public int Sign { get; set; }
    public string SignName { get; set; } = "";
    public double Degree { get; set; }
    public double DegreeInSign { get; set; }
    public int Nakshatra { get; set; }
    public string NakshatraName { get; set; } = "";
    public int Pada { get; set; }

    // Formatted display properties
    public string DegreeDisplay => $"{(int)DegreeInSign}°{(int)((DegreeInSign % 1) * 60)}'{(int)(((DegreeInSign % 1) * 60 % 1) * 60)}\"";
    public string TimeFormat => $"{(int)DegreeInSign}:{(int)((DegreeInSign % 1) * 60):D2}";
}
