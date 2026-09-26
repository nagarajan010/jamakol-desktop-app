using System;
using System.Collections.Generic;
using System.Linq;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Kalachakra dasha - periods ruled by RASHIS rather than grahas.
///
/// The Moon's nakshatra PADA, not the nakshatra itself, selects one of sixteen progressions of
/// nine rashis; the periods follow that progression in order, each lasting the years assigned to
/// its rashi. Ported from the Android app, which takes the tables from PVR Narasimha Rao.
/// </summary>
public class KalachakraDashaCalculator
{
    public const int MaxLevel = 6;

    /// <summary>Years each rashi rules, Aries through Pisces - Rao's Table 48.</summary>
    public static readonly double[] RashiYears = { 7, 16, 9, 21, 5, 9, 16, 7, 10, 4, 4, 10 };

    private const int AR = 0, TA = 1, GE = 2, CN = 3, LE = 4, VI = 5,
                      LI = 6, SC = 7, SG = 8, CP = 9, AQ = 10, PI = 11;

    /// <summary>
    /// The nine-rashi sequence for each group and pada - Rao's Tables 44 to 47.
    ///
    /// These are NOT consecutive runs of nine signs, and generating them arithmetically is the
    /// bug the Android app recorded: that is right for the first pada of savya-1 and wrong for
    /// the other fifteen. Savya-1's second pada really does jump from Pisces back to Scorpio and
    /// descend, and its third repeats Aries and Taurus inside the same nine. They are a table.
    /// Each row sums to the paramayush the book prints beside it - 100, 85, 83 or 86.
    /// </summary>
    public static readonly int[][][] Groups =
    {
        // Savya-1 (Table 44): Aswini, Krittika, Punarvasu, Aslesha, Hasta, Swati, Moola, U.Ashadha, P.Bhadrapada
        new[]
        {
            new[] { AR, TA, GE, CN, LE, VI, LI, SC, SG }, // 100
            new[] { CP, AQ, PI, SC, LI, VI, CN, LE, GE }, // 85
            new[] { TA, AR, PI, AQ, CP, SG, AR, TA, GE }, // 83
            new[] { CN, LE, VI, LI, SC, SG, CP, AQ, PI }, // 86
        },
        // Savya-2 (Table 45): Bharani, Pushya, Chitra, P.Ashadha, Revati
        new[]
        {
            new[] { SC, LI, VI, CN, LE, GE, TA, AR, PI }, // 100
            new[] { AQ, CP, SG, AR, TA, GE, CN, LE, VI }, // 85
            new[] { LI, SC, SG, CP, AQ, PI, SC, LI, VI }, // 83
            new[] { CN, LE, GE, TA, AR, PI, AQ, CP, SG }, // 86
        },
        // Apasavya-1 (Table 46): Rohini, Magha, Vishakha, Shravana
        new[]
        {
            new[] { SG, CP, AQ, PI, AR, TA, GE, LE, CN }, // 86
            new[] { VI, LI, SC, PI, AQ, CP, SG, SC, LI }, // 83
            new[] { VI, LE, CN, GE, TA, AR, SG, CP, AQ }, // 85
            new[] { PI, AR, TA, GE, LE, CN, VI, LI, SC }, // 100
        },
        // Apasavya-2 (Table 47): Mrigashira, Ardra, P.Phalguni, U.Phalguni, Anuradha, Jyeshtha, Dhanishtha, Shatabhisha
        new[]
        {
            new[] { PI, AQ, CP, SG, SC, LI, VI, LE, CN }, // 86
            new[] { GE, TA, AR, SG, CP, AQ, PI, AR, TA }, // 83
            new[] { GE, LE, CN, VI, LI, SC, PI, AQ, CP }, // 85
            new[] { SG, SC, LI, VI, LE, CN, GE, TA, AR }, // 100
        },
    };

    /// <summary>
    /// Which of the four tables a nakshatra (0-based) belongs to. The groups interleave rather
    /// than alternate - the book lists each table's members by name.
    /// </summary>
    public static readonly int[] GroupOfNakshatra =
    {
        0, 1, 0, 2, 3, 3, 0, 1, 0, 2, 3, 3, 0, 1, 0, 2, 3, 3, 0, 1, 0, 2, 3, 3, 0, 0, 1
    };

    private static readonly Planet[] SignLords =
    {
        Planet.Mars, Planet.Venus, Planet.Mercury, Planet.Moon, Planet.Sun, Planet.Mercury,
        Planet.Venus, Planet.Mars, Planet.Jupiter, Planet.Saturn, Planet.Saturn, Planet.Jupiter
    };

    public static bool IsApasavya(int nakshatra) => GroupOfNakshatra[Math.Clamp(nakshatra, 0, 26)] >= 2;

    public string Name => "Kalachakra";
    public override string ToString() => Name;

    private double _timeZoneOffset;
    private int _lagnaSign; // 1-12, for the house shown beside each sign; 0 to omit

    /// <summary>The nine rashis this Moon's periods run through, straight from the table.</summary>
    public static int[] Progression(double moonLongitude)
    {
        double moon = ((moonLongitude % 360.0) + 360.0) % 360.0;
        double span = NakshatraDashaSystem.NakshatraSpan;
        int nakshatra = Math.Clamp((int)(moon / span), 0, 26);
        int pada = Math.Clamp((int)(moon % span / (span / 4)), 0, 3);
        return Groups[GroupOfNakshatra[nakshatra]][pada];
    }

    public DashaResult Calculate(
        double moonLongitude,
        double birthJulianDay,
        double currentJulianDay,
        int lagnaSign = 0,
        int levels = MaxLevel,
        double timeZoneOffset = 0.0)
    {
        _timeZoneOffset = timeZoneOffset;
        _lagnaSign = lagnaSign;
        levels = Math.Clamp(levels, 1, MaxLevel);

        var result = new DashaResult();
        double moon = ((moonLongitude % 360.0) + 360.0) % 360.0;
        double span = NakshatraDashaSystem.NakshatraSpan;
        int nakshatra = Math.Clamp((int)(moon / span), 0, 26);
        result.MoonNakshatra = ZodiacUtils.NakshatraNames[nakshatra + 1];
        result.MoonNakshatraPada = Math.Clamp((int)(moon % span / (span / 4)), 0, 3) + 1;

        int[] signs = Progression(moon);

        // The first period is reduced to the part still unspent, measured against the PADA the
        // Moon occupies - a pada, not a whole nakshatra, maps to one sign here.
        double padaSpan = span / 4.0;
        double balance = 1.0 - (moon % span % padaSpan) / padaSpan;

        double startJd = birthJulianDay;
        for (int i = 0; i < signs.Length; i++)
        {
            double years = RashiYears[signs[i]] * (i == 0 ? balance : 1.0);
            if (i == 0) result.BalanceAtBirthDays = years * UduDashaCalculator.DaysPerYear;

            double endJd = startJd + years * UduDashaCalculator.DaysPerYear;
            var period = MakePeriod(signs[i], 1, startJd, endJd, years, currentJulianDay);
            if (levels >= 2)
            {
                period.SubPeriods = SubPeriods(signs, startJd, endJd, years, 2, levels, currentJulianDay);
            }
            result.MahaDashas.Add(period);
            startJd = endJd;
        }

        UduDashaCalculator.SetCurrentChain(result);
        return result;
    }

    /// <summary>
    /// Sub-periods divide their parent among the SAME nine signs, in the proportions the signs
    /// hold in the progression as a whole - the Android app's rule, which its tests do not pin.
    /// The last child takes the parent's exact end so the tiling does not drift.
    /// </summary>
    private List<DashaPeriod> SubPeriods(
        int[] signs, double parentStartJd, double parentEndJd, double parentYears,
        int level, int maxLevel, double currentJd)
    {
        double total = signs.Sum(s => RashiYears[s]);
        double parentSpan = parentEndJd - parentStartJd;
        var children = new List<DashaPeriod>();
        double startJd = parentStartJd;

        for (int i = 0; i < signs.Length; i++)
        {
            double proportion = RashiYears[signs[i]] / total;
            double endJd = i == signs.Length - 1 ? parentEndJd : startJd + parentSpan * proportion;
            double years = parentYears * proportion;

            var child = MakePeriod(signs[i], level, startJd, endJd, years, currentJd);
            if (level < maxLevel)
            {
                child.SubPeriods = SubPeriods(signs, startJd, endJd, years, level + 1, maxLevel, currentJd);
            }
            children.Add(child);
            startJd = endJd;
        }
        return children;
    }

    private DashaPeriod MakePeriod(int sign, int level, double startJd, double endJd, double years, double currentJd)
    {
        Planet lord = SignLords[sign];
        string signName = ZodiacUtils.GetSignName(sign + 1);
        string label = _lagnaSign >= 1 && _lagnaSign <= 12
            ? $"{signName} ({((sign + 1 - _lagnaSign + 12) % 12) + 1})"
            : signName;

        return new DashaPeriod
        {
            // A sign rules this period, not a graha. The ruler goes in Planet so shared code has
            // a graha to read; the sign is what displays.
            Planet = lord.ToString(),
            Symbol = ZodiacUtils.SignAbbreviations[sign + 1],
            LordLabel = label,
            Level = level,
            StartJulianDay = startJd,
            EndJulianDay = endJd,
            StartDate = JdToLocal(startJd),
            EndDate = JdToLocal(endJd),
            DurationYears = years,
            IsActive = currentJd >= startJd && currentJd < endJd,
            TimeZoneOffset = _timeZoneOffset
        };
    }

    private DateTime JdToLocal(double jd)
    {
        if (jd < 1721426) return DateTime.MinValue;
        return new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Unspecified)
            .AddDays(jd - 2451545.0)
            .AddHours(_timeZoneOffset);
    }
}
