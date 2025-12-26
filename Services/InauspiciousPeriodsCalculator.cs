using System;
using System.Collections.Generic;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Calculator for inauspicious time periods: Rahu Kalam, Yamagandam, Gulikai Kalam
/// These are calculated by dividing the daylight period into 8 equal portions
/// and assigning specific portions to each period based on the weekday.
/// </summary>
public class InauspiciousPeriodsCalculator
{
    // Portion indices for each period by weekday (0=Sun, 1=Mon, ..., 6=Sat)
    // Portions are numbered 1-8, where 1 is the first portion after sunrise
    
    /// <summary>
    /// Rahu Kalam portion index by weekday
    /// Sun=8, Mon=2, Tue=7, Wed=5, Thu=6, Fri=4, Sat=3
    /// </summary>
    private static readonly int[] RahuKalamPortions = { 8, 2, 7, 5, 6, 4, 3 };

    /// <summary>
    /// Yamagandam (Yama Kandam) portion index by weekday
    /// Sun=5, Mon=4, Tue=3, Wed=2, Thu=1, Fri=7, Sat=6
    /// </summary>
    private static readonly int[] YamagandamPortions = { 5, 4, 3, 2, 1, 7, 6 };

    /// <summary>
    /// Gulikai Kalam portion index by weekday
    /// Sun=7, Mon=6, Tue=5, Wed=4, Thu=3, Fri=2, Sat=1
    /// </summary>
    private static readonly int[] GulikaiPortions = { 7, 6, 5, 4, 3, 2, 1 };

    /// <summary>
    /// Calculate all inauspicious periods for the given day
    /// </summary>
    /// <param name="sunrise">Sunrise time</param>
    /// <param name="sunset">Sunset time</param>
    /// <param name="birthTime">Birth/query time to check if within period</param>
    /// <param name="dayOfWeek">Day of week (Sunday=0, Monday=1, etc.)</param>
    /// <returns>List of inauspicious periods with their time windows</returns>
    public List<InauspiciousPeriod> Calculate(DateTime sunrise, DateTime sunset, DateTime birthTime, DayOfWeek dayOfWeek)
    {
        var result = new List<InauspiciousPeriod>();
        int vara = (int)dayOfWeek; // 0=Sunday, 1=Monday, etc.

        // Calculate daylight duration and portion size
        TimeSpan daylightDuration = sunset - sunrise;
        TimeSpan portionDuration = TimeSpan.FromTicks(daylightDuration.Ticks / 8);

        // Calculate Rahu Kalam
        var rahuKalam = CalculatePeriod("Rahu Kalam", "RK", sunrise, portionDuration, 
            RahuKalamPortions[vara], birthTime);
        result.Add(rahuKalam);

        // Calculate Yamagandam
        var yamagandam = CalculatePeriod("Yamagandam", "YG", sunrise, portionDuration, 
            YamagandamPortions[vara], birthTime);
        result.Add(yamagandam);

        // Calculate Gulikai Kalam
        var gulikai = CalculatePeriod("Gulikai Kalam", "GK", sunrise, portionDuration, 
            GulikaiPortions[vara], birthTime);
        result.Add(gulikai);

        return result;
    }

    private InauspiciousPeriod CalculatePeriod(
        string name, 
        string symbol, 
        DateTime sunrise, 
        TimeSpan portionDuration, 
        int portionIndex, 
        DateTime birthTime)
    {
        // Calculate start and end times
        // Portion index is 1-based, so (portionIndex - 1) gives the 0-based offset
        DateTime startTime = sunrise.Add(TimeSpan.FromTicks(portionDuration.Ticks * (portionIndex - 1)));
        DateTime endTime = sunrise.Add(TimeSpan.FromTicks(portionDuration.Ticks * portionIndex));

        // Check if birth time falls within this period
        bool isActive = birthTime >= startTime && birthTime < endTime;

        return new InauspiciousPeriod
        {
            Name = name,
            Symbol = symbol,
            StartTime = startTime,
            EndTime = endTime,
            IsActive = isActive
        };
    }
}
