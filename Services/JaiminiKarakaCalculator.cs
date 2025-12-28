using System.Collections.Generic;
using System.Linq;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Calculates Jaimini Karakas based on 8-Chara scheme
/// Karakas are assigned based on the degree of planets within their signs
/// </summary>
public class JaiminiKarakaCalculator
{
    // Karaka names in order from highest degree to lowest
    private static readonly string[] KarakaNames = 
    {
        "AK",   // Atmakaraka - highest degree
        "AmK",  // Amatyakaraka - 2nd highest
        "BK",   // Bhratrikaraka - 3rd highest
        "MK",   // Matrikaraka - 4th highest
        "PiK",  // Pitrikaraka - 5th highest
        "PuK",  // Putrakaraka - 6th highest
        "GK",   // Gnatikaraka - 7th highest
        "DK"    // Darakaraka - 8th (lowest degree)
    };

    private static readonly string[] KarakaNamesTamil = 
    {
        "ஆத்ம",   // AK
        "அமாத்",  // AmK
        "பிராத்", // BK
        "மாத்ரு", // MK
        "பித்ரு", // PiK
        "புத்ர",  // PuK
        "ஞாதி",   // GK
        "தார"    // DK
    };

    // Planets that participate in Karaka calculation (exclude Ketu, include Rahu)
    private static readonly HashSet<string> KarakaPlanets = new()
    {
        "Sun", "Moon", "Mars", "Mercury", "Jupiter", "Venus", "Saturn", "Rahu"
    };

    /// <summary>
    /// Calculate and assign Jaimini Karakas to planets
    /// Based on 8-Chara scheme using degree within sign
    /// </summary>
    public void CalculateKarakas(List<PlanetPosition> planets)
    {
        // Get only karaka-participating planets
        var karakaPlanets = planets
            .Where(p => KarakaPlanets.Contains(p.Name))
            .ToList();

        if (karakaPlanets.Count == 0) return;

        // For Rahu, use (30 - degree) since Rahu moves backward
        // Sort by degree in sign (highest to lowest)
        var sortedPlanets = karakaPlanets
            .Select(p => new 
            { 
                Planet = p, 
                KarakaDegree = p.Name == "Rahu" ? (30.0 - p.DegreeInSign) : p.DegreeInSign 
            })
            .OrderByDescending(x => x.KarakaDegree)
            .ToList();

        // Assign karakas based on order
        var names = ZodiacUtils.IsTamil ? KarakaNamesTamil : KarakaNames;
        for (int i = 0; i < sortedPlanets.Count && i < names.Length; i++)
        {
            sortedPlanets[i].Planet.Karaka = names[i];
        }

        // Ketu doesn't get a Karaka - ensure it's empty
        var ketu = planets.FirstOrDefault(p => p.Name == "Ketu");
        if (ketu != null) ketu.Karaka = "";
    }
}
