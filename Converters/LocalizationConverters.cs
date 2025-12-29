using System;
using System.Globalization;
using System.Windows.Data;
using JamakolAstrology.Services;

namespace JamakolAstrology.Converters
{
    /// <summary>
    /// Converts English planet names to localized names based on current UI culture
    /// </summary>
    public class PlanetNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string englishName && ZodiacUtils.IsTamil)
            {
                // Map English names to Tamil
                return englishName switch
                {
                    "Sun" => "சூரியன்",
                    "Moon" => "சந்திரன்",
                    "Mars" => "செவ்வாய்",
                    "Mercury" => "புதன்",
                    "Jupiter" => "குரு",
                    "Venus" => "சுக்கிரன்",
                    "Saturn" => "சனி",
                    "Rahu" => "ராகு",
                    "Ketu" => "கேது",
                    "Lagna" => "லக்னம்",
                    // Aprakash Graha - keep as transliterated
                    "Dhooma" => "தூமம்",
                    "Vyatipata" => "வியதிபாதம்",
                    "Parivesha" => "பரிவேஷம்",
                    "Indrachapa" => "இந்திரசாபம்",
                    "Upaketu" => "உபகேது",
                    // Jamakol Special Points
                    "Udayam" => "உதயம்",
                    "Aarudam" => "ஆருடம்",
                    "Kavippu" => "கவிப்பு",
                    "Yemakandam" => "எமகண்டம்",
                    "Mrithyu" => "மிருத்யு",
                    "Mandhi" => "மாந்தி",
                    "Rahu Kalam" => "ராகு காலம்",
                    "Snake" => "பாம்பு",
                    _ => englishName
                };
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts English nakshatra names to localized names based on current UI culture
    /// </summary>
    public class NakshatraNameConverter : IValueConverter
    {
        private static readonly string[] TamilNames = {
            "", "அஸ்வினி", "பரணி", "கார்த்திகை", "ரோகிணி", "மிருகசீரிஷம்", "திருவாதிரை",
            "புனர்பூசம்", "பூசம்", "ஆயில்யம்", "மகம்", "பூரம்", "உத்திரம்",
            "ஹஸ்தம்", "சித்திரை", "சுவாதி", "விசாகம்", "அனுஷம்", "கேட்டை",
            "மூலம்", "பூராடம்", "உத்திராடம்", "திருவோணம்", "அவிட்டம்",
            "சதயம்", "பூரட்டாதி", "உத்திரட்டாதி", "ரேவதி"
        };

        private static readonly string[] EnglishNames = {
            "", "Ashwini", "Bharani", "Krittika", "Rohini", "Mrigashira", "Ardra",
            "Punarvasu", "Pushya", "Ashlesha", "Magha", "Purva Phalguni", "Uttara Phalguni",
            "Hasta", "Chitra", "Swati", "Vishakha", "Anuradha", "Jyeshtha",
            "Mula", "Purva Ashadha", "Uttara Ashadha", "Shravana", "Dhanishta", 
            "Shatabhisha", "Purva Bhadrapada", "Uttara Bhadrapada", "Revati"
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string englishName && ZodiacUtils.IsTamil)
            {
                for (int i = 1; i <= 27; i++)
                {
                    if (EnglishNames[i] == englishName)
                        return TamilNames[i];
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts English sign names or abbreviations to localized names based on current UI culture
    /// </summary>
    public class SignNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string englishName && ZodiacUtils.IsTamil)
            {
                return englishName switch
                {
                    // Full names
                    "Aries" => "மேஷம்",
                    "Taurus" => "ரிஷபம்",
                    "Gemini" => "மிதுனம்",
                    "Cancer" => "கடகம்",
                    "Leo" => "சிம்மம்",
                    "Virgo" => "கன்னி",
                    "Libra" => "துலாம்",
                    "Scorpio" => "விருச்சிகம்",
                    "Sagittarius" => "தனுசு",
                    "Capricorn" => "மகரம்",
                    "Aquarius" => "கும்பம்",
                    "Pisces" => "மீனம்",
                    // Abbreviations (2-letter)
                    "Ar" => "மேஷ",
                    "Ta" => "ரிஷ",
                    "Ge" => "மிது",
                    "Cn" => "கட",
                    "Le" => "சிம்",
                    "Vi" => "கன்",
                    "Li" => "துலா",
                    "Sc" => "விரு",
                    "Sg" => "தனு",
                    "Cp" => "மக",
                    "Aq" => "கும்",
                    "Pi" => "மீன",
                    _ => englishName
                };
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
