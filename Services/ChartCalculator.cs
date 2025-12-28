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

        // Calculate Julian Day (supports BC dates via GetUtcComponents)
        var (utcYear, utcMonth, utcDay, utcHour) = birthData.GetUtcComponents();
        chartData.JulianDay = _ephemeris.GetJulianDay(utcYear, utcMonth, utcDay, utcHour);

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
        chartData.AscendantSignName = ZodiacUtils.GetSignName(chartData.AscendantSign);
        
        // Calculate Ascendant nakshatra info
        var ascNakshatra = ZodiacUtils.GetNakshatraInfo(chartData.AscendantDegree);
        chartData.AscendantNakshatraName = ascNakshatra.name;
        chartData.AscendantNakshatraPada = ascNakshatra.pada;
        
        // --- KP HOUSE CUSPS (Placidus) ---
        var cusps = _ephemeris.GetHouses(chartData.JulianDay, birthData.Latitude, birthData.Longitude, (int)ayanamsha);
        chartData.HouseCusps = new List<HouseCusp>();
        
        for (int i = 0; i < 12; i++)
        {
            double cuspLongitude = cusps[i];
            // Start of house = previous house cusp
            int prevIndex = (i == 0) ? 11 : i - 1;
            double startLongitude = cusps[prevIndex];
            // End of house = next house cusp
            int nextIndex = (i + 1) % 12;
            double endLongitude = cusps[nextIndex];
            
            var cusp = new HouseCusp
            {
                HouseNumber = i + 1,
                Degree = cuspLongitude,
                StartDegree = startLongitude,
                EndDegree = endLongitude,
                SignName = ZodiacUtils.GetSignName(ZodiacUtils.DegreeToSign(cuspLongitude)),
                DegreeDisplay = FormatDegreeWithSign(cuspLongitude),
                StartDisplay = FormatDegreeWithSign(startLongitude),
                EndDisplay = FormatDegreeWithSign(endLongitude),
                KpDetails = _kpCalculator.Calculate(cuspLongitude)
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
        ketuPosition.SignName = ZodiacUtils.GetSignName(ketuPosition.Sign);
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

        // Calculate Jaimini Karakas (8-Chara scheme)
        var karakaCalc = new JaiminiKarakaCalculator();
        karakaCalc.CalculateKarakas(chartData.Planets);

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

        position.SignName = ZodiacUtils.GetSignName(position.Sign);
        position.NakshatraName = ZodiacUtils.NakshatraNames[position.Nakshatra];
        position.House = ZodiacUtils.CalculateHouse(position.Sign, ascendantSign);
        position.Gati = CalculateGati(planet, speed);

        return position;
    }

    /// <summary>
    /// Calculate Gati (motion state) based on speed and average daily motion
    /// </summary>
    /// <summary>
    /// Calculate Gati (motion state) based on speed and average daily motion
    /// Implements the 8 Gatis system for retrogradable planets.
    /// </summary>
    private string CalculateGati(Planet planet, double speed)
    {
        // Helper for translation
        string T(string en, string ta) => ZodiacUtils.IsTamil ? ta : en;

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
                return T("Vakra (Retrograde)", "வக்ரம்"); 
        }
        
        if (meanSpeed == 0) return ""; 

        // 2. Sun/Moon
        if (planet == Planet.Sun || planet == Planet.Moon)
        {
             if (Math.Abs(speed) < 0.002) return T("Vikala (Stationary)", "விகலா");
             
             double ratio = speed / meanSpeed;
             if (ratio > 1.1) return T("Sheeghra (Fast)", "சீக்ரம்");
             if (ratio < 0.9) return T("Manda (Slow)", "மந்தம்");
             return T("Sama (Normal)", "சமம்");
        }

        double absSpeed = Math.Abs(speed);
        double pct = absSpeed / meanSpeed;

        if (pct < 0.10) return T("Kutilla (Stationary)", "குடிலா");

        if (speed < 0)
        {
            if (pct <= 0.60) return T("Anuvakra (Slow Retro)", "அனுவக்ரம்");
            return T("Vakra (Retrograde)", "வக்ரம்");
        }
        else
        {
            if (pct <= 0.60) return T("Mandatara (Very Slow)", "அதி மந்தம்");
            if (pct <= 0.90) return T("Manda (Slow)", "மந்தம்");
            if (pct <= 1.25) return T("Sama (Normal)", "சமம்");
            if (pct <= 1.50) return T("Sheeghra (Fast)", "சீக்ரம்");
            return T("Sheeghratara (Very Fast)", "அதி சீக்ரம்");
        }
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

        position.SignName = ZodiacUtils.GetSignName(position.Sign);
        position.NakshatraName = ZodiacUtils.NakshatraNames[position.Nakshatra];
        position.House = ZodiacUtils.CalculateHouse(position.Sign, ascendantSign);
        position.Gati = ""; // Shadow planets don't have motion
        position.KpDetails = _kpCalculator.Calculate(longitude);

        return position;
    }

    /// <summary>
    /// Format degree with sign abbreviation like "12 Li 56' 11"" (JHora style)
    /// </summary>
    private string FormatDegreeWithSign(double degree)
    {
        degree = ZodiacUtils.NormalizeDegree(degree);
        int signIndex = ZodiacUtils.DegreeToSign(degree);
        double degInSign = ZodiacUtils.DegreeInSign(degree);
        
        int deg = (int)degInSign;
        double minVal = (degInSign - deg) * 60;
        int min = (int)minVal;
        int sec = (int)((minVal - min) * 60);
        
        // Sign abbreviations (2 letters)
        string[] signAbbr = { "", "Ar", "Ta", "Ge", "Cn", "Le", "Vi", "Li", "Sc", "Sg", "Cp", "Aq", "Pi" };
        
        return $"{deg} {signAbbr[signIndex]} {min:D2}' {sec:D2}\"";
    }

    public void Dispose()
    {
        _ephemeris?.Dispose();
    }
}

