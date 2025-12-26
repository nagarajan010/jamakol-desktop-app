using System.Collections.Generic;

namespace JamakolAstrology.Models;

/// <summary>
/// Holds Ashtakavarga calculation results
/// </summary>
public class AshtakavargaData
{
    /// <summary>
    /// Pinda calculations for each planet (and Lagna if applicable).
    /// </summary>
    public Dictionary<Planet, PindaResult> Pindas { get; set; } = new();

    // Also store the "Lagna" pinda separately if needed, or stick to Planet key.
    // Since Lagna isn't in Planet enum, I'll add a property or use a specific convention.
    public PindaResult LagnaPinda { get; set; } = new();

    /// <summary>
    /// Bhinnashtakavarga (Individual AV) for each planet (Sun to Saturn + Lagna).
    /// Key is the Planet ID (or enum value). 
    /// Value is an array of 12 integers representing points in signs 1-12 (index 0-11).
    /// </summary>
    public Dictionary<Planet, int[]> Bhinnashtakavarga { get; set; } = new();

    /// <summary>
    /// Sarvashtakavarga (Total AV).
    /// Array of 12 integers representing total points in signs 1-12.
    /// Typically sums Sun to Saturn.
    /// </summary>
    public int[] Sarvashtakavarga { get; set; } = new int[12];

    /// <summary>
    /// Sarvashtakavarga including Lagna.
    /// Array of 12 integers.
    /// </summary>
    public int[] SarvashtakavargaWithLagna { get; set; } = new int[12];

    /// <summary>
    /// Lagna Ashtakavarga points (1-12).
    /// </summary>
    public int[] LagnaAshtakavarga { get; set; } = new int[12];
}
