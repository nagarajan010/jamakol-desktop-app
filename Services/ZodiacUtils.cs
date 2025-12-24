namespace JamakolAstrology.Services;

/// <summary>
/// Utility class for zodiac signs, nakshatras, and planet symbols
/// </summary>
public static class ZodiacUtils
{
    public static readonly string[] SignNames = 
    {
        "", "Aries", "Taurus", "Gemini", "Cancer", "Leo", "Virgo",
        "Libra", "Scorpio", "Sagittarius", "Capricorn", "Aquarius", "Pisces"
    };

    public static readonly string[] SignNamesTamil = 
    {
        "", "மேஷம்", "ரிஷபம்", "மிதுனம்", "கடகம்", "சிம்மம்", "கன்னி",
        "துலாம்", "விருச்சிகம்", "தனுசு", "மகரம்", "கும்பம்", "மீனம்"
    };

    public static readonly string[] SignSymbols = 
    {
        "", "♈", "♉", "♊", "♋", "♌", "♍", "♎", "♏", "♐", "♑", "♒", "♓"
    };

    public static readonly string[] NakshatraNames = 
    {
        "", "Ashwini", "Bharani", "Krittika", "Rohini", "Mrigashira", "Ardra",
        "Punarvasu", "Pushya", "Ashlesha", "Magha", "Purva Phalguni", "Uttara Phalguni",
        "Hasta", "Chitra", "Swati", "Vishakha", "Anuradha", "Jyeshtha",
        "Mula", "Purva Ashadha", "Uttara Ashadha", "Shravana", "Dhanishta", 
        "Shatabhisha", "Purva Bhadrapada", "Uttara Bhadrapada", "Revati"
    };

    public static readonly string[] NakshatraNamesTamil = 
    {
        "", "அஸ்வினி", "பரணி", "கார்த்திகை", "ரோகிணி", "மிருகசீரிஷம்", "திருவாதிரை",
        "புனர்பூசம்", "பூசம்", "ஆயில்யம்", "மகம்", "பூரம்", "உத்திரம்",
        "ஹஸ்தம்", "சித்திரை", "சுவாதி", "விசாகம்", "அனுஷம்", "கேட்டை",
        "மூலம்", "பூராடம்", "உத்திராடம்", "திருவோணம்", "அவிட்டம்",
        "சதயம்", "பூரட்டாதி", "உத்திரட்டாதி", "ரேவதி"
    };

    public static readonly Dictionary<Models.Planet, string> PlanetNames = new()
    {
        { Models.Planet.Sun, "Sun" },
        { Models.Planet.Moon, "Moon" },
        { Models.Planet.Mars, "Mars" },
        { Models.Planet.Mercury, "Mercury" },
        { Models.Planet.Jupiter, "Jupiter" },
        { Models.Planet.Venus, "Venus" },
        { Models.Planet.Saturn, "Saturn" },
        { Models.Planet.Rahu, "Rahu" },
        { Models.Planet.Ketu, "Ketu" }
    };

    public static readonly Dictionary<Models.Planet, string> PlanetNamesTamil = new()
    {
        { Models.Planet.Sun, "சூரியன்" },
        { Models.Planet.Moon, "சந்திரன்" },
        { Models.Planet.Mars, "செவ்வாய்" },
        { Models.Planet.Mercury, "புதன்" },
        { Models.Planet.Jupiter, "குரு" },
        { Models.Planet.Venus, "சுக்கிரன்" },
        { Models.Planet.Saturn, "சனி" },
        { Models.Planet.Rahu, "ராகு" },
        { Models.Planet.Ketu, "கேது" }
    };

    public static readonly Dictionary<Models.Planet, string> PlanetSymbols = new()
    {
        { Models.Planet.Sun, "☉" },
        { Models.Planet.Moon, "☽" },
        { Models.Planet.Mars, "♂" },
        { Models.Planet.Mercury, "☿" },
        { Models.Planet.Jupiter, "♃" },
        { Models.Planet.Venus, "♀" },
        { Models.Planet.Saturn, "♄" },
        { Models.Planet.Rahu, "☊" },
        { Models.Planet.Ketu, "☋" }
    };

    public static readonly Dictionary<Models.Planet, string> PlanetAbbreviations = new()
    {
        { Models.Planet.Sun, "Su" },
        { Models.Planet.Moon, "Mo" },
        { Models.Planet.Mars, "Ma" },
        { Models.Planet.Mercury, "Me" },
        { Models.Planet.Jupiter, "Ju" },
        { Models.Planet.Venus, "Ve" },
        { Models.Planet.Saturn, "Sa" },
        { Models.Planet.Rahu, "Ra" },
        { Models.Planet.Ketu, "Ke" }
    };

    /// <summary>
    /// Convert degree (0-360) to sign number (1-12)
    /// </summary>
    public static int DegreeToSign(double degree)
    {
        degree = NormalizeDegree(degree);
        return (int)(degree / 30) + 1;
    }

    /// <summary>
    /// Get degree within sign (0-30)
    /// </summary>
    public static double DegreeInSign(double degree)
    {
        degree = NormalizeDegree(degree);
        return degree % 30;
    }

    /// <summary>
    /// Convert degree to nakshatra (1-27)
    /// </summary>
    public static int DegreeToNakshatra(double degree)
    {
        degree = NormalizeDegree(degree);
        return (int)(degree / (360.0 / 27.0)) + 1;
    }

    /// <summary>
    /// Get nakshatra pada (1-4)
    /// </summary>
    public static int GetNakshatraPada(double degree)
    {
        degree = NormalizeDegree(degree);
        double nakshatraSpan = 360.0 / 27.0; // 13.333... degrees
        double padaSpan = nakshatraSpan / 4.0; // 3.333... degrees
        double degreeInNakshatra = degree % nakshatraSpan;
        return (int)(degreeInNakshatra / padaSpan) + 1;
    }

    /// <summary>
    /// Calculate house number based on planet position and ascendant
    /// In South Indian chart, houses are counted clockwise from ascendant sign
    /// </summary>
    public static int CalculateHouse(int planetSign, int ascendantSign)
    {
        int house = planetSign - ascendantSign + 1;
        if (house <= 0) house += 12;
        return house;
    }

    /// <summary>
    /// Normalize degree to 0-360 range
    /// </summary>
    public static double NormalizeDegree(double degree)
    {
        while (degree < 0) degree += 360;
        while (degree >= 360) degree -= 360;
        return degree;
    }

    /// <summary>
    /// Format degree as degrees:minutes:seconds
    /// </summary>
    public static string FormatDegree(double degree)
    {
        degree = NormalizeDegree(degree);
        int deg = (int)degree;
        double minVal = (degree - deg) * 60;
        int min = (int)minVal;
        int sec = (int)((minVal - min) * 60);
        return $"{deg}°{min:D2}'{sec:D2}\"";
    }

    /// <summary>
    /// Format degree within sign
    /// </summary>
    public static string FormatDegreeInSign(double degree)
    {
        double degInSign = DegreeInSign(degree);
        return FormatDegree(degInSign);
    }
}
