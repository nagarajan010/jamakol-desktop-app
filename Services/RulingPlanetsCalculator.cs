using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Calculator for Ruling Planets (RP) at a given moment
/// Used in KP Horary astrology
/// </summary>
public class RulingPlanetsCalculator
{
    private readonly KPCalculator _kpCalculator = new();
    
    // Day lords (0=Sunday, 6=Saturday)
    private static readonly string[] DayLords = 
    { 
        "Sun", "Moon", "Mars", "Mercury", "Jupiter", "Venus", "Saturn" 
    };
    
    /// <summary>
    /// Calculate ruling planets for the given chart/moment
    /// </summary>
    public RulingPlanets Calculate(ChartData chart)
    {
        var rp = new RulingPlanets
        {
            JudgmentTime = chart.BirthData.BirthDateTime
        };
        
        // Get Lagna (Ascendant) KP lords
        if (chart.HouseCusps != null && chart.HouseCusps.Count > 0)
        {
            var lagnaKP = chart.HouseCusps[0].KpDetails;
            rp.LagnaSignLord = lagnaKP.SignLord;
            rp.LagnaStarLord = lagnaKP.StarLord;
            rp.LagnaSubLord = lagnaKP.SubLord;
        }
        
        // Get Moon KP lords
        var moon = chart.Planets.FirstOrDefault(p => p.Planet == Planet.Moon);
        if (moon != null)
        {
            rp.MoonSignLord = moon.KpDetails.SignLord;
            rp.MoonStarLord = moon.KpDetails.StarLord;
            rp.MoonSubLord = moon.KpDetails.SubLord;
        }
        
        // Get Day Lord (Vara)
        int dayOfWeek = (int)chart.BirthData.BirthDateTime.DayOfWeek;
        rp.DayLord = DayLords[dayOfWeek];
        
        // Combine and count occurrences
        var planetCounts = new Dictionary<string, List<string>>();
        
        void AddPlanet(string planet, string source)
        {
            if (string.IsNullOrEmpty(planet)) return;
            if (!planetCounts.ContainsKey(planet))
                planetCounts[planet] = new List<string>();
            planetCounts[planet].Add(source);
        }
        
        AddPlanet(rp.LagnaSignLord, "Lagna Sign");
        AddPlanet(rp.LagnaStarLord, "Lagna Star");
        AddPlanet(rp.LagnaSubLord, "Lagna Sub");
        AddPlanet(rp.MoonSignLord, "Moon Sign");
        AddPlanet(rp.MoonStarLord, "Moon Star");
        AddPlanet(rp.MoonSubLord, "Moon Sub");
        AddPlanet(rp.DayLord, "Day Lord");
        
        // Sort by count (descending) then alphabetically
        rp.CombinedRulers = planetCounts
            .OrderByDescending(kv => kv.Value.Count)
            .ThenBy(kv => kv.Key)
            .Select(kv => new RulingPlanetEntry
            {
                Planet = kv.Key,
                Count = kv.Value.Count,
                Sources = string.Join(", ", kv.Value)
            })
            .ToList();
        
        return rp;
    }
    
    /// <summary>
    /// Calculate ruling planets for current moment (requires ephemeris calculation)
    /// </summary>
    public RulingPlanets CalculateForNow(double latitude, double longitude, double timezone, int ayanamshaId, double ayanamshaOffset)
    {
        using var calculator = new ChartCalculator();
        
        var birthData = new BirthData
        {
            BirthDateTime = DateTime.Now,
            Latitude = latitude,
            Longitude = longitude,
            TimeZoneOffset = timezone,
            Location = "Current Location"
        };
        
        var chart = calculator.CalculateChart(birthData, (AyanamshaType)ayanamshaId);
        return Calculate(chart);
    }
}
