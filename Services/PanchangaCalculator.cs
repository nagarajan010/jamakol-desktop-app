using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Calculator for Panchanga (Tamil/Vedic almanac) details
/// </summary>
public class PanchangaCalculator
{
    // Tamil day names
    private static readonly string[] TamilDays = 
    {
        "ஞாயிறு", "திங்கள்", "செவ்வாய்", "புதன்", "வியாழன்", "வெள்ளி", "சனி"
    };

    // English day names
    private static readonly string[] EnglishDays = 
    {
        "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
    };

    // Hora lords in order (from Sunday sunrise)
    private static readonly string[] HoraLords = 
    {
        "Sun", "Venus", "Mercury", "Moon", "Saturn", "Jupiter", "Mars"
    };

    private static readonly string[] HoraLordsTamil = 
    {
        "சூரியன்", "சுக்கிரன்", "புதன்", "சந்திரன்", "சனி", "குரு", "செவ்வாய்"
    };

    // Tithi names (15 tithis)
    private static readonly string[] TithiNames = 
    {
        "Pratipada", "Dwitiya", "Tritiya", "Chaturthi", "Panchami",
        "Shashthi", "Saptami", "Ashtami", "Navami", "Dashami",
        "Ekadashi", "Dwadashi", "Trayodashi", "Chaturdashi", "Purnima/Amavasya"
    };

    private static readonly string[] TithiNamesTamil = 
    {
        "பிரதமை", "துவிதியை", "திரிதியை", "சதுர்த்தி", "பஞ்சமி",
        "சஷ்டி", "சப்தமி", "அஷ்டமி", "நவமி", "தசமி",
        "ஏகாதசி", "துவாதசி", "திரயோதசி", "சதுர்தசி", "பௌர்ணமி/அமாவாசை"
    };

    // Yoga names (27 yogas)
    private static readonly string[] YogaNames = 
    {
        "Vishkumbha", "Priti", "Ayushman", "Saubhagya", "Shobhana",
        "Atiganda", "Sukarma", "Dhriti", "Shoola", "Ganda",
        "Vriddhi", "Dhruva", "Vyaghata", "Harshana", "Vajra",
        "Siddhi", "Vyatipata", "Variyan", "Parigha", "Shiva",
        "Siddha", "Sadhya", "Shubha", "Shukla", "Brahma",
        "Indra", "Vaidhriti"
    };

    private static readonly string[] YogaNamesTamil = 
    {
        "விஷ்கும்பம்", "ப்ரீதி", "ஆயுஷ்மான்", "சௌபாக்கியம்", "சோபனம்",
        "அதிகண்டம்", "சுகர்மா", "த்ருதி", "சூலம்", "கண்டம்",
        "விருத்தி", "த்ருவம்", "வியாகாதம்", "ஹர்ஷணம்", "வஜ்ரம்",
        "சித்தி", "வியதீபாதம்", "வரீயான்", "பரிகம்", "சிவம்",
        "சித்தம்", "சாத்தியம்", "சுபம்", "சுக்லம்", "பிரம்மம்",
        "இந்திரம்", "வைத்ருதி"
    };

    // Karana names (11 karanas, repeating)
    private static readonly string[] KaranaNames = 
    {
        "Bava", "Balava", "Kaulava", "Taitila", "Gara",
        "Vanija", "Vishti", "Shakuni", "Chatushpada", "Naga", "Kimstughna"
    };

    private static readonly string[] KaranaNamesTamil = 
    {
        "பவம்", "பாலவம்", "கௌலவம்", "தைதுலம்", "கரம்",
        "வணிஜம்", "விஷ்டி", "சகுனி", "சதுஷ்பாதம்", "நாகம்", "கிம்ஸ்துக்னம்"
    };

    // Tamil rasi names
    private static readonly string[] RasiNamesTamil = 
    {
        "", "மேஷம்", "ரிஷபம்", "மிதுனம்", "கடகம்", "சிம்மம்", "கன்னி",
        "துலாம்", "விருச்சிகம்", "தனுசு", "மகரம்", "கும்பம்", "மீனம்"
    };

    // Tamil nakshatra names (matching ZodiacUtils order, 1-indexed)
    private static readonly string[] NakshatraNamesTamil = 
    {
        "", "அசுவினி", "பரணி", "கிருத்திகை", "ரோகிணி", "மிருகசீரிஷம்",
        "திருவாதிரை", "புனர்பூசம்", "பூசம்", "ஆயில்யம்", "மகம்",
        "பூரம்", "உத்திரம்", "ஹஸ்தம்", "சித்திரை", "சுவாதி",
        "விசாகம்", "அனுஷம்", "கேட்டை", "மூலம்", "பூராடம்",
        "உத்திராடம்", "திருவோணம்", "அவிட்டம்", "சதயம்", "பூரட்டாதி",
        "உத்திரட்டாதி", "ரேவதி"
    };

    // Tamil year names (60-year cycle)
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

    // Tamil month names
    private static readonly string[] TamilMonths = 
    {
        "", "சித்திரை", "வைகாசி", "ஆனி", "ஆடி", "ஆவணி", "புரட்டாசி",
        "ஐப்பசி", "கார்த்திகை", "மார்கழி", "தை", "மாசி", "பங்குனி"
    };

    /// <summary>
    /// Calculate Panchanga for given date, time, and chart data
    /// </summary>
    public PanchangaDetails Calculate(
        ChartData chartData,
        DateTime sunrise,
        DateTime sunset,
        double ayanamsa,
        double siderealTime)
    {
        var details = new PanchangaDetails();

        // Get Sun and Moon positions
        var sun = chartData.Planets.FirstOrDefault(p => p.Name == "Sun");
        var moon = chartData.Planets.FirstOrDefault(p => p.Name == "Moon");

        if (sun == null || moon == null)
            return details;

        double sunLong = sun.Longitude;
        double moonLong = moon.Longitude;

        // Day of week
        int dayOfWeek = (int)chartData.BirthData.BirthDateTime.DayOfWeek;
        details.DayName = EnglishDays[dayOfWeek];
        details.DayTamil = TamilDays[dayOfWeek];
        
        // Day Lord Abbr
        // Day of week: 0 Sunday ... 6 Saturday
        // Map to planet: 0 Sun ... 6 Saturn
        var dayPlanet = (Models.Planet)dayOfWeek; // Enum matches 0=Sun..6=Saturn?
        // Let's check Planet enum in ZodiacUtils or Models. 
        // Planet enum: Sun=0, Moon=1, Mars=2, Mercury=3, Jupiter=4, Venus=5, Saturn=6, Rahu=7, Ketu=8.
        // Yes, standard order matches DayOfWeek values if Sunday=0.
        if (ZodiacUtils.PlanetAbbreviations.TryGetValue(dayPlanet, out string? abbr))
        {
            details.DayLordAbbr = abbr ?? "";
        }

        // Nakshatra (Moon's star)
        double nakLength = 360.0 / 27.0;
        int nakIndex = (int)(moonLong / nakLength); // 0-26
        // int nakIndex = moon.Nakshatra; // Use from Planet if consistent, but manual calc for progress
        // Note: Planet.Nakshatra might be 1-based.
        
        details.NakshatraName = ZodiacUtils.NakshatraNames[nakIndex + 1];
        details.NakshatraTamil = NakshatraNamesTamil[nakIndex + 1];
        details.NakshatraPada = moon.NakshatraPada;
        
        double nakProgress = moonLong % nakLength;
        details.NakshatraPercentLeft = ((nakLength - nakProgress) / nakLength) * 100.0;
        
        // Nakshatra Lord
        if (nakIndex >= 0 && nakIndex < 27)
        {
            var lord = ZodiacUtils.NakshatraLords[nakIndex];
            if (ZodiacUtils.PlanetAbbreviations.TryGetValue(lord, out string? nakAbbr))
            {
                details.NakshatraLord = nakAbbr ?? "";
            }
        }

        // Sun and Moon Rasi
        details.SunRasi = ZodiacUtils.SignNames[sun.Sign];
        details.SunRasiTamil = RasiNamesTamil[sun.Sign];
        details.MoonRasi = ZodiacUtils.SignNames[moon.Sign];
        details.MoonRasiTamil = RasiNamesTamil[moon.Sign];

        // Tithi calculation
        CalculateTithi(sunLong, moonLong, details);

        // Yoga calculation
        CalculateYoga(sunLong, moonLong, details);

        // Karana calculation
        CalculateKarana(sunLong, moonLong, details);

        // Hora calculation
        CalculateHora(chartData.BirthData.BirthDateTime, sunrise, dayOfWeek, details);

        // Sunrise/Sunset
        details.Sunrise = sunrise.ToString("h:mm:ss tt").ToLower();
        details.Sunset = sunset.ToString("h:mm:ss tt").ToLower();

        // Ayanamsa
        details.AyanamsaValue = ayanamsa;

        // Udayadi Nazhikai (time from sunrise) and Janma Ghatis
        CalculateNazhikai(chartData.BirthData.BirthDateTime, sunrise, details);

        // Tamil year and month
        CalculateTamilYearMonth(chartData.BirthData.BirthDateTime, sun.Sign, details);

        // Sidereal Time
        var stSpan = TimeSpan.FromHours(siderealTime);
        details.SiderealTime = $"{(int)stSpan.TotalHours:00}:{stSpan.Minutes:00}:{stSpan.Seconds:00}";

        return details;
    }

    private void CalculateTithi(double sunLong, double moonLong, PanchangaDetails details)
    {
        // Tithi = Moon - Sun, normalized to 360
        double diff = moonLong - sunLong;
        if (diff < 0) diff += 360;

        // Each tithi is 12 degrees (360/30)
        double tithiProgress = diff % 12;
        details.TithiPercentLeft = ((12 - tithiProgress) / 12) * 100.0;

        int tithiNumber = (int)(diff / 12) + 1;
        if (tithiNumber > 30) tithiNumber = 30;

        // Determine Paksha
        if (tithiNumber <= 15)
        {
            details.Paksha = "Shukla";
            details.PakshaTamil = "சுக்ல";
            int idx = tithiNumber - 1;
            if (idx >= 0 && idx < 15)
            {
                details.TithiName = TithiNames[idx];
                details.TithiTamil = TithiNamesTamil[idx];
                
                 int tithiCycleIndex = (tithiNumber - 1) % 8;
                 if (tithiNumber == 30) tithiCycleIndex = 7; // Amavasya = Rahu

                 var tLord = ZodiacUtils.TithiLords[tithiCycleIndex];
                 if (ZodiacUtils.PlanetAbbreviations.TryGetValue(tLord, out string? tAbbr))
                 {
                     details.TithiLord = tAbbr ?? "";
                 }
            }
        }
        else
        {
            details.Paksha = "Krishna";
            details.PakshaTamil = "கிருஷ்ண";
            int idx = tithiNumber - 16;
            if (idx >= 0 && idx < 15)
            {
                details.TithiName = TithiNames[idx];
                details.TithiTamil = TithiNamesTamil[idx];
                
                // Tithi Lord
                // Calculate effective index 1-30. TithiLords likely map 1-based index or cycle
                // Valid cycle 1-15 repeated?
                // TithiLords array in ZodiacUtils has 8 elements?
                // Let's check logic:
                // 1 Sun, 2 Moon ... 8 Rahu. 9 Sun ...
                // Cycle of 8? Or mapping specific to 15?
                // Standard: 15 tithis.
                // 1-8 are Sun-Rahu. 9-14 are Sun-Venus. 15 is Saturn. 30 is Rahu.
                // Let's implement cyclic lookup: (tithi-1) % 8 gives 0-7 index?
                // 1 (Sun) -> 0. 9 (Sun) -> 8%8=0. Correct.
                // 15 (Saturn) -> 14%8 = 6 -> Saturn. Correct.
                // 30 (Amavasya - Rahu). 30->29%8 = 5 (Venus). INCORRECT.
                // Special case for Amavasya (30)?
                // Let's look at user example "Sukla Chaturthi (Me)".
                // Chaturthi is 4th. 4 -> Mercury.
                // TithiLords[3] is Mercury. (4-1)%8 = 3. Correct.
                
                int tithiCycleIndex = (tithiNumber - 1) % 8; // Default cycle
                // Handle Amavasya (30) if it differs
                 if (tithiNumber == 30) 
                 {
                     // Amavasya is Rahu. 29%8 = 5 (Venus). So special case.
                     // Wait, 30 is Amavasya.
                     // Let's use the Utils array which I added: Sun..Rahu (8 items).
                     // If index 7 is Rahu.
                     // Tithi 30 should be Rahu.
                     tithiCycleIndex = 7; 
                 }
                 
                 var tLord = ZodiacUtils.TithiLords[tithiCycleIndex];
                 if (ZodiacUtils.PlanetAbbreviations.TryGetValue(tLord, out string? tAbbr))
                 {
                     details.TithiLord = tAbbr ?? "";
                 }
            }
        }
    }

    private void CalculateYoga(double sunLong, double moonLong, PanchangaDetails details)
    {
        // Yoga = (Sun + Moon) / (360/27)
        double sum = sunLong + moonLong;
        if (sum >= 360) sum -= 360;
        
        double yogaLength = 360.0 / 27.0;
        double yogaProgress = sum % yogaLength;
        details.YogaPercentLeft = ((yogaLength - yogaProgress) / yogaLength) * 100.0;
        
        int yogaIndex = (int)(sum / yogaLength);
        if (yogaIndex >= 0 && yogaIndex < 27)
        {
            details.YogaName = YogaNames[yogaIndex];
            details.YogaTamil = YogaNamesTamil[yogaIndex];
        }
    }

    private void CalculateKarana(double sunLong, double moonLong, PanchangaDetails details)
    {
        // Karana = half tithi, 60 karanas total
        double diff = moonLong - sunLong;
        if (diff < 0) diff += 360;

        int karanaNumber = (int)(diff / 6) + 1;
        if (karanaNumber > 60) karanaNumber = 60;
        
        double karanaProgress = diff % 6;
        details.KaranaPercentLeft = ((6 - karanaProgress) / 6.0) * 100.0;

        // First karana is Kimstughna, last 4 are fixed
        int karanaIndex;
        if (karanaNumber == 1)
            karanaIndex = 10; // Kimstughna
        else if (karanaNumber >= 58)
            karanaIndex = 7 + (karanaNumber - 58); // Shakuni, Chatushpada, Naga
        else
            karanaIndex = (karanaNumber - 2) % 7; // Repeating 7 karanas

        if (karanaIndex >= 0 && karanaIndex < KaranaNames.Length)
        {
            details.KaranaName = KaranaNames[karanaIndex];
            details.KaranaTamil = KaranaNamesTamil[karanaIndex];
        }
    }

    private void CalculateHora(DateTime currentTime, DateTime sunrise, int dayOfWeek, PanchangaDetails details)
    {
        // Hours from sunrise
        double hoursFromSunrise = (currentTime - sunrise).TotalHours;
        if (hoursFromSunrise < 0) hoursFromSunrise += 24;

        int horaNumber = (int)hoursFromSunrise;

        // Day lord index (Sun=0, Moon=1, Mars=2, etc)
        // Hora sequence starts with day lord and follows planetary hours
        int[] dayLordSequence = { 0, 3, 6, 2, 5, 1, 4 }; // Sun, Moon, Mars, Mercury, Jupiter, Venus, Saturn
        int startIndex = dayLordSequence[dayOfWeek];

        int horaIndex = (startIndex + horaNumber) % 7;
        
        details.HoraLord = HoraLords[horaIndex];
        details.HoraLordTamil = HoraLordsTamil[horaIndex];
    }

    private void CalculateNazhikai(DateTime currentTime, DateTime sunrise, PanchangaDetails details)
    {
        // Nazhikai = 24 minutes, 60 Nazhikai in a day
        double minutesFromSunrise = (currentTime - sunrise).TotalMinutes;
        if (minutesFromSunrise < 0) minutesFromSunrise += 24 * 60;

        double nazhikai = minutesFromSunrise / 24;
        details.JanmaGhatis = nazhikai; // Nazhikai is same as Ghatis (24 mins)
        
        int nazhikaiWhole = (int)nazhikai;
        double vinazhikai = (nazhikai % 1) * 60;

        details.UdayadiNazhikai = $"{nazhikaiWhole}°{(int)vinazhikai}'{(int)((vinazhikai % 1) * 60)}\"";
    }

    private void CalculateTamilYearMonth(DateTime date, int sunSign, PanchangaDetails details)
    {
        // Tamil year calculation (approximate - starts mid-April)
        // Using a simplified calculation
        int year = date.Year;
        if (date.Month < 4 || (date.Month == 4 && date.Day < 14))
            year--;

        // 60-year cycle starting from specific base year
        int cycleYear = ((year - 1987) % 60 + 60) % 60;
        if (cycleYear >= 0 && cycleYear < TamilYears.Length)
        {
            details.TamilYear = TamilYears[cycleYear];
        }

        // Tamil month from Sun sign
        if (sunSign >= 1 && sunSign <= 12)
        {
            details.TamilMonth = TamilMonths[sunSign];
        }
    }
}
