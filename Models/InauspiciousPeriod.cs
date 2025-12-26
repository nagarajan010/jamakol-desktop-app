using System;
using System.Collections.Generic;

namespace JamakolAstrology.Models;

/// <summary>
/// Represents an inauspicious time period during the day
/// </summary>
public class InauspiciousPeriod
{
    /// <summary>Name of the period (Rahu Kalam, Yamagandam, etc.)</summary>
    public string Name { get; set; } = "";

    /// <summary>Short symbol</summary>
    public string Symbol { get; set; } = "";

    /// <summary>Start time of the period</summary>
    public DateTime StartTime { get; set; }

    /// <summary>End time of the period</summary>
    public DateTime EndTime { get; set; }

    /// <summary>Whether the given birth time falls within this period</summary>
    public bool IsActive { get; set; }

    /// <summary>Display format for start time (HH:mm)</summary>
    public string StartDisplay => StartTime.ToString("HH:mm");

    /// <summary>Display format for end time (HH:mm)</summary>
    public string EndDisplay => EndTime.ToString("HH:mm");

    /// <summary>Full display format</summary>
    public string TimeRangeDisplay => $"{StartDisplay} - {EndDisplay}";
}
