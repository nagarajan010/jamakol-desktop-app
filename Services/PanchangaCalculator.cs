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

    // English year names (60-year cycle - Sanskrit transliteration)
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

    // Tamil month names
    private static readonly string[] TamilMonths = 
    {
        "", "சித்திரை", "வைகாசி", "ஆனி", "ஆடி", "ஆவணி", "புரட்டாசி",
        "ஐப்பசி", "கார்த்திகை", "மார்கழி", "தை", "மாசி", "பங்குனி"
    };

    // English month names (Solar months)
    private static readonly string[] EnglishMonths = 
    {
        "", "Chithirai", "Vaikasi", "Aani", "Aadi", "Aavani", "Purattasi",
        "Aippasi", "Karthigai", "Margazhi", "Thai", "Maasi", "Panguni"
    };

    /// <summary>
    /// Calculate Panchanga for given date, time, and chart data
    /// </summary>
    public PanchangaDetails Calculate(
        ChartData chartData,
        DateTime sunrise,
        DateTime sunset,
        DateTime localNoon,
        DateTime nextSunrise,
        double ayanamsa,
        double siderealTime,
        DayOfWeek vedicDayOfWeek,
        EphemerisService ephemerisService) // Added dependencies
    {
        var details = new PanchangaDetails();

        // Get Sun and Moon positions from ChartData (already calculated)
        var sun = chartData.Planets.FirstOrDefault(p => p.Name == "Sun");
        var moon = chartData.Planets.FirstOrDefault(p => p.Name == "Moon");

        if (sun == null || moon == null)
            return details;

        double sunLong = sun.Longitude;
        double moonLong = moon.Longitude;

        // --- VARA (Weekday) ---
        // Use Vedic Day Lord (passed from Orchestrator based on Sunrise logic)
        int dayIndex = (int)vedicDayOfWeek; 
        details.DayName = EnglishDays[dayIndex];
        details.DayTamil = TamilDays[dayIndex];

        var dayPlanet = (Models.Planet)dayIndex;
        if (ZodiacUtils.PlanetAbbreviations.TryGetValue(dayPlanet, out string? abbr))
        {
            details.DayLordAbbr = abbr ?? "";
        }

        // --- NAKSHATRA ---
        // Using standard 27 Nakshatra system as requested
        CalculateNakshatra(moonLong, details, chartData.JulianDay, chartData.AyanamsaValue, ephemerisService);

        // Sun and Moon Rasi
        details.SunRasi = ZodiacUtils.SignNames[sun.Sign];
        details.SunRasiTamil = RasiNamesTamil[sun.Sign];
        details.MoonRasi = ZodiacUtils.SignNames[moon.Sign];
        details.MoonRasiTamil = RasiNamesTamil[moon.Sign];

        // --- TITHI ---
        CalculateTithi(sunLong, moonLong, details, chartData.JulianDay, chartData.AyanamsaValue, ephemerisService);

        // --- YOGA ---
        CalculateYoga(sunLong, moonLong, details, chartData.JulianDay, chartData.AyanamsaValue, ephemerisService);

        // --- KARANA ---
        CalculateKarana(sunLong, moonLong, details, chartData.JulianDay, chartData.AyanamsaValue, ephemerisService);

        // --- HORA (Variable Length) ---
        // Pass the Vedic Day Index
        CalculateHora(chartData.BirthData.BirthDateTime, sunrise, sunset, nextSunrise, dayIndex, details);

        // --- KALA HORA (Fixed Length from Local Noon) ---
        CalculateKalaHora(chartData.BirthData.BirthDateTime, localNoon, dayIndex, details);

        // Sunrise/Sunset
        details.Sunrise = sunrise.ToString("h:mm:ss tt").ToLower();
        details.Sunset = sunset.ToString("h:mm:ss tt").ToLower();

        // Ayanamsa
        details.AyanamsaValue = ayanamsa;

        // Udayadi Nazhikai and Janma Ghatis
        CalculateNazhikai(chartData.BirthData.BirthDateTime, sunrise, details);

        // Tamil year and month
        CalculateTamilYearMonth(chartData.BirthData.BirthDateTime, sun.Sign, details);

        // Sidereal Time
        var stSpan = TimeSpan.FromHours(siderealTime);
        details.SiderealTime = $"{(int)stSpan.TotalHours:00}:{stSpan.Minutes:00}:{stSpan.Seconds:00}";

        return details;
    }

    private void CalculateTithi(double sunLong, double moonLong, PanchangaDetails details, double julianDay, double ayanamsa, EphemerisService ephemeris)
    {
        double diff = moonLong - sunLong;
        if (diff < 0) diff += 360;

        // Tithi length = 12 degrees
        double tithiProgress = diff % 12;
        details.TithiPercentLeft = ((12 - tithiProgress) / 12) * 100.0;

        int tithiNumber = (int)(diff / 12) + 1;
        if (tithiNumber > 30) tithiNumber = 30;

        // Populate Names
        if (tithiNumber <= 15)
        {
            details.Paksha = "Shukla";
            details.PakshaTamil = "சுக்ல";
            int idx = tithiNumber - 1;
            details.TithiName = TithiNames[idx];
            details.TithiTamil = TithiNamesTamil[idx];
        }
        else
        {
            details.Paksha = "Krishna";
            details.PakshaTamil = "கிருஷ்ண";
            int idx = tithiNumber - 16;
            details.TithiName = TithiNames[idx];
            details.TithiTamil = TithiNamesTamil[idx];
        }

        // Tithi Lord
        int tithiCycleIndex = (tithiNumber - 1) % 8;
        if (tithiNumber == 30) tithiCycleIndex = 7; // Amavasya = Rahu

        var tLord = ZodiacUtils.TithiLords[tithiCycleIndex];
        if (ZodiacUtils.PlanetAbbreviations.TryGetValue(tLord, out string? tAbbr))
        {
            details.TithiLord = tAbbr ?? "";
        }

        // Calculate End Time
        // Target: next multiple of 12 degrees
        double nextTithiDegree = tithiNumber * 12.0; 
        
        details.TithiEndTime = CalculateEndTime(
            julianDay, 
            ayanamsa, 
            ephemeris, 
            (jd, ay) => {
                var s = ephemeris.GetPlanetPosition(jd, (int)Models.Planet.Sun, 1);
                return s.longitude;
            },
            (jd, ay) => {
                var m = ephemeris.GetPlanetPosition(jd, (int)Models.Planet.Moon, 1);
                return m.longitude;
            },
            diff, // Current value
            nextTithiDegree,
            true // Is Relative (Moon - Sun)
        );
    }
    
    private void CalculateNakshatra(double moonLong, PanchangaDetails details, double julianDay, double ayanamsa, EphemerisService ephemeris)
    {
        double nakLength = 360.0 / 27.0;
        int nakIndex = (int)(moonLong / nakLength);
        
        details.NakshatraName = ZodiacUtils.NakshatraNames[nakIndex + 1];
        details.NakshatraTamil = NakshatraNamesTamil[nakIndex + 1];
        details.NakshatraPada = (int)((moonLong % nakLength) / (nakLength / 4)) + 1;
        
        double nakProgress = moonLong % nakLength;
        details.NakshatraPercentLeft = ((nakLength - nakProgress) / nakLength) * 100.0;
        
        // Lord
        if (nakIndex >= 0 && nakIndex < 27)
        {
            var lord = ZodiacUtils.NakshatraLords[nakIndex];
            if (ZodiacUtils.PlanetAbbreviations.TryGetValue(lord, out string? nakAbbr))
            {
                details.NakshatraLord = nakAbbr ?? "";
            }
        }

        // End Time
        // Target: next multiple of nakLength
        // We find when Moon reaches (nakIndex + 1) * nakLength
        
        // Pass Ayanamsa ID = 1 (Lahiri) for now.
        details.NakshatraEndTime = CalculateEndTime(
            julianDay,
            ayanamsa,
            ephemeris,
            null, // No Sun needed
            (jd, ay) => {
                var m = ephemeris.GetPlanetPosition(jd, (int)Models.Planet.Moon, 1);
                return m.longitude;
            },
            moonLong, // Current
            nakLength * (nakIndex + 1), // Target Absolute Longitude
            false // Absolute
        );
    }

    private void CalculateYoga(double sunLong, double moonLong, PanchangaDetails details, double julianDay, double ayanamsa, EphemerisService ephemeris)
    {
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

        // End Time
        // Target: Next multiple of yogaLength
        // Check (Sun + Moon)
        double nextYogaTarget = (yogaIndex + 1) * yogaLength;
        
        details.YogaEndTime = CalculateEndTime(
            julianDay, 
            ayanamsa, 
            ephemeris, 
            (jd, ay) => ephemeris.GetPlanetPosition(jd, (int)Models.Planet.Sun, 1).longitude,
            (jd, ay) => ephemeris.GetPlanetPosition(jd, (int)Models.Planet.Moon, 1).longitude,
            sum, 
            nextYogaTarget,
            false, // Treated as Absolute for the function (sum increases 0-360)
            true // Is Addition
        );
    }

    private void CalculateKarana(double sunLong, double moonLong, PanchangaDetails details, double julianDay, double ayanamsa, EphemerisService ephemeris)
    {
        double diff = moonLong - sunLong;
        if (diff < 0) diff += 360;

        int karanaNumber = (int)(diff / 6) + 1;
        double karanaProgress = diff % 6;
        details.KaranaPercentLeft = ((6 - karanaProgress) / 6.0) * 100.0;

        // Names
        int karanaIndex;
        if (karanaNumber == 1) karanaIndex = 10;
        else if (karanaNumber >= 58) karanaIndex = 7 + (karanaNumber - 58);
        else karanaIndex = (karanaNumber - 2) % 7;

        if (karanaIndex >= 0 && karanaIndex < KaranaNames.Length)
        {
            details.KaranaName = KaranaNames[karanaIndex];
            details.KaranaTamil = KaranaNamesTamil[karanaIndex];
        }

        // End Time
        double nextKaranaTarget = karanaNumber * 6.0;

        details.KaranaEndTime = CalculateEndTime(
            julianDay,
            ayanamsa,
            ephemeris,
            (jd, ay) => ephemeris.GetPlanetPosition(jd, (int)Models.Planet.Sun, 1).longitude,
            (jd, ay) => ephemeris.GetPlanetPosition(jd, (int)Models.Planet.Moon, 1).longitude,
            diff,
            nextKaranaTarget, // Target is specific angle (e.g. 6, 12, 18...)
            true // Relative
        );
    }

    /// <summary>
    /// Generic method to calculate when an astronomical condition is met (End Time)
    /// </summary>
    private string CalculateEndTime(
        double startJulianDay, 
        double ayanamsa,
        EphemerisService ephemeris, 
        Func<double, double, double>? getSunLong,
        Func<double, double, double> getMoonLong,
        double currentValue,
        double targetValue, 
        bool isRelativeDifference = false, // True for Tithi/Karana
        bool isAddition = false) // True for Yoga
    {
        // Simple iterative search 
        double currentJD = startJulianDay;
        double step = 1.0 / 24.0; // 1 hour step initial
        int maxSteps = 48; // Look ahead 2 days max
        
        // Find rough crossing
        double prevVal = currentValue;
        double prevJD = currentJD;
        
        // Loop
        for (int i = 0; i < maxSteps; i++)
        {
            currentJD += step;
            
            double m = getMoonLong(currentJD, 1);
            double s = getSunLong != null ? getSunLong(currentJD, 1) : 0;
            
            double val;
            if (isRelativeDifference) {
                val = m - s;
                if (val < 0) val += 360;
            } else if (isAddition) {
                val = m + s;
                if (val >= 360) val -= 360;
            } else {
                val = m; // Absolute Moon
            }
            
            // Crossing logic:
            // 1. Normal increasing: prev < target <= val
            // 2. Wrap around 360: prev > 300 && val < 50 && target < 50 (target is small, we wrapped)
            // 3. Wrap around 360 (target is 360/0): prev > 300 && val < 50
            
            bool crossed = false;
            
            // Normalize target 360 -> 0 for wrap checks if needed? 
            // Better to handle 0-360 range.
            
            if (targetValue >= 360) {
                // Target is effectively 0 (start of new cycle)
                if (prevVal > 300 && val < 60) crossed = true;
            }
            else {
                // Normal target
                if (prevVal < targetValue && val >= targetValue) crossed = true;
                
                // Wrap case: Target is small (e.g. 13 deg), prev was 355, val is 15.
                if (prevVal > targetValue && val >= targetValue && prevVal > 300 && val < 100) crossed = true;
            }

            if (crossed)
            {
                 return RefineEndTime(prevJD, currentJD, targetValue, getSunLong, getMoonLong, isRelativeDifference, isAddition, ephemeris);
            }
            
            prevVal = val;
            prevJD = currentJD;
        }

        return ""; // Not found within limit
    }

    private string RefineEndTime(
        double startJD, 
        double endJD, 
        double target, 
        Func<double, double, double>? getSun, 
        Func<double, double, double> getMoon,
        bool isDiff, 
        bool isAdd,
        EphemerisService eph)
    {
        double low = startJD;
        double high = endJD;
        
        for(int k=0; k<12; k++) // Binary search refinement
        {
            double mid = (low + high) / 2;
            
            double m = getMoon(mid, 1);
            double s = getSun != null ? getSun(mid, 1) : 0;
             
            double val;
            if (isDiff) {
                val = m - s;
                if (val < 0) val += 360;
            } else if (isAdd) {
                val = m + s;
                if (val >= 360) val -= 360;
            } else {
                val = m;
            }
            
            // Comparison
            bool isLess = false;
            if (target >= 360 || target == 0) {
                if (val > 180) isLess = true; // Still in previous cycle high numbers
                else isLess = false; // Wrapped to low numbers (past target)
            } else {
                 if (val < target) isLess = true;
                 // Handle wrap logic for binary search if interval spans 360
                 if (val > 300 && target < 60) isLess = true; // e.g. val 350, target 10. 350 is "less" in cycle progress
            }
            
            if (isLess) low = mid;
            else high = mid;
        }
        
        DateTime date = EphemerisService.JulianDateToDateTime(high);
        // Format relative to today? Just Time if same day?
        // User probably wants full date or clear time.
        // Let's return "MMM dd, h:mm tt"
        return date.ToString("MMM dd, h:mm tt");
    }

    private void CalculateHora(DateTime currentTime, DateTime sunrise, DateTime sunset, DateTime nextSunrise, int dayIndex, PanchangaDetails details)
    {
        bool isDay = currentTime >= sunrise && currentTime < sunset;
        int horaNumber;
        
        if (isDay)
        {
            TimeSpan dayDuration = sunset - sunrise;
            double horaLengthSeconds = dayDuration.TotalSeconds / 12.0;
            double secondsFromSunrise = (currentTime - sunrise).TotalSeconds;
            
            horaNumber = (int)(secondsFromSunrise / horaLengthSeconds);
            if (horaNumber >= 12) horaNumber = 11; 
        }
        else
        {
            TimeSpan nightDuration = nextSunrise - sunset;
            double horaLengthSeconds = nightDuration.TotalSeconds / 12.0;
            double secondsFromSunset = (currentTime - sunset).TotalSeconds;
            horaNumber = 12 + (int)(secondsFromSunset / horaLengthSeconds);
            if (horaNumber >= 24) horaNumber = 23;
        }

        int[] dayToLordCycleIndex = { 0, 3, 6, 2, 5, 1, 4 };
        int startingIndex = dayToLordCycleIndex[dayIndex];
        int horaIndex = (startingIndex + horaNumber) % 7;
        
        if (horaIndex >= 0 && horaIndex < 7)
        {
            details.HoraLord = HoraLords[horaIndex];
            details.HoraLordTamil = HoraLordsTamil[horaIndex];
        }
    }
    
    private void CalculateKalaHora(DateTime currentTime, DateTime localNoon, int dayIndex, PanchangaDetails details)
    {
        DateTime derivedSixAm = localNoon.AddHours(-6);
        double hoursPassed = (currentTime - derivedSixAm).TotalHours;
        
        if (hoursPassed < 0) hoursPassed += 24; 
        if (hoursPassed >= 24) hoursPassed %= 24; // ensure within 0-24
        
        int horaNumber = (int)hoursPassed;
        
        int[] dayToLordCycleIndex = { 0, 3, 6, 2, 5, 1, 4 };
        int startingIndex = dayToLordCycleIndex[dayIndex];
        int horaIndex = (startingIndex + horaNumber) % 7;

        if (horaIndex >= 0 && horaIndex < 7)
        {
            details.KalaHoraLord = HoraLords[horaIndex];
            details.KalaHoraLordTamil = HoraLordsTamil[horaIndex];
        }
    }

    private void CalculateNazhikai(DateTime currentTime, DateTime sunrise, PanchangaDetails details)
    {
        double minutesFromSunrise = (currentTime - sunrise).TotalMinutes;
        if (minutesFromSunrise < 0) minutesFromSunrise += 24 * 60;

        double nazhikai = minutesFromSunrise / 24;
        details.JanmaGhatis = nazhikai;
        
        int nazhikaiWhole = (int)nazhikai;
        double vinazhikai = (nazhikai % 1) * 60;

        details.UdayadiNazhikai = $"{nazhikaiWhole}°{(int)vinazhikai}'{(int)((vinazhikai % 1) * 60)}\"";
    }

    private void CalculateTamilYearMonth(DateTime date, int sunSign, PanchangaDetails details)
    {
        int year = date.Year;
        if (date.Month < 4 || (date.Month == 4 && date.Day < 14))
            year--;

        int cycleYear = ((year - 1987) % 60 + 60) % 60;
        if (cycleYear >= 0 && cycleYear < TamilYears.Length)
        {
            details.TamilYear = TamilYears[cycleYear];
            details.EnglishYear = EnglishYears[cycleYear];
        }

        if (sunSign >= 1 && sunSign <= 12)
        {
            details.TamilMonth = TamilMonths[sunSign];
            details.EnglishMonth = EnglishMonths[sunSign];
        }
    }
}
