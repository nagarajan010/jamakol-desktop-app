using System;
using System.Collections.Generic;
using System.Linq;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services.Shadbala;

/// <summary>
/// The five sthana (positional) components of shadbala. Ported from the Android app, where each
/// was measured against a 100-chart deva.guru reference set.
/// </summary>
public static class SthanaBala
{
    /// <summary>The grahas that carry a shadbala. The nodes do not.</summary>
    public static readonly Planet[] Scorable =
    {
        Planet.Sun, Planet.Moon, Planet.Mars, Planet.Mercury, Planet.Jupiter, Planet.Venus, Planet.Saturn
    };

    internal static double Norm(double lon) => ((lon % 360.0) + 360.0) % 360.0;
    internal static int SignOf(double lon) => (int)(Norm(lon) / 30.0) % 12;

    // ---------------------------------------------------------------- Uccha

    /// <summary>
    /// Deep exaltation as a sidereal longitude from 0 Aries: Sun 10 Ar, Moon 3 Ta, Mars 28 Cp,
    /// Mercury 15 Vi, Jupiter 5 Cn, Venus 27 Pi, Saturn 20 Li.
    /// </summary>
    public static readonly Dictionary<Planet, double> Exaltation = new()
    {
        { Planet.Sun, 10 }, { Planet.Moon, 33 }, { Planet.Mars, 298 }, { Planet.Mercury, 165 },
        { Planet.Jupiter, 95 }, { Planet.Venus, 357 }, { Planet.Saturn, 200 }
    };

    /// <summary>
    /// UCCHA: 60 at the deepest exaltation, none at the deepest debilitation opposite it, linear
    /// between - the arc from the debilitation point divided by three. Over the 700 reference
    /// cells the worst disagreement is the reference's own one-decimal rounding.
    /// </summary>
    public static double Uccha(Planet graha, double longitude)
    {
        double debilitation = (Exaltation[graha] + 180.0) % 360.0;
        double diff = Math.Abs(Norm(longitude) - debilitation) % 360.0;
        double arc = diff > 180.0 ? 360.0 - diff : diff;
        return arc / 3.0;
    }

    // ---------------------------------------------------------------- Oja-Yugma

    /// <summary>The female grahas want an EVEN sign; the male and eunuch grahas want odd.</summary>
    private static readonly HashSet<Planet> WantsEven = new() { Planet.Moon, Planet.Venus };

    /// <summary>The navamsa sign, 0-11: the ninth-parts run continuously from Aries.</summary>
    public static int NavamsaSign(double longitude)
    {
        double lon = Norm(longitude);
        int rasi = (int)lon / 30;
        int ninth = (int)((lon % 30.0) / (30.0 / 9.0));
        return (rasi * 9 + ninth) % 12;
    }

    /// <summary>An even 0-based index is an ODD sign, since Aries is the first.</summary>
    private static bool IsOdd(int signIndex) => signIndex % 2 == 0;

    /// <summary>
    /// OJA-YUGMA, the shadbala form: the rasi and the navamsa each worth 15 when the sign's
    /// oddity is what the graha wants, so 0, 15 or 30. (Not the six-varga count that shares the
    /// name.) Reproduces the reference on all 700 cells.
    /// </summary>
    public static double OjaYugma(Planet graha, double longitude)
    {
        bool wantsOdd = !WantsEven.Contains(graha);
        double v = 0;
        if (IsOdd(SignOf(longitude)) == wantsOdd) v += 15.0;
        if (IsOdd(NavamsaSign(longitude)) == wantsOdd) v += 15.0;
        return v;
    }

    // ---------------------------------------------------------------- Kendradi

    /// <summary>
    /// KENDRADI, the shadbala form: kendra 60, panaphara 30, apoklima 15, by WHOLE SIGN from the
    /// lagna, not by cusp. Reproduces the reference on all 700 cells.
    /// </summary>
    public static double Kendradi(double longitude, double ascendant)
    {
        int house = ((SignOf(longitude) - SignOf(ascendant) + 12) % 12) + 1;
        return ((house - 1) % 3) switch { 0 => 60.0, 1 => 30.0, _ => 15.0 };
    }

    // ---------------------------------------------------------------- Drekkana

    private enum Gender { Male, Eunuch, Female }

    private static readonly Dictionary<Planet, Gender> Genders = new()
    {
        { Planet.Sun, Gender.Male }, { Planet.Mars, Gender.Male }, { Planet.Jupiter, Gender.Male },
        { Planet.Mercury, Gender.Eunuch }, { Planet.Saturn, Gender.Eunuch },
        { Planet.Moon, Gender.Female }, { Planet.Venus, Gender.Female }
    };

    /// <summary>
    /// DREKKANA: 15 when a graha stands in the third of its sign its nature wants - male in the
    /// first 0-10, eunuch in the middle 10-20, female in the last 20-30. Many translators SWAP
    /// the female and eunuch thirds, which yields entirely plausible wrong numbers; Rath keeps
    /// male first, mixed in the middle, female last. No reversal for even signs.
    /// </summary>
    public static double Drekkana(Planet graha, double longitude)
    {
        double deg = Norm(longitude) % 30.0;
        int third = deg < 10.0 ? 0 : deg < 20.0 ? 1 : 2;
        int wanted = Genders[graha] switch { Gender.Male => 0, Gender.Eunuch => 1, _ => 2 };
        return third == wanted ? 15.0 : 0.0;
    }

    // ---------------------------------------------------------------- Saptavargiya

    /// <summary>The seven vargas, in the printed order.</summary>
    public static readonly int[] Saptavarga = { 1, 9, 2, 3, 7, 12, 30 };

    private static readonly Planet[] SignLords =
    {
        Planet.Mars, Planet.Venus, Planet.Mercury, Planet.Moon, Planet.Sun, Planet.Mercury,
        Planet.Venus, Planet.Mars, Planet.Jupiter, Planet.Saturn, Planet.Saturn, Planet.Jupiter
    };

    /// <summary>Moolatrikona sign, and the degree range tested in the RASI only.</summary>
    private static readonly Dictionary<Planet, (int Sign, double From, double To)> Moolatrikona = new()
    {
        { Planet.Sun, (4, 0, 20) },
        // Taurus 3-30, meeting the exaltation span with no gap: in every other graha the spans
        // adjoin and the shared degree opens the later dignity, so 4-30 would orphan Taurus 3-4.
        { Planet.Moon, (1, 3, 30) },
        { Planet.Mars, (0, 0, 12) },
        { Planet.Mercury, (5, 15, 20) },
        { Planet.Jupiter, (8, 0, 10) },
        { Planet.Venus, (6, 0, 15) },
        { Planet.Saturn, (10, 0, 20) }
    };

    /// <summary>
    /// Natural friendship, 2 friend / 1 neutral / 0 enemy, [from][to], order Su Mo Ma Me Ju Ve Sa.
    /// Checked against the course table and the published Natural Relationships table, and
    /// derived cell by cell from Parasara's rule in the Android app. The Moon has no natural
    /// enemy, and Mercury alone is the Sun's neutral.
    /// </summary>
    private static readonly int[,] Naisargika =
    {
        { 2, 2, 2, 1, 2, 0, 0 }, // Sun
        { 2, 2, 1, 2, 1, 1, 1 }, // Moon
        { 2, 2, 2, 0, 2, 1, 1 }, // Mars
        { 2, 0, 1, 2, 1, 2, 1 }, // Mercury
        { 2, 2, 2, 0, 2, 0, 1 }, // Jupiter
        { 0, 0, 1, 2, 1, 2, 2 }, // Venus
        { 0, 0, 0, 2, 1, 2, 2 }, // Saturn
    };

    private static int FriendIndex(Planet p) => Array.IndexOf(Scorable, p);

    /// <summary>Virupa per standing - sloka 3.5: 45, 30, 20, 15, 10, 4, 2.</summary>
    public const double MoolatrikonaV = 45, OwnV = 30, AtiMitraV = 20, MitraV = 15, SamaV = 10, SatruV = 4, AtiSatruV = 2;

    /// <summary>
    /// The PARIVRTTI hora: twenty-four half-signs numbered cyclically from Aries. Saptavargiya
    /// scores dignity against the sign lord, and the Parasari hora puts every graha in Cancer or
    /// Leo - leaving only two possible lords - which the Android app shipped once and found broke
    /// the measure rather than giving a different reading.
    /// </summary>
    public static int ParivrttiHoraSign(double longitude)
    {
        double lon = Norm(longitude);
        int half = lon % 30.0 < 15.0 ? 0 : 1;
        return (2 * SignOf(lon) + half) % 12;
    }

    /// <summary>
    /// SAPTAVARGIYA: dignity in each of the seven vargas, added (14 to 315).
    ///
    /// In each varga a graha is scored by how it stands to the lord of the sign it occupies,
    /// using the COMPOUND relationship - natural plus temporal. Temporal friendship is judged
    /// from the RASI and carried into every varga: read from D1 throughout, the published worked
    /// example matches 48 of 49 cells; per-varga, only 29.
    /// </summary>
    public static double Saptavargiya(
        Planet graha, IReadOnlyDictionary<Planet, double> longitudes, DivisionalChartService vargas)
    {
        var rasiSign = longitudes.ToDictionary(kv => kv.Key, kv => SignOf(kv.Value));
        double lon = longitudes[graha];
        double total = 0;

        foreach (int division in Saptavarga)
        {
            int sign = division switch
            {
                1 => SignOf(lon),
                2 => ParivrttiHoraSign(lon),
                _ => vargas.CalculateDivisionalPosition(lon, division).Sign - 1
            };
            Planet lord = SignLords[sign];

            var mt = Moolatrikona[graha];
            bool moolatrikona = sign == mt.Sign
                && (division != 1 || (Norm(lon) % 30.0 >= mt.From && Norm(lon) % 30.0 < mt.To));

            if (moolatrikona) { total += MoolatrikonaV; continue; }
            if (lord == graha) { total += OwnV; continue; }

            int natural = Naisargika[FriendIndex(graha), FriendIndex(lord)];
            bool temporalFriend = rasiSign.TryGetValue(graha, out int gs) && rasiSign.TryGetValue(lord, out int ls)
                                  && IsTemporalFriend(gs, ls);
            total += natural switch
            {
                2 => temporalFriend ? AtiMitraV : SamaV,
                1 => temporalFriend ? MitraV : SatruV,
                _ => temporalFriend ? SamaV : AtiSatruV
            };
        }
        return total;
    }

    /// <summary>
    /// The 2nd, 3rd, 4th, 10th, 11th and 12th from a graha are its temporary friends; the rest,
    /// the 1st INCLUDED, its temporary enemies - two grahas in one sign are temporally inimical.
    /// </summary>
    public static bool IsTemporalFriend(int fromSign, int otherSign)
    {
        int house = ((otherSign - fromSign + 12) % 12) + 1;
        return house is 2 or 3 or 4 or 10 or 11 or 12;
    }

    /// <summary>Natural relationship 2/1/0, for the compound relationship elsewhere.</summary>
    public static int NaturalRelation(Planet from, Planet to)
    {
        int a = FriendIndex(from), b = FriendIndex(to);
        return a < 0 || b < 0 ? 1 : Naisargika[a, b];
    }
}
