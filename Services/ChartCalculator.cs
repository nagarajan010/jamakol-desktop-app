using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Main chart calculation service
/// </summary>
public class ChartCalculator
{
    private readonly EphemerisService _ephemeris;
    private readonly DivisionalChartService _divisionalChartService;
    private readonly KPCalculator _kpCalculator;

    public ChartCalculator()
    {
        _ephemeris = new EphemerisService();
        _divisionalChartService = new DivisionalChartService();
        _kpCalculator = new KPCalculator();
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
        
        // --- KP HOUSE CUSPS (Placidus) ---
        var cusps = _ephemeris.GetHouses(chartData.JulianDay, birthData.Latitude, birthData.Longitude, (int)ayanamsha);
        chartData.HouseCusps = new List<HouseCusp>();
        
        for (int i = 0; i < 12; i++)
        {
            double longitude = cusps[i];
            var cusp = new HouseCusp
            {
                HouseNumber = i + 1,
                Degree = longitude,
                SignName = ZodiacUtils.SignNames[ZodiacUtils.DegreeToSign(longitude)],
                DegreeDisplay = ZodiacUtils.FormatDegreeInSign(longitude),
                KpDetails = _kpCalculator.Calculate(longitude)
            };
            chartData.HouseCusps.Add(cusp);
        }

        // Calculate positions for all planets
        chartData.Planets = new List<PlanetPosition>();

        // Main planets (Sun through Saturn)
        foreach (Planet planet in Enum.GetValues<Planet>())
        {
            if (planet == Planet.Ketu) continue; // Handle separately based on Rahu

            var position = CalculatePlanetPosition(chartData.JulianDay, planet, chartData.AscendantSign, (int)ayanamsha);
            
            // Calculate KP Lords for Planet
            position.KpDetails = _kpCalculator.Calculate(position.Longitude);
            
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
        
        // Calculate KP Lords for Ketu
        ketuPosition.KpDetails = _kpCalculator.Calculate(ketuPosition.Longitude);
        ketuPosition.Gati = CalculateGati(Planet.Ketu, rahu.Speed); // Use Rahu's speed for Ketu Gati
        
        chartData.Planets.Add(ketuPosition);

        // Calculate Aprakash Graha (Shadow Planets) based on Sun's longitude
        var sun = chartData.Planets.First(p => p.Planet == Planet.Sun);
        var aprakashGraha = CalculateAprakashGraha(sun.Longitude, chartData.AscendantSign);
        chartData.Planets.AddRange(aprakashGraha);

        // Calculate Combustion Status
        CalculateCombustion(chartData.Planets);

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
        position.Gati = CalculateGati(planet, speed);

        return position;
    }

    /// <summary>
    /// Calculate Gati (motion state) based on speed and average daily motion
    /// </summary>
    private string CalculateGati(Planet planet, double speed)
    {
        // 1. Vakra (Retrograde)
        if (speed < 0) return "Vakra (Retrograde)";

        // 2. Vikala (Stationary/Zero Speed)
        if (Math.Abs(speed) < 0.002) return "Vikala (Stationary)";

        // Mean daily motions in degrees (approximate)
        double meanSpeed = 0;
        switch (planet)
        {
            case Planet.Sun: meanSpeed = 0.985608235827358; break;
            case Planet.Moon: meanSpeed = 13.176404401210586; break;
            case Planet.Mars: meanSpeed = 0.5237984622356734; break;
            case Planet.Mercury: meanSpeed = 0.9860065818020693; break; 
            case Planet.Jupiter: meanSpeed = 0.08319592078852339; break;
            case Planet.Venus: meanSpeed = 0.9857931126635223; break; 
            case Planet.Saturn: meanSpeed = 0.03360972270041907; break;
            case Planet.Rahu: 
            case Planet.Ketu:
                // User provided -0.05299198558774121 for Nodes.
                // Standard logic returns Vakra for speed < 0 anyway.
                // If we ever need mean speed for nodes (unlikely for standard Gati logic which defaults to Vakra), it would be approx 0.053.
                return "Vakra (Retrograde)"; 
        }

        if (meanSpeed == 0) return ""; 

        // 3. Atichara (Accelerated/Fast) - significantly faster than mean
        // Threshold: > 125% of mean speed is a good approximation for Atichara entering next sign quickly
        if (speed > meanSpeed * 1.25) return "Atichara (Accelerated)";

        // 4. Chara (Moving/Fast) - faster than mean but not excessive
        if (speed > meanSpeed) return "Chara (Fast)";

        // 5. Mandatara (Very Slow) - significantly slower than mean
        if (speed < meanSpeed * 0.5) return "Mandatara (Very Slow)";

        // 6. Manda (Slow) - slower than mean
        if (speed < meanSpeed) return "Manda (Slow)";

        // 7. Sama (Even/Mean) - roughly equal to mean
        return "Sama (Even)"; 
    }

    /// <summary>
    /// Calculates combustion status for planets based on their distance from Sun
    /// </summary>
    private void CalculateCombustion(List<PlanetPosition> planets)
    {
        var sun = planets.FirstOrDefault(p => p.Planet == Planet.Sun);
        if (sun == null) return;

        foreach (var planet in planets)
        {
            if (planet.Planet == Planet.Sun || planet.Planet == Planet.Rahu || planet.Planet == Planet.Ketu || planet.Planet == Planet.Moon)
                continue;

            double diff = Math.Abs(sun.Longitude - planet.Longitude);
            if (diff > 180) diff = 360 - diff;

            double orb = 0;
            switch (planet.Planet)
            {
                case Planet.Mars:
                    orb = 17;
                    break;
                case Planet.Mercury:
                    orb = planet.IsRetrograde ? 12 : 14;
                    break;
                case Planet.Jupiter:
                    orb = 11;
                    break;
                case Planet.Venus:
                    orb = planet.IsRetrograde ? 8 : 10;
                    break;
                case Planet.Saturn:
                    orb = 15;
                    break;
            }

            if (diff <= orb)
            {
                planet.IsCombust = true;
            }
        }
    }

    /// <summary>
    /// Calculate Aprakash Graha (Shadow Planets) based on Sun's longitude
    /// Dhooma (Dh) - Sun's longitude + 133°20' 
    /// Vyatipata (Vy) - 360° - Dhooma longitude 
    /// Parivesha (Pa) - Vyatipata longitude + 180° 
    /// Indrachapa (In) - 360° - Parivesha longitude 
    /// Upaketu (Uk) - Indrachapa longitude + 16°40' 
    /// </summary>
    private List<PlanetPosition> CalculateAprakashGraha(double sunLongitude, int ascendantSign)
    {
        var result = new List<PlanetPosition>();

        // Dhooma = Sun + 133°20' (133.333...°)
        double dhoomaLong = ZodiacUtils.NormalizeDegree(sunLongitude + 133.0 + (20.0 / 60.0));
        result.Add(CreateAprakashPosition("Dhooma", "Dh", dhoomaLong, ascendantSign));

        // Vyatipata = 360° - Dhooma
        double vyatipataLong = ZodiacUtils.NormalizeDegree(360.0 - dhoomaLong);
        result.Add(CreateAprakashPosition("Vyatipata", "Vy", vyatipataLong, ascendantSign));

        // Parivesha = Vyatipata + 180°
        double pariveshaLong = ZodiacUtils.NormalizeDegree(vyatipataLong + 180.0);
        result.Add(CreateAprakashPosition("Parivesha", "Pa", pariveshaLong, ascendantSign));

        // Indrachapa = 360° - Parivesha
        double indrachapaLong = ZodiacUtils.NormalizeDegree(360.0 - pariveshaLong);
        result.Add(CreateAprakashPosition("Indrachapa", "In", indrachapaLong, ascendantSign));

        // Upaketu = Indrachapa + 16°40' (16.666...°)
        double upaketuLong = ZodiacUtils.NormalizeDegree(indrachapaLong + 16.0 + (40.0 / 60.0));
        result.Add(CreateAprakashPosition("Upaketu", "Uk", upaketuLong, ascendantSign));

        return result;
    }

    /// <summary>
    /// Create a PlanetPosition for an Aprakash Graha (shadow planet)
    /// </summary>
    private PlanetPosition CreateAprakashPosition(string name, string symbol, double longitude, int ascendantSign)
    {
        var position = new PlanetPosition
        {
            Planet = Planet.Sun, // Placeholder - these are mathematical points, not real planets
            Name = name,
            Symbol = symbol,
            Longitude = longitude,
            Latitude = 0,
            Speed = 0,
            Sign = ZodiacUtils.DegreeToSign(longitude),
            DegreeInSign = ZodiacUtils.DegreeInSign(longitude),
            Nakshatra = ZodiacUtils.DegreeToNakshatra(longitude),
            NakshatraPada = ZodiacUtils.GetNakshatraPada(longitude)
        };

        position.SignName = ZodiacUtils.SignNames[position.Sign];
        position.NakshatraName = ZodiacUtils.NakshatraNames[position.Nakshatra];
        position.House = ZodiacUtils.CalculateHouse(position.Sign, ascendantSign);
        position.Gati = ""; // Shadow planets don't have motion
        position.KpDetails = _kpCalculator.Calculate(longitude);

        return position;
    }

    public void Dispose()
    {
        _ephemeris?.Dispose();
    }
}
