using System;
using System.Collections.Generic;
using JamakolAstrology.Services;

namespace JamakolAstrology.Models;

/// <summary>
/// Represents one Sudarshana Chakra Dasha period.
/// </summary>
public class SudarshanaDashaPeriod
{
    public int DashaYear { get; set; }
    public int MainDashaHouse { get; set; }
    public int SubDashaHouse { get; set; }

    /// <summary>
    /// The rashi (1-12) ruling this period. A SIGN rules a Sudarshana period, not a graha, so
    /// the sign's name is what displays - reference software prints a rashi here, not a house
    /// index. House numbers alone lost that identity because the calculator never read the lagna.
    /// </summary>
    public int Sign { get; set; }

    /// <summary>Nesting depth: 1 = mahadasha (12 years), 2 = annual, and so on down.</summary>
    public int Level { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public List<SudarshanaDashaPeriod> SubPeriods { get; set; } = new();

    public string SignName => ZodiacUtils.GetSignName(Sign);

    public string DisplayName => Level == 1
        ? SignName
        : $"{SignName} ({SubDisplayName})";

    public string MainDisplayName => ZodiacUtils.GetSignName(MainDashaHouse);
    public string SubDisplayName => ZodiacUtils.GetSignName(SubDashaHouse);

    /// <summary>
    /// Periods below the year run to minutes, so they need the time of day; the top two levels
    /// are solar returns and read naturally as dates.
    /// </summary>
    public string DateRange => Level <= 2
        ? $"{StartDate:dd-M-yyyy} to {EndDate:dd-M-yyyy}"
        : $"{StartDate:dd-M-yyyy HH:mm} to {EndDate:dd-M-yyyy HH:mm}";

    public string ActiveLabel => IsActive ? " > Current" : "";
}

/// <summary>
/// Complete Sudarshana Chakra Dasha calculation result.
/// </summary>
public class SudarshanaDashaResult
{
    public List<SudarshanaDashaPeriod> MainDashas { get; set; } = new();
    public List<SudarshanaDashaPeriod> Years { get; set; } = new();
    public SudarshanaDashaPeriod? CurrentMainDasha { get; set; }
    public SudarshanaDashaPeriod? CurrentPeriod { get; set; }

    /// <summary>The running period at each level, outermost first.</summary>
    public List<SudarshanaDashaPeriod> CurrentChain { get; set; } = new();

    public string CurrentDisplay => CurrentChain.Count > 0
        ? string.Join(" / ", CurrentChain.ConvertAll(p => p.SignName))
        : "-";
}
