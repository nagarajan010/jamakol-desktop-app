using System;
using System.Collections.Generic;
using System.Linq;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// The positions Narayana dasha needs. Sign indices are 0-based from Aries.
/// Narrowed to this so the calculation can be tested without a full chart object.
/// </summary>
public interface IRashiChart
{
    /// <summary>0-based sign of the ascendant.</summary>
    int Lagna { get; }

    /// <summary>0-based sign a graha occupies.</summary>
    int SignOf(Planet planet);

    /// <summary>Degrees into its sign, for the Moolatrikona band in dignity.</summary>
    double DegreeInSignOf(Planet planet);
}

/// <summary>
/// Narayana (Chara) dasha - Jaimini's pada krama dasha, the rasi dasha of circumstance.
///
/// Where a graha dasha like Vimshottari shows what the native wants, a rasi dasha shows what is
/// available to them. That is why it is read for circumstances rather than for intent.
///
/// ## The method
///
/// Every rule is from Sanjay Rath's *Narayana Dasa*, with his numbering and the BPHS or Jaimini
/// Sutra citation he gives.
///
/// ### 1. Where it starts - rule (6), J.S. 2.4.7
/// The stronger of the 1st and 7th houses. Not necessarily the lagna.
///
/// ### 2. Which way it runs - rule (6)
/// From the 9TH HOUSE counted from the starting sign - Jupiter's house, which is why the dasha is
/// said to "follow the guru". If that ninth sign is forward-footed (Ar, Ta, Ge, Li, Sc, Sg) the
/// dasha runs zodiacal; if backward-footed (Cn, Le, Vi, Cp, Aq, Pi) it runs reverse.
///
/// This is NOT the odd/even nature of the starting sign. That reading fits most charts and fails
/// some, which is exactly how it survives a casual check.
///
/// ### 3. How far it steps - rules (8), (9), (10), by the type of the STARTING sign
///
/// | Type | Forward | Backward |
/// |---|---|---|
/// | Movable | the next sign | the 12th |
/// | Fixed | the 6th - five signs on | the 8th |
/// | Dual | trines, then the 10th and its trines | trines the other way, then the 4th |
///
/// ### 4. Two exceptions
/// Saturn in the starting sign - rule (14), *Sanou Chetyeke* - makes the order "zodiacal and
/// regular" irrespective of the nature of the sign. Ketu in the starting sign - rule (15),
/// *Vipareetam Ketou* - reverses the direction the ninth house gave and changes nothing else.
/// Saturn REPLACES the rule; Ketu only flips its direction.
///
/// ### 5. How long each lasts - rule (2), BPHS 46.155, and rule (3)
/// Count from the sign to the sign its lord occupies and subtract one. The direction of THAT
/// count is the *pada* of the sign being measured, a different grouping from movable/fixed and
/// from odd/even. A lord exalted adds a year, debilitated subtracts one; a lord in its own sign
/// gives twelve rather than zero; no period exceeds twelve years.
///
/// ### 6. The second cycle - rule (5)
/// After twelve signs the dashas begin again in the same order, each period now 12 MINUS the
/// first. A sign that gave twelve gives none.
///
/// ### 7. Antardashas
/// Twelve EQUAL divisions - no weighting, unlike the udu family.
/// </summary>
public class NarayanaDashaCalculator
{
    public const int MaxYears = 12;

    private readonly IRashiChart _chart;

    public NarayanaDashaCalculator(IRashiChart chart)
    {
        _chart = chart;
    }

    /// <summary>The eight grahas that take chara-karaka rank. Ketu is the Moksha-karaka and is out.</summary>
    private static readonly Planet[] CharaKarakas =
    {
        Planet.Sun, Planet.Moon, Planet.Mars, Planet.Mercury,
        Planet.Jupiter, Planet.Venus, Planet.Saturn, Planet.Rahu
    };

    /// <summary>
    /// Exaltation signs for PHALITA dasha - Rath's Table 8.
    ///
    /// The seven visible grahas match the usual table, but the NODES do not: Rath puts Rahu's
    /// exaltation in Gemini and Ketu's in Sagittarius, where the common scheme has Taurus and
    /// Scorpio. He is explicit the choice is contested and that the one he uses for a phalita
    /// dasha is Manteswara's. Reading the nodes through the ordinary table made Rahu debilitated
    /// in Scorpio - the opposite of Rath's reckoning, where Scorpio is merely neutral for it.
    ///
    /// Debilitation is always the opposite sign, so only exaltation is listed. Rule 3(a) is also
    /// explicit that the DEGREE does not matter here.
    /// </summary>
    private static readonly Dictionary<Planet, int> PhalitaExaltation = new()
    {
        { Planet.Sun, 0 },      // Aries
        { Planet.Moon, 1 },     // Taurus
        { Planet.Mars, 9 },     // Capricorn
        { Planet.Mercury, 5 },  // Virgo
        { Planet.Jupiter, 3 },  // Cancer
        { Planet.Venus, 11 },   // Pisces
        { Planet.Saturn, 6 },   // Libra
        { Planet.Rahu, 2 },     // Gemini - not Taurus
        { Planet.Ketu, 8 }      // Sagittarius - not Scorpio
    };

    /// <summary>The two signs with two lords. Scorpio: Mars and Ketu. Aquarius: Saturn and Rahu.</summary>
    private static readonly Dictionary<int, (Planet First, Planet Second)> CoLords = new()
    {
        { 7, (Planet.Mars, Planet.Ketu) },
        { 10, (Planet.Saturn, Planet.Rahu) }
    };

    /// <summary>Sole lord of each sign, 0-based from Aries.</summary>
    private static readonly Planet[] SignLords =
    {
        Planet.Mars,    // Aries
        Planet.Venus,   // Taurus
        Planet.Mercury, // Gemini
        Planet.Moon,    // Cancer
        Planet.Sun,     // Leo
        Planet.Mercury, // Virgo
        Planet.Venus,   // Libra
        Planet.Mars,    // Scorpio
        Planet.Jupiter, // Sagittarius
        Planet.Saturn,  // Capricorn
        Planet.Saturn,  // Aquarius
        Planet.Jupiter  // Pisces
    };

    /// <summary>Own signs, for the dignity score.</summary>
    private static readonly Dictionary<Planet, int[]> OwnSigns = new()
    {
        { Planet.Sun, new[] { 4 } },
        { Planet.Moon, new[] { 3 } },
        { Planet.Mars, new[] { 0, 7 } },
        { Planet.Mercury, new[] { 2, 5 } },
        { Planet.Jupiter, new[] { 8, 11 } },
        { Planet.Venus, new[] { 1, 6 } },
        { Planet.Saturn, new[] { 9, 10 } }
    };

    /// <summary>Moolatrikona sign and the degree range within it.</summary>
    private static readonly Dictionary<Planet, (int Sign, double From, double To)> Moolatrikona = new()
    {
        { Planet.Sun, (4, 0.0, 20.0) },
        { Planet.Moon, (1, 4.0, 30.0) },
        { Planet.Mars, (0, 0.0, 12.0) },
        { Planet.Mercury, (5, 16.0, 20.0) },
        { Planet.Jupiter, (8, 0.0, 10.0) },
        { Planet.Venus, (6, 0.0, 15.0) },
        { Planet.Saturn, (10, 0.0, 20.0) }
    };

    private static readonly Planet[] Navagrahas =
    {
        Planet.Sun, Planet.Moon, Planet.Mars, Planet.Mercury, Planet.Jupiter,
        Planet.Venus, Planet.Saturn, Planet.Rahu, Planet.Ketu
    };

    public static bool IsMovable(int sign) => Mod12(sign) % 3 == 0;
    public static bool IsFixed(int sign) => Mod12(sign) % 3 == 1;

    /// <summary>
    /// Whether a sign is forward-footed, which decides direction when read of the 9th.
    /// Aries, Taurus, Gemini and Libra, Scorpio, Sagittarius walk forward.
    /// </summary>
    public static bool IsForwardFooted(int sign) => Mod12(sign) % 6 < 3;

    /// <summary>
    /// Whether a sign is vimshapada - odd-footed - which decides the direction of the count
    /// giving its dasha period. The same six signs as <see cref="IsForwardFooted"/>, kept
    /// separate because they answer different questions and the sources name them differently.
    /// </summary>
    public static bool IsVimshapada(int sign) => Mod12(sign) % 6 < 3;

    /// <summary>All twenty-four periods, both cycles, with sub-periods when asked.</summary>
    public List<NarayanaPeriod> Calculate(bool withSubPeriods = true)
    {
        var order = DashaOrder();
        var firstCycle = order.Select(PeriodYears).ToList();

        var result = new List<NarayanaPeriod>();
        double age = 0.0;

        for (int cycle = 1; cycle <= 2; cycle++)
        {
            for (int i = 0; i < order.Count; i++)
            {
                int sign = order[i];
                int years = cycle == 1 ? firstCycle[i] : MaxYears - firstCycle[i];

                var period = new NarayanaPeriod
                {
                    Rashi = sign,
                    Years = years,
                    SpanYears = years,
                    StartAge = age,
                    Cycle = cycle,
                    Level = 1
                };

                if (withSubPeriods && years > 0)
                {
                    period.SubPeriods = SubPeriodsOf(sign, years, age, 2);
                }

                result.Add(period);
                age += years;
            }
        }

        return result;
    }

    /// <summary>
    /// The sign the dasha opens in: the stronger of the lagna and the seventh from it.
    ///
    /// The second source counts BOTH lords of a two-lorded sign HERE AND NOWHERE ELSE. Rath
    /// scopes it that way explicitly: the second source is to be used "in comparing the relative
    /// strength of THE ASCENDING SIGN / 7TH THERE FROM". Everywhere else a two-lorded sign speaks
    /// through one lord. The distinction is not cosmetic - counting both everywhere is right for
    /// this comparison and wrong below it.
    /// </summary>
    public int StartingSign() => StrongerSign(_chart.Lagna, (_chart.Lagna + 6) % 12, bothCoLords: true);

    /// <summary>The twelve signs in the order their dashas run.</summary>
    public List<int> DashaOrder()
    {
        int start = StartingSign();

        // Rule (14): Saturn in the starting sign overrides type and direction alike.
        if (_chart.SignOf(Planet.Saturn) == start)
        {
            return Enumerable.Range(0, 12).Select(i => (start + i) % 12).ToList();
        }

        bool forward = IsForwardFooted((start + 8) % 12);

        // Rule (15): Ketu reverses the direction, and only the direction.
        if (_chart.SignOf(Planet.Ketu) == start) forward = !forward;

        int step = forward ? 1 : -1;

        if (IsMovable(start))
            return Enumerable.Range(0, 12).Select(i => Mod12(start + step * i)).ToList();

        if (IsFixed(start))
            return Enumerable.Range(0, 12).Select(i => Mod12(start + step * 5 * i)).ToList();

        // Dual: four groups of trines, each group head a further 10th (or 4th) on.
        var order = new List<int>();
        int head = start;
        for (int g = 0; g < 4; g++)
        {
            for (int t = 0; t < 3; t++) order.Add(Mod12(head + step * 4 * t));
            head = Mod12(head + step * 9);
        }
        return order;
    }

    /// <summary>
    /// The first-cycle period of one sign, in whole years.
    ///
    /// The count runs from the sign to the sign its lord occupies, in the direction the sign's
    /// pada gives, less one.
    /// </summary>
    public int PeriodYears(int sign)
    {
        Planet? lord = MeasuringLord(sign);
        if (lord == null) return MaxYears; // both co-lords at home

        return PeriodFrom(sign, lord.Value);
    }

    /// <summary>
    /// The period a sign would take if measured from one nominated lord.
    ///
    /// Includes the dignity adjustment, because rule (e) compares the PERIODS two lords give and
    /// those can differ even when the count does not.
    /// </summary>
    private int PeriodFrom(int sign, Planet lord)
    {
        int lordSign = _chart.SignOf(lord);

        int count = IsVimshapada(sign)
            ? Mod12(lordSign - sign) + 1
            : Mod12(sign - lordSign) + 1;

        // A lord in its own sign counts one, and one less one is no period at all - so the sign
        // takes the full twelve instead.
        int years = count == 1 ? MaxYears : count - 1;

        if (PhalitaExaltation.TryGetValue(lord, out int exalt))
        {
            if (lordSign == exalt) years += 1;
            if (lordSign == (exalt + 6) % 12) years -= 1;
        }

        // Capped at twelve - rule 3(b) - but NOT floored at one. A debilitated lord can take a
        // one-year period down to nothing, and published tables print exactly that. An earlier
        // floor of one would invent a rule the sources never state.
        return Math.Clamp(years, 0, MaxYears);
    }

    /// <summary>
    /// Which lord measures a sign's period - rule (4), BPHS 46.158-163.
    ///
    /// Scorpio answers to Mars AND Ketu, Aquarius to Saturn AND Rahu. Getting this wrong is not a
    /// rounding error: a co-lord sitting in its own sign collapses the count to one and hands the
    /// sign a spurious twelve years.
    ///
    /// (a) both lords in the sign - twelve years, returned as null for the caller to shortcut.
    /// (b) both lords together elsewhere - falls through to (e).
    /// (c) one lord in the sign, one outside - measure from the one OUTSIDE.
    /// (d) both elsewhere, apart - the stronger of the signs they occupy decides.
    /// (e) those signs equally strong - the lord giving the LONGER period.
    /// </summary>
    private Planet? MeasuringLord(int sign)
    {
        if (!CoLords.TryGetValue(Mod12(sign), out var co)) return SignLords[Mod12(sign)];

        int atFirst = _chart.SignOf(co.First);
        int atSecond = _chart.SignOf(co.Second);

        bool firstHome = atFirst == Mod12(sign);
        bool secondHome = atSecond == Mod12(sign);

        if (firstHome && secondHome) return null;   // (a)
        if (firstHome) return co.Second;            // (c)
        if (secondHome) return co.First;            // (c)

        if (atFirst != atSecond)                    // (d)
        {
            int stronger = StrongerSign(atFirst, atSecond);
            if (stronger == atFirst) return co.First;
            if (stronger == atSecond) return co.Second;
        }

        // (e) Either lord gives the same COUNT when together, but not the same period: dignity
        // belongs to the graha, so a debilitated co-lord loses a year its partner keeps.
        return PeriodFrom(sign, co.First) >= PeriodFrom(sign, co.Second) ? co.First : co.Second;
    }

    /// <summary>
    /// The twelve sub-periods of any period, at any depth.
    ///
    /// Rath states the recursion once: "For calculating the Antardasa, Pratyantar dasa etc, the
    /// standard rule of dividing the period into 12 equal portions for each of the 12 signs is
    /// adopted. OTHER RULES FOR INITIATION OF ANTARDASA REMAIN INTACT." So the seed and the
    /// direction are worked out from the period's own sign exactly as for the antardashas, and
    /// only the length shrinks by a twelfth each time.
    ///
    /// The FIRST sub-period is the sign holding the lord of the stronger of this sign and the
    /// seventh from it (BPHS 50.30-31).
    ///
    /// The DIRECTION has nothing to do with the ninth house - Rath is explicit that the rule for
    /// the dasha sequence does not carry over. What decides it is the oddity of the STARTING sign
    /// (the sign the lord occupies), not of the dasha sign. Cole is unambiguous on this and his
    /// worked example turns on exactly that reading.
    /// </summary>
    public List<NarayanaPeriod> SubPeriodsOf(int dashaSign, double span, double startAge, int level)
    {
        var result = new List<NarayanaPeriod>();
        if (span <= 0.0) return result;

        int seventh = (dashaSign + 6) % 12;
        int stronger = StrongerSign(dashaSign, seventh);
        Planet lord = RepresentingLord(stronger);
        int first = _chart.SignOf(lord);

        // Sign indices are 0-based, so an EVEN index is an ODD sign.
        bool forward = first % 2 == 0;

        bool saturnHere = _chart.SignOf(Planet.Saturn) == first;
        bool ketuHere = _chart.SignOf(Planet.Ketu) == first;

        if (saturnHere && ketuHere)
        {
            // Both in the starting sign: the stronger of the two decides, by chara-karaka degree.
            forward = CharaKarakaDegree(Planet.Saturn) > CharaKarakaDegree(Planet.Ketu) ? true : !forward;
        }
        else if (saturnHere) forward = true;
        else if (ketuHere) forward = !forward;

        int step = forward ? 1 : -1;
        double each = span / 12.0;

        for (int i = 0; i < 12; i++)
        {
            result.Add(new NarayanaPeriod
            {
                Rashi = Mod12(first + step * i),
                Years = (int)span,
                SpanYears = each,
                StartAge = startAge + i * each,
                Cycle = 0,
                Level = level
            });
        }

        return result;
    }

    /// <summary>
    /// The stronger of two signs, by the hierarchy in Rath's chapter I.
    ///
    /// Rules (1) and (5) of the first source are omitted at the top deliberately: they rest on
    /// the Atmakaraka, and Rath excludes them because "in Narayana dasa, the Ascendant is the
    /// focal point instead of the Atmakaraka". The order he recommends is rule (2), then the
    /// second source, then rule (3), then rule (4).
    ///
    /// Then rule (7), and only after it rules (5), (6) and (8). That ordering comes from Rath's
    /// Indira Gandhi walkthrough rather than from the order the rules are printed in, because the
    /// two disagree: he tries (7), finds it cannot separate the signs, and only then reaches for
    /// the chara-karaka.
    ///
    /// <paramref name="bothCoLords"/> controls whether Scorpio and Aquarius contribute BOTH their
    /// lords to the second source. True for the lagna/seventh comparison alone.
    /// </summary>
    public int StrongerSign(int a, int b, bool bothCoLords = false)
    {
        a = Mod12(a);
        b = Mod12(b);
        if (a == b) return a;

        // Rule (2): more planets.
        var pa = Occupants(a);
        var pb = Occupants(b);
        if (pa.Count != pb.Count) return pa.Count > pb.Count ? a : b;

        // Second source: aspected or occupied by Mercury, Jupiter or the sign's own lord.
        int fa = BenefactorFactors(a, bothCoLords);
        int fb = BenefactorFactors(b, bothCoLords);
        if (fa != fb) return fa > fb ? a : b;

        // Rule (3): the standing of the planets present.
        int da = pa.Sum(DignityScore);
        int db = pb.Sum(DignityScore);
        if (da != db) return da > db ? a : b;

        // Rule (4): dual is stronger than fixed, fixed than movable.
        int na = NaturalStrength(a);
        int nb = NaturalStrength(b);
        if (na != nb) return na > nb ? a : b;

        // From here the comparison is about the signs' LORDS, so a dual-lorded sign has to say
        // which of its two speaks for it.
        Planet la = RepresentingLord(a);
        Planet lb = RepresentingLord(b);

        // Rule (7): a sign whose lord stands in a sign of the OPPOSITE oddity is stronger.
        bool favouredA = HasOppositeOddity(a, la);
        bool favouredB = HasOppositeOddity(b, lb);
        if (favouredA != favouredB) return favouredA ? a : b;

        // Rule (5): the sign whose lord is the Atmakaraka. Rath sets this aside for Narayana but
        // reaches for it once everything above has failed - the exclusion is an ordering
        // preference, not a prohibition.
        Planet atmakaraka = Atmakaraka();
        if (la != lb)
        {
            if (la == atmakaraka) return a;
            if (lb == atmakaraka) return b;
        }

        // Rule (6): otherwise the lord standing at the higher chara-karaka longitude.
        if (la != lb)
        {
            double degA = CharaKarakaDegree(la);
            double degB = CharaKarakaDegree(lb);
            if (Math.Abs(degA - degB) > 1e-9) return degA > degB ? a : b;
        }

        // Rule (8): the sign giving the longer dasha. The last rule that can separate two signs.
        int ya = PeriodYears(a);
        int yb = PeriodYears(b);
        if (ya != yb) return ya > yb ? a : b;

        return a;
    }

    /// <summary>
    /// Which of a sign's lords speaks for it when its strength is being compared.
    ///
    /// Page 22 is explicit that the dual-lordship rules settle which lord represents a two-lorded
    /// sign, so this reuses <see cref="MeasuringLord"/>. The null it can return - both lords at
    /// home - has no bearing here, so the first lord stands in.
    /// </summary>
    private Planet RepresentingLord(int sign)
    {
        sign = Mod12(sign);
        if (CoLords.TryGetValue(sign, out var co)) return MeasuringLord(sign) ?? co.First;
        return SignLords[sign];
    }

    /// <summary>
    /// Whether a sign's lord stands in a sign of the opposite oddity - rule (7).
    /// Sign indices are 0-based, so Aries at index 0 is the FIRST sign and therefore odd.
    /// </summary>
    private bool HasOppositeOddity(int sign, Planet lord)
    {
        bool signIsOdd = Mod12(sign) % 2 == 0;
        bool lordSignIsOdd = _chart.SignOf(lord) % 2 == 0;
        return signIsOdd != lordSignIsOdd;
    }

    /// <summary>
    /// A planet's longitude as the chara-karaka reckoning takes it.
    ///
    /// Rahu and Ketu are counted from the END of their sign - Rath's Table 5, rule 9. A node high
    /// in its sign is WEAK on this reckoning, so a comparison using the raw degree would rank it
    /// near the top instead of near the bottom.
    /// </summary>
    private double CharaKarakaDegree(Planet planet)
    {
        double degree = _chart.DegreeInSignOf(planet);
        return planet == Planet.Rahu || planet == Planet.Ketu ? 30.0 - degree : degree;
    }

    /// <summary>The Atmakaraka - the graha at the highest chara-karaka longitude. Ketu excluded.</summary>
    private Planet Atmakaraka()
    {
        Planet best = Planet.Sun;
        double bestDegree = -1.0;
        foreach (var planet in CharaKarakas)
        {
            double d = CharaKarakaDegree(planet);
            if (d > bestDegree) { bestDegree = d; best = planet; }
        }
        return best;
    }

    /// <summary>The grahas in a sign. The nodes are included; they are grahas for this purpose.</summary>
    private List<Planet> Occupants(int sign) =>
        Navagrahas.Where(p => _chart.SignOf(p) == Mod12(sign)).ToList();

    /// <summary>
    /// How many of Mercury, Jupiter and the sign's own lord reach it - the second source, scored.
    ///
    /// Counted as a LIST of three and NOT a set, which is the whole point. Rath's worked example:
    /// "Pisces is the Lagna and is conjoined Jupiter and which is also its Lord (2 factors out of
    /// 3)". Jupiter scores once as Jupiter and again as the lord of Pisces. Deduplicating
    /// collapses his two into one and loses the case the example is built on.
    ///
    /// Conjunction and aspect both count, and the aspect is RASI drishti, not graha drishti.
    /// </summary>
    private int BenefactorFactors(int sign, bool bothCoLords)
    {
        sign = Mod12(sign);

        var lords = new List<Planet>();
        if (bothCoLords && CoLords.TryGetValue(sign, out var co))
        {
            lords.Add(co.First);
            lords.Add(co.Second);
        }
        else if (bothCoLords)
        {
            lords.Add(SignLords[sign]);
        }
        else
        {
            lords.Add(RepresentingLord(sign));
        }

        var factors = new List<Planet> { Planet.Mercury, Planet.Jupiter };
        factors.AddRange(lords);

        return factors.Count(p =>
        {
            int at = _chart.SignOf(p);
            return at == sign || RasiDrishtiCalculator.Aspects(at, sign);
        });
    }

    /// <summary>
    /// A graha's standing where it sits, as rule (3) weighs it.
    ///
    /// Exaltation and debilitation come from the PHALITA table and not the app's general dignity,
    /// because Rath puts Rahu's exaltation in Gemini and Ketu's in Sagittarius. Moolatrikona and
    /// own sign still come from the ordinary tables, which the nodes never report.
    /// </summary>
    private int DignityScore(Planet planet)
    {
        int sign = _chart.SignOf(planet);

        if (PhalitaExaltation.TryGetValue(planet, out int exalt))
        {
            if (sign == exalt) return 4;
            if (sign == (exalt + 6) % 12) return 0;
        }

        if (Moolatrikona.TryGetValue(planet, out var mt))
        {
            double deg = _chart.DegreeInSignOf(planet);
            if (sign == mt.Sign && deg >= mt.From && deg < mt.To) return 3;
        }

        if (OwnSigns.TryGetValue(planet, out var own) && own.Contains(sign)) return 2;

        return 1;
    }

    /// <summary>Rule (4): dual 3, fixed 2, movable 1.</summary>
    private static int NaturalStrength(int sign)
    {
        if (IsMovable(sign)) return 1;
        if (IsFixed(sign)) return 2;
        return 3;
    }

    private static int Mod12(int value) => ((value % 12) + 12) % 12;
}
