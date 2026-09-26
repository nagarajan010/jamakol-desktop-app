using System;
using System.Collections.Generic;
using System.Linq;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Builds a full month of Panchanga data for the Calendar tab.
///
/// Rather than running the whole chart pipeline once per day (which would repeat
/// the expensive boundary search ~31 times), this sweeps Sun and Moon across the
/// month ONCE at a coarse step, detects every Tithi/Nakshatra/Yoga/Karana boundary
/// crossing in that single pass, refines each crossing by bisection, and then
/// buckets the resulting segments into days. That yields exact start AND end
/// instants natively, and naturally handles days that carry two (or more)
/// segments of the same limb.
/// </summary>
public class CalendarService
{
    // Coarse sweep step. The fastest-moving limb is Karana (6 degrees of
    // Moon-Sun elongation, ~6h minimum). A 30-minute step cannot skip one.
    private const double SweepStepDays = 30.0 / (24.0 * 60.0);

    // Bisection passes to pin a crossing. 20 halvings of a 30-minute bracket
    // lands well under a second.
    private const int RefinementPasses = 20;

    private const double NakshatraArc = 360.0 / 27.0;
    private const double YogaArc = 360.0 / 27.0;
    private const double TithiArc = 12.0;
    private const double KaranaArc = 6.0;

    private static readonly string[] EnglishDays =
    {
        "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
    };

    private static readonly string[] TamilDays =
    {
        "ஞாயிறு", "திங்கள்", "செவ்வாய்", "புதன்", "வியாழன்", "வெள்ளி", "சனி"
    };

    private static readonly string[] HoraLords =
    {
        "Sun", "Venus", "Mercury", "Moon", "Saturn", "Jupiter", "Mars"
    };

    private static readonly string[] HoraLordsTamil =
    {
        "சூரியன்", "சுக்கிரன்", "புதன்", "சந்திரன்", "சனி", "குரு", "செவ்வாய்"
    };

    // Maps weekday -> index into the hora lord cycle at sunrise.
    private static readonly int[] DayToHoraCycleIndex = { 0, 3, 6, 2, 5, 1, 4 };

    private readonly InauspiciousPeriodsCalculator _inauspiciousCalculator = new();
    private readonly ChartCalculator _chartCalculator = new();

    /// <summary>
    /// Calculate every day of the given civil month, plus leading/trailing padding
    /// days so the result fills whole weeks (Sunday-first) for a 7-column grid.
    /// </summary>
    public List<CalendarDay> CalculateMonth(
        int year,
        int month,
        double latitude,
        double longitude,
        double timezoneOffset,
        AppSettings settings)
    {
        var firstOfMonth = new DateTime(year, month, 1);
        int daysInMonth = DateTime.DaysInMonth(year, month);

        // Pad to whole weeks so the grid never has to special-case a ragged edge.
        int leadingPad = (int)firstOfMonth.DayOfWeek;
        DateTime gridStart = firstOfMonth.AddDays(-leadingPad);

        int totalCells = leadingPad + daysInMonth;
        int trailingPad = (7 - (totalCells % 7)) % 7;
        int gridDays = totalCells + trailingPad;

        DateTime gridEnd = gridStart.AddDays(gridDays);

        int ayanamshaId = (int)settings.Ayanamsha;
        double ayanamshaOffset = settings.AyanamshaOffset;

        var days = new List<CalendarDay>(gridDays);

        using var ephemeris = new EphemerisService();
        using var sunrise = new SunriseCalculator(settings.SunriseMode);

        // --- Pass 1: sunrise/sunset for every day in the grid, plus one extra
        // day so the last day knows where its Vedic day closes.
        var sunriseTimes = new DateTime[gridDays + 1];
        var sunsetTimes = new DateTime[gridDays + 1];
        for (int i = 0; i <= gridDays; i++)
        {
            DateTime d = gridStart.AddDays(i);
            sunriseTimes[i] = sunrise.CalculateSunrise(d, latitude, longitude, timezoneOffset);
            sunsetTimes[i] = sunrise.CalculateSunset(d, latitude, longitude, timezoneOffset);
        }

        // --- Pass 2: one continuous sweep over the whole window.
        // Start a little before the grid so the first day's opening segments have
        // known start times, and run a little past it for the closing ones.
        DateTime sweepStart = gridStart.AddDays(-2);
        DateTime sweepEnd = gridEnd.AddDays(2);

        var tithiSegments = new List<PanchangaSegment>();
        var nakshatraSegments = new List<PanchangaSegment>();
        var yogaSegments = new List<PanchangaSegment>();
        var karanaSegments = new List<PanchangaSegment>();

        SweepSegments(
            ephemeris, sweepStart, sweepEnd, timezoneOffset, ayanamshaId, ayanamshaOffset,
            tithiSegments, nakshatraSegments, yogaSegments, karanaSegments);

        // --- Pass 3: assemble each day.
        for (int i = 0; i < gridDays; i++)
        {
            DateTime date = gridStart.AddDays(i);
            var day = new CalendarDay
            {
                Date = date,
                Sunrise = sunriseTimes[i],
                Sunset = sunsetTimes[i],
                NextSunrise = sunriseTimes[i + 1],
                IsInDisplayedMonth = date.Month == month && date.Year == year
            };

            int dayIndex = (int)date.DayOfWeek;
            day.DayName = EnglishDays[dayIndex];
            day.DayTamil = TamilDays[dayIndex];
            day.DayLord = JamaGrahaCalculator.GetDayLord(date.DayOfWeek);

            // A Vedic day runs sunrise -> next sunrise; segments are those
            // overlapping that window.
            day.Tithis = SegmentsOverlapping(tithiSegments, day.Sunrise, day.NextSunrise);
            day.Nakshatras = SegmentsOverlapping(nakshatraSegments, day.Sunrise, day.NextSunrise);
            day.Yogas = SegmentsOverlapping(yogaSegments, day.Sunrise, day.NextSunrise);
            day.Karanas = SegmentsOverlapping(karanaSegments, day.Sunrise, day.NextSunrise);

            day.InauspiciousPeriods = _inauspiciousCalculator
                .Calculate(day.Sunrise, day.Sunset, day.Sunrise, date.DayOfWeek)
                .Select(p => new CalendarPeriod
                {
                    Name = p.Name,
                    Symbol = p.Symbol,
                    Start = p.StartTime,
                    End = p.EndTime
                })
                .ToList();

            day.Horas = BuildHoraTable(day.Sunrise, day.Sunset, day.NextSunrise, dayIndex);

            // Tamil year/month and rasi context, sampled at sunrise.
            double sunriseJd = ToJulianDay(ephemeris, day.Sunrise, timezoneOffset);
            double sunLong = ephemeris.GetPlanetPosition(sunriseJd, (int)Planet.Sun, ayanamshaId, ayanamshaOffset).longitude;
            double moonLong = ephemeris.GetPlanetPosition(sunriseJd, (int)Planet.Moon, ayanamshaId, ayanamshaOffset).longitude;

            int sunSign = ZodiacUtils.DegreeToSign(sunLong);
            int moonSign = ZodiacUtils.DegreeToSign(moonLong);
            day.SunRasi = ZodiacUtils.SignNames[sunSign];
            day.SunRasiTamil = ZodiacUtils.SignNamesTamil[sunSign];
            day.MoonRasi = ZodiacUtils.SignNames[moonSign];
            day.MoonRasiTamil = ZodiacUtils.SignNamesTamil[moonSign];

            var (tamilYear, englishYear, tamilMonth, englishMonth) = PanchangaNames.GetTamilYearMonth(date, sunSign);
            day.TamilYear = tamilYear;
            day.EnglishYear = englishYear;
            day.TamilMonth = tamilMonth;
            day.EnglishMonth = englishMonth;

            days.Add(day);
        }

        return days;
    }

    /// <summary>
    /// Planetary positions at a given day's sunrise, for the full-screen day view.
    /// Computed on demand (one day at a time) rather than for every cell in the
    /// month, since it is far heavier than the Panchanga sweep.
    /// </summary>
    public ChartData CalculateDayPlanets(
        CalendarDay day,
        double latitude,
        double longitude,
        double timezoneOffset,
        AppSettings settings)
    {
        var sunriseLocal = day.Sunrise;

        var birthData = new BirthData
        {
            Name = day.Date.ToString("dd MMM yyyy"),
            Year = sunriseLocal.Year,
            Month = sunriseLocal.Month,
            Day = sunriseLocal.Day,
            Hour = sunriseLocal.Hour,
            Minute = sunriseLocal.Minute,
            Second = sunriseLocal.Second,
            Latitude = latitude,
            Longitude = longitude,
            Location = "Calendar",
            TimeZoneOffset = timezoneOffset
        };

        return _chartCalculator.CalculateChart(birthData, settings.Ayanamsha);
    }

    /// <summary>
    /// Walk the window once, recording every boundary crossing of all four limbs.
    /// </summary>
    private void SweepSegments(
        EphemerisService ephemeris,
        DateTime sweepStart,
        DateTime sweepEnd,
        double timezoneOffset,
        int ayanamshaId,
        double ayanamshaOffset,
        List<PanchangaSegment> tithis,
        List<PanchangaSegment> nakshatras,
        List<PanchangaSegment> yogas,
        List<PanchangaSegment> karanas)
    {
        double startJd = ToJulianDay(ephemeris, sweepStart, timezoneOffset);
        double endJd = ToJulianDay(ephemeris, sweepEnd, timezoneOffset);

        double prevJd = startJd;
        var prev = Sample(ephemeris, prevJd, ayanamshaId, ayanamshaOffset);

        // Index of the segment currently open for each limb.
        int tithiIdx = TithiIndex(prev.Elongation);
        int nakIdx = (int)(prev.Moon / NakshatraArc);
        int yogaIdx = (int)(prev.Sum / YogaArc);
        int karanaIdx = (int)(prev.Elongation / KaranaArc);

        // Open segments start unknown — they began before the sweep window.
        DateTime? tithiStart = null, nakStart = null, yogaStart = null, karanaStart = null;

        for (double jd = startJd + SweepStepDays; jd <= endJd; jd += SweepStepDays)
        {
            var cur = Sample(ephemeris, jd, ayanamshaId, ayanamshaOffset);

            int curTithi = TithiIndex(cur.Elongation);
            int curNak = (int)(cur.Moon / NakshatraArc);
            int curYoga = (int)(cur.Sum / YogaArc);
            int curKarana = (int)(cur.Elongation / KaranaArc);

            if (curTithi != tithiIdx)
            {
                DateTime boundary = RefineCrossing(
                    ephemeris, prevJd, jd, timezoneOffset, ayanamshaId, ayanamshaOffset,
                    s => TithiIndex(s.Elongation), tithiIdx);

                tithis.Add(PanchangaNames.BuildTithi(tithiIdx, tithiStart, boundary));
                tithiStart = boundary;
                tithiIdx = curTithi;
            }

            if (curNak != nakIdx)
            {
                DateTime boundary = RefineCrossing(
                    ephemeris, prevJd, jd, timezoneOffset, ayanamshaId, ayanamshaOffset,
                    s => (int)(s.Moon / NakshatraArc), nakIdx);

                nakshatras.Add(PanchangaNames.BuildNakshatra(nakIdx, nakStart, boundary));
                nakStart = boundary;
                nakIdx = curNak;
            }

            if (curYoga != yogaIdx)
            {
                DateTime boundary = RefineCrossing(
                    ephemeris, prevJd, jd, timezoneOffset, ayanamshaId, ayanamshaOffset,
                    s => (int)(s.Sum / YogaArc), yogaIdx);

                yogas.Add(PanchangaNames.BuildYoga(yogaIdx, yogaStart, boundary));
                yogaStart = boundary;
                yogaIdx = curYoga;
            }

            if (curKarana != karanaIdx)
            {
                DateTime boundary = RefineCrossing(
                    ephemeris, prevJd, jd, timezoneOffset, ayanamshaId, ayanamshaOffset,
                    s => (int)(s.Elongation / KaranaArc), karanaIdx);

                karanas.Add(PanchangaNames.BuildKarana(karanaIdx, karanaStart, boundary));
                karanaStart = boundary;
                karanaIdx = curKarana;
            }

            prevJd = jd;
            prev = cur;
        }

        // Close the trailing segments with an unknown end.
        tithis.Add(PanchangaNames.BuildTithi(tithiIdx, tithiStart, null));
        nakshatras.Add(PanchangaNames.BuildNakshatra(nakIdx, nakStart, null));
        yogas.Add(PanchangaNames.BuildYoga(yogaIdx, yogaStart, null));
        karanas.Add(PanchangaNames.BuildKarana(karanaIdx, karanaStart, null));
    }

    /// <summary>
    /// Bisect a bracketed crossing until the instant the limb index leaves
    /// <paramref name="indexBefore"/>. Works for every limb because it compares
    /// index identity rather than raw degrees, so the 360-degree wrap needs no
    /// special handling.
    /// </summary>
    private DateTime RefineCrossing(
        EphemerisService ephemeris,
        double lowJd,
        double highJd,
        double timezoneOffset,
        int ayanamshaId,
        double ayanamshaOffset,
        Func<Sampled, int> indexOf,
        int indexBefore)
    {
        for (int i = 0; i < RefinementPasses; i++)
        {
            double mid = (lowJd + highJd) / 2.0;
            var sample = Sample(ephemeris, mid, ayanamshaId, ayanamshaOffset);

            if (indexOf(sample) == indexBefore)
                lowJd = mid;
            else
                highJd = mid;
        }

        return FromJulianDay(highJd, timezoneOffset);
    }

    private readonly struct Sampled
    {
        public readonly double Sun;
        public readonly double Moon;

        public Sampled(double sun, double moon)
        {
            Sun = sun;
            Moon = moon;
        }

        /// <summary>Moon - Sun, normalized to 0-360. Drives Tithi and Karana.</summary>
        public double Elongation
        {
            get
            {
                double d = Moon - Sun;
                while (d < 0) d += 360;
                while (d >= 360) d -= 360;
                return d;
            }
        }

        /// <summary>Sun + Moon, normalized to 0-360. Drives Yoga.</summary>
        public double Sum
        {
            get
            {
                double s = Sun + Moon;
                while (s >= 360) s -= 360;
                while (s < 0) s += 360;
                return s;
            }
        }
    }

    private Sampled Sample(EphemerisService ephemeris, double jd, int ayanamshaId, double ayanamshaOffset)
    {
        double sun = ephemeris.GetPlanetPosition(jd, (int)Planet.Sun, ayanamshaId, ayanamshaOffset).longitude;
        double moon = ephemeris.GetPlanetPosition(jd, (int)Planet.Moon, ayanamshaId, ayanamshaOffset).longitude;
        return new Sampled(sun, moon);
    }

    /// <summary>Tithi index 0-29 from elongation.</summary>
    private static int TithiIndex(double elongation)
    {
        int idx = (int)(elongation / TithiArc);
        return idx > 29 ? 29 : idx;
    }

    /// <summary>
    /// Segments overlapping [windowStart, windowEnd). A null Start/End means the
    /// segment extends beyond the swept window, so it counts as overlapping.
    /// </summary>
    private static List<PanchangaSegment> SegmentsOverlapping(
        List<PanchangaSegment> all, DateTime windowStart, DateTime windowEnd)
    {
        return all
            .Where(s => (!s.End.HasValue || s.End.Value > windowStart) &&
                        (!s.Start.HasValue || s.Start.Value < windowEnd))
            .OrderBy(s => s.Start ?? DateTime.MinValue)
            .ToList();
    }

    /// <summary>
    /// 24 horas: 12 unequal day slots sunrise-&gt;sunset, 12 night slots sunset-&gt;next sunrise.
    /// </summary>
    private static List<HoraSlot> BuildHoraTable(DateTime sunrise, DateTime sunset, DateTime nextSunrise, int dayIndex)
    {
        var slots = new List<HoraSlot>(24);
        int startingIndex = DayToHoraCycleIndex[dayIndex];

        double dayTicks = (sunset - sunrise).Ticks / 12.0;
        for (int i = 0; i < 12; i++)
        {
            int lordIdx = (startingIndex + i) % 7;
            slots.Add(new HoraSlot
            {
                Start = sunrise.AddTicks((long)(dayTicks * i)),
                End = sunrise.AddTicks((long)(dayTicks * (i + 1))),
                Lord = HoraLords[lordIdx],
                LordTamil = HoraLordsTamil[lordIdx],
                IsDay = true
            });
        }

        double nightTicks = (nextSunrise - sunset).Ticks / 12.0;
        for (int i = 0; i < 12; i++)
        {
            int lordIdx = (startingIndex + 12 + i) % 7;
            slots.Add(new HoraSlot
            {
                Start = sunset.AddTicks((long)(nightTicks * i)),
                End = sunset.AddTicks((long)(nightTicks * (i + 1))),
                Lord = HoraLords[lordIdx],
                LordTamil = HoraLordsTamil[lordIdx],
                IsDay = false
            });
        }

        return slots;
    }

    private static double ToJulianDay(EphemerisService ephemeris, DateTime localTime, double timezoneOffset)
        => ephemeris.GetJulianDay(localTime.AddHours(-timezoneOffset));

    private static DateTime FromJulianDay(double jd, double timezoneOffset)
        => EphemerisService.JulianDateToDateTime(jd).AddHours(timezoneOffset);
}
