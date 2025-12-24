using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Calculates Prasanna (Horary) analysis details
/// </summary>
public class PrasannaCalculator
{
    // Sign lords mapping (1-12 = Aries to Pisces)
    private static readonly Dictionary<int, string> SignLords = new()
    {
        { 1, "Mars" },      // Aries
        { 2, "Venus" },     // Taurus
        { 3, "Mercury" },   // Gemini
        { 4, "Moon" },      // Cancer
        { 5, "Sun" },       // Leo
        { 6, "Mercury" },   // Virgo
        { 7, "Venus" },     // Libra
        { 8, "Mars" },      // Scorpio
        { 9, "Jupiter" },   // Sagittarius
        { 10, "Saturn" },   // Capricorn
        { 11, "Saturn" },   // Aquarius
        { 12, "Jupiter" }   // Pisces
    };

    // Exaltation signs for planets
    private static readonly Dictionary<string, int> ExaltationSigns = new()
    {
        { "Sun", 1 },       // Aries
        { "Moon", 2 },      // Taurus
        { "Mars", 10 },     // Capricorn
        { "Mercury", 6 },   // Virgo
        { "Jupiter", 4 },   // Cancer
        { "Venus", 12 },    // Pisces
        { "Saturn", 7 },    // Libra
        { "Rahu", 3 },      // Gemini (some traditions)
        { "Ketu", 9 }       // Sagittarius (some traditions)
    };

    // Debilitation signs for planets (opposite of exaltation)
    private static readonly Dictionary<string, int> DebilitationSigns = new()
    {
        { "Sun", 7 },       // Libra
        { "Moon", 8 },      // Scorpio
        { "Mars", 4 },      // Cancer
        { "Mercury", 12 },  // Pisces
        { "Jupiter", 10 },  // Capricorn
        { "Venus", 6 },     // Virgo
        { "Saturn", 1 },    // Aries
        { "Rahu", 9 },      // Sagittarius
        { "Ketu", 3 }       // Gemini
    };

    /// <summary>
    /// Calculate Prasanna details from Jama Graha positions and special points
    /// Uses Jama Graha degrees for proximity calculations (always real degree)
    /// Mode affects only: house counts, exaltation, debilitation, parivarthana
    /// </summary>
    /// <param name="jamaGrahas">Jama Graha positions</param>
    /// <param name="specialPoints">Special points (Udayam, Aarudam, Kavippu)</param>
    /// <param name="mode">JamaGrahaBoxSign: use House property; JamaGrahaRealDegree: use Sign from Degree</param>
    public PrasannaDetails Calculate(List<JamaGrahaPosition> jamaGrahas, List<SpecialPoint> specialPoints, PrasannaCalcMode mode = PrasannaCalcMode.JamaGrahaBoxSign)
    {
        var details = new PrasannaDetails();

        // Safety check - if no data, return empty details
        if (jamaGrahas == null || jamaGrahas.Count == 0 || specialPoints == null || specialPoints.Count == 0)
        {
            return details;
        }

        bool useBoxSign = mode == PrasannaCalcMode.JamaGrahaBoxSign;

        // Get special points (case-insensitive search)
        var aarudam = specialPoints.FirstOrDefault(sp => 
            sp.Name.Equals("Aarudam", StringComparison.OrdinalIgnoreCase) || 
            sp.Symbol.Equals("AR", StringComparison.OrdinalIgnoreCase));
        var udayam = specialPoints.FirstOrDefault(sp => 
            sp.Name.Equals("Udayam", StringComparison.OrdinalIgnoreCase) || 
            sp.Symbol.Equals("UD", StringComparison.OrdinalIgnoreCase));
        var kavippu = specialPoints.FirstOrDefault(sp => 
            sp.Name.Equals("Kavippu", StringComparison.OrdinalIgnoreCase) || 
            sp.Symbol.Equals("KV", StringComparison.OrdinalIgnoreCase));

        // Calculate Udhayam Lord & Bhava
        if (udayam != null)
        {
            int udayamSign = udayam.SignIndex + 1; // 1-based sign
            string udayamLord = SignLords.GetValueOrDefault(udayamSign, "Unknown");
            details.UdhayamLord = udayamLord;
            
            // Find where the Udayam Lord is positioned (using Jama Graha) and calculate its house from Udayam
            var lordPlanet = jamaGrahas.FirstOrDefault(p => 
                p.Name.Equals(udayamLord, StringComparison.OrdinalIgnoreCase));
            if (lordPlanet != null)
            {
                // Get sign based on mode: Box position (House) or Real Degree
                int lordSign = useBoxSign ? lordPlanet.House : lordPlanet.Sign;
                int lordSignIndex = lordSign - 1; // Convert 1-based to 0-based
                details.UdhayamBhava = CalculateHouseFromLagna(udayam.SignIndex, lordSignIndex);
            }
            else
            {
                details.UdhayamBhava = 1; // Default if lord not found
            }
            
            // Find Jama Graha closest to Udayam
            var closest = FindClosestJamaGraha(jamaGrahas, udayam.AbsoluteLongitude);
            if (closest != null)
            {
                details.PlanetTowardsUdhayam = closest.Value.planet;
                details.PlanetTowardsUdhayamPercent = closest.Value.percent;
            }
        }

        // Calculate Arudam Bhava & Planet towards Arudam
        // Bhava is counted from Udayam (1st house)
        if (aarudam != null)
        {
            int lagnaSign = udayam?.SignIndex ?? 0;
            int arudamSign = aarudam.SignIndex;
            // Calculate house from Udayam: if same sign = 1, next sign = 2, etc.
            details.ArudamBhava = CalculateHouseFromLagna(lagnaSign, arudamSign);
            
            var closest = FindClosestJamaGraha(jamaGrahas, aarudam.AbsoluteLongitude);
            if (closest != null)
            {
                details.PlanetTowardsArudam = closest.Value.planet;
                details.PlanetTowardsArudamPercent = closest.Value.percent;
            }
        }

        // Calculate Planet towards Kavippu & Bhava in Kavippu
        // Bhava is counted from Udayam (1st house)
        if (kavippu != null)
        {
            int lagnaSign = udayam?.SignIndex ?? 0;
            int kavippuSign = kavippu.SignIndex;
            details.BhavaInKavippu = CalculateHouseFromLagna(lagnaSign, kavippuSign);
            
            var closest = FindClosestJamaGraha(jamaGrahas, kavippu.AbsoluteLongitude);
            if (closest != null)
            {
                details.PlanetTowardsKavippu = closest.Value.planet;
                details.PlanetTowardsKavippuPercent = closest.Value.percent;
            }
        }

        // Calculate Exalted, Debilitated, and Parivarthana planets using Jama Graha
        // Mode affects which sign is used: Box position (House) or Real Degree calculated sign
        details.ExaltedPlanets = GetExaltedJamaGrahas(jamaGrahas, useBoxSign);
        details.DebilitatedPlanets = GetDebilitatedJamaGrahas(jamaGrahas, useBoxSign);
        details.ParivarthanaPlanets = GetParivarthanaJamaGrahas(jamaGrahas, useBoxSign);

        // Emakandam - Saturn-related (using Jama Graha)
        var saturn = jamaGrahas.FirstOrDefault(p => p.Name.Equals("Saturn", StringComparison.OrdinalIgnoreCase));
        if (saturn != null)
        {
            // Find Jama Graha closest to Saturn (Emakandam)
            var closest = FindClosestJamaGraha(jamaGrahas.Where(p => !p.Name.Equals("Saturn", StringComparison.OrdinalIgnoreCase)).ToList(), saturn.Degree);
            if (closest != null)
            {
                details.PlanetTowardsEmakandam = closest.Value.planet;
                details.PlanetTowardsEmakandamPercent = closest.Value.percent;
            }
        }

        // Rahu Time and Mrithyu - left empty for now
        details.PlanetInRahuTime = "-";
        details.PlanetTowardsMrithyu = "-";

        return details;
    }

    /// <summary>
    /// Find the planet closest to a given longitude and calculate the proximity percentage
    /// </summary>
    private (string planet, double percent)? FindClosestPlanet(List<PlanetPosition> planets, double targetLongitude)
    {
        if (planets == null || planets.Count == 0)
            return null;

        string? closestPlanet = null;
        double minDistance = 360;

        foreach (var planet in planets)
        {
            double distance = GetAngularDistance(planet.Longitude, targetLongitude);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestPlanet = planet.Name;
            }
        }

        if (closestPlanet == null)
            return null;

        // Calculate proximity percentage (closer = higher percentage)
        // Max distance is 180 degrees, so: percent = (180 - distance) / 180 * 100
        double percent = Math.Max(0, (180 - minDistance) / 180 * 100);
        
        return (closestPlanet, Math.Round(percent, 2));
    }

    /// <summary>
    /// Get angular distance between two longitudes (0-180 degrees)
    /// </summary>
    private double GetAngularDistance(double long1, double long2)
    {
        double diff = Math.Abs(long1 - long2);
        return diff > 180 ? 360 - diff : diff;
    }

    /// <summary>
    /// Calculate house number from lagna sign (0-based sign indices)
    /// Returns 1-12 where lagna sign = 1
    /// </summary>
    private int CalculateHouseFromLagna(int lagnaSignIndex, int targetSignIndex)
    {
        // Both are 0-based (0-11)
        int diff = targetSignIndex - lagnaSignIndex;
        if (diff < 0) diff += 12;
        return diff + 1; // Convert to 1-12
    }

    /// <summary>
    /// Find the Jama Graha closest to a given longitude and calculate the proximity percentage.
    /// Since Jama Graha moves in REVERSE (decreasing degrees), only considers planets
    /// that have NOT yet crossed the target point (i.e., planets with HIGHER degrees).
    /// </summary>
    private (string planet, double percent)? FindClosestJamaGraha(List<JamaGrahaPosition> jamaGrahas, double targetLongitude)
    {
        if (jamaGrahas == null || jamaGrahas.Count == 0)
            return null;

        string? closestPlanet = null;
        double minDistance = 360;

        foreach (var graha in jamaGrahas)
        {
            // Jama Graha moves in REVERSE (backwards/decreasing degrees)
            // Example: If moving backward from 100° to 90° to 80°...
            // - Target (special point) is at 85°
            // - Planet at 90° is APPROACHING (will reach 85° as it moves backward)
            // - Planet at 80° has ALREADY PASSED (it was at 80° before reaching 85°)
            
            // So we want planets with degrees HIGHER than target (they're approaching in reverse)
            // Distance = graha.Degree - targetLongitude (how far ahead the planet is)
            double forwardDistance = graha.Degree - targetLongitude;
            if (forwardDistance < 0)
                forwardDistance += 360; // Wrap around (e.g., graha at 10°, target at 350°)
            
            // If forward distance > 180, the planet is actually behind target in forward direction
            // which means it's "ahead" in reverse direction - but too far, so skip
            // We only consider planets within the next 180 degrees ahead in reverse direction
            if (forwardDistance > 180)
                continue; // This planet has already been crossed (it's behind in reverse motion)
            
            if (forwardDistance < minDistance)
            {
                minDistance = forwardDistance;
                closestPlanet = graha.Name;
            }
        }

        if (closestPlanet == null)
            return null;

        // Calculate proximity percentage (closer = higher percentage)
        // Max considered distance is 180 degrees, so: percent = (180 - distance) / 180 * 100
        double percent = Math.Max(0, (180 - minDistance) / 180 * 100);
        
        return (closestPlanet, Math.Round(percent, 2));
    }

    /// <summary>
    /// Get list of exalted Jama Grahas
    /// </summary>
    private string GetExaltedJamaGrahas(List<JamaGrahaPosition> jamaGrahas, bool useBoxSign)
    {
        var exalted = new List<string>();
        foreach (var graha in jamaGrahas)
        {
            int sign = useBoxSign ? graha.House : graha.Sign;
            if (ExaltationSigns.TryGetValue(graha.Name, out int exaltSign) && sign == exaltSign)
            {
                exalted.Add(graha.Name.ToUpper());
            }
        }
        return exalted.Count > 0 ? string.Join(", ", exalted) : "-";
    }

    /// <summary>
    /// Get list of debilitated Jama Grahas
    /// </summary>
    private string GetDebilitatedJamaGrahas(List<JamaGrahaPosition> jamaGrahas, bool useBoxSign)
    {
        var debilitated = new List<string>();
        foreach (var graha in jamaGrahas)
        {
            int sign = useBoxSign ? graha.House : graha.Sign;
            if (DebilitationSigns.TryGetValue(graha.Name, out int debilSign) && sign == debilSign)
            {
                debilitated.Add(graha.Name.ToUpper());
            }
        }
        return debilitated.Count > 0 ? string.Join(", ", debilitated) : "-";
    }

    /// <summary>
    /// Get Parivarthana (mutual exchange) Jama Grahas.
    /// Parivartana occurs when two planets are in each other's signs.
    /// Example: Mars in Cancer (Moon's sign) AND Moon in Aries (Mars' sign)
    /// </summary>
    private string GetParivarthanaJamaGrahas(List<JamaGrahaPosition> jamaGrahas, bool useBoxSign)
    {
        var exchanges = new HashSet<string>(); // Use HashSet to avoid duplicates
        
        // Planet to owned signs mapping (reverse of SignLords)
        var planetOwnedSigns = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase)
        {
            { "Mars", new List<int> { 1, 8 } },       // Aries, Scorpio
            { "Venus", new List<int> { 2, 7 } },     // Taurus, Libra
            { "Mercury", new List<int> { 3, 6 } },   // Gemini, Virgo
            { "Moon", new List<int> { 4 } },         // Cancer
            { "Sun", new List<int> { 5 } },          // Leo
            { "Jupiter", new List<int> { 9, 12 } },  // Sagittarius, Pisces
            { "Saturn", new List<int> { 10, 11 } }   // Capricorn, Aquarius
            // Rahu and Ketu don't own signs, so they can't do parivartana
        };
        
        foreach (var graha1 in jamaGrahas)
        {
            // Skip Rahu and Ketu as they don't own signs
            if (graha1.Name.Equals("Rahu", StringComparison.OrdinalIgnoreCase) || 
                graha1.Name.Equals("Ketu", StringComparison.OrdinalIgnoreCase))
                continue;
                
            int sign1 = useBoxSign ? graha1.House : graha1.Sign;
            string lord1 = SignLords.GetValueOrDefault(sign1, "");
            
            // Skip if graha1 is in its own sign (no exchange needed)
            if (lord1.Equals(graha1.Name, StringComparison.OrdinalIgnoreCase))
                continue;
            
            // Find the lord of the sign graha1 is in
            var graha2 = jamaGrahas.FirstOrDefault(p => 
                p.Name.Equals(lord1, StringComparison.OrdinalIgnoreCase));
            
            if (graha2 == null) continue;
            
            int sign2 = useBoxSign ? graha2.House : graha2.Sign;
            
            // Check if graha2 is in a sign owned by graha1
            if (planetOwnedSigns.TryGetValue(graha1.Name, out var ownedSigns) && 
                ownedSigns.Contains(sign2))
            {
                // Create a sorted pair to avoid duplicates like "Mars-Moon" and "Moon-Mars"
                var pair = new[] { graha1.Name, graha2.Name }.OrderBy(n => n).ToArray();
                exchanges.Add($"{pair[0]}-{pair[1]}");
            }
        }
        
        return exchanges.Count > 0 ? string.Join(", ", exchanges) : "-";
    }

    /// Get list of exalted planets
    /// </summary>
    private string GetExaltedPlanets(List<PlanetPosition> planets)
    {
        var exalted = new List<string>();
        foreach (var planet in planets)
        {
            if (ExaltationSigns.TryGetValue(planet.Name, out int exaltSign) && planet.Sign == exaltSign)
            {
                exalted.Add(planet.Name.ToUpper());
            }
        }
        return exalted.Count > 0 ? string.Join(", ", exalted) : "-";
    }

    /// <summary>
    /// Get list of debilitated planets
    /// </summary>
    private string GetDebilitatedPlanets(List<PlanetPosition> planets)
    {
        var debilitated = new List<string>();
        foreach (var planet in planets)
        {
            if (DebilitationSigns.TryGetValue(planet.Name, out int debilSign) && planet.Sign == debilSign)
            {
                debilitated.Add(planet.Name.ToUpper());
            }
        }
        return debilitated.Count > 0 ? string.Join(", ", debilitated) : "-";
    }

    /// <summary>
    /// Get Parivarthana (mutual exchange) planets
    /// </summary>
    private string GetParivarthanaPlanets(List<PlanetPosition> planets)
    {
        var exchanges = new List<string>();
        
        foreach (var planet1 in planets)
        {
            string lord1 = SignLords.GetValueOrDefault(planet1.Sign, "");
            
            // Find if there's a planet in the sign owned by planet1
            var planet2 = planets.FirstOrDefault(p => 
                p.Name == lord1 && 
                SignLords.GetValueOrDefault(p.Sign, "") == planet1.Name);
            
            if (planet2 != null && !exchanges.Contains($"{planet2.Name}-{planet1.Name}"))
            {
                exchanges.Add($"{planet1.Name}-{planet2.Name}");
            }
        }
        
        return exchanges.Count > 0 ? string.Join(", ", exchanges) : "-";
    }
}
