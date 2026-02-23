using System;
using System.Collections.Generic;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Service to calculate DNA Astrology details
/// Based on a specific 8-lord sequence repeating through 27 Nakshatras
/// Sequence: Sun, Moon, Mars, Mercury, Jupiter, Venus, Saturn, Rahu
/// </summary>
public class DnaAstrologyService
{
    // The specific 8-Lord sequence for DNA Astrology
    // Note: User specified 7 planets + Rahu = 8 lords.
    // Order of week days from Sunday: Sun, Moon, Mars, Mercury, Jupiter, Venus, Saturn + Rahu
    private static readonly Planet[] DnaLords = 
    { 
        Planet.Sun,      // 1 (Sunday)
        Planet.Moon,     // 2 (Monday)
        Planet.Mars,     // 3 (Tuesday)
        Planet.Mercury,  // 4 (Wednesday)
        Planet.Jupiter,  // 5 (Thursday)
        Planet.Venus,    // 6 (Friday)
        Planet.Saturn,   // 7 (Saturday)
        Planet.Rahu      // 8 (Node)
    };

    /// <summary>
    /// Gets the DNA Lord for a given Nakshatra (1-27)
    /// </summary>
    /// <param name="nakshatraNumber">1-based Nakshatra number</param>
    /// <returns>Planet enum of the DNA Lord</returns>
    public Planet GetDnaLord(int nakshatraNumber)
    {
        if (nakshatraNumber < 1 || nakshatraNumber > 27)
            return Planet.Sun; // Default fallback, though shouldn't happen for valid range

        // 0-based index
        int index = (nakshatraNumber - 1) % 8;
        return DnaLords[index];
    }

    /// <summary>
    /// Calculate DNA details for a chart
    /// </summary>
    public List<DnaRowItem> CalculateDnaDetails(ChartData chart)
    {
        var results = new List<DnaRowItem>();

        if (chart == null) return results;

        // 1. Lagna
        // Recalculate Nakshatra number from degree to be safe and consistent with logic
        int lagnaNakshatra = GetNakshatraNumber(chart.AscendantDegree);
        var lagnaDnaLord = GetDnaLord(lagnaNakshatra);

        // For localization, we use zodiac utils if available, but service should ideally return raw data 
        // and let UI localize. However, DnaRowItem is a view model. 
        // We will return the Planet enum in DnaRowItem so UI can localize it.
        // We also populate string properties for convenience using current culture (though mostly handled in UI)
        
        results.Add(new DnaRowItem
        {
            Body = "Lagna", // "Lagna" string might need localization in UI
            IsLagna = true,
            Longitude = chart.AscendantDegree,
            DegreeDisplay = ZodiacUtils.FormatDegreeInSign(chart.AscendantDegree),
            NakshatraName = ZodiacUtils.GetNakshatraName(lagnaNakshatra),
            NakshatraNumber = lagnaNakshatra,
            DnaLordPlanet = lagnaDnaLord,
            DnaLord = ZodiacUtils.GetPlanetName(lagnaDnaLord)
        });

        // 2. Planets
        foreach (var planet in chart.Planets)
        {
             var dnaLord = GetDnaLord(planet.Nakshatra);
             
            results.Add(new DnaRowItem
            {
                Body = (planet.Planet == Planet.Sun && !planet.Name.Equals("Sun", StringComparison.OrdinalIgnoreCase)) 
                       ? planet.Name 
                       : ZodiacUtils.GetPlanetName(planet.Planet),
                Longitude = planet.Longitude,
                DegreeDisplay = ZodiacUtils.FormatDegreeInSign(planet.Longitude),
                NakshatraName = ZodiacUtils.GetNakshatraName(planet.Nakshatra), // Recalculate/Get localized name
                NakshatraNumber = planet.Nakshatra,
                DnaLordPlanet = dnaLord,
                DnaLord = ZodiacUtils.GetPlanetName(dnaLord)
            });
        }

        return results;
    }

    private int GetNakshatraNumber(double longitude)
    {
        // 360 degrees / 27 nakshatras = 13.3333... degrees per nakshatra
        // or use ZodiacUtils if available
        return ZodiacUtils.DegreeToNakshatra(longitude);
    }

    /// <summary>
    /// Calculate DNA details for House Cusps
    /// </summary>
    public List<DnaRowItem> CalculateHouseDetails(ChartData chart)
    {
        var results = new List<DnaRowItem>();

        if (chart == null || chart.HouseCusps == null) return results;

        foreach (var cusp in chart.HouseCusps)
        {
             // HouseCusp has Degree
             int nakshatra = GetNakshatraNumber(cusp.Degree);
             var dnaLord = GetDnaLord(nakshatra);
             
             results.Add(new DnaRowItem
             {
                 Body = $"House {cusp.HouseNumber}",
                 Longitude = cusp.Degree,
                 DegreeDisplay = ZodiacUtils.FormatDegreeInSign(cusp.Degree),
                 NakshatraName = ZodiacUtils.GetNakshatraName(nakshatra),
                 NakshatraNumber = nakshatra,
                 DnaLordPlanet = dnaLord,
                 DnaLord = ZodiacUtils.GetPlanetName(dnaLord)
             });
        }

        return results;
    }
}

public class DnaRowItem
{
    public string Body { get; set; } = string.Empty;
    public bool IsLagna { get; set; }
    public double Longitude { get; set; }
    public string DegreeDisplay { get; set; } = string.Empty;
    public string NakshatraName { get; set; } = string.Empty;
    public int NakshatraNumber { get; set; }
    public Planet DnaLordPlanet { get; set; }
    public string DnaLord { get; set; } = string.Empty;
    
    // For specific UI coloring
    public string LordColor => DnaLordPlanet == Planet.Rahu ? "#cc0000" : "Black"; 
}
