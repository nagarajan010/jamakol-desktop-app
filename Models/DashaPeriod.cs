using System;
using System.Collections.Generic;

namespace JamakolAstrology.Models;

/// <summary>
/// Represents a Dasa period at any level (Maha, Antar, Pratyantara, Sookshma, Prana)
/// </summary>
public class DashaPeriod
{
    /// <summary>Planet ruling this Dasa period</summary>
    public string Planet { get; set; } = "";
    
    /// <summary>Short symbol for the planet</summary>
    public string Symbol { get; set; } = "";
    
    /// <summary>Level: 1=Maha, 2=Antar, 3=Pratyantara, 4=Sookshma, 5=Prana</summary>
    public int Level { get; set; }
    
    /// <summary>Start date of this period</summary>
    public DateTime StartDate { get; set; }
    
    /// <summary>End date of this period</summary>
    public DateTime EndDate { get; set; }
    
    /// <summary>Duration in years (fractional)</summary>
    public double DurationYears { get; set; }
    
    /// <summary>Sub-periods within this Dasa</summary>
    public List<DashaPeriod> SubPeriods { get; set; } = new();
    
    /// <summary>Whether this period is currently active</summary>
    public bool IsActive { get; set; }
    
    /// <summary>Display name with level prefix</summary>
    public string DisplayName => Level switch
    {
        1 => $"{Planet} Maha Dasa",
        2 => $"{Planet} Bhukti",
        3 => $"{Planet} Pratyantara",
        4 => $"{Planet} Sookshma",
        5 => $"{Planet} Prana",
        6 => $"{Planet} Deha",
        _ => Planet
    };
    
    /// <summary>Date range display</summary>
    public string DateRange => $"{StartDate:dd-MM-yyyy} to {EndDate:dd-MM-yyyy}";
    
    /// <summary>Short date range for compact display</summary>
    public string ShortDateRange => Level >= 5 
        ? $"{StartDate:dd-MMM HH:mm} - {EndDate:dd-MMM HH:mm}" // Show time for Prana/Deha
        : $"{StartDate:dd-MMM-yyyy} - {EndDate:dd-MMM-yyyy}";
}

/// <summary>
/// Complete Dasha calculation result
/// </summary>
public class DashaResult
{
    /// <summary>Moon's nakshatra used for calculation</summary>
    public string MoonNakshatra { get; set; } = "";
    
    /// <summary>Moon's nakshatra pada</summary>
    public int MoonNakshatraPada { get; set; }
    
    /// <summary>Balance of first dasa at birth (in days)</summary>
    public double BalanceAtBirthDays { get; set; }
    
    /// <summary>All Maha Dasa periods (containing sub-periods recursively)</summary>
    public List<DashaPeriod> MahaDashas { get; set; } = new();
    
    /// <summary>Currently running Maha Dasa</summary>
    public DashaPeriod? CurrentMahaDasha { get; set; }
    
    /// <summary>Currently running Antar Dasa (Bhukti)</summary>
    public DashaPeriod? CurrentAntarDasha { get; set; }
    
    /// <summary>Currently running Pratyantara Dasa</summary>
    public DashaPeriod? CurrentPratyantaraDasha { get; set; }
    
    /// <summary>Currently running Sookshma Dasa</summary>
    public DashaPeriod? CurrentSookshmaDasha { get; set; }
    
    /// <summary>Currently running Prana Dasa</summary>
    public DashaPeriod? CurrentPranaDasha { get; set; }
    
    /// <summary>Currently running Deha Dasa</summary>
    public DashaPeriod? CurrentDehaDasha { get; set; }
    
    /// <summary>Full current dasa string for display</summary>
    public string CurrentDashaDisplay => 
        $"{CurrentMahaDasha?.Planet ?? "-"} / {CurrentAntarDasha?.Planet ?? "-"} / {CurrentPratyantaraDasha?.Planet ?? "-"}";
}
