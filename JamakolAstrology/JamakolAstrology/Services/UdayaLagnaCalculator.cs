using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Calculates Udaya Lagna (Rising Sign/Ascendant) based on sunrise/sunset times
/// </summary>
public class UdayaLagnaCalculator
{
    private readonly DateTime _todaySunrise;
    private readonly DateTime _todaySunset;
    private readonly DateTime _tomorrowSunrise;
    private readonly double _sunriseAscendant;
    private readonly double _sunsetAscendant;

    private readonly double _daySeconds;
    private readonly double _nightSeconds;
    private readonly double _daySecondsPerDegree;
    private readonly double _nightSecondsPerDegree;

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

    /// <summary>
    /// Initialize the calculator with sunrise/sunset data
    /// </summary>
    /// <param name="todaySunrise">Today's sunrise time</param>
    /// <param name="todaySunset">Today's sunset time</param>
    /// <param name="tomorrowSunrise">Tomorrow's sunrise time</param>
    /// <param name="sunriseAscendant">Ascendant longitude at sunrise (0-360)</param>
    /// <param name="sunsetAscendant">Ascendant longitude at sunset (0-360)</param>
    public UdayaLagnaCalculator(
        DateTime todaySunrise,
        DateTime todaySunset,
        DateTime tomorrowSunrise,
        double sunriseAscendant,
        double sunsetAscendant)
    {
        _todaySunrise = todaySunrise;
        _todaySunset = todaySunset;
        _tomorrowSunrise = tomorrowSunrise;
        _sunriseAscendant = sunriseAscendant;
        _sunsetAscendant = sunsetAscendant;

        // Calculate day and night durations in seconds
        _daySeconds = (_todaySunset - _todaySunrise).TotalSeconds;
        _nightSeconds = (_tomorrowSunrise - _todaySunset).TotalSeconds;

        // Calculate seconds per degree (360 degrees in a full rotation)
        _daySecondsPerDegree = _daySeconds / 360.0;
        _nightSecondsPerDegree = _nightSeconds / 360.0;
    }

    /// <summary>
    /// Create calculator with automatic ascendant calculation
    /// Ascendant at sunrise = Sun's position (same sign)
    /// Ascendant at sunset = Sun's position + 180° (opposite sign)
    /// </summary>
    public static UdayaLagnaCalculator CreateWithSunPosition(
        DateTime todaySunrise,
        DateTime todaySunset,
        DateTime tomorrowSunrise,
        double sunLongitude)
    {
        // At sunrise, ascendant is roughly where the Sun is
        double sunriseAscendant = sunLongitude;
        
        // At sunset, ascendant is roughly opposite to the Sun (180° ahead)
        double sunsetAscendant = (sunLongitude + 180.0) % 360.0;

        return new UdayaLagnaCalculator(
            todaySunrise,
            todaySunset,
            tomorrowSunrise,
            sunriseAscendant,
            sunsetAscendant);
    }

    /// <summary>
    /// Calculate Udaya Lagna (Ascendant) for a given time
    /// </summary>
    public SpecialPoint CalculateUdayam(DateTime time)
    {
        double udayaLagna = CalculateUdayaLagnaAtTime(time);

        int signIndex = (int)(udayaLagna / 30);
        if (signIndex >= 12) signIndex = 11;
        if (signIndex < 0) signIndex = 0;

        double degreeInSign = udayaLagna - (signIndex * 30);

        // Calculate nakshatra
        double nakshatraSize = 360.0 / 27.0;
        int nakshatraIndex = (int)(udayaLagna / nakshatraSize);
        if (nakshatraIndex >= 27) nakshatraIndex = 26;
        if (nakshatraIndex < 0) nakshatraIndex = 0;

        double offsetInNak = udayaLagna - (nakshatraIndex * nakshatraSize);
        int pada = (int)(offsetInNak / (nakshatraSize / 4)) + 1;
        if (pada > 4) pada = 4;
        if (pada < 1) pada = 1;

        return new SpecialPoint
        {
            Name = "Udayam",
            Symbol = "UD",
            Sign = SignNames[signIndex],
            SignIndex = signIndex,
            DegreeInSign = degreeInSign,
            AbsoluteLongitude = udayaLagna,
            NakshatraName = NakshatraNames[nakshatraIndex],
            Pada = pada
        };
    }

    /// <summary>
    /// Calculate Udaya Lagna longitude for a given time
    /// </summary>
    private double CalculateUdayaLagnaAtTime(DateTime time)
    {
        double udayaLagna;

        // Check if time is during day or night
        if (time >= _todaySunrise && time <= _todaySunset)
        {
            // During day
            double elapsedSeconds = (time - _todaySunrise).TotalSeconds;
            double degreesElapsed = elapsedSeconds / _daySecondsPerDegree;
            udayaLagna = _sunriseAscendant + degreesElapsed;
        }
        else
        {
            // During night
            double elapsedSeconds = (time - _todaySunset).TotalSeconds;
            double degreesElapsed = elapsedSeconds / _nightSecondsPerDegree;
            udayaLagna = _sunsetAscendant + degreesElapsed;
        }

        // Normalize to 0-360
        udayaLagna %= 360;
        if (udayaLagna < 0) udayaLagna += 360;

        return udayaLagna;
    }

    /// <summary>
    /// Get day duration in hours
    /// </summary>
    public double DayHours => _daySeconds / 3600.0;

    /// <summary>
    /// Get night duration in hours
    /// </summary>
    public double NightHours => _nightSeconds / 3600.0;
}
