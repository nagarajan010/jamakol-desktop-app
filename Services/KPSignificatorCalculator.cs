using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Calculator for KP Significators
/// Implements the 4-level hierarchy:
/// Level 1: Planets in the star of planets occupying a house (Strongest)
/// Level 2: Planets occupying the house
/// Level 3: Planets in the star of the house owner
/// Level 4: The house owner (Weakest)
/// </summary>
public class KPSignificatorCalculator
{
    // Sign lords (0=Aries, 11=Pisces)
    private static readonly string[] SignLords =
    {
        "Mars", "Venus", "Mercury", "Moon", "Sun", "Mercury", 
        "Venus", "Mars", "Jupiter", "Saturn", "Saturn", "Jupiter"
    };

    /// <summary>
    /// Calculate significators for all 12 houses
    /// </summary>
    public List<HouseSignificators> CalculateHouseSignificators(ChartData chart)
    {
        var result = new List<HouseSignificators>();
        
        // Build lookup: which planets are in each house
        var planetsInHouse = new Dictionary<int, List<PlanetPosition>>();
        for (int h = 1; h <= 12; h++)
            planetsInHouse[h] = new List<PlanetPosition>();
        
        foreach (var planet in chart.Planets)
        {
            if (planet.House >= 1 && planet.House <= 12)
                planetsInHouse[planet.House].Add(planet);
        }
        
        // Build lookup: which planets are in the star of each planet
        var planetsInStarOf = new Dictionary<string, List<PlanetPosition>>();
        foreach (var planet in chart.Planets)
        {
            string starLord = planet.KpDetails.StarLord;
            if (!planetsInStarOf.ContainsKey(starLord))
                planetsInStarOf[starLord] = new List<PlanetPosition>();
            planetsInStarOf[starLord].Add(planet);
        }
        
        // Calculate for each house
        for (int house = 1; house <= 12; house++)
        {
            var sig = new HouseSignificators { HouseNumber = house };
            
            // Get house owner (from cusp sign)
            string owner = GetHouseOwner(chart, house);
            sig.Level4_Owner = owner;
            
            // Level 2: Planets occupying this house
            var occupants = planetsInHouse[house];
            sig.Level2_OccupantPlanets = occupants.Select(p => p.Name).ToList();
            
            // Level 1: Planets in the star of occupants
            foreach (var occupant in occupants)
            {
                if (planetsInStarOf.TryGetValue(occupant.Name, out var inStar))
                {
                    foreach (var p in inStar)
                    {
                        if (!sig.Level1_OccupantStarPlanets.Contains(p.Name))
                            sig.Level1_OccupantStarPlanets.Add(p.Name);
                    }
                }
            }
            
            // Level 3: Planets in the star of the owner
            if (planetsInStarOf.TryGetValue(owner, out var inOwnerStar))
            {
                sig.Level3_OwnerStarPlanets = inOwnerStar.Select(p => p.Name).ToList();
            }
            
            result.Add(sig);
        }
        
        return result;
    }
    
    /// <summary>
    /// Calculate which houses each planet signifies
    /// </summary>
    public List<PlanetSignification> CalculatePlanetSignifications(ChartData chart)
    {
        var result = new List<PlanetSignification>();
        
        // First get house significators
        var houseSignificators = CalculateHouseSignificators(chart);
        
        foreach (var planet in chart.Planets)
        {
            var sig = new PlanetSignification
            {
                PlanetName = planet.Name,
                Symbol = planet.Symbol,
                OccupiesHouse = planet.House,
                InStarOf = planet.KpDetails.StarLord,
                OwnsHouses = GetOwnedHouses(chart, planet.Name)
            };
            
            // Find all houses this planet signifies
            var houses = new HashSet<int>();
            
            foreach (var houseSig in houseSignificators)
            {
                if (houseSig.Level1_OccupantStarPlanets.Contains(planet.Name) ||
                    houseSig.Level2_OccupantPlanets.Contains(planet.Name) ||
                    houseSig.Level3_OwnerStarPlanets.Contains(planet.Name) ||
                    houseSig.Level4_Owner == planet.Name)
                {
                    houses.Add(houseSig.HouseNumber);
                }
            }
            
            sig.SignifiesHouses = houses.OrderBy(h => h).ToList();
            result.Add(sig);
        }
        
        return result;
    }
    
    /// <summary>
    /// Get the owner (lord) of a house based on the cusp sign
    /// </summary>
    private string GetHouseOwner(ChartData chart, int houseNumber)
    {
        if (chart.HouseCusps == null || chart.HouseCusps.Count < houseNumber)
            return "";
            
        var cusp = chart.HouseCusps[houseNumber - 1];
        int signIndex = (int)(cusp.Degree / 30.0);
        if (signIndex >= 12) signIndex = 11;
        
        return SignLords[signIndex];
    }
    
    /// <summary>
    /// Get the houses owned by a planet (based on cusp signs)
    /// </summary>
    private List<int> GetOwnedHouses(ChartData chart, string planetName)
    {
        var houses = new List<int>();
        
        if (chart.HouseCusps == null) return houses;
        
        for (int i = 0; i < chart.HouseCusps.Count; i++)
        {
            var cusp = chart.HouseCusps[i];
            int signIndex = (int)(cusp.Degree / 30.0);
            if (signIndex >= 12) signIndex = 11;
            
            if (SignLords[signIndex] == planetName)
                houses.Add(i + 1);
        }
        
        return houses;
    }
}
