using SwissEphNet;
using JamakolAstrology.Models;
using System.IO;
using System.Linq;

namespace JamakolAstrology.Services;

/// <summary>
/// Swiss Ephemeris wrapper for planetary calculations
/// </summary>
public class EphemerisService : IDisposable
{
    private readonly SwissEph _sweph;
    private bool _disposed;

    private string? _ephePath;

    public EphemerisService()
    {
        _sweph = new SwissEph();
        
        // Search for ephemeris files in common locations
        string[] searchPaths = new[]
        {
            // Executable directory (production/build output)
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ephe"),
            
            // Current working directory
            Path.Combine(Environment.CurrentDirectory, "ephe"),
            
            // Resource folder in base directory
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "ephe"),
            
            // Development project root (look up from bin\Debug\netX.X)
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "ephe")),
            
            "ephe"
        };

        foreach (var path in searchPaths)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                if (Directory.Exists(fullPath) && File.Exists(Path.Combine(fullPath, "sefstars.txt")))
                {
                    _ephePath = fullPath;
                    if (!_ephePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    {
                        _ephePath += Path.DirectorySeparatorChar;
                    }
                    _sweph.swe_set_ephe_path(_ephePath);
                    break;
                }
            }
            catch
            {
                // Ignore path errors
            }
        }

        // Register custom file loader for SwissEphNet
        _sweph.OnLoadFile += (sender, e) =>
        {
            if (_ephePath != null)
            {
                var filePath = Path.Combine(_ephePath, e.FileName);
                if (File.Exists(filePath))
                {
                    e.File = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
            }
        };
    }

    /// <summary>
    /// Calculate Julian Day for a given UTC date/time
    /// </summary>
    public double GetJulianDay(DateTime utcDateTime)
    {
        double hour = utcDateTime.Hour + utcDateTime.Minute / 60.0 + utcDateTime.Second / 3600.0;
        return _sweph.swe_julday(
            utcDateTime.Year,
            utcDateTime.Month,
            utcDateTime.Day,
            hour,
            SwissEph.SE_GREG_CAL
        );
    }

    /// <summary>
    /// Calculate Julian Day from individual date components
    /// Supports BC dates (negative years using astronomical year numbering)
    /// Year 0 = 1 BCE, Year -1 = 2 BCE, etc.
    /// Uses Julian calendar for dates before Oct 15, 1582 (when Gregorian was introduced)
    /// </summary>
    public double GetJulianDay(int year, int month, int day, double hour)
    {
        // Use Julian calendar for dates before October 15, 1582
        // Gregorian calendar didn't exist before then
        int calendarType = (year < 1582 || (year == 1582 && month < 10) || (year == 1582 && month == 10 && day < 15))
            ? SwissEph.SE_JUL_CAL
            : SwissEph.SE_GREG_CAL;
            
        return _sweph.swe_julday(
            year,
            month,
            day,
            hour,
            calendarType
        );
    }


    /// <summary>
    /// Get planet position at given Julian Day
    /// </summary>
    public (double longitude, double latitude, double speed) GetPlanetPosition(double julianDay, int planetId, int ayanamshaId, double ayanamshaOffset = 0)
    {
        double[] result = new double[6];
        string errorMsg = "";
        
        int flags = SwissEph.SEFLG_SWIEPH | SwissEph.SEFLG_SPEED;
        
        if (ayanamshaOffset == 0)
        {
            // Standard behavior: Use sidereal (Vedic) zodiac with selected ayanamsa
            flags |= SwissEph.SEFLG_SIDEREAL;
            _sweph.swe_set_sid_mode(ayanamshaId, 0, 0);
            
            int retVal = _sweph.swe_calc_ut(julianDay, planetId, flags, result, ref errorMsg);
            if (retVal < 0) throw new Exception($"Error calculating planet position: {errorMsg}");
            
            return (result[0], result[1], result[3]);
        }
        else
        {
            // Manual offset calculation: Get Tropical position then subtract (Ayanamsha + Offset)
            // Do NOT set SEFLG_SIDEREAL
            
            int retVal = _sweph.swe_calc_ut(julianDay, planetId, flags, result, ref errorMsg);
            if (retVal < 0) throw new Exception($"Error calculating planet position: {errorMsg}");
            
            double tropicalLongitude = result[0];
            double standardAyanamsha = GetAyanamsa(julianDay, ayanamshaId); // Note: This calls the base version without offset
            double totalAyanamsha = standardAyanamsha + ayanamshaOffset;
            
            double siderealLongitude = ZodiacUtils.NormalizeDegree(tropicalLongitude - totalAyanamsha);
            
            return (siderealLongitude, result[1], result[3]);
        }
    }

    /// <summary>
    /// Get Rahu (Mean Node) position
    /// </summary>
    public (double longitude, double latitude, double speed) GetRahuPosition(double julianDay, int ayanamshaId, double ayanamshaOffset = 0)
    {
        return GetPlanetPosition(julianDay, SwissEph.SE_MEAN_NODE, ayanamshaId, ayanamshaOffset);
    }

    /// <summary>
    /// Calculate Ketu position (opposite to Rahu)
    /// </summary>
    public (double longitude, double latitude, double speed) GetKetuPosition(double julianDay, int ayanamshaId, double ayanamshaOffset = 0)
    {
        var rahu = GetRahuPosition(julianDay, ayanamshaId, ayanamshaOffset);
        double ketuLongitude = ZodiacUtils.NormalizeDegree(rahu.longitude + 180);
        return (ketuLongitude, -rahu.latitude, rahu.speed);
    }

    /// <summary>
    /// Calculate Ascendant (Lagna) at given time and location
    /// </summary>
    public double GetAscendant(double julianDay, double latitude, double longitude, int ayanamshaId, double ayanamshaOffset = 0)
    {
        double[] cusps = new double[13];
        double[] ascmc = new double[10];
        
        int flags = 0;
        
        if (ayanamshaOffset == 0)
        {
            flags = SwissEph.SEFLG_SIDEREAL;
            _sweph.swe_set_sid_mode(ayanamshaId, 0, 0);
        }
        // Else: Tropical (flags = 0)
        
        int retVal = _sweph.swe_houses_ex(
            julianDay,
            flags,
            latitude,
            longitude,
            'P',  // Placidus house system
            cusps,
            ascmc
        );
        
        double ascendant = ascmc[0];
        
        if (ayanamshaOffset != 0)
        {
            double standardAyanamsha = GetAyanamsa(julianDay, ayanamshaId);
            double totalAyanamsha = standardAyanamsha + ayanamshaOffset;
            ascendant = ZodiacUtils.NormalizeDegree(ascendant - totalAyanamsha);
        }
        
        return ascendant; 
    }

    /// <summary>
    /// Get all 12 House Cusps with specified house system
    /// </summary>
    public double[] GetHouses(double julianDay, double latitude, double longitude, int ayanamshaId, char houseSystem, double ayanamshaOffset = 0)
    {
        double[] cusps = new double[13];
        double[] ascmc = new double[10];

        int flags = 0;
        if (ayanamshaOffset == 0)
        {
            flags = SwissEph.SEFLG_SIDEREAL;
            _sweph.swe_set_sid_mode(ayanamshaId, 0, 0);
        }

        _sweph.swe_houses_ex(
            julianDay,
            flags,
            latitude,
            longitude,
            houseSystem,  // House system code: P=Placidus, K=Koch, O=Porphyry, etc.
            cusps,
            ascmc
        );

        // Cusps are at indices 1-12
        var resultCusps = cusps.Skip(1).Take(12).ToArray();

        if (ayanamshaOffset != 0)
        {
            double standardAyanamsha = GetAyanamsa(julianDay, ayanamshaId);
            double totalAyanamsha = standardAyanamsha + ayanamshaOffset;
            
            for (int i = 0; i < resultCusps.Length; i++)
            {
                resultCusps[i] = ZodiacUtils.NormalizeDegree(resultCusps[i] - totalAyanamsha);
            }
        }
        
        return resultCusps;
    }

    /// <summary>
    /// Get ayanamsa value for given Julian Day
    /// </summary>
    public double GetAyanamsa(double julianDay, int ayanamshaId, double ayanamshaOffset = 0)
    {
        _sweph.swe_set_sid_mode(ayanamshaId, 0, 0);
        // Returns the standard Ayanamsha (positive value typically)
        // Sidereal = Tropical - Ayanamsha
        return _sweph.swe_get_ayanamsa_ut(julianDay) + ayanamshaOffset;
    }

    /// <summary>
    /// Get Local Sidereal Time in hours
    /// </summary>
    public double GetSiderealTime(double julianDay, double longitude)
    {
        // GMST in hours
        double gmst = _sweph.swe_sidtime(julianDay);
        
        // Add longitude offset (longitude is in degrees, convert to hours: 15 deg = 1 hour)
        double offset = longitude / 15.0;
        
        double lst = gmst + offset;
        
        // Normalize to 0-24
        while (lst < 0) lst += 24.0;
        while (lst >= 24) lst -= 24.0;
        
        return lst;
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

    /// <summary>
    /// Convert Julian Day to DateTime (UTC)
    /// Returns DateTime.MinValue for dates before 0001-01-01 CE (JD ~1721425.5)
    /// Returns DateTime.MaxValue for dates after 9999-12-31 CE (JD ~5373484.5)
    /// </summary>
    public static DateTime JulianDateToDateTime(double julianDay)
    {
        // DateTime.MinValue (0001-01-01 00:00:00) corresponds to JD ~1721425.5
        // DateTime.MaxValue (9999-12-31 23:59:59) corresponds to JD ~5373484.5
        const double MinJulianDay = 1721425.5;  // 0001-01-01 CE
        const double MaxJulianDay = 5373484.5;  // 9999-12-31 CE
        
        if (julianDay < MinJulianDay)
            return DateTime.MinValue;
        if (julianDay > MaxJulianDay)
            return DateTime.MaxValue;
            
        // Julian Day 2440587.5 is 1970-01-01 00:00:00 UTC
        double unixTime = (julianDay - 2440587.5) * 86400.0;
        return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTime);
    }
}
