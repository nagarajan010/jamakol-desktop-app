using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using JamakolAstrology.Services;

namespace JamakolAstrology.Models;

/// <summary>
/// Represents one Sudarshana Chakra Dasha period.
/// </summary>
public class SudarshanaDashaPeriod : INotifyPropertyChanged
{
    /// <summary>
    /// Deepest level the dasha runs to. Six levels puts the last one at about twenty-five
    /// minutes, which is as fine as the reckoning is meaningful.
    /// </summary>
    public const int MaxLevel = 6;

    public int DashaYear { get; set; }

    /// <summary>The rashi (1-12) of the mahadasha this period sits under.</summary>
    public int MainDashaSign { get; set; }

    /// <summary>The lagna's rashi (1-12), which the house is counted from.</summary>
    public int LagnaSign { get; set; }

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
    private ObservableCollection<SudarshanaDashaPeriod> _subPeriods = new();

    /// <summary>
    /// Children of this period. Observable so a level filled in on demand reaches the view.
    /// </summary>
    public ObservableCollection<SudarshanaDashaPeriod> SubPeriods
    {
        get => _subPeriods;
        set
        {
            _subPeriods = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SubPeriods)));
        }
    }

    /// <summary>
    /// Whether this period has a level beneath it. Twelve to the power of six is over three
    /// million periods, so the tree is built a few levels deep and the rest is filled in as the
    /// reader opens it - this is what tells the view to offer an expander meanwhile.
    /// </summary>
    public bool CanExpand => Level < MaxLevel;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string SignName => ZodiacUtils.GetSignName(Sign);

    /// <summary>
    /// The house this period's sign holds from the LAGNA.
    ///
    /// A rashi dasha is read by house - the 2nd for family and wealth, the 7th for partnership -
    /// so a bare sign name asks the reader to count round the zodiac from their own lagna on
    /// every row.
    /// </summary>
    public int House => LagnaSign >= 1 && LagnaSign <= 12 && Sign >= 1 && Sign <= 12
        ? ((Sign - LagnaSign + 12) % 12) + 1
        : 0;

    /// <summary>The sign with the house it holds, as "Scorpio (2)".</summary>
    public string DisplayName => House > 0 ? $"{SignName} ({House})" : SignName;

    public string MainDisplayName => ZodiacUtils.GetSignName(MainDashaSign);
    public string SubDisplayName => DisplayName;

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

    /// <summary>
    /// The running chain, each link named with the house it holds from the lagna:
    /// "Aquarius (5) / Scorpio (2) / Libra (1)".
    /// </summary>
    public string CurrentDisplay => CurrentChain.Count > 0
        ? string.Join(" / ", CurrentChain.ConvertAll(p => p.DisplayName))
        : "-";
}
