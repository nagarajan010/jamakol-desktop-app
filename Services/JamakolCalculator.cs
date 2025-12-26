using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Jamakol (ஜாமக்கோள்) calculation service
/// Calculates special Tamil astrological numbers based on planetary positions
/// </summary>
public class JamakolCalculator
{
    // Jamakol base values for each planet
    private static readonly Dictionary<Planet, int> PlanetBaseValues = new()
    {
        { Planet.Sun, 6 },
        { Planet.Moon, 15 },
        { Planet.Mars, 8 },
        { Planet.Mercury, 17 },
        { Planet.Jupiter, 19 },
        { Planet.Venus, 21 },
        { Planet.Saturn, 10 },
        { Planet.Rahu, 18 },
        { Planet.Ketu, 7 }
    };

    // Jamakol Tamil names for planets
    public static readonly Dictionary<Planet, string> PlanetTamilNames = new()
    {
        { Planet.Sun, "சூரியன்" },
        { Planet.Moon, "சந்திரன்" },
        { Planet.Mars, "செவ்வாய்" },
        { Planet.Mercury, "புதன்" },
        { Planet.Jupiter, "குரு" },
        { Planet.Venus, "சுக்கிரன்" },
        { Planet.Saturn, "சனி" },
        { Planet.Rahu, "ராகு" },
        { Planet.Ketu, "கேது" }
    };

    // Jamakol Tamil names for zodiac signs
    public static readonly string[] SignTamilNames = 
    {
        "", "மேஷம்", "ரிஷபம்", "மிதுனம்", "கடகம்", "சிம்மம்", "கன்னி",
        "துலாம்", "விருச்சிகம்", "தனுசு", "மகரம்", "கும்பம்", "மீனம்"
    };

    /// <summary>
    /// Calculate Jamakol data from chart data
    /// </summary>
    public JamakolData Calculate(ChartData chartData)
    {
        var jamakolData = new JamakolData
        {
            ChartData = chartData
        };

        // Calculate Jamakol numbers for each house
        for (int house = 1; house <= 12; house++)
        {
            int jamakolValue = CalculateHouseValue(chartData, house);
            jamakolData.HouseValues[house] = jamakolValue;
        }

        // Calculate special points
        jamakolData.LagnaValue = CalculateLagnaValue(chartData);
        jamakolData.SuryaValue = CalculateSuryaValue(chartData);
        jamakolData.ChandraValue = CalculateChandraValue(chartData);

        // Calculate planetary Jamakol positions
        foreach (var planet in chartData.Planets)
        {
            var jamakolPlanet = new JamakolPlanetPosition
            {
                Planet = planet.Planet,
                TamilName = PlanetTamilNames[planet.Planet],
                Sign = planet.Sign,
                SignTamilName = SignTamilNames[planet.Sign],
                Degree = planet.Longitude,
                DegreeInSign = planet.DegreeInSign,
                Nakshatra = planet.Nakshatra,
                NakshatraTamilName = ZodiacUtils.NakshatraNamesTamil[planet.Nakshatra],
                NakshatraPada = planet.NakshatraPada,
                IsRetrograde = planet.IsRetrograde,
                IsCombust = planet.IsCombust,
                JamakolValue = CalculatePlanetJamakolValue(planet),
                Gati = planet.Gati
            };
            jamakolData.PlanetPositions.Add(jamakolPlanet);
        }

        return jamakolData;
    }

    /// <summary>
    /// Calculate Jamakol value for a house based on planets present
    /// </summary>
    private int CalculateHouseValue(ChartData chartData, int house)
    {
        int total = 0;
        var planetsInHouse = chartData.GetPlanetsInHouse(house);

        foreach (var planet in planetsInHouse)
        {
            total += PlanetBaseValues[planet.Planet];
            // Add degree influence
            total += (int)(planet.DegreeInSign / 3);
        }

        // Add house base value
        total += GetHouseBaseValue(house);

        return total;
    }

    /// <summary>
    /// Get base value for each house
    /// </summary>
    private int GetHouseBaseValue(int house)
    {
        return house switch
        {
            1 => 5,
            2 => 7,
            3 => 3,
            4 => 6,
            5 => 9,
            6 => 4,
            7 => 8,
            8 => 2,
            9 => 11,
            10 => 10,
            11 => 12,
            12 => 1,
            _ => 0
        };
    }

    /// <summary>
    /// Calculate planet's individual Jamakol value
    /// </summary>
    private int CalculatePlanetJamakolValue(PlanetPosition planet)
    {
        int baseValue = PlanetBaseValues[planet.Planet];
        int degreeInfluence = (int)(planet.DegreeInSign / 5);
        int nakshatraInfluence = planet.Nakshatra % 9;
        
        return baseValue + degreeInfluence + nakshatraInfluence;
    }

    /// <summary>
    /// Calculate Lagna (Ascendant) Jamakol value
    /// </summary>
    private int CalculateLagnaValue(ChartData chartData)
    {
        int ascSign = chartData.AscendantSign;
        double degree = chartData.AscendantDegree % 30;
        return ascSign * 3 + (int)(degree / 2.5);
    }

    /// <summary>
    /// Calculate Surya (Sun) Jamakol value
    /// </summary>
    private int CalculateSuryaValue(ChartData chartData)
    {
        var sun = chartData.Planets.FirstOrDefault(p => p.Planet == Planet.Sun);
        if (sun == null) return 0;
        return (int)(sun.Longitude / 10) + sun.Sign;
    }

    /// <summary>
    /// Calculate Chandra (Moon) Jamakol value
    /// </summary>
    private int CalculateChandraValue(ChartData chartData)
    {
        var moon = chartData.Planets.FirstOrDefault(p => p.Planet == Planet.Moon);
        if (moon == null) return 0;
        return (int)(moon.Longitude / 10) + moon.Nakshatra;
    }
}


