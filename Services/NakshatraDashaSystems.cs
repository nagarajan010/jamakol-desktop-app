using System;
using System.Collections.Generic;
using System.Linq;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// A nakshatra-based (udu) dasha system.
///
/// Every system in this family works the same way: the Moon's nakshatra picks a starting lord,
/// the unelapsed part of that nakshatra (or of the lord's whole arc) gives the balance of the
/// first period, and the lords then run in a fixed cycle whose lengths sum to the system's span.
/// Sub-periods at every depth divide their parent in the same proportions. Only the tables differ,
/// so <see cref="UduDashaCalculator"/> implements the shared machinery once.
///
/// Every table and rule here is from Sanjay Rath, with the section he gives.
/// </summary>
public abstract class NakshatraDashaSystem
{
    public const double NakshatraSpan = 360.0 / 27.0;

    /// <summary>Name shown in the system picker.</summary>
    public abstract string Name { get; }

    /// <summary>The lords in cycle order.</summary>
    public abstract Planet[] Sequence { get; }

    /// <summary>Years each lord rules. Covers every entry in <see cref="Sequence"/>.</summary>
    public abstract Dictionary<Planet, double> Years { get; }

    /// <summary>Total span in years - the sum of the lords' years.</summary>
    public double TotalYears => Sequence.Sum(p => Years[p]);

    /// <summary>
    /// The birth criterion the classics attach to this system, or null for the ones in general
    /// use. Shown beside the periods rather than tested: the reader who knows the rule is better
    /// served by seeing the periods and the condition than by a system silently hidden.
    /// </summary>
    public virtual string? Condition => null;

    /// <summary>The lord a Moon in this nakshatra (0-based) starts the chart under.</summary>
    public abstract Planet StartingLord(int nakshatra);

    /// <summary>
    /// How much of the starting lord's period is still unspent at birth, as a fraction.
    /// The default measures against the one nakshatra the Moon is in.
    /// </summary>
    public virtual double BalanceFraction(int nakshatra, double degreeInNakshatra)
        => 1.0 - degreeInNakshatra / NakshatraSpan;

    /// <summary>Label for a lord, so systems whose lords are not grahas can name them.</summary>
    public virtual string? LordLabel(Planet lord) => null;

    /// <summary>
    /// Whether sub-periods run BACKWARD through the sequence rather than forward.
    ///
    /// Only Ashtottari. Rath 7.2.1.2: "Venus as the Guru is left handed i.e. the antardasa shall
    /// be reckoned in the reverse... The first antardasa is of the planet preceding the dasa lord
    /// ... till the last antardasa is of the dasa lord itself." It applies at every depth.
    /// </summary>
    public virtual bool SubPeriodsReversed => false;

    public override string ToString() => Name;
}

/// <summary>
/// Vimshottari - 120 years over nine grahas. Kept in the family so the shared engine can be
/// checked against the app's own Vimshottari calculator, which it must reproduce exactly.
/// </summary>
public sealed class VimshottariSystem : NakshatraDashaSystem
{
    public override string Name => "Vimshottari";

    public override Planet[] Sequence { get; } =
    {
        Planet.Ketu, Planet.Venus, Planet.Sun, Planet.Moon, Planet.Mars,
        Planet.Rahu, Planet.Jupiter, Planet.Saturn, Planet.Mercury
    };

    public override Dictionary<Planet, double> Years { get; } = new()
    {
        { Planet.Ketu, 7 }, { Planet.Venus, 20 }, { Planet.Sun, 6 }, { Planet.Moon, 10 },
        { Planet.Mars, 7 }, { Planet.Rahu, 18 }, { Planet.Jupiter, 16 }, { Planet.Saturn, 19 },
        { Planet.Mercury, 17 }
    };

    public override Planet StartingLord(int nakshatra) => Sequence[nakshatra % 9];
}

/// <summary>
/// Ashtottari - 108 years over eight grahas, Ketu taking no period.
///
/// The periods do not map one-to-one onto nakshatras: each lord owns an unequal ARC, and the
/// balance at birth is measured against that whole arc - Rath: "Subtract the Moon longitude from
/// the end longitude of the group, divide by the group span and multiply by the dasa period."
/// </summary>
public sealed class AshtottariSystem : NakshatraDashaSystem
{
    public override string Name => "Ashtottari";

    public override Planet[] Sequence { get; } =
    {
        Planet.Sun, Planet.Moon, Planet.Mars, Planet.Mercury,
        Planet.Saturn, Planet.Jupiter, Planet.Rahu, Planet.Venus
    };

    public override Dictionary<Planet, double> Years { get; } = new()
    {
        { Planet.Sun, 6 }, { Planet.Moon, 15 }, { Planet.Mars, 8 }, { Planet.Mercury, 17 },
        { Planet.Saturn, 10 }, { Planet.Jupiter, 19 }, { Planet.Rahu, 12 }, { Planet.Venus, 21 }
    };

    public override bool SubPeriodsReversed => true;

    /// <summary>
    /// The nakshatra each lord's arc begins at, 0-based from Ashwini.
    ///
    /// Rath's Table 36 and PVR Narasimha Rao's Table 39 both give Saturn FOUR stars - P.Ashadha,
    /// U.Ashadha, Abhijit and Sravana - across only 40 degrees, because Abhijit is intercalary.
    /// Counting stars gives the wrong sizes 3,4,4,3,3,4,3,3 against the true 4,3,4,3,(4),3,4,3,
    /// which leaves the LORD often right and only the DATES wrong - a Shatabhisha Moon's first
    /// period came out 10.62 years instead of 7.82.
    ///
    /// Kept as indices and multiplied out: 360/27 does not terminate, and a typed-out 66.6667 for
    /// Ardra sits a fraction above the real boundary.
    /// </summary>
    private static readonly int[] ArcStartNakshatras = { 5, 9, 12, 16, 19, 22, 25, 2 };

    private static (int Arc, double Fraction) ArcOf(double longitude)
    {
        double normalised = ((longitude % 360.0) + 360.0) % 360.0;
        for (int i = 0; i < ArcStartNakshatras.Length; i++)
        {
            double from = ArcStartNakshatras[i] * NakshatraSpan;
            double to = ArcStartNakshatras[(i + 1) % ArcStartNakshatras.Length] * NakshatraSpan;
            // Each arc is walked as a forward sweep from its own start, so the one wrapping past
            // Aries 0 needs no special case.
            double span = ((to - from) % 360.0 + 360.0) % 360.0;
            double into = ((normalised - from) % 360.0 + 360.0) % 360.0;
            if (into < span) return (i, into / span);
        }
        return (0, 0.0);
    }

    /// <summary>
    /// Taken from the MIDDLE of the nakshatra: an arc boundary never falls inside a nakshatra,
    /// so any interior point names the same lord, and the midpoint cannot be pushed over an edge
    /// by rounding.
    /// </summary>
    public override Planet StartingLord(int nakshatra)
        => Sequence[ArcOf(Math.Clamp(nakshatra, 0, 26) * NakshatraSpan + NakshatraSpan / 2.0).Arc];

    public override double BalanceFraction(int nakshatra, double degreeInNakshatra)
        => 1.0 - ArcOf(Math.Clamp(nakshatra, 0, 26) * NakshatraSpan + degreeInNakshatra).Fraction;
}

/// <summary>
/// Yogini - 36 years over eight yoginis, each associated with a graha.
/// </summary>
public sealed class YoginiSystem : NakshatraDashaSystem
{
    public override string Name => "Yogini";

    public override Planet[] Sequence { get; } =
    {
        Planet.Moon,    // Mangala
        Planet.Sun,     // Pingala
        Planet.Jupiter, // Dhanya
        Planet.Mars,    // Bhramari
        Planet.Mercury, // Bhadrika
        Planet.Saturn,  // Ulka
        Planet.Venus,   // Siddha
        Planet.Rahu     // Sankata
    };

    public override Dictionary<Planet, double> Years { get; } = new()
    {
        { Planet.Moon, 1 }, { Planet.Sun, 2 }, { Planet.Jupiter, 3 }, { Planet.Mars, 4 },
        { Planet.Mercury, 5 }, { Planet.Saturn, 6 }, { Planet.Venus, 7 }, { Planet.Rahu, 8 }
    };

    private static readonly string[] YoginiNames =
        { "Mangala", "Pingala", "Dhanya", "Bhramari", "Bhadrika", "Ulka", "Siddha", "Sankata" };

    /// <summary>
    /// The classical rule is "nakshatra plus three, modulo eight", and the trap is which count
    /// the three is added to. The index here is ZERO-based, so adding one to reach the classical
    /// number AND then three double-counts and shifts all twenty-seven nakshatras to the next
    /// yogini - invisible in any single chart.
    /// </summary>
    public override Planet StartingLord(int nakshatra) => Sequence[((nakshatra + 3) % 8 + 8) % 8];

    public override string? LordLabel(Planet lord)
    {
        int i = Array.IndexOf(Sequence, lord);
        return i >= 0 ? $"{YoginiNames[i]} ({lord})" : null;
    }
}

/// <summary>
/// A conditional system whose lords are DEALT round-robin across the 27 nakshatras from a seed
/// star, so a lord's stars are spread through the zodiac rather than adjacent.
///
/// The balance is measured against the CURRENT nakshatra - the stars a lord holds elsewhere have
/// no bearing on how much of this period is left - so the default balance applies.
/// </summary>
public class DealtNakshatraSystem : NakshatraDashaSystem
{
    private readonly Planet[] _lordByNakshatra;

    public override string Name { get; }
    public override string? Condition { get; }
    public override Planet[] Sequence { get; }
    public override Dictionary<Planet, double> Years { get; }

    /// <param name="seedStar">Nakshatra the deal starts from, 1-based as the classics count.</param>
    /// <param name="direction">+1 deals forward through the zodiac, -1 backward.</param>
    public DealtNakshatraSystem(
        string name, string condition, Planet[] sequence, Dictionary<Planet, double> years,
        int seedStar, int direction = 1)
    {
        Name = name;
        Condition = condition;
        Sequence = sequence;
        Years = years;

        _lordByNakshatra = new Planet[27];
        int nak = seedStar - 1;
        for (int i = 0; i < 27; i++)
        {
            _lordByNakshatra[nak] = sequence[i % sequence.Length];
            nak = ((nak + direction) % 27 + 27) % 27;
        }
    }

    public override Planet StartingLord(int nakshatra) => _lordByNakshatra[Math.Clamp(nakshatra, 0, 26)];
}

/// <summary>
/// Shastihayani - 60 years over eight lords in CONTIGUOUS blocks counted over 28 slots (the
/// classical scheme includes Abhijit). Because a lord holds a contiguous block, the balance is
/// measured against the whole block, as Ashtottari's is.
/// </summary>
public sealed class ShastihayaniSystem : NakshatraDashaSystem
{
    public override string Name => "Shastihayani";

    public override string? Condition => "The Sun in lagna";

    /// <summary>
    /// Rath 14.2.1, Table 51: the eight chara karakas ordered by their relationship with the Sun,
    /// who governs this scheme. Jupiter, Sun and Mars take the first thirty years.
    /// </summary>
    public override Planet[] Sequence { get; } =
    {
        Planet.Jupiter, Planet.Sun, Planet.Mars, Planet.Moon,
        Planet.Mercury, Planet.Venus, Planet.Saturn, Planet.Rahu
    };

    public override Dictionary<Planet, double> Years { get; } = new()
    {
        { Planet.Jupiter, 10 }, { Planet.Sun, 10 }, { Planet.Mars, 10 }, { Planet.Moon, 6 },
        { Planet.Mercury, 6 }, { Planet.Venus, 6 }, { Planet.Saturn, 6 }, { Planet.Rahu, 6 }
    };

    /// <summary>Nakshatras per lord, in sequence order - three or four, summing to 28.</summary>
    private static readonly int[] BlockSizes = { 3, 4, 3, 4, 3, 4, 3, 4 };

    private static (int Block, int Within) BlockOf(int nakshatra)
    {
        int target = Math.Clamp(nakshatra, 0, 26) + 1;
        int slot = 1;
        for (int i = 0; i < BlockSizes.Length; i++)
        {
            for (int within = 0; within < BlockSizes[i]; within++)
            {
                if (slot == target) return (i, within);
                slot = (slot + 1) % 28;
            }
        }
        return (BlockSizes.Length - 1, 0);
    }

    public override Planet StartingLord(int nakshatra) => Sequence[BlockOf(nakshatra).Block];

    public override double BalanceFraction(int nakshatra, double degreeInNakshatra)
    {
        var (block, within) = BlockOf(nakshatra);
        double elapsed = (within + degreeInNakshatra / NakshatraSpan) / BlockSizes[block];
        return 1.0 - elapsed;
    }
}

/// <summary>The nakshatra systems on offer, in picker order.</summary>
public static class NakshatraDashaSystems
{
    public static readonly VimshottariSystem Vimshottari = new();
    public static readonly AshtottariSystem Ashtottari = new();
    public static readonly YoginiSystem Yogini = new();

    /// <summary>Rath Table 40, 8.2. Rahu governs the scheme and takes no period; Ketu does.</summary>
    public static readonly DealtNakshatraSystem Shodasottari = new(
        "Shodasottari",
        "Shukla paksha with lagna in the Sun's hora, or Krishna paksha with lagna in the Moon's",
        new[] { Planet.Sun, Planet.Mars, Planet.Jupiter, Planet.Saturn, Planet.Ketu, Planet.Moon, Planet.Mercury, Planet.Venus },
        new() { { Planet.Sun, 11 }, { Planet.Mars, 12 }, { Planet.Jupiter, 13 }, { Planet.Saturn, 14 },
                { Planet.Ketu, 15 }, { Planet.Moon, 16 }, { Planet.Mercury, 17 }, { Planet.Venus, 18 } },
        seedStar: 8); // Pushya

    /// <summary>Rath Table 42, 9.2. Venus governs and is excluded. The only one dealt backward.</summary>
    public static readonly DealtNakshatraSystem Dwadasottari = new(
        "Dwadasottari",
        "Lagna in Sukramsaka - the sign Venus occupies in the navamsa",
        new[] { Planet.Sun, Planet.Jupiter, Planet.Ketu, Planet.Mercury, Planet.Rahu, Planet.Mars, Planet.Saturn, Planet.Moon },
        new() { { Planet.Sun, 7 }, { Planet.Jupiter, 9 }, { Planet.Ketu, 11 }, { Planet.Mercury, 13 },
                { Planet.Rahu, 15 }, { Planet.Mars, 17 }, { Planet.Saturn, 19 }, { Planet.Moon, 21 } },
        seedStar: 27, direction: -1); // Revati, anti-zodiac

    /// <summary>Rath Table 44, 10.2. The nodes are excluded.</summary>
    public static readonly DealtNakshatraSystem Panchottari = new(
        "Panchottari",
        "Cancer lagna AND Cancer dwadasamsa - within Cancer 0 to 2.30",
        new[] { Planet.Sun, Planet.Mercury, Planet.Saturn, Planet.Mars, Planet.Venus, Planet.Moon, Planet.Jupiter },
        new() { { Planet.Sun, 12 }, { Planet.Mercury, 13 }, { Planet.Saturn, 14 }, { Planet.Mars, 15 },
                { Planet.Venus, 16 }, { Planet.Moon, 17 }, { Planet.Jupiter, 18 } },
        seedStar: 17); // Anuradha

    /// <summary>Rath Table 46, 11.2. The nodes are excluded; the luminaries lead.</summary>
    public static readonly DealtNakshatraSystem Shataabdika = new(
        "Shataabdika",
        "Vargottama lagna - the same sign in rasi and navamsa",
        new[] { Planet.Sun, Planet.Moon, Planet.Venus, Planet.Mercury, Planet.Jupiter, Planet.Mars, Planet.Saturn },
        new() { { Planet.Sun, 5 }, { Planet.Moon, 5 }, { Planet.Venus, 10 }, { Planet.Mercury, 10 },
                { Planet.Jupiter, 20 }, { Planet.Mars, 20 }, { Planet.Saturn, 30 } },
        seedStar: 27); // Revati

    /// <summary>Rath Table 49, 13.1. Weekday order; Ketu governs and is excluded.</summary>
    public static readonly DealtNakshatraSystem Dwisaptati = new(
        "Dwisaptati Sama",
        "Lagna lord in the 7th house, or the 7th lord in lagna, or both",
        new[] { Planet.Sun, Planet.Moon, Planet.Mars, Planet.Mercury, Planet.Jupiter, Planet.Venus, Planet.Saturn, Planet.Rahu },
        new() { { Planet.Sun, 9 }, { Planet.Moon, 9 }, { Planet.Mars, 9 }, { Planet.Mercury, 9 },
                { Planet.Jupiter, 9 }, { Planet.Venus, 9 }, { Planet.Saturn, 9 }, { Planet.Rahu, 9 } },
        seedStar: 19); // Mula

    /// <summary>Rath Table 53, 15.2. Ketu is excluded.</summary>
    public static readonly DealtNakshatraSystem Shattrimsa = new(
        "Shattrimsa Sama",
        "Daytime birth with lagna in the Sun's hora, or night birth with lagna in the Moon's",
        new[] { Planet.Moon, Planet.Sun, Planet.Jupiter, Planet.Mars, Planet.Mercury, Planet.Saturn, Planet.Venus, Planet.Rahu },
        new() { { Planet.Moon, 1 }, { Planet.Sun, 2 }, { Planet.Jupiter, 3 }, { Planet.Mars, 4 },
                { Planet.Mercury, 5 }, { Planet.Saturn, 6 }, { Planet.Venus, 7 }, { Planet.Rahu, 8 } },
        seedStar: 22); // Sravana

    /// <summary>Rath 12.1. Weekday order without jumps; the nodes take no dasa.</summary>
    public static readonly DealtNakshatraSystem Chaturaseethi = new(
        "Chaturaseethi Sama",
        "The 10th lord placed in the 10th house",
        new[] { Planet.Sun, Planet.Moon, Planet.Mars, Planet.Mercury, Planet.Jupiter, Planet.Venus, Planet.Saturn },
        new() { { Planet.Sun, 12 }, { Planet.Moon, 12 }, { Planet.Mars, 12 }, { Planet.Mercury, 12 },
                { Planet.Jupiter, 12 }, { Planet.Venus, 12 }, { Planet.Saturn, 12 } },
        seedStar: 15); // Swati

    public static readonly ShastihayaniSystem Shastihayani = new();

    public static IReadOnlyList<NakshatraDashaSystem> All { get; } = new NakshatraDashaSystem[]
    {
        Vimshottari, Ashtottari, Yogini, Shodasottari, Dwadasottari, Panchottari,
        Shataabdika, Dwisaptati, Shattrimsa, Chaturaseethi, Shastihayani
    };
}
