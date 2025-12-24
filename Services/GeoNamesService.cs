using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

public class GeoNamesService
{
    private const string DataFileName = "Data/cities.txt";
    private string _dataFilePath;
    private bool _isDataLoaded = false;
    private List<GeoLocation> _cachedLocations = new List<GeoLocation>();
    
    // Maintain HttpClient just in case we switch back or for mixed usage
    private readonly HttpClient _httpClient;

    public GeoNamesService()
    {
        _httpClient = new HttpClient();
        _dataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DataFileName);
    }

    /// <summary>
    /// Search for places by name using local data dump.
    /// Expects standard GeoNames export format (tab separated).
    /// </summary>
    public async Task<List<GeoLocation>> SearchPlaceAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<GeoLocation>();
        
        // Load data if not already loaded
        if (!_isDataLoaded)
        {
            await LoadDataAsync();
        }

        if (_cachedLocations == null || _cachedLocations.Count == 0) return new List<GeoLocation>();

        // Perform search (case-insensitive, checks name, ascii name, and alternates)
        return _cachedLocations
            .Where(l => l.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase) || 
                        l.AsciiName.StartsWith(query, StringComparison.OrdinalIgnoreCase) ||
                        l.AlternateNames.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        l.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(20)
            .ToList();
    }

    private async Task LoadDataAsync()
    {
        if (!File.Exists(_dataFilePath))
        {
            // Try searching in common fallback locations if not in BaseDirectory
            string fallback = Path.Combine(Environment.CurrentDirectory, DataFileName);
            if (File.Exists(fallback)) 
            {
                 _dataFilePath = fallback;
            }
            else
            {
                 throw new FileNotFoundException($"City data file not found at: {_dataFilePath}\nPlease ensure 'Data/cities.txt' exists.");
            }
        }

        var lines = await File.ReadAllLinesAsync(_dataFilePath);
        _cachedLocations = new List<GeoLocation>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split('\t');
            
            // GeoNames cities15000 format expected:
            // 0: geonameid, 1: name, 2: asciiname, 3: alternatenames, 4: lat, 5: lon, ..., 8: country, ..., 17: timezone
            
            if (parts.Length < 9) continue; // Minimum required columns

            var loc = new GeoLocation
            {
                Name = parts[1],
                AsciiName = parts.Length > 2 ? parts[2] : parts[1],
                AlternateNames = parts.Length > 3 ? parts[3] : "",
                Lat = parts[4],
                Lng = parts[5],
                CountryName = parts[8]                    
            };
            
            // Timezone is at index 17 usually
            if (parts.Length > 17)
            {
                loc.Timezone = new GeoTimezone { TimeZoneId = parts[17] };
            }

            _cachedLocations.Add(loc);
        }
        
        _isDataLoaded = true;
    }

    /// <summary>
    /// Get Timezone info - for local dump we use what's in the file or default
    /// </summary>
    /// <summary>
    /// Get Timezone info - for local dump we use what's in the file or default
    /// </summary>
    public double GetTimezoneOffset(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId)) return 5.5;

        try
        {
            // Try standard lookup (Works for IANA IDs on newer .NET/Windows)
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return tz.GetUtcOffset(DateTime.Now).TotalHours;
        }
        catch (TimeZoneNotFoundException)
        {
            // Fallback mapping for common IDs on Windows where IANA might fail
            if (timeZoneId.Equals("Asia/Kolkata", StringComparison.OrdinalIgnoreCase) || 
                timeZoneId.Equals("Asia/Calcutta", StringComparison.OrdinalIgnoreCase))
            {
                return 5.5;
            }
            if (timeZoneId.StartsWith("Europe/London" )) return 0;
            if (timeZoneId.StartsWith("America/New_York")) return -5; // Approx, DST varies
            
            // Try to find by standard name matching
            try 
            {
               // Attempt to map "Asia/Kolkata" -> "India Standard Time" in a general way?
               // For now, return default if unknown
               return 5.5; 
            }
            catch { return 5.5; }
        }
        catch
        {
            return 5.5;
        }
    }
}
