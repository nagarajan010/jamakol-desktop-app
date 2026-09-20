using System;
using System.Collections.Generic;
using JamakolAstrology.Services;

namespace JamakolAstrology.Models;

/// <summary>
/// One Narayana dasha period, ruled by a RASI rather than a graha.
///
/// <see cref="Years"/> can be zero: a sign giving twelve years in the first cycle gives twelve
/// minus twelve in the second. Reference software prints such a row with equal start and end
/// dates, and so does this - the period is a real part of the reckoning and dropping it would
/// silently renumber everything after.
/// </summary>
public class NarayanaPeriod
{
    /// <summary>0-based sign, Aries = 0.</summary>
    public int Rashi { get; set; }

    /// <summary>
    /// The period in WHOLE years, as the sources tabulate a mahadasha.
    ///
    /// Below the first level this rounds down and is nearly always zero, because a twelfth of a
    /// few years is months. Read <see cref="SpanYears"/> for the real length; this is kept
    /// because every published table gives a mahadasha as an integer.
    /// </summary>
    public int Years { get; set; }

    /// <summary>The exact length in years, fractional below the first level.</summary>
    public double SpanYears { get; set; }

    /// <summary>Years of life elapsed when this period opens.</summary>
    public double StartAge { get; set; }

    /// <summary>Which pass through the twelve signs - 1 or 2, or 0 for a sub-period.</summary>
    public int Cycle { get; set; }

    /// <summary>Nesting depth: 1 = mahadasha, 2 = antardasha, and so on down.</summary>
    public int Level { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }

    public List<NarayanaPeriod> SubPeriods { get; set; } = new();

    /// <summary>The lagna's rashi (1-12), which the house is counted from.</summary>
    public int LagnaSign { get; set; }

    /// <summary>Sign name, 1-based for the shared lookup.</summary>
    public string SignName => ZodiacUtils.GetSignName(Rashi + 1);

    /// <summary>
    /// The house this period's sign holds from the LAGNA. A rasi dasha is read by house, so a
    /// bare sign name would ask the reader to count round the zodiac on every row.
    /// </summary>
    public int House => LagnaSign >= 1 && LagnaSign <= 12
        ? ((Rashi + 1 - LagnaSign + 12) % 12) + 1
        : 0;

    public string DisplayName => House > 0 ? $"{SignName} ({House})" : SignName;

    /// <summary>A zero-length period is real and is shown, but says so rather than looking broken.</summary>
    public string DurationLabel => Level == 1
        ? (Years == 0 ? "0 yrs" : $"{Years} yrs")
        : FormatSpan(SpanYears);

    private static string FormatSpan(double years)
    {
        double days = years * 365.256363;
        if (days >= 365.0) return $"{years:F2} yrs";
        if (days >= 1.0) return $"{days:F0} d";
        return $"{days * 24:F1} h";
    }

    public string DateRange => Level <= 2
        ? $"{StartDate:dd-M-yyyy} to {EndDate:dd-M-yyyy}"
        : $"{StartDate:dd-M-yyyy HH:mm} to {EndDate:dd-M-yyyy HH:mm}";

    public string ActiveLabel => IsActive ? " > Current" : "";
}

/// <summary>Complete Narayana dasha result.</summary>
public class NarayanaDashaResult
{
    /// <summary>The sign the dasha opens in - the stronger of the lagna and the seventh.</summary>
    public int StartingSign { get; set; }

    public string StartingSignName => ZodiacUtils.GetSignName(StartingSign + 1);

    /// <summary>The twelve signs in the order their dashas run.</summary>
    public List<int> Order { get; set; } = new();

    public List<NarayanaPeriod> MahaDashas { get; set; } = new();

    /// <summary>The running period at each level, outermost first.</summary>
    public List<NarayanaPeriod> CurrentChain { get; set; } = new();

    public string CurrentDisplay => CurrentChain.Count > 0
        ? string.Join(" / ", CurrentChain.ConvertAll(p => p.DisplayName))
        : "-";
}
