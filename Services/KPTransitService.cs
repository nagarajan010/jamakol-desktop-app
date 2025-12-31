using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

public class KPTransitService
{
    private readonly EphemerisService _ephemeris;
    private readonly KPCalculator _kpCalculator;
    private readonly List<double> _subLordBoundaries;

    public KPTransitService(EphemerisService ephemeris)
    {
        _ephemeris = ephemeris;
        _kpCalculator = new KPCalculator();
        // Boundaries no longer needed for direct state check
    }

    public async Task<List<TransitEvent>> CalculateTransitsAsync(DateTime startUtc, DateTime endUtc, ChartData locData, int ayanamshaId, double ayanamshaOffset, IProgress<string>? progress = null, int? targetBodyId = null)
    {
        return await Task.Run(() =>
        {
            var events = new List<TransitEvent>();
            
            var allPlanets = new int[] 
            { 
                 SwissEphNet.SwissEph.SE_SUN,
                 SwissEphNet.SwissEph.SE_MOON,
                 SwissEphNet.SwissEph.SE_MARS,
                 SwissEphNet.SwissEph.SE_MERCURY,
                 SwissEphNet.SwissEph.SE_JUPITER,
                 SwissEphNet.SwissEph.SE_VENUS,
                 SwissEphNet.SwissEph.SE_SATURN,
                 SwissEphNet.SwissEph.SE_MEAN_NODE // Rahu
            };

            // Filter planets
            var planets = targetBodyId.HasValue 
                ? (targetBodyId.Value == -1 ? Array.Empty<int>() : allPlanets.Where(id => id == targetBodyId.Value).ToArray())
                : allPlanets;

            double startJd = _ephemeris.GetJulianDay(startUtc);
            double endJd = _ephemeris.GetJulianDay(endUtc);
            
            if ((endUtc - startUtc).TotalDays > 365)
            {
                progress?.Report("Range too large. Limiting to 1 year.");
                endJd = _ephemeris.GetJulianDay(startUtc.AddYears(1));
            }

            // Reduce Moon step to 30 mins for safety
            double defaultStep = 1.0 / 24.0; // 1 hour
            double moonStep = 1.0 / 48.0;    // 30 mins

            foreach (var planetId in planets)
            {
                Models.Planet planetEnum = (Models.Planet)planetId;
                if (planetId == SwissEphNet.SwissEph.SE_MEAN_NODE) planetEnum = Models.Planet.Rahu;
                string pName = ZodiacUtils.GetPlanetName(planetEnum);
                progress?.Report($"Calculating {pName}...");

                double step = (planetId == SwissEphNet.SwissEph.SE_MOON) ? moonStep : defaultStep;
                
                double currJd = startJd;
                var prevPos = GetPosition(currJd, planetId, ayanamshaId, ayanamshaOffset, locData);
                var prevLords = _kpCalculator.Calculate(prevPos);

                while (currJd < endJd)
                {
                    double nextJd = currJd + step;
                    if (nextJd > endJd) nextJd = endJd;

                    var nextPos = GetPosition(nextJd, planetId, ayanamshaId, ayanamshaOffset, locData);
                    var nextLords = _kpCalculator.Calculate(nextPos);

                    if (prevLords.SubLord != nextLords.SubLord)
                    {
                        // Sub Lord Changed!
                        FindChange(events, pName, currJd, nextJd, prevLords, nextLords, planetId, ayanamshaId, ayanamshaOffset, locData);
                    }

                    prevLords = nextLords;
                    currJd = nextJd;
                    if (currJd >= endJd) break;
                }
            }
            
            // Lagna
            if (targetBodyId == null || targetBodyId == -1)
            {
                progress?.Report("Calculating Lagna...");
                double lagnaStep = 5.0 / 1440.0; // 5 mins
                double lCurrJd = startJd;
                
                var lPrevPos = GetPosition(lCurrJd, -1, ayanamshaId, ayanamshaOffset, locData);
                var lPrevLords = _kpCalculator.Calculate(lPrevPos);

                while (lCurrJd < endJd)
                {
                    double lNextJd = lCurrJd + lagnaStep;
                    if (lNextJd > endJd) lNextJd = endJd;

                    var lNextPos = GetPosition(lNextJd, -1, ayanamshaId, ayanamshaOffset, locData);
                    var lNextLords = _kpCalculator.Calculate(lNextPos);

                    if (lPrevLords.SubLord != lNextLords.SubLord)
                    {
                         FindChange(events, "Lagna", lCurrJd, lNextJd, lPrevLords, lNextLords, -1, ayanamshaId, ayanamshaOffset, locData);
                    }

                    lPrevLords = lNextLords;
                    lCurrJd = lNextJd;
                    if (lCurrJd >= endJd) break;
                }
            }

            return events.OrderBy(e => e.TimeUtc).ToList();
        });
    }

    private void FindChange(List<TransitEvent> events, string bodyName, double t1, double t2, KPLords oldLords, KPLords newLords, int planetId, int ayanId, double ayanOffset, ChartData? locData)
    {
        // Binary search for the moment of change
        // We know oldLords.SubLord != newLords.SubLord
        // We want to find T where State(T) changes from OldSub

        double low = t1;
        double high = t2;
        string startSub = oldLords.SubLord;
        
        KPLords finalLords = newLords; 

        for (int i = 0; i < 20; i++) // 20 iterations ~ 0.003s precision for 1h window
        {
            double mid = (low + high) / 2;
            double midPos = GetPosition(mid, planetId, ayanId, ayanOffset, locData);
            var midLords = _kpCalculator.Calculate(midPos);

            if (midLords.SubLord == startSub)
            {
                // Change happens in [mid, high]
                low = mid;
            }
            else
            {
                // Change happens in [low, mid]
                high = mid;
                finalLords = midLords; 
            }
        }
        
        double changeTime = high; // Upper bound is the "New state" time
        
        // Final lords at change time
        var finalPos = GetPosition(changeTime, planetId, ayanId, ayanOffset, locData);
        var calculatedLords = _kpCalculator.Calculate(finalPos);

        // Verify valid change (ignore jitter if any, though unlikely with binary search)
        if (calculatedLords.SubLord != oldLords.SubLord)
        {
             events.Add(new TransitEvent
             {
                 TimeUtc = EphemerisService.JulianDateToDateTime(changeTime),
                 Body = bodyName,
                 NewSubLord = calculatedLords.SubLord,
                 OldSubLord = oldLords.SubLord,
                 Sign = calculatedLords.SignLord,
                 Star = calculatedLords.StarLord,
             });
        }
    }

    private double GetPosition(double jd, int planetId, int ayanId, double ayanOffset, ChartData? locData)
    {
        if (planetId == -1 && locData != null)
        {
            return _ephemeris.GetAscendant(jd, locData.BirthData.Latitude, locData.BirthData.Longitude, ayanId, ayanOffset);
        }
        else
        {
            return _ephemeris.GetPlanetPosition(jd, planetId, ayanId, ayanOffset).longitude;
        }
    }
}

public class TransitEvent
{
    public DateTime TimeUtc { get; set; }
    public string Body { get; set; } = "";
    public string Sign { get; set; } = "";
    public string Star { get; set; } = "";
    public string OldSub { get; set; } = ""; // Renaming prop to simplify
    public string NewSubLord { get; set; } = "";
    public string OldSubLord { get; set; } = "";
    public double BoundaryDegree { get; set; }
    
    public string DisplayTime => TimeUtc.ToLocalTime().ToString("dd-MMM-yyyy h:mm:ss tt");
}
