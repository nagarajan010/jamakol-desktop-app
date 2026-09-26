using System;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Naming tables for the five Panchanga limbs, shared by the Calendar.
/// Builds a <see cref="PanchangaSegment"/> from a limb index plus its start/end
/// instants, so the sweep in <see cref="CalendarService"/> stays purely
/// astronomical and carries no naming logic.
/// </summary>
public static class PanchangaNames
{
    public static readonly string[] TithiNames =
    {
        "Pratipada", "Dwitiya", "Tritiya", "Chaturthi", "Panchami",
        "Shashthi", "Saptami", "Ashtami", "Navami", "Dashami",
        "Ekadashi", "Dwadashi", "Trayodashi", "Chaturdashi", "Purnima"
    };

    public static readonly string[] TithiNamesTamil =
    {
        "பிரதமை", "துவிதியை", "திரிதியை", "சதுர்த்தி", "பஞ்சமி",
        "சஷ்டி", "சப்தமி", "அஷ்டமி", "நவமி", "தசமி",
        "ஏகாதசி", "துவாதசி", "திரயோதசி", "சதுர்தசி", "பௌர்ணமி"
    };

    public static readonly string[] YogaNames =
    {
        "Vishkumbha", "Priti", "Ayushman", "Saubhagya", "Shobhana",
        "Atiganda", "Sukarma", "Dhriti", "Shoola", "Ganda",
        "Vriddhi", "Dhruva", "Vyaghata", "Harshana", "Vajra",
        "Siddhi", "Vyatipata", "Variyan", "Parigha", "Shiva",
        "Siddha", "Sadhya", "Shubha", "Shukla", "Brahma",
        "Indra", "Vaidhriti"
    };

    public static readonly string[] YogaNamesTamil =
    {
        "விஷ்கும்பம்", "ப்ரீதி", "ஆயுஷ்மான்", "சௌபாக்கியம்", "சோபனம்",
        "அதிகண்டம்", "சுகர்மா", "த்ருதி", "சூலம்", "கண்டம்",
        "விருத்தி", "த்ருவம்", "வியாகாதம்", "ஹர்ஷணம்", "வஜ்ரம்",
        "சித்தி", "வியதீபாதம்", "வரீயான்", "பரிகம்", "சிவம்",
        "சித்தம்", "சாத்தியம்", "சுபம்", "சுக்லம்", "பிரம்மம்",
        "இந்திரம்", "வைத்ருதி"
    };

    public static readonly string[] KaranaNames =
    {
        "Bava", "Balava", "Kaulava", "Taitila", "Gara",
        "Vanija", "Vishti", "Shakuni", "Chatushpada", "Naga", "Kimstughna"
    };

    public static readonly string[] KaranaNamesTamil =
    {
        "பவம்", "பாலவம்", "கௌலவம்", "தைதுலம்", "கரம்",
        "வணிஜம்", "விஷ்டி", "சகுனி", "சதுஷ்பாதம்", "நாகம்", "கிம்ஸ்துக்னம்"
    };

    public static readonly string[] NakshatraNamesTamil =
    {
        "", "அசுவினி", "பரணி", "கிருத்திகை", "ரோகிணி", "மிருகசீரிஷம்",
        "திருவாதிரை", "புனர்பூசம்", "பூசம்", "ஆயில்யம்", "மகம்",
        "பூரம்", "உத்திரம்", "ஹஸ்தம்", "சித்திரை", "சுவாதி",
        "விசாகம்", "அனுஷம்", "கேட்டை", "மூலம்", "பூராடம்",
        "உத்திராடம்", "திருவோணம்", "அவிட்டம்", "சதயம்", "பூரட்டாதி",
        "உத்திரட்டாதி", "ரேவதி"
    };

    private static readonly string[] TamilYears =
    {
        "பிரபவ", "விபவ", "சுக்ல", "பிரமோதூத", "பிரசோற்பத்தி",
        "ஆங்கீரச", "ஸ்ரீமுக", "பவ", "யுவ", "தாது",
        "ஈஸ்வர", "வெகுதான்ய", "பிரமாதி", "விக்கிரம", "விஷு",
        "சித்திரபானு", "சுபானு", "தாரண", "பார்த்திப", "விய",
        "சர்வஜித்து", "சர்வதாரி", "விரோதி", "விக்ருதி", "கர",
        "நந்தன", "விஜய", "ஜய", "மன்மத", "துர்முகி",
        "ஹேவிளம்பி", "விளம்பி", "விகாரி", "சார்வரி", "பிலவ",
        "சுபகிருது", "சோபகிருது", "குரோதி", "விசுவாவசு", "பராபவ",
        "பிலவங்க", "கீலக", "சௌமிய", "சாதாரண", "விரோதகிருது",
        "பரிதாபி", "பிரமாதீச", "ஆனந்த", "ராட்சச", "நள",
        "பிங்கள", "காளயுக்தி", "சித்தார்த்தி", "ரௌத்திரி", "துன்மதி",
        "துந்துபி", "ருத்ரோத்காரி", "ரக்தாட்சி", "குரோதன", "அட்சய"
    };

    private static readonly string[] EnglishYears =
    {
        "Prabhava", "Vibhava", "Shukla", "Pramodoota", "Prajotpatti",
        "Angirasa", "Srimukha", "Bhava", "Yuva", "Dhatu",
        "Eswara", "Bahudhanya", "Pramathi", "Vikrama", "Vishu",
        "Chitrabhanu", "Subhanu", "Tarana", "Parthiva", "Vyaya",
        "Sarvajit", "Sarvadhari", "Virodhi", "Vikruti", "Khara",
        "Nandana", "Vijaya", "Jaya", "Manmatha", "Durmukhi",
        "Hevilambi", "Vilambi", "Vikari", "Sharvari", "Plava",
        "Shubhakrut", "Shobhakrut", "Krodhi", "Vishvavasu", "Parabhava",
        "Plavanga", "Keelaka", "Saumya", "Sadharana", "Virodhikrut",
        "Paridhaavi", "Pramadicha", "Ananda", "Rakshasa", "Nala",
        "Pingala", "Kalayukti", "Siddharthi", "Raudri", "Durmathi",
        "Dundubhi", "Rudhirodgari", "Raktakshi", "Krodhana", "Akshaya"
    };

    private static readonly string[] TamilMonths =
    {
        "", "சித்திரை", "வைகாசி", "ஆனி", "ஆடி", "ஆவணி", "புரட்டாசி",
        "ஐப்பசி", "கார்த்திகை", "மார்கழி", "தை", "மாசி", "பங்குனி"
    };

    private static readonly string[] EnglishMonths =
    {
        "", "Chithirai", "Vaikasi", "Aani", "Aadi", "Aavani", "Purattasi",
        "Aippasi", "Karthigai", "Margazhi", "Thai", "Maasi", "Panguni"
    };

    /// <summary>
    /// Build a Tithi segment from its 0-based index (0-29).
    /// </summary>
    public static PanchangaSegment BuildTithi(int index, DateTime? start, DateTime? end)
    {
        var segment = new PanchangaSegment { Start = start, End = end };

        int tithiNumber = index + 1; // 1-30

        if (tithiNumber <= 15)
        {
            segment.Paksha = "Shukla";
            segment.PakshaTamil = "சுக்ல";
            segment.Name = TithiNames[tithiNumber - 1];
            segment.NameTamil = TithiNamesTamil[tithiNumber - 1];
        }
        else
        {
            segment.Paksha = "Krishna";
            segment.PakshaTamil = "கிருஷ்ண";

            if (tithiNumber == 30)
            {
                segment.Name = "Amavasya";
                segment.NameTamil = "அமாவாசை";
            }
            else
            {
                segment.Name = TithiNames[tithiNumber - 16];
                segment.NameTamil = TithiNamesTamil[tithiNumber - 16];
            }
        }

        var lord = ZodiacUtils.GetTithiLord(tithiNumber);
        if (ZodiacUtils.PlanetAbbreviations.TryGetValue(lord, out string? abbr))
            segment.Lord = abbr ?? "";

        return segment;
    }

    /// <summary>
    /// Build a Nakshatra segment from its 0-based index (0-26).
    /// Pada is not meaningful across a whole segment, so it is left at 0.
    /// </summary>
    public static PanchangaSegment BuildNakshatra(int index, DateTime? start, DateTime? end)
    {
        var segment = new PanchangaSegment { Start = start, End = end };

        if (index >= 0 && index < 27)
        {
            segment.Name = ZodiacUtils.NakshatraNames[index + 1];
            segment.NameTamil = NakshatraNamesTamil[index + 1];

            var lord = ZodiacUtils.NakshatraLords[index];
            if (ZodiacUtils.PlanetAbbreviations.TryGetValue(lord, out string? abbr))
                segment.Lord = abbr ?? "";
        }

        return segment;
    }

    /// <summary>
    /// Build a Yoga segment from its 0-based index (0-26).
    /// </summary>
    public static PanchangaSegment BuildYoga(int index, DateTime? start, DateTime? end)
    {
        var segment = new PanchangaSegment { Start = start, End = end };

        if (index >= 0 && index < 27)
        {
            segment.Name = YogaNames[index];
            segment.NameTamil = YogaNamesTamil[index];
        }

        return segment;
    }

    /// <summary>
    /// Build a Karana segment from its 0-based index (0-59 across a lunar month).
    /// The 11 Karanas cycle: index 0 is the fixed Kimstughna, indices 1-56 cycle
    /// the 7 movable Karanas, and the last three are the fixed Shakuni,
    /// Chatushpada and Naga.
    /// </summary>
    public static PanchangaSegment BuildKarana(int index, DateTime? start, DateTime? end)
    {
        var segment = new PanchangaSegment { Start = start, End = end };

        int karanaNumber = index + 1; // 1-60

        int nameIndex;
        if (karanaNumber == 1) nameIndex = 10;               // Kimstughna
        else if (karanaNumber >= 58) nameIndex = 7 + (karanaNumber - 58); // Shakuni, Chatushpada, Naga
        else nameIndex = (karanaNumber - 2) % 7;             // Bava..Vishti

        if (nameIndex >= 0 && nameIndex < KaranaNames.Length)
        {
            segment.Name = KaranaNames[nameIndex];
            segment.NameTamil = KaranaNamesTamil[nameIndex];
        }

        return segment;
    }

    /// <summary>
    /// Tamil year (60-year cycle) and solar month for a date and Sun sign.
    /// </summary>
    public static (string tamilYear, string englishYear, string tamilMonth, string englishMonth)
        GetTamilYearMonth(DateTime date, int sunSign)
    {
        int year = date.Year;
        if (date.Month < 4 || (date.Month == 4 && date.Day < 14))
            year--;

        int cycleYear = ((year - 1987) % 60 + 60) % 60;

        string tamilYear = cycleYear >= 0 && cycleYear < TamilYears.Length ? TamilYears[cycleYear] : "";
        string englishYear = cycleYear >= 0 && cycleYear < EnglishYears.Length ? EnglishYears[cycleYear] : "";

        string tamilMonth = sunSign >= 1 && sunSign <= 12 ? TamilMonths[sunSign] : "";
        string englishMonth = sunSign >= 1 && sunSign <= 12 ? EnglishMonths[sunSign] : "";

        return (tamilYear, englishYear, tamilMonth, englishMonth);
    }
}
