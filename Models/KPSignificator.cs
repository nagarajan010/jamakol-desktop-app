namespace JamakolAstrology.Models;

/// <summary>
/// Represents the significators for a single house in KP System
/// Uses the 4-level hierarchy: Occupant Star > Occupant > Owner Star > Owner
/// </summary>
public class HouseSignificators
{
    public int HouseNumber { get; set; }
    
    /// <summary>Level 1: Planets in the star of planets OCCUPYING this house (Strongest)</summary>
    public List<string> Level1_OccupantStarPlanets { get; set; } = new();
    
    /// <summary>Level 2: Planets OCCUPYING this house</summary>
    public List<string> Level2_OccupantPlanets { get; set; } = new();
    
    /// <summary>Level 3: Planets in the star of the OWNER (lord) of this house</summary>
    public List<string> Level3_OwnerStarPlanets { get; set; } = new();
    
    /// <summary>Level 4: The OWNER (lord) of this house (Weakest)</summary>
    public string Level4_Owner { get; set; } = "";
    
    /// <summary>All significators combined (for quick lookup)</summary>
    public List<string> AllSignificators => 
        Level1_OccupantStarPlanets
            .Concat(Level2_OccupantPlanets)
            .Concat(Level3_OwnerStarPlanets)
            .Append(Level4_Owner)
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .ToList();
}

/// <summary>
/// Represents which houses a planet signifies
/// </summary>
public class PlanetSignification
{
    public string PlanetName { get; set; } = "";
    public string Symbol { get; set; } = "";
    
    /// <summary>House this planet occupies</summary>
    public int OccupiesHouse { get; set; }
    
    /// <summary>Whose star this planet is in</summary>
    public string InStarOf { get; set; } = "";
    
    /// <summary>Houses this planet owns (lords over)</summary>
    public List<int> OwnsHouses { get; set; } = new();
    
    /// <summary>All houses this planet signifies (combined)</summary>
    public List<int> SignifiesHouses { get; set; } = new();
    
    /// <summary>Display string for signified houses</summary>
    public string SignifiesDisplay => string.Join(", ", SignifiesHouses.OrderBy(h => h));
}
