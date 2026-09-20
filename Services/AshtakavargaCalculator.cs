using System;
using System.Collections.Generic;
using System.Linq;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Calculator for Ashtakavarga points.
/// Ported from Jyotish\Bala\AshtakaVarga.
/// </summary>
public class AshtakavargaCalculator
{
    // Bindu maps: Key is the "Contributor" planet, Value is list of houses (1-based) from that contributor where points are gained.
    
    // Sun's AV (BinduSy)
    // Bindu maps: Key is the "Contributor" planet, Value is list of houses (1-based) from that contributor where points are gained.
    // Keys match the PHP implementation (SY, CH, MA, BU, GU, SK, SA, LG).

    
    // Let's define the tables exactly as in PHP.
    
    // 1. Sun AV
    private static readonly Dictionary<string, int[]> BinduSy = new()
    {
        { "SY", [1, 2, 4, 7, 8, 9, 10, 11] },
        { "CH", [3, 6, 10, 11] },
        { "MA", [1, 2, 4, 7, 8, 9, 10, 11] },
        { "BU", [3, 5, 6, 9, 10, 11, 12] },
        { "GU", [5, 6, 9, 11] },
        { "SK", [6, 7, 12] },
        { "SA", [1, 2, 4, 7, 8, 9, 10, 11] },
        { "LG", [3, 4, 6, 10, 11, 12] }
    };

    // 2. Moon AV (with JH variations applied)
    private static readonly Dictionary<string, int[]> BinduCh = new()
    {
        { "SY", [3, 6, 7, 8, 10, 11] },
        { "CH", [1, 3, 6, 7, 9, 10, 11] }, // JH: +9th from Moon
        { "MA", [2, 3, 5, 6, 10, 11] }, // JH: -9th from Mars
        { "BU", [1, 3, 4, 5, 7, 8, 10, 11] },
        { "GU", [1, 2, 4, 7, 8, 10, 11] }, // JH: +2nd, -12th from Jupiter
        { "SK", [3, 4, 5, 7, 9, 10, 11] },
        { "SA", [3, 5, 6, 11] },
        { "LG", [3, 6, 10, 11] }
    };

    // 3. Mars AV
    private static readonly Dictionary<string, int[]> BinduMa = new()
    {
        { "SY", [3, 5, 6, 10, 11] },
        { "CH", [3, 6, 11] },
        { "MA", [1, 2, 4, 7, 8, 10, 11] },
        { "BU", [3, 5, 6, 11] },
        { "GU", [6, 10, 11, 12] },
        { "SK", [6, 8, 11, 12] },
        { "SA", [1, 4, 7, 8, 9, 10, 11] },
        { "LG", [1, 3, 6, 10, 11] }
    };

    // 4. Mercury AV
    private static readonly Dictionary<string, int[]> BinduBu = new()
    {
        { "SY", [5, 6, 9, 11, 12] },
        { "CH", [2, 4, 6, 8, 10, 11] },
        { "MA", [1, 2, 4, 7, 8, 9, 10, 11] },
        { "BU", [1, 3, 5, 6, 9, 10, 11, 12] },
        { "GU", [6, 8, 11, 12] },
        { "SK", [1, 2, 3, 4, 5, 8, 9, 11] },
        { "SA", [1, 2, 4, 7, 8, 9, 10, 11] },
        { "LG", [1, 2, 4, 6, 8, 10, 11] }
    };

    // 5. Jupiter AV
    private static readonly Dictionary<string, int[]> BinduGu = new()
    {
        { "SY", [1, 2, 3, 4, 7, 8, 9, 10, 11] },
        { "CH", [2, 5, 7, 9, 11] },
        { "MA", [1, 2, 4, 7, 8, 10, 11] },
        { "BU", [1, 2, 4, 5, 6, 9, 10, 11] },
        { "GU", [1, 2, 3, 4, 7, 8, 10, 11] },
        { "SK", [2, 5, 6, 9, 10, 11] },
        { "SA", [3, 5, 6, 12] },
        // 9 points from Lagna, per BPHS. House 7 was missing, which made Jupiter's
        // bhinnashtakavarga total 55 instead of 56 and left the Sarvashtakavarga one short of 337.
        { "LG", [1, 2, 4, 5, 6, 7, 9, 10, 11] }
    };

    // 6. Venus AV (with JH variations applied)
    private static readonly Dictionary<string, int[]> BinduSk = new()
    {
        { "SY", [8, 11, 12] },
        { "CH", [1, 2, 3, 4, 5, 8, 9, 11, 12] },
        { "MA", [3, 4, 6, 9, 11, 12] }, // JH: +4th, -5th from Mars
        { "BU", [3, 5, 6, 9, 11] },
        { "GU", [5, 8, 9, 10, 11] },
        { "SK", [1, 2, 3, 4, 5, 8, 9, 10, 11] },
        { "SA", [3, 4, 5, 8, 9, 10, 11] },
        { "LG", [1, 2, 3, 4, 5, 8, 9, 11] }
    };

    // 7. Saturn AV
    private static readonly Dictionary<string, int[]> BinduSa = new()
    {
        { "SY", [1, 2, 4, 7, 8, 10, 11] },
        { "CH", [3, 6, 11] },
        { "MA", [3, 5, 6, 10, 11, 12] },
        { "BU", [6, 8, 9, 10, 11, 12] },
        { "GU", [5, 6, 11, 12] },
        { "SK", [6, 11, 12] },
        { "SA", [3, 5, 6, 11] },
        { "LG", [1, 3, 4, 6, 10, 11] }
    };

    // 8. Lagna AV
    private static readonly Dictionary<string, int[]> BinduLg = new()
    {
        { "SY", [3, 4, 6, 10, 11, 12] },
        { "CH", [3, 6, 10, 11, 12] },
        { "MA", [1, 3, 6, 10, 11] },
        { "BU", [1, 2, 4, 6, 8, 10, 11] },
        { "GU", [1, 2, 4, 5, 6, 7, 9, 10, 11] },
        { "SK", [1, 2, 3, 4, 5, 8, 9] },
        { "SA", [1, 3, 4, 6, 10, 11] },
        { "LG", [3, 6, 10, 11] }
    };

    public AshtakavargaData Calculate(ChartData chart)
    {
        var result = new AshtakavargaData();

        // Map short codes to Planet enum and positions
        // We need 7 planets + Lagna positions.
        var positions = new Dictionary<string, int>();

        foreach (var p in chart.Planets)
        {
            // Skip Aprakash Graha (shadow planets) - they use Planet.Sun as placeholder
            // and would overwrite the real Sun's position
            if (p.Name == "Dhooma" || p.Name == "Vyatipata" || p.Name == "Parivesha" || 
                p.Name == "Indrachapa" || p.Name == "Upaketu")
            {
                continue;
            }
            
            string key = GetPlanetKey(p.Planet);
            if (key != "")
            {
                positions[key] = p.Sign; // 1-12
            }
        }
        positions["LG"] = chart.AscendantSign; // 1-12



        // Process each AV
        // Order: Sun, Moon, Mars, Mercury, Jupiter, Venus, Saturn, Lagna
        // Keys in output: Sun...Saturn. For Lagna, we can use a special key or just map it?
        // Dictionary<Planet, int[]> Bhinnashtakavarga supports Planet enum. 
        // We should add a pseudo-planet for Lagna or just handle it.
        // Wait, Planet enum doesn't have Lagna. I'll add "Lagna" to the dict?
        // Constraints: Dictionary<Planet, ...>. 
        // I will stick to Planet enum for standard 7.
        // For Lagna, I might need to abuse an unused enum or just add a separate property in the model, or user won't see Lagna AV in the same list?
        // Re-reading Model: "Dictionary<Planet, int[]>".
        // I should probably change the Model Key to string or add Lagna to Planet enum (risky).
        // Or I can just store Lagna AV separately in the model? 
        // Let's modify the Model in the next step to support Lagna Key if needed, or I'll just use a Dictionary<string, int[]> in local vars and decide how to expose it.
        // Actually, let's keep the model using Planet enum for the main 7, and maybe add "Lagna" as a property?
        // Or better, let's cast Lagna to a specific int key if needed. 
        // But for cleaner code, I'll modify `AshtakavargaData` to use `Dictionary<string, int[]>` or just add `LagnaAshtakavarga` property.

        // Actually, the user asked to "include lagna also".
        // I'll calculate it first.

        var avs = new Dictionary<string, Dictionary<string, int[]>>()
        {
            { "SY", BinduSy },
            { "CH", BinduCh },
            { "MA", BinduMa },
            { "BU", BinduBu },
            { "GU", BinduGu },
            { "SK", BinduSk },
            { "SA", BinduSa },
            { "LG", BinduLg }
        };

        var bhinna = new Dictionary<string, int[]>();

        foreach (var avConfig in avs)
        {
            string ownerKey = avConfig.Key; // e.g. "SY" (Sun's AV)
            var binduTable = avConfig.Value;

            int[] points = new int[12]; // for signs 1..12 (indices 0..11)

            foreach (var contributor in binduTable)
            {
                string contribKey = contributor.Key; // e.g. "SY", "LG"...
                int[] houses = contributor.Value;

                if (positions.TryGetValue(contribKey, out int contribSign))
                {
                    // Convert 1-based sign (1-12) to 0-based index (0-11)
                    int contribIndex = contribSign - 1;
                    
                    foreach (int house in houses)
                    {
                        // Calculate target sign using 0-based indexing
                        // Formula: (donor_index + house_offset - 1) % 12
                        // Example: Aries(0) + 1st house = (0 + 1 - 1) % 12 = 0 (Aries)
                        // Example: Pisces(11) + 2nd house = (11 + 2 - 1) % 12 = 0 (Aries)
                        
                        int targetIndex = (contribIndex + house - 1) % 12;



                        // Add point to target sign (index 0-11)
                        points[targetIndex]++;
                    }
                }
            }
            

            
            bhinna[ownerKey] = points;
        }

        // Map back to Model
        foreach (var kvp in bhinna)
        {
            if (kvp.Key == "LG")
            {
                result.LagnaAshtakavarga = kvp.Value;
            }
            else
            {
                Planet p = GetPlanetFromKey(kvp.Key);
                result.Bhinnashtakavarga[p] = kvp.Value;
            }
        }
        
        // Populate Sarvashtakavarga (Sum of Sun..Saturn)
        // And with Lagna
        
        for (int i = 0; i < 12; i++)
        {
            int sumNormal = 0;
            int sumWithLagna = 0;

            foreach (var kvp in bhinna)
            {
                int val = kvp.Value[i];
                if (kvp.Key != "LG")
                {
                    sumNormal += val;
                }
                sumWithLagna += val;
            }
            result.Sarvashtakavarga[i] = sumNormal;
            result.SarvashtakavargaWithLagna[i] = sumWithLagna;
        }

        // Calculate Pindas
        CalculateAllPindas(result, chart, bhinna);

        return result;
    }

    /// <summary>
    /// Calculates reductions and Pindas for all profiles and updates the result.
    /// This should be called before returning result.
    /// </summary>
    private void CalculateAllPindas(AshtakavargaData result, ChartData chart, Dictionary<string, int[]> bhinna)
    {
        // Calculate for each Planet
        foreach (var kvp in bhinna)
        {
            // 1. Get original points (clone it to avoid modifying BAV)
            int[] points = (int[])kvp.Value.Clone();

            // 2. Trikona Shodhana
            PerformTrikonaShodhana(points);

            // 3. Ekadhipatya Shodhana
            PerformEkadhipatyaShodhana(points, chart.Planets);

            // 4. Calculate Pindas
            // Planet occupying signs need to be passed for Graha Pinda
            var pinda = CalculatePinda(points, chart.Planets);

            if (kvp.Key == "LG")
            {
                result.LagnaPinda = pinda;
            }
            else
            {
                Planet p = GetPlanetFromKey(kvp.Key);
                result.Pindas[p] = pinda;
            }
        }
    }

    private void PerformTrikonaShodhana(int[] points)
    {
        // 4 Groups of 3 signs
        // Fire: Aries(0), Leo(4), Sag(8)
        // Earth: Taurus(1), Virgo(5), Cap(9)
        // Air: Gemini(2), Libra(6), Aqu(10)
        // Water: Cancer(3), Scorpio(7), Pisces(11)

        int[][] groups = new int[][]
        {
            new[] { 0, 4, 8 },
            new[] { 1, 5, 9 },
            new[] { 2, 6, 10 },
            new[] { 3, 7, 11 }
        };

        foreach (var group in groups)
        {
            int min = 1000;
            foreach (int idx in group)
            {
                if (points[idx] < min) min = points[idx];
            }

            // Where the smallest count in a trine is zero the WHOLE trine is voided, rather
            // than subtracting zero and leaving the other two signs' bindus standing.
            foreach (int idx in group)
            {
                points[idx] = (min == 0) ? 0 : points[idx] - min;
            }
        }
    }

    private void PerformEkadhipatyaShodhana(int[] points, List<PlanetPosition> planets)
    {
        // Pairs owned by same planet. Sun(4) and Moon(3) have only 1 sign, so skipped.
        // Mars: 0, 7 (Aries, Scorpio)
        // Mercury: 2, 5 (Gemini, Virgo)
        // Jupiter: 8, 11 (Sag, Pisces)
        // Venus: 1, 6 (Taurus, Libra)
        // Saturn: 9, 10 (Cap, Aqu)

        var pairs = new int[][]
        {
            new[] { 0, 7 },
            new[] { 2, 5 },
            new[] { 8, 11 },
            new[] { 1, 6 },
            new[] { 9, 10 }
        };

        // Only the SEVEN grahas count as occupants. Ekadhipatya is a shared-LORDSHIP reduction,
        // and Rahu/Ketu own no sign, so they take no part in it - testing every body in the list
        // treated a node-tenanted sign as occupied and wrongly protected it from reduction.
        // The Aprakash Grahas must be excluded for the same reason: they carry Planet.Sun as a
        // placeholder, so each would otherwise read as a Sun occupying its own sign.
        var sevenGrahas = new[]
        {
            Planet.Sun, Planet.Moon, Planet.Mars,
            Planet.Mercury, Planet.Jupiter, Planet.Venus, Planet.Saturn
        };

        var occupants = planets
            .Where(p => sevenGrahas.Contains(p.Planet) && p.Name == ZodiacUtils.PlanetNames[p.Planet])
            .ToList();

        bool IsOccupied(int signIndex) // 0-based
            => occupants.Any(p => (p.Sign - 1) == signIndex);

        foreach (var pair in pairs)
        {
            int idx1 = pair[0];
            int idx2 = pair[1];
            int p1 = points[idx1];
            int p2 = points[idx2];

            // A sign already at zero is left alone.
            if (p1 == 0 || p2 == 0) continue;

            bool occ1 = IsOccupied(idx1);
            bool occ2 = IsOccupied(idx2);

            // Both tenanted: no reduction.
            if (occ1 && occ2) continue;

            if (!occ1 && !occ2)
            {
                // Neither tenanted: equal counts both go to zero, otherwise the higher
                // comes down to the lower.
                if (p1 == p2)
                {
                    points[idx1] = 0;
                    points[idx2] = 0;
                }
                else
                {
                    int low = Math.Min(p1, p2);
                    points[idx1] = low;
                    points[idx2] = low;
                }
            }
            else
            {
                // One tenanted: the empty sign is zeroed when it holds no more than the
                // occupied one, otherwise it is brought down to the occupied one's count.
                int emptyIdx = occ1 ? idx2 : idx1;
                int occIdx = occ1 ? idx1 : idx2;
                points[emptyIdx] = (points[emptyIdx] <= points[occIdx]) ? 0 : points[occIdx];
            }
        }
    }

    private PindaResult CalculatePinda(int[] reducedPoints, List<PlanetPosition> planets)
    {
        // Rasi Multipliers
        // Aries(0): 7, Tau: 10, Gem: 8, Can: 4, Leo: 10, Vir: 5, Lib: 7, Sco: 8, Sag: 9, Cap: 5, Aqu: 11, Pis: 12
        int[] rasiMult = { 7, 10, 8, 4, 10, 5, 7, 8, 9, 5, 11, 12 };
        
        // Graha Multipliers
        // Standard BPHS: Sun: 5, Moon: 21, Mars: 8, Merc: 5, Jup: 10, Ven: 6, Sat: 3
        // User Provided Example imply: Sun: 5, Moon: 5, Mars: 8, Merc: 5, Jup: 10, Ven: 7, Sat: 5
        // We will use the User's values as they match their "Working Software".
        var grahaMult = new Dictionary<Planet, int>
        {
            { Planet.Sun, 5 }, 
            { Planet.Moon, 5 }, 
            { Planet.Mars, 8 }, 
            { Planet.Mercury, 5 },
            { Planet.Jupiter, 10 }, 
            { Planet.Venus, 7 }, 
            { Planet.Saturn, 5 }
        };

        long rasiSum = 0;
        long grahaSum = 0;

        for (int i = 0; i < 12; i++)
        {
            // Rasi Pinda contribution
            rasiSum += reducedPoints[i] * rasiMult[i];

            // Graha Pinda contribution
            // Find planets in this sign (i+1)
            var occupants = planets.Where(p => (p.Sign - 1) == i).ToList();
            foreach (var p in occupants)
            {
                if (grahaMult.TryGetValue(p.Planet, out int mult))
                {
                    grahaSum += reducedPoints[i] * mult;
                }
            }
        }

        return new PindaResult
        {
            RasiPinda = (int)rasiSum,
            GrahaPinda = (int)grahaSum
        };
    }

    private string GetPlanetKey(Planet p)
    {
        return p switch
        {
            Planet.Sun => "SY",
            Planet.Moon => "CH",
            Planet.Mars => "MA",
            Planet.Mercury => "BU",
            Planet.Jupiter => "GU",
            Planet.Venus => "SK",
            Planet.Saturn => "SA",
            _ => ""
        };
    }

    private Planet GetPlanetFromKey(string key)
    {
        return key switch
        {
            "SY" => Planet.Sun,
            "CH" => Planet.Moon,
            "MA" => Planet.Mars,
            "BU" => Planet.Mercury,
            "GU" => Planet.Jupiter,
            "SK" => Planet.Venus,
            "SA" => Planet.Saturn,
            _ => Planet.Sun // Should not happen for standard 7
        };
    }
}
