using SwissEphNet;
using JamakolAstrology.Models;
using System.IO;

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
            // User's known location first
            @"C:\laragon\www\jamakol-desktop-app\JamakolAstrology\JamakolAstrology\ephe",
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ephe"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "ephe"),
            Path.Combine(Environment.CurrentDirectory, "ephe"),
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
    /// Get planet position at given Julian Day
    /// </summary>
    public (double longitude, double latitude, double speed) GetPlanetPosition(double julianDay, int planetId, int ayanamshaId)
    {
        double[] result = new double[6];
        string errorMsg = "";
        
        int flags = SwissEph.SEFLG_SWIEPH | SwissEph.SEFLG_SPEED;
        
        // Use sidereal (Vedic) zodiac with selected ayanamsa
        flags |= SwissEph.SEFLG_SIDEREAL;
        _sweph.swe_set_sid_mode(ayanamshaId, 0, 0);
        
        int retVal = _sweph.swe_calc_ut(julianDay, planetId, flags, result, ref errorMsg);
        
        if (retVal < 0)
        {
            throw new Exception($"Error calculating planet position: {errorMsg}");
        }
        
        return (result[0], result[1], result[3]);
    }

    /// <summary>
    /// Get Rahu (Mean Node) position
    /// </summary>
    public (double longitude, double latitude, double speed) GetRahuPosition(double julianDay, int ayanamshaId)
    {
        return GetPlanetPosition(julianDay, SwissEph.SE_MEAN_NODE, ayanamshaId);
    }

    /// <summary>
    /// Calculate Ketu position (opposite to Rahu)
    /// </summary>
    public (double longitude, double latitude, double speed) GetKetuPosition(double julianDay, int ayanamshaId)
    {
        var rahu = GetRahuPosition(julianDay, ayanamshaId);
        double ketuLongitude = ZodiacUtils.NormalizeDegree(rahu.longitude + 180);
        return (ketuLongitude, -rahu.latitude, rahu.speed);
    }

    /// <summary>
    /// Calculate Ascendant (Lagna) at given time and location
    /// </summary>
    public double GetAscendant(double julianDay, double latitude, double longitude, int ayanamshaId)
    {
        double[] cusps = new double[13];
        double[] ascmc = new double[10];
        
        // Use sidereal zodiac
        _sweph.swe_set_sid_mode(ayanamshaId, 0, 0);
        
        int retVal = _sweph.swe_houses_ex(
            julianDay,
            SwissEph.SEFLG_SIDEREAL,
            latitude,
            longitude,
            'P',  // Placidus house system
            cusps,
            ascmc
        );
        
        return ascmc[0]; // Ascendant
    }

    /// <summary>
    /// Get ayanamsa value for given Julian Day
    /// </summary>
    public double GetAyanamsa(double julianDay, int ayanamshaId)
    {
        _sweph.swe_set_sid_mode(ayanamshaId, 0, 0);
        return _sweph.swe_get_ayanamsa_ut(julianDay);
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
