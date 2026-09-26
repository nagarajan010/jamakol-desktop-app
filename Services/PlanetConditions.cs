using System;
using System.Collections.Generic;
using System.Linq;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

public enum DignityState { Exalted, Moolatrikona, OwnSign, Neutral, Debilitated }

/// <summary>
/// Exaltation, moolatrikona, own sign and debilitation. Ported from the Android app. The nodes
/// have no agreed exaltation; the commonest assignment (Taurus/Scorpio) is used and flagged.
/// </summary>
public static class Dignity
{
    /// <summary>Exaltation sign (0-based) and degree of deepest exaltation.</summary>
    private static readonly Dictionary<Planet, (int Sign, double Degree)> Exaltation = new()
    {
        { Planet.Sun, (0, 10) }, { Planet.Moon, (1, 3) }, { Planet.Mars, (9, 28) },
        { Planet.Mercury, (5, 15) }, { Planet.Jupiter, (3, 5) }, { Planet.Venus, (11, 27) },
        { Planet.Saturn, (6, 20) },
        { Planet.Rahu, (1, 20) }, { Planet.Ketu, (7, 20) } // disputed
    };

    private static readonly Dictionary<Planet, int[]> OwnSigns = new()
    {
        { Planet.Sun, new[] { 4 } }, { Planet.Moon, new[] { 3 } }, { Planet.Mars, new[] { 0, 7 } },
        { Planet.Mercury, new[] { 2, 5 } }, { Planet.Jupiter, new[] { 8, 11 } },
        { Planet.Venus, new[] { 1, 6 } }, { Planet.Saturn, new[] { 9, 10 } }
    };

    private static readonly Dictionary<Planet, (int Sign, double From, double To)> Moolatrikona = new()
    {
        { Planet.Sun, (4, 0, 20) }, { Planet.Moon, (1, 4, 30) }, { Planet.Mars, (0, 0, 12) },
        { Planet.Mercury, (5, 16, 20) }, { Planet.Jupiter, (8, 0, 10) },
        { Planet.Venus, (6, 0, 15) }, { Planet.Saturn, (10, 0, 20) }
    };

    public static bool IsDisputed(Planet p) => p == Planet.Rahu || p == Planet.Ketu;

    /// <param name="rashi">0-based sign.</param>
    public static DignityState Of(Planet planet, int rashi, double degreeInSign)
    {
        if (Exaltation.TryGetValue(planet, out var ex))
        {
            if (rashi == ex.Sign) return DignityState.Exalted;
            if (rashi == (ex.Sign + 6) % 12) return DignityState.Debilitated;
        }
        if (Moolatrikona.TryGetValue(planet, out var mt) && rashi == mt.Sign && degreeInSign >= mt.From && degreeInSign < mt.To)
            return DignityState.Moolatrikona;
        if (OwnSigns.TryGetValue(planet, out var own) && own.Contains(rashi)) return DignityState.OwnSign;
        return DignityState.Neutral;
    }

    /// <summary>Degrees from the peak of exaltation (or the matching debilitation point), when in either sign.</summary>
    public static double? DegreesFromPeak(Planet planet, int rashi, double degreeInSign)
    {
        if (!Exaltation.TryGetValue(planet, out var ex)) return null;
        return rashi == ex.Sign || rashi == (ex.Sign + 6) % 12 ? Math.Abs(degreeInSign - ex.Degree) : null;
    }
}

/// <summary>
/// Gandanta - the knot where a water sign ends and a fire sign begins (Pisces-Aries, Cancer-Leo,
/// Scorpio-Sagittarius): the last 3 20' of the one and the first 3 20' of the next, a nakshatra
/// pada either side.
/// </summary>
public static class Gandanta
{
    private const double Orb = 3.0 + 20.0 / 60.0;
    private static readonly int[] WaterSigns = { 3, 7, 11 };

    public static bool Is(int rashi, double degreeInSign)
    {
        if (WaterSigns.Contains(rashi) && degreeInSign >= 30.0 - Orb) return true;
        return WaterSigns.Contains((rashi + 11) % 12) && degreeInSign <= Orb;
    }
}

/// <summary>
/// Mrityubhaga - the fateful degrees. Ported from the Android app, where the table was verified
/// cell by cell against Jataka Parijata p. 38, and against Jagannatha Hora (PyJHora) on all 132
/// cells and all eleven orbs. The Lagna row follows the two sources over Wilhelm's three differing
/// cells, and the Moon row follows Brhat Prajapatya / Phaladipika.
///
/// No text states an orb. The default is Jagannatha Hora's: 40' for the Lagna, Moon and Mercury,
/// 20' for the Sun, 15' for the rest. Wilhelm's +/-1 degree is the wider alternative.
/// </summary>
public static class Mrityubhaga
{
    private static readonly Dictionary<Planet, int[]> Table = new()
    {
        { Planet.Sun, new[] { 20, 9, 12, 6, 8, 24, 16, 17, 22, 2, 3, 23 } },
        { Planet.Moon, new[] { 26, 12, 13, 25, 24, 11, 26, 14, 13, 25, 5, 12 } },
        { Planet.Mars, new[] { 19, 28, 25, 23, 29, 28, 14, 21, 2, 15, 11, 6 } },
        { Planet.Mercury, new[] { 15, 14, 13, 12, 8, 18, 20, 10, 21, 22, 7, 5 } },
        { Planet.Jupiter, new[] { 19, 29, 12, 27, 6, 4, 13, 10, 17, 11, 15, 28 } },
        { Planet.Venus, new[] { 28, 15, 11, 17, 10, 13, 4, 6, 27, 12, 29, 19 } },
        { Planet.Saturn, new[] { 10, 4, 7, 9, 12, 16, 3, 18, 28, 14, 13, 15 } },
        { Planet.Rahu, new[] { 14, 13, 12, 11, 24, 23, 22, 21, 10, 20, 18, 8 } },
        { Planet.Ketu, new[] { 8, 18, 20, 10, 21, 22, 23, 24, 11, 12, 13, 14 } }
    };

    /// <summary>The Lagna's row, as Jataka Parijata and consultlunarastro give it.</summary>
    public static readonly int[] Lagna = { 1, 9, 22, 22, 25, 2, 4, 23, 18, 20, 24, 10 };

    /// <summary>Orb in degrees; the Lagna is passed as null.</summary>
    public static double OrbFor(Planet? graha, bool wilhelm = false)
    {
        if (wilhelm) return 1.0;
        return graha switch
        {
            null or Planet.Moon or Planet.Mercury => 40.0 / 60.0,
            Planet.Sun => 20.0 / 60.0,
            _ => 15.0 / 60.0
        };
    }

    /// <summary>The fateful degree for a subject in the sign a longitude falls in.</summary>
    public static double? FatalDegreeFor(double longitude, Planet? graha)
    {
        int sign = ((int)(longitude / 30.0) % 12 + 12) % 12;
        if (graha == null) return Lagna[sign];
        return Table.TryGetValue(graha.Value, out var row) ? row[sign] : null;
    }

    public static bool IsFatal(double longitude, Planet? graha, bool wilhelm = false)
    {
        double? degree = FatalDegreeFor(longitude, graha);
        if (degree == null) return false;
        double inSign = ((longitude % 360.0) + 360.0) % 360.0 % 30.0;
        return Math.Abs(inSign - degree.Value) <= OrbFor(graha, wilhelm);
    }
}

/// <summary>
/// Graha yuddha - planetary war. Ported from the Android app.
///
/// Two grahas within a degree - and in the same or adjacent sign and nakshatra - are at war. The
/// common shortcut "the higher degree wins" is what Sanjay Rath files under "Classical Confusion":
/// the war goes on a MAJORITY of three senses of being further north - longitude (resources),
/// latitude (skill), declination (focus). All nine grahas can fight. A node has no latitude or
/// declination to compare, so a war involving one is decided on longitude alone.
///
/// The Android app's natal check stood ecliptic latitude in for declination, since its positions
/// do not carry it. Here the TRUE declination is read from the ephemeris, so all three criteria are
/// real.
/// </summary>
public static class GrahaYuddha
{
    public const double WarOrb = 1.0;

    public record Combatant(Planet Planet, double Longitude, double Latitude, double Declination, bool IsRetrograde = false);

    public record Result(Planet Winner, Planet Loser, Planet LongitudeWinner, Planet LatitudeWinner,
                         Planet DeclinationWinner, double GapDegrees)
    {
        /// <summary>Whether the war went against the planet holding the higher longitude.</summary>
        public bool DefiesLongitude => Winner != LongitudeWinner;

        /// <summary>
        /// Share of the loser's strength taken, 0-100: the longitudinal gap over the one-degree
        /// range. A WIDER gap costs the loser more - at exact conjunction neither has the upper hand.
        /// </summary>
        public double LossPercent => Math.Clamp(Math.Abs(GapDegrees) / WarOrb * 100.0, 0, 100);
    }

    private static readonly HashSet<Planet> Nodes = new() { Planet.Rahu, Planet.Ketu };

    public static double Separation(double a, double b)
    {
        double d = Math.Abs(a - b) % 360.0;
        return d > 180.0 ? 360.0 - d : d;
    }

    private static bool Adjacent(int a, int b, int count)
    {
        int d = Math.Abs(a - b);
        return d <= 1 || d == count - 1;
    }

    public static bool IsWar(Combatant a, Combatant b)
    {
        if (a.Planet == b.Planet || Separation(a.Longitude, b.Longitude) > WarOrb) return false;
        int SignOf(double l) => (int)(l / 30.0) % 12;
        int NakOf(double l) => (int)(l / (360.0 / 27.0)) % 27;
        return Adjacent(SignOf(a.Longitude), SignOf(b.Longitude), 12)
            && Adjacent(NakOf(a.Longitude), NakOf(b.Longitude), 27);
    }

    public static Result Judge(Combatant a, Combatant b)
    {
        // Further north in longitude is the one ahead along the SHORTER arc - a pair straddling
        // Aries 0 has the greater number behind.
        double forward = ((b.Longitude - a.Longitude) % 360.0 + 360.0) % 360.0;
        var lonW = forward > 180.0 ? a : b;
        var latW = a.Latitude > b.Latitude ? a : b;
        var decW = a.Declination > b.Declination ? a : b;

        Combatant winner;
        if (Nodes.Contains(a.Planet) || Nodes.Contains(b.Planet)) winner = lonW;
        else winner = new[] { lonW, latW, decW }.Count(x => ReferenceEquals(x, a)) >= 2 ? a : b;
        var loser = ReferenceEquals(winner, a) ? b : a;

        return new Result(winner.Planet, loser.Planet, lonW.Planet, latW.Planet, decW.Planet,
                          Separation(a.Longitude, b.Longitude));
    }

    /// <summary>Every war among the combatants, each pair once.</summary>
    public static List<Result> Wars(IReadOnlyList<Combatant> combatants)
    {
        var wars = new List<Result>();
        for (int i = 0; i < combatants.Count; i++)
            for (int j = i + 1; j < combatants.Count; j++)
                if (IsWar(combatants[i], combatants[j]))
                    wars.Add(Judge(combatants[i], combatants[j]));
        return wars;
    }
}

/// <summary>One graha's conditions, for the planet-conditions table.</summary>
public class PlanetCondition
{
    public Planet Graha { get; set; }
    public string GrahaName => ZodiacUtils.GetPlanetName(Graha);
    public string Position { get; set; } = "";
    public string Dignity { get; set; } = "";
    public string Gandanta { get; set; } = "";
    public string Mrityubhaga { get; set; } = "";
    public string War { get; set; } = "";
}

/// <summary>Builds the per-graha condition rows for a chart.</summary>
public static class PlanetConditions
{
    private static readonly Planet[] Grahas =
    {
        Planet.Sun, Planet.Moon, Planet.Mars, Planet.Mercury, Planet.Jupiter,
        Planet.Venus, Planet.Saturn, Planet.Rahu, Planet.Ketu
    };

    public static List<PlanetCondition> For(ChartData chart, EphemerisService eph)
    {
        // Only the nine real graha rows - the Aprakash grahas reuse Planet.Sun as a placeholder.
        var rows = chart.Planets
            .Where(p => Grahas.Contains(p.Planet)
                        && ZodiacUtils.PlanetNames.TryGetValue(p.Planet, out var n) && p.Name == n)
            .GroupBy(p => p.Planet).Select(g => g.First())
            .ToList();

        var combatants = rows.Select(p => new GrahaYuddha.Combatant(
            p.Planet, p.Longitude,
            p.Planet is Planet.Rahu or Planet.Ketu ? 0.0 : p.Latitude,
            p.Planet is Planet.Rahu or Planet.Ketu ? 0.0 : eph.GetDeclination(chart.JulianDay, (int)p.Planet),
            p.IsRetrograde)).ToList();
        var wars = GrahaYuddha.Wars(combatants);

        string Abbr(Planet p) => ZodiacUtils.PlanetAbbreviations.TryGetValue(p, out var a) ? a : p.ToString();

        var result = new List<PlanetCondition>();
        foreach (var p in rows.OrderBy(r => Array.IndexOf(Grahas, r.Planet)))
        {
            int rashi = p.Sign - 1;
            var state = Services.Dignity.Of(p.Planet, rashi, p.DegreeInSign);
            string dignity = state switch
            {
                DignityState.Exalted => "Exalted",
                DignityState.Moolatrikona => "Moolatrikona",
                DignityState.OwnSign => "Own sign",
                DignityState.Debilitated => "Debilitated",
                _ => ""
            };
            if (state is DignityState.Exalted or DignityState.Debilitated)
            {
                double? off = Services.Dignity.DegreesFromPeak(p.Planet, rashi, p.DegreeInSign);
                if (off != null) dignity += $" ({off:F1} from peak)";
            }
            if (dignity.Length > 0 && Services.Dignity.IsDisputed(p.Planet)) dignity += " - disputed for the nodes";

            var war = wars.FirstOrDefault(w => w.Winner == p.Planet || w.Loser == p.Planet);
            string warText = war == null ? ""
                : war.Winner == p.Planet
                    ? $"Wins against {Abbr(war.Loser)}"
                    : $"Loses to {Abbr(war.Winner)} ({war.LossPercent:F0}% of strength){(war.DefiesLongitude ? " - despite the higher longitude" : "")}";

            double? fatal = Services.Mrityubhaga.FatalDegreeFor(p.Longitude, p.Planet);
            result.Add(new PlanetCondition
            {
                Graha = p.Planet,
                Position = $"{ZodiacUtils.GetSignName(p.Sign)} {ZodiacUtils.FormatDegree(p.DegreeInSign)}",
                Dignity = dignity,
                Gandanta = Services.Gandanta.Is(rashi, p.DegreeInSign) ? "Gandanta" : "",
                Mrityubhaga = Services.Mrityubhaga.IsFatal(p.Longitude, p.Planet) ? $"Fatal degree ({fatal:F0})" : "",
                War = warText
            });
        }
        return result;
    }
}
