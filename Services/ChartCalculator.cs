using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Main chart calculation service
/// </summary>
public class ChartCalculator
{
    private readonly EphemerisService _ephemeris;
    private readonly DivisionalChartService _divisionalChartService;

    public ChartCalculator()
    {
        _ephemeris = new EphemerisService();
        _divisionalChartService = new DivisionalChartService();
    }

    /// <summary>
    /// Calculate complete chart from birth data
    /// </summary>
    public ChartData CalculateChart(BirthData birthData, AyanamshaType ayanamsha)
    {
        var chartData = new ChartData
        {
            BirthData = birthData
        };

        // Calculate Julian Day
        chartData.JulianDay = _ephemeris.GetJulianDay(birthData.UtcDateTime);

        // Calculate and store the actual Ayanamsa value for this chart
        chartData.AyanamsaValue = _ephemeris.GetAyanamsa(chartData.JulianDay, (int)ayanamsha);

        // Calculate Ascendant
        chartData.AscendantDegree = _ephemeris.GetAscendant(
            chartData.JulianDay,
            birthData.Latitude,
            birthData.Longitude,
            (int)ayanamsha
        );
        chartData.AscendantSign = ZodiacUtils.DegreeToSign(chartData.AscendantDegree);
        chartData.AscendantSignName = ZodiacUtils.SignNames[chartData.AscendantSign];
        
        // Calculate Ascendant nakshatra info
        var ascNakshatra = ZodiacUtils.GetNakshatraInfo(chartData.AscendantDegree);
        chartData.AscendantNakshatraName = ascNakshatra.name;
        chartData.AscendantNakshatraPada = ascNakshatra.pada;

        // Calculate positions for all planets
        chartData.Planets = new List<PlanetPosition>();

        // Main planets (Sun through Saturn)
        foreach (Planet planet in Enum.GetValues<Planet>())
        {
            if (planet == Planet.Ketu) continue; // Handle separately based on Rahu

            var position = CalculatePlanetPosition(chartData.JulianDay, planet, chartData.AscendantSign, (int)ayanamsha);
            chartData.Planets.Add(position);
        }

        // Calculate Ketu (opposite to Rahu)
        var rahu = chartData.Planets.First(p => p.Planet == Planet.Rahu);
        var ketuPosition = new PlanetPosition
        {
            Planet = Planet.Ketu,
            Name = ZodiacUtils.PlanetNames[Planet.Ketu],
            Symbol = ZodiacUtils.PlanetSymbols[Planet.Ketu],
            Longitude = ZodiacUtils.NormalizeDegree(rahu.Longitude + 180),
            Latitude = -rahu.Latitude,
            Speed = rahu.Speed
        };
        ketuPosition.Sign = ZodiacUtils.DegreeToSign(ketuPosition.Longitude);
        ketuPosition.SignName = ZodiacUtils.SignNames[ketuPosition.Sign];
        ketuPosition.DegreeInSign = ZodiacUtils.DegreeInSign(ketuPosition.Longitude);
        ketuPosition.House = ZodiacUtils.CalculateHouse(ketuPosition.Sign, chartData.AscendantSign);
        ketuPosition.Nakshatra = ZodiacUtils.DegreeToNakshatra(ketuPosition.Longitude);
        ketuPosition.NakshatraName = ZodiacUtils.NakshatraNames[ketuPosition.Nakshatra];
        ketuPosition.NakshatraPada = ZodiacUtils.GetNakshatraPada(ketuPosition.Longitude);
        
        chartData.Planets.Add(ketuPosition);

        // Calculate divisional charts (commonly used ones)
        // D-9 Navamsa is the most important divisional chart
        chartData.DivisionalCharts[9] = _divisionalChartService.CalculateDivisionalChart(chartData, 9);

        return chartData;
    }

    private PlanetPosition CalculatePlanetPosition(double julianDay, Planet planet, int ascendantSign, int ayanamshaId)
    {
        int planetId = (int)planet;
        
        // For Rahu, use Mean Node
        if (planet == Planet.Rahu)
        {
            planetId = 11; // SE_MEAN_NODE
        }

        var (longitude, latitude, speed) = planet == Planet.Rahu 
            ? _ephemeris.GetRahuPosition(julianDay, ayanamshaId)
            : _ephemeris.GetPlanetPosition(julianDay, planetId, ayanamshaId);

        var position = new PlanetPosition
        {
            Planet = planet,
            Name = ZodiacUtils.PlanetNames[planet],
            Symbol = ZodiacUtils.PlanetSymbols[planet],
            Longitude = longitude,
            Latitude = latitude,
            Speed = speed,
            Sign = ZodiacUtils.DegreeToSign(longitude),
            DegreeInSign = ZodiacUtils.DegreeInSign(longitude),
            Nakshatra = ZodiacUtils.DegreeToNakshatra(longitude),
            NakshatraPada = ZodiacUtils.GetNakshatraPada(longitude)
        };

        position.SignName = ZodiacUtils.SignNames[position.Sign];
        position.NakshatraName = ZodiacUtils.NakshatraNames[position.Nakshatra];
        position.House = ZodiacUtils.CalculateHouse(position.Sign, ascendantSign);

        return position;
    }

    public void Dispose()
    {
        _ephemeris?.Dispose();
    }
}
