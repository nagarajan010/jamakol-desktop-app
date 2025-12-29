using System;

namespace JamakolAstrology.Helpers;

public static class TimeFormatHelper
{
    /// <summary>
    /// Format Julian Day to string (handles BC dates)
    /// </summary>
    public static string FormatJulianDay(double jd, bool includeTime = false)
    {
        // Simple conversion algorithm
        // Note: This is a simplified Gregorian conversion proper for standard use
        // For ancient dates, Swiss Ephemeris or rigorous algo is better, but this suffices for display
        
        long L = (long)Math.Floor(jd + 0.5);
        long n = L + 32044;
        long d = (4 * n + 3) / 146097;
        long n2 = n - (146097 * d) / 4;
        long i = (4 * n2 + 3) / 1461;
        long n3 = n2 - (1461 * i) / 4;
        long j = (80 * n3 + 13) / 2447; // 80 l + 13
        
        long day = n3 - (2447 * j) / 80;
        long month = j - (11 * (j / 14)); // Floor
        long year = 100 * d + i + (j / 14) - 4800 + ((j>=14)?1:0); // Corrected formula
        
        // Re-calculate accurately using a robust method if the above simple integer math is off for BC
        // Actually, let's use a known reliable C# implementation for JD to Gregorian
        
        if (jd < 0) return "Unknown";

        // Decompose JD
        double z = Math.Floor(jd + 0.5);
        double f = (jd + 0.5) - z;

        double alpha;
        if (z < 2299161)
        {
            alpha = z; // Julian calendar
        }
        else
        {
            double alpha1 = Math.Floor((z - 1867216.25) / 36524.25);
            alpha = z + 1 + alpha1 - Math.Floor(alpha1 / 4);
        }

        double b = alpha + 1524;
        double c = Math.Floor((b - 122.1) / 365.25);
        double d_val = Math.Floor(365.25 * c);
        double e = Math.Floor((b - d_val) / 30.6001);

        int D = (int)(b - d_val - Math.Floor(30.6001 * e) + f); // Day part includes fraction f?? No, D is integer day
        
        // Let's separate integer day and time
        int dayInt = (int)(b - d_val - Math.Floor(30.6001 * e));
        
        int M = (int)(e < 14 ? e - 1 : e - 13);
        int Y = (int)(M > 2 ? c - 4716 : c - 4715);

        // Calculate time from fraction f
        double hourDouble = f * 24.0;
        int hours = (int)Math.Floor(hourDouble);
        double minutesDouble = (hourDouble - hours) * 60.0;
        int minutes = (int)Math.Floor(minutesDouble);
        int seconds = (int)Math.Floor((minutesDouble - minutes) * 60.0);

        string suffix = "";
        if (Y <= 0)
        {
            Y = Math.Abs(Y) + 1; // 0 = 1 BC, -1 = 2 BC
            suffix = " BC";
        }

        string monthName = GetMonthName(M);
        string dateStr = $"{dayInt:00}-{monthName}-{Y}{suffix}";

        if (includeTime)
        {
            return $"{dateStr} {hours:00}:{minutes:00}";
        }
        
        return dateStr;
    }

    private static string GetMonthName(int m)
    {
        return m switch
        {
            1 => "Jan", 2 => "Feb", 3 => "Mar", 4 => "Apr", 5 => "May", 6 => "Jun",
            7 => "Jul", 8 => "Aug", 9 => "Sep", 10 => "Oct", 11 => "Nov", 12 => "Dec",
            _ => "???"
        };
    }
}
