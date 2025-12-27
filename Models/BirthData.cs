namespace JamakolAstrology.Models;

/// <summary>
/// Represents birth data for chart calculation
/// Supports BC dates using Year/Month/Day integer properties (Year can be negative for BC)
/// </summary>
public class BirthData
{
    public string Name { get; set; } = string.Empty;
    
    // Date components - Year can be negative for BC dates (astronomical year numbering)
    // Year 0 = 1 BCE, Year -1 = 2 BCE, etc.
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
    public int Hour { get; set; }
    public int Minute { get; set; }
    public int Second { get; set; }
    
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Location { get; set; } = string.Empty;
    public double TimeZoneOffset { get; set; } // In hours (e.g., +5.5 for IST)

    /// <summary>
    /// Returns true if this is a BC date (Year <= 0)
    /// </summary>
    public bool IsBCDate => Year <= 0;

    /// <summary>
    /// For AD dates, returns a DateTime. For BC dates, returns DateTime.MinValue.
    /// Use GetUtcComponents() for BC date UTC calculations.
    /// </summary>
    public DateTime BirthDateTime
    {
        get
        {
            if (Year > 0 && Year <= 9999)
            {
                try
                {
                    return new DateTime(Year, Month, Day, Hour, Minute, Second);
                }
                catch
                {
                    return DateTime.MinValue;
                }
            }
            return DateTime.MinValue;
        }
        set
        {
            Year = value.Year;
            Month = value.Month;
            Day = value.Day;
            Hour = value.Hour;
            Minute = value.Minute;
            Second = value.Second;
        }
    }

    /// <summary>
    /// For AD dates only. For BC dates, use GetUtcHour() instead.
    /// </summary>
    public DateTime UtcDateTime => IsBCDate ? DateTime.MinValue : BirthDateTime.AddHours(-TimeZoneOffset);

    /// <summary>
    /// Calculate UTC hour (for Julian Day calculation) accounting for timezone
    /// Works for both BC and AD dates
    /// </summary>
    public double GetUtcHour()
    {
        double localHour = Hour + Minute / 60.0 + Second / 3600.0;
        return localHour - TimeZoneOffset;
    }

    /// <summary>
    /// Get UTC date components accounting for timezone offset
    /// Handles day rollover when timezone adjustment crosses midnight
    /// </summary>
    public (int year, int month, int day, double hour) GetUtcComponents()
    {
        double utcHour = GetUtcHour();
        int year = Year;
        int month = Month;
        int day = Day;

        // Handle day rollover
        if (utcHour < 0)
        {
            utcHour += 24;
            day--;
            if (day < 1)
            {
                month--;
                if (month < 1)
                {
                    month = 12;
                    year--;
                }
                day = GetDaysInMonth(year, month);
            }
        }
        else if (utcHour >= 24)
        {
            utcHour -= 24;
            day++;
            if (day > GetDaysInMonth(year, month))
            {
                day = 1;
                month++;
                if (month > 12)
                {
                    month = 1;
                    year++;
                }
            }
        }

        return (year, month, day, utcHour);
    }

    /// <summary>
    /// Get days in month (works for BC dates too using proleptic Gregorian calendar)
    /// </summary>
    private static int GetDaysInMonth(int year, int month)
    {
        int[] daysInMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        if (month == 2 && IsLeapYear(year))
            return 29;
        return daysInMonth[month - 1];
    }

    /// <summary>
    /// Check if year is a leap year (proleptic Gregorian calendar)
    /// </summary>
    private static bool IsLeapYear(int year)
    {
        // For negative years, use astronomical year numbering
        if (year <= 0) year = 1 - year; // Convert to BC year for leap calculation
        return (year % 4 == 0 && year % 100 != 0) || (year % 400 == 0);
    }

    /// <summary>
    /// Format date for display, handling BC dates
    /// </summary>
    public string GetDisplayDate()
    {
        if (IsBCDate)
        {
            int bceYear = 1 - Year; // Convert astronomical year to BCE year
            return $"{bceYear} BCE {Month:D2}/{Day:D2}";
        }
        return $"{Year:D4}/{Month:D2}/{Day:D2}";
    }

    /// <summary>
    /// Format time for display
    /// </summary>
    public string GetDisplayTime()
    {
        return $"{Hour:D2}:{Minute:D2}:{Second:D2}";
    }
}
