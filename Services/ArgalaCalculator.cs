using System.Collections.Generic;
using System.Linq;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>One intervening house acting on a reference sign.</summary>
public class Argala
{
    /// <summary>Which house from the reference intervenes: 2, 4, 11 or 5.</summary>
    public int ArgalaHouse { get; set; }
    public int ArgalaRashi { get; set; }

    /// <summary>Planets standing there - they are what gives the Argala its force.</summary>
    public List<Planet> ArgalaPlanets { get; set; } = new();

    /// <summary>The counter-house that can obstruct it: 12, 10, 3 or 9.</summary>
    public int VirodhaHouse { get; set; }
    public int VirodhaRashi { get; set; }
    public List<Planet> VirodhaPlanets { get; set; } = new();

    /// <summary>True when the obstruction holds and the intervention does not operate.</summary>
    public bool Obstructed { get; set; }

    /// <summary>The 5th is a secondary Argala, weaker than the primary three.</summary>
    public bool Secondary { get; set; }

    /// <summary>An Argala with nothing in it has nothing to intervene with.</summary>
    public bool Active => ArgalaPlanets.Count > 0 && !Obstructed;

    /// <summary>One table cell: the planets, and what blocks them if anything does.</summary>
    public string Summary
    {
        get
        {
            if (ArgalaPlanets.Count == 0) return "-";
            string planets = string.Join(" ", ArgalaPlanets.Select(Abbr));
            return Obstructed
                ? $"{planets}  (blocked by {string.Join(" ", VirodhaPlanets.Select(Abbr))})"
                : planets;
        }
    }

    // The two-letter names the charts use (Su, Mo...), not the astronomical glyphs.
    private static string Abbr(Planet p) => ZodiacUtils.PlanetAbbreviations.TryGetValue(p, out var s) ? s : p.ToString();
}

/// <summary>Every intervention acting on one house.</summary>
public class ArgalaReport
{
    public int House { get; set; }
    public int Rashi { get; set; }
    public List<Argala> Argalas { get; set; } = new();

    public string HouseLabel => $"{House} - {ZodiacUtils.GetSignName(Rashi + 1)}";
    public string Second => Argalas.First(a => a.ArgalaHouse == 2).Summary;
    public string Fourth => Argalas.First(a => a.ArgalaHouse == 4).Summary;
    public string Eleventh => Argalas.First(a => a.ArgalaHouse == 11).Summary;
    public string Fifth => Argalas.First(a => a.ArgalaHouse == 5).Summary;
}

/// <summary>
/// Argala - Jaimini's doctrine of intervention. Ported from the Android app.
///
/// Planets in certain houses counted from a house intervene in its affairs, and planets in
/// specific counter-houses obstruct that intervention. The 2nd, 4th and 11th cause primary Argala,
/// obstructed by the 12th, 10th and 3rd. The 5th causes a secondary, weaker Argala, obstructed by
/// the 9th.
///
/// Obstruction is decided by COUNT OF OCCUPANTS: the counter-house must hold at least as many
/// planets as the Argala house to block it, so equal counts obstruct. An empty Argala house is
/// reported as inactive rather than obstructed - there is nothing there to intervene.
///
/// The nodes count as occupants: Jaimini's test is a headcount, not a judgement of nature.
/// </summary>
public class ArgalaCalculator
{
    private static readonly (int Argala, int Virodha, bool Secondary)[] Rules =
    {
        (2, 12, false), (4, 10, false), (11, 3, false), (5, 9, true)
    };

    private static readonly Planet[] Grahas =
    {
        Planet.Sun, Planet.Moon, Planet.Mars, Planet.Mercury, Planet.Jupiter,
        Planet.Venus, Planet.Saturn, Planet.Rahu, Planet.Ketu
    };

    private readonly IRashiChart _chart;
    private readonly Dictionary<int, List<Planet>> _bySign;

    public ArgalaCalculator(IRashiChart chart)
    {
        _chart = chart;
        _bySign = Grahas.GroupBy(chart.SignOf).ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>Interventions acting on a 0-based sign.</summary>
    public List<Argala> ForRashi(int fromRashi)
    {
        return Rules.Select(rule =>
        {
            // Houses count inclusively, so the Nth house is N-1 signs along.
            int aRashi = (fromRashi + rule.Argala - 1) % 12;
            int vRashi = (fromRashi + rule.Virodha - 1) % 12;
            var aPlanets = _bySign.TryGetValue(aRashi, out var a) ? a : new List<Planet>();
            var vPlanets = _bySign.TryGetValue(vRashi, out var v) ? v : new List<Planet>();

            return new Argala
            {
                ArgalaHouse = rule.Argala,
                ArgalaRashi = aRashi,
                ArgalaPlanets = aPlanets,
                VirodhaHouse = rule.Virodha,
                VirodhaRashi = vRashi,
                VirodhaPlanets = vPlanets,
                // Equal counts obstruct: the intervention must be the stronger side to survive.
                Obstructed = aPlanets.Count > 0 && vPlanets.Count >= aPlanets.Count,
                Secondary = rule.Secondary
            };
        }).ToList();
    }

    /// <summary>Argala on each of the twelve houses, in house order.</summary>
    public List<ArgalaReport> ForAllHouses() =>
        Enumerable.Range(0, 12).Select(h =>
        {
            int rashi = (_chart.Lagna + h) % 12;
            return new ArgalaReport { House = h + 1, Rashi = rashi, Argalas = ForRashi(rashi) };
        }).ToList();
}
