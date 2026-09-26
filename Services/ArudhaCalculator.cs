using System.Collections.Generic;
using System.Linq;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>One arudha pada - the visible image of a house.</summary>
public class ArudhaPada
{
    /// <summary>House whose image this is, 1-12.</summary>
    public int House { get; set; }

    /// <summary>AL for the first, UL for the twelfth, A2..A11 otherwise.</summary>
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    /// <summary>0-based sign the pada falls in.</summary>
    public int Rashi { get; set; }

    /// <summary>The house's lord, whose placement decides the pada.</summary>
    public Planet Lord { get; set; }

    /// <summary>0-based sign that lord occupies.</summary>
    public int LordRashi { get; set; }

    /// <summary>True when the raw count landed in the 1st or 7th and was moved on.</summary>
    public bool Adjusted { get; set; }

    /// <summary>0-based sign of the lagna, for the house the pada falls in.</summary>
    public int LagnaRashi { get; set; }

    public string SignName => ZodiacUtils.GetSignName(Rashi + 1);
    public int HouseFromLagna => ((Rashi - LagnaRashi + 12) % 12) + 1;
    public string LordText => $"{ZodiacUtils.GetPlanetName(Lord)} in {ZodiacUtils.GetSignName(LordRashi + 1)}";
    public string Note => Adjusted ? "Moved to the 10th - fell in the 1st/7th from the house" : "";
}

/// <summary>
/// Arudha padas - what a house SHOWS, as against what it is. Ported from the Android app.
///
/// The pada is found by counting from the house to its lord and then the same distance again: a
/// lord in the 5th from its own house puts the pada in the 9th.
///
/// Two exclusions are the part most implementations get wrong. A pada may not sit in the 1st or
/// the 7th from its own house - satya and maya cannot share a seat, nor stand opposite - and in
/// either case it moves to the 10th from where it fell. That leaves the 2nd, 6th, 8th and 12th
/// unreachable as a consequence, not as a separate rule.
///
/// Scorpio and Aquarius are given to Mars and Saturn, their classical rulers. Rath's course notes a
/// second arudha from Ketu and Rahu respectively; that needs a strength comparison between the two
/// lords and is deliberately not attempted, as in the Android app.
/// </summary>
public class ArudhaCalculator
{
    private static readonly Planet[] SignLords =
    {
        Planet.Mars, Planet.Venus, Planet.Mercury, Planet.Moon, Planet.Sun, Planet.Mercury,
        Planet.Venus, Planet.Mars, Planet.Jupiter, Planet.Saturn, Planet.Saturn, Planet.Jupiter
    };

    private readonly IRashiChart _chart;

    public ArudhaCalculator(IRashiChart chart)
    {
        _chart = chart;
    }

    /// <summary>The pada for one house, 1-12.</summary>
    public ArudhaPada ForHouse(int house)
    {
        int lagna = _chart.Lagna;
        int houseRashi = (lagna + house - 1) % 12;
        Planet lord = SignLords[houseRashi];
        int lordRashi = _chart.SignOf(lord);

        // Count from the house to its lord, then as far again.
        int distance = ((lordRashi - houseRashi) % 12 + 12) % 12;
        int raw = (lordRashi + distance) % 12;

        // The 1st and 7th from the house itself are barred; either moves to the 10th from where
        // it landed. Counting inclusively, the 10th along is nine signs on.
        int fromHouse = ((raw - houseRashi) % 12 + 12) % 12;
        bool barred = fromHouse == 0 || fromHouse == 6;

        (string code, string name) = house switch
        {
            1 => ("AL", "Arudha Lagna"),
            12 => ("UL", "Upapada Lagna"),
            _ => ($"A{house}", $"Arudha of house {house}")
        };

        return new ArudhaPada
        {
            House = house,
            Code = code,
            Name = name,
            Rashi = barred ? (raw + 9) % 12 : raw,
            Lord = lord,
            LordRashi = lordRashi,
            Adjusted = barred,
            LagnaRashi = lagna
        };
    }

    /// <summary>All twelve padas, in house order.</summary>
    public List<ArudhaPada> ForAllHouses() => Enumerable.Range(1, 12).Select(ForHouse).ToList();
}
