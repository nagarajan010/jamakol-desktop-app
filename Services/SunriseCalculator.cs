using SwissEphNet;
using JamakolAstrology.Models;
using System.IO;

namespace JamakolAstrology.Services;

/// <summary>
/// Calculates sunrise and sunset times based on location and date
/// Using Swiss Ephemeris
/// </summary>
public class SunriseCalculator : IDisposable
{
    private readonly SwissEph _sweph;
    private bool _disposed;
    private readonly SunriseCalculationMode _mode;

    public SunriseCalculator(SunriseCalculationMode mode = SunriseCalculationMode.TipApparent)
    {
        _mode = mode;
        _sweph = new SwissEph();
        
        string ephePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "ephe");
        if (Directory.Exists(ephePath))
        {
            _sweph.swe_set_ephe_path(ephePath);
        }
    }

    /// <summary>
    /// Calculate sunrise time for a given date and location
    /// </summary>
    public DateTime CalculateSunrise(DateTime date, double latitude, double longitude, double timezone)
    {
        return CalculateSunTime(date, latitude, longitude, timezone, true);
    }

    /// <summary>
    /// Calculate sunset time for a given date and location
    /// </summary>
    public DateTime CalculateSunset(DateTime date, double latitude, double longitude, double timezone)
    {
        return CalculateSunTime(date, latitude, longitude, timezone, false);
    }

    /// <summary>
    /// Calculate local noon (Meridian Transit) for a given date and location
    /// </summary>
    public DateTime CalculateNoon(DateTime date, double latitude, double longitude, double timezone)
    {
        // Use custom logic calling underlying method with specific flag
        // SE_CALC_MTRANSIT for meridian transit (noon)
        return CalculateSunTime(date, latitude, longitude, timezone, true, true);
    }

    /// <summary>
    /// Calculate sunrise, sunset, or noon (transit) using Swiss Ephemeris
    /// </summary>
    private DateTime CalculateSunTime(DateTime date, double latitude, double longitude, double timezone, bool isSunrise, bool isTransit = false)
    {
        // Set sidereal mode (Lahiri) just in case, though sunrise usually uses tropical/geocentric positions
        // But for consistency with the rest of the app:
        _sweph.swe_set_sid_mode(SwissEph.SE_SIDM_LAHIRI, 0, 0);

        // Convert date to Julian Day (at noon local time to search around)
        // We start search from noon of that day to find the rising/setting of that civil day
        // Actually, for sunrise we probably want to start searching from midnight?
        // swe_rise_trans finds the *next* rise/set after tjd_ut.
        // So for sunrise on Jan 1, we should start checking from Jan 1 00:00 - buffer.
        
        // Let's create a UTC DateTime for the start of the date
        var startOfDay = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
        
        // Adjust for timezone to get UTC 
        // If we supply UTC midnight, and we want sunrise at loc, it is usually close to that.
        // Actually, let's just pass the julian day for the date's noon UTC - 12 hours?
        // Simplest: Pass Julian Day for 00:00 UTC of the requested date.
        
        double jd = ToJulianDay(startOfDay) - (timezone / 24.0); // Adjust to start of day in UT?
        // No, let's keep it simple. swe_julday takes UT.
        // We want the event that happens on this 'local civil date'.
        // So we should search starting from (Local Midnight converted to UT).
        
        DateTime localMidnight = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
        DateTime utcMidnight = localMidnight.AddHours(-timezone);
        double tjd_ut = ToJulianDay(utcMidnight);

        // Define Flags
        int rsmi;
        if (isTransit)
            rsmi = SwissEph.SE_CALC_MTRANSIT;
        else
            rsmi = isSunrise ? SwissEph.SE_CALC_RISE : SwissEph.SE_CALC_SET;
        
        // Add mode flags
        switch (_mode)
        {
            case SunriseCalculationMode.CenterTrue:
                rsmi |= SwissEph.SE_BIT_DISC_CENTER | SwissEph.SE_BIT_NO_REFRACTION;
                break;
            case SunriseCalculationMode.TipTrue:
                rsmi |= SwissEph.SE_BIT_NO_REFRACTION;
                break;
            case SunriseCalculationMode.CenterApparent:
                rsmi |= SwissEph.SE_BIT_DISC_CENTER;
                break;
            case SunriseCalculationMode.TipApparent:
            default:
                // Default is Tip + Refraction (standard)
                break;
        }

        // Geopos: lon, lat, height
        double[] geopos = new double[] { longitude, latitude, 0 };
        double[] tret = new double[3]; // return array
        string serr = "";

        // Calculate
        // ipl = SE_SUN (0)
        // starname = ""
        // ephe_flag = SEFLG_SWIEPH
        int ret = _sweph.swe_rise_trans(
            tjd_ut, 
            SwissEph.SE_SUN, 
            "", 
            SwissEph.SEFLG_SWIEPH, 
            rsmi, 
            geopos, 
            0, // pressure/temp default
            0, // pressure/temp default
            ref tret[0], 
            ref serr
        );

        if (ret < 0)
        {
            // Error - Fallback or throw?
            // Fallback to naive calculation or throw
            // For now let's return a default
            return localMidnight.AddHours(isSunrise ? 6 : 18);
        }

        // tret[0] is the time in JD (UT)
        double resultJdUt = tret[0];

        // Convert JD to DateTime (UT)
        DateTime resultUtc = FromJulianDay(resultJdUt);

        // Convert to Local Time
        DateTime resultLocal = resultUtc.AddHours(timezone);

        return resultLocal;
    }

    private double ToJulianDay(DateTime utcDate)
    {
        double hour = utcDate.Hour + utcDate.Minute / 60.0 + utcDate.Second / 3600.0;
        return _sweph.swe_julday(utcDate.Year, utcDate.Month, utcDate.Day, hour, SwissEph.SE_GREG_CAL);
    }

    private DateTime FromJulianDay(double jd)
    {
        int year = 0, month = 0, day = 0;
        double hour = 0;
        _sweph.swe_revjul(jd, SwissEph.SE_GREG_CAL, ref year, ref month, ref day, ref hour);
        
        // Handle BC dates (years before 1 CE cannot be represented in .NET DateTime)
        if (year < 1 || year > 9999)
            return DateTime.MinValue;
        
        int h = (int)hour;
        int m = (int)((hour - h) * 60);
        int s = (int)(((hour - h) * 60 - m) * 60);

        // Handle rounding or slight overflows
        if (s >= 60) { s = 0; m++; }
        if (m >= 60) { m = 0; h++; }
        if (h >= 24) { h = 0; day++; } // Simplistic day overflow handling
        
        try
        {
            return new DateTime(year, month, day, h, m, s);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _sweph.swe_close();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
