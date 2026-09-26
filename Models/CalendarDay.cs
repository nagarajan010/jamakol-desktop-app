using System;
using System.Collections.Generic;
using System.Linq;

namespace JamakolAstrology.Models;

/// <summary>
/// A single Panchanga element (Tithi/Nakshatra/Yoga/Karana) with its exact
/// start and end instants. A calendar day can hold more than one of each.
/// </summary>
public class PanchangaSegment
{
    public string Name { get; set; } = "";
    public string NameTamil { get; set; } = "";

    /// <summary>Lord abbreviation (e.g. "Su", "Mo") where applicable.</summary>
    public string Lord { get; set; } = "";

    /// <summary>Pada 1-4, Nakshatra only. 0 when not applicable.</summary>
    public int Pada { get; set; }

    /// <summary>Shukla/Krishna, Tithi only.</summary>
    public string Paksha { get; set; } = "";
    public string PakshaTamil { get; set; } = "";

    /// <summary>
    /// Local start instant. Null when the segment began before the sweep
    /// window opened (i.e. its true start is unknown).
    /// </summary>
    public DateTime? Start { get; set; }

    /// <summary>
    /// Local end instant. Null when the segment runs past the sweep window.
    /// </summary>
    public DateTime? End { get; set; }

    public string EndDisplay => End.HasValue ? End.Value.ToString("HH:mm") : "—";
    public string StartDisplay => Start.HasValue ? Start.Value.ToString("HH:mm") : "—";

    /// <summary>
    /// End time as shown on a specific day's row. A segment routinely ends after
    /// midnight, so a bare "HH:mm" would read as earlier than its own start;
    /// suffix "+1" (or "+n") to show which day the instant falls on.
    /// </summary>
    public string EndDisplayRelativeTo(DateTime day)
    {
        if (!End.HasValue) return "—";

        int dayOffset = (End.Value.Date - day.Date).Days;
        string time = End.Value.ToString("HH:mm");
        return dayOffset > 0 ? $"{time}+{dayOffset}" : time;
    }

    public string StartDisplayRelativeTo(DateTime day)
    {
        if (!Start.HasValue) return "—";

        int dayOffset = (Start.Value.Date - day.Date).Days;
        string time = Start.Value.ToString("HH:mm");
        if (dayOffset > 0) return $"{time}+{dayOffset}";
        if (dayOffset < 0) return $"{time}{dayOffset}";
        return time;
    }

    /// <summary>"06:12 – 09:45" range anchored to the day the row belongs to.</summary>
    public string RangeDisplayRelativeTo(DateTime day)
        => $"{StartDisplayRelativeTo(day)} – {EndDisplayRelativeTo(day)}";

    /// <summary>Absolute range, used where no owning day is in scope.</summary>
    public string RangeDisplay => $"{StartDisplay} – {EndDisplay}";
}

/// <summary>
/// One inauspicious window (Rahu Kalam / Yamagandam / Gulikai) for a day.
/// </summary>
public class CalendarPeriod
{
    public string Name { get; set; } = "";
    public string Symbol { get; set; } = "";
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string RangeDisplay => $"{Start:HH:mm} – {End:HH:mm}";
}

/// <summary>
/// One hora slot within a day, used by the detail pane's hora table.
/// </summary>
public class HoraSlot
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Lord { get; set; } = "";
    public string LordTamil { get; set; } = "";
    public bool IsDay { get; set; }

    /// <summary>Night horas run past midnight, so mark which day the bound falls on.</summary>
    public string RangeDisplayRelativeTo(DateTime day)
    {
        string Fmt(DateTime t)
        {
            int offset = (t.Date - day.Date).Days;
            return offset > 0 ? $"{t:HH:mm}+{offset}" : t.ToString("HH:mm");
        }
        return $"{Fmt(Start)} – {Fmt(End)}";
    }

    public string RangeDisplay => $"{Start:HH:mm} – {End:HH:mm}";
}

/// <summary>
/// Full Panchanga for one civil day, as rendered by the Calendar tab.
/// Segments are those active between this day's sunrise and the next day's sunrise.
/// </summary>
public class CalendarDay
{
    public DateTime Date { get; set; }

    public DateTime Sunrise { get; set; }
    public DateTime Sunset { get; set; }
    public DateTime NextSunrise { get; set; }

    public string DayName { get; set; } = "";
    public string DayTamil { get; set; } = "";
    public string DayLord { get; set; } = "";

    public List<PanchangaSegment> Tithis { get; set; } = new();
    public List<PanchangaSegment> Nakshatras { get; set; } = new();
    public List<PanchangaSegment> Yogas { get; set; } = new();
    public List<PanchangaSegment> Karanas { get; set; } = new();

    public List<CalendarPeriod> InauspiciousPeriods { get; set; } = new();
    public List<HoraSlot> Horas { get; set; } = new();

    // Tamil almanac context (from the Sun's sidereal position at sunrise)
    public string TamilYear { get; set; } = "";
    public string TamilMonth { get; set; } = "";
    public string EnglishYear { get; set; } = "";
    public string EnglishMonth { get; set; } = "";

    public string SunRasi { get; set; } = "";
    public string SunRasiTamil { get; set; } = "";
    public string MoonRasi { get; set; } = "";
    public string MoonRasiTamil { get; set; } = "";

    /// <summary>True when this day belongs to the month being displayed (vs. grid padding).</summary>
    public bool IsInDisplayedMonth { get; set; } = true;

    public bool IsToday => Date.Date == DateTime.Today;

    /// <summary>The segment in force at sunrise — what a printed panchangam names the day by.</summary>
    public PanchangaSegment? PrimaryTithi => Tithis.FirstOrDefault();
    public PanchangaSegment? PrimaryNakshatra => Nakshatras.FirstOrDefault();

    public string SunriseDisplay => Sunrise.ToString("HH:mm");
    public string SunsetDisplay => Sunset.ToString("HH:mm");
}
