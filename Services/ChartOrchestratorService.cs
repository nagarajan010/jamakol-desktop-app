using System;
using System.Collections.Generic;
using System.Linq;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology.Services;

/// <summary>
/// Service that orchestrates the entire chart calculation pipeline.
/// Encapsulates the dependency between various calculators.
/// </summary>
public class ChartOrchestratorService
{
    private readonly ChartCalculator _chartCalculator;
    private readonly JamakolCalculator _jamakolCalculator;
    private readonly JamaGrahaCalculator _jamaGrahaCalculator;
    private readonly SpecialPointsCalculator _specialPointsCalculator;
    private readonly SupplementaryPlanetsCalculator _supplementaryPlanetsCalculator;
    private readonly InauspiciousPeriodsCalculator _inauspiciousPeriodsCalculator;
    private readonly PrasannaCalculator _prasannaCalculator;
    private readonly PanchangaCalculator _panchangaCalculator;
    private readonly VimshottariDashaCalculator _vimshottariDashaCalculator;
    private readonly AshtakavargaCalculator _ashtakavargaCalculator;
    private SunriseCalculator _sunriseCalculator;

    public ChartOrchestratorService()
    {
        _chartCalculator = new ChartCalculator();
        _jamakolCalculator = new JamakolCalculator();
        _jamaGrahaCalculator = new JamaGrahaCalculator();
        _specialPointsCalculator = new SpecialPointsCalculator();
        _supplementaryPlanetsCalculator = new SupplementaryPlanetsCalculator();
        _inauspiciousPeriodsCalculator = new InauspiciousPeriodsCalculator();
        _prasannaCalculator = new PrasannaCalculator();
        _panchangaCalculator = new PanchangaCalculator();
        _vimshottariDashaCalculator = new VimshottariDashaCalculator();
        _sunriseCalculator = new SunriseCalculator();
        _ashtakavargaCalculator = new AshtakavargaCalculator();
    }

    /// <summary>
    /// Update sunrise calculator mode if settings change
    /// </summary>
    public void UpdateSunriseMode(SunriseCalculationMode mode)
    {
        _sunriseCalculator?.Dispose();
        _sunriseCalculator = new SunriseCalculator(mode);
    }

    /// <summary>
    /// Calculates all chart details: Basic, Jamakol, Jama Grahas, Special Points, Panchanga
    /// </summary>
    public CompositeChartResult CalculateFullChart(BirthData birthData, AppSettings settings)
    {
        var result = new CompositeChartResult();

        // 1. Calculate Basic Chart (works for BC dates)
        result.ChartData = _chartCalculator.CalculateChart(birthData, settings.Ayanamsha);
        
        // 1.1 Calculate Ashtakavarga
        result.ChartData.Ashtakavarga = _ashtakavargaCalculator.Calculate(result.ChartData);

        // For BC dates, skip DateTime-dependent calculations (sunrise, Jama Graha, Panchanga, etc.)
        // These features are not meaningful for ancient/mythological dates
        if (birthData.IsBCDate)
        {
            // Return basic chart data for BC dates
            result.DayLord = GetApproximateDayLord(birthData);
            result.JamakolData = _jamakolCalculator.Calculate(result.ChartData);
            
            // Calculate Vimshottari Dasha for BC dates (doesn't need DateTime operations)
            var moon = result.ChartData.Planets.FirstOrDefault(p => p.Name == "Moon");
            if (moon != null)
            {
                // For BC dates, use a dummy DateTime for dasha start
                result.DashaResult = _vimshottariDashaCalculator.Calculate(
                    moon.Longitude, DateTime.Now, DateTime.Now, 6);
            }
            
            return result;
        }

        // 2. Determine Vedic Day and Sunrise/Sunset logic (AD dates only)
        var civilDate = birthData.BirthDateTime.Date;
        var civilSunrise = _sunriseCalculator.CalculateSunrise(
            civilDate, birthData.Latitude, birthData.Longitude, birthData.TimeZoneOffset);

        DateTime vedicDate;
        DateTime todaySunrise, todaySunset, tomorrowSunrise;

        if (birthData.BirthDateTime < civilSunrise)
        {
            vedicDate = civilDate.AddDays(-1);
            todaySunrise = _sunriseCalculator.CalculateSunrise(
                vedicDate, birthData.Latitude, birthData.Longitude, birthData.TimeZoneOffset);
            todaySunset = _sunriseCalculator.CalculateSunset(
                vedicDate, birthData.Latitude, birthData.Longitude, birthData.TimeZoneOffset);
            tomorrowSunrise = civilSunrise;
        }
        else
        {
            vedicDate = civilDate;
            todaySunrise = civilSunrise;
            todaySunset = _sunriseCalculator.CalculateSunset(
                vedicDate, birthData.Latitude, birthData.Longitude, birthData.TimeZoneOffset);
            tomorrowSunrise = _sunriseCalculator.CalculateSunrise(
                vedicDate.AddDays(1), birthData.Latitude, birthData.Longitude, birthData.TimeZoneOffset);
        }

        result.VedicDate = vedicDate;
        result.Sunrise = todaySunrise;
        result.Sunset = todaySunset;

        // 3. Day Lord Calculation
        // For Jama Graha: Day is based on 6 AM to 6 AM (not sunrise)
        // If time is before 6 AM, it's still the previous day for Jama Graha purposes
        DateTime jamaGrahaDate = birthData.BirthDateTime.Hour < 6 
            ? birthData.BirthDateTime.Date.AddDays(-1) 
            : birthData.BirthDateTime.Date;
        
        // Use Vedic Day for display/general astrology, but check if Jama Graha logic differs strictly (seems previously it used vedicDate logic implicitly via hour check)
        // The original logic separated "vedicDate" (Sunrise rule) vs "jamaGrahaDate" (6AM rule). preserving that.
        string dayLord = JamaGrahaCalculator.GetDayLord(jamaGrahaDate.DayOfWeek);
        result.DayLord = dayLord;

        // 4. Calculate Jamakol Data
        result.JamakolData = _jamakolCalculator.Calculate(result.ChartData);

        // 5. Calculate Jama Grahas
        result.JamaGrahas = _jamaGrahaCalculator.Calculate(birthData.BirthDateTime, dayLord);

        // 6. Calculate Special Points (Aarudam, Udayam, Kavippu)
        result.SpecialPoints = CalculateSpecialPoints(birthData, result.ChartData, dayLord, todaySunrise, todaySunset, tomorrowSunrise);

        // 7. Calculate Prasanna Details (using Jama Graha positions)
        // Derive PrasannaMode from UseFixedSignBoxes: 
        // - UseFixedSignBoxes = true → use RealDegree (actual sign position)
        // - UseFixedSignBoxes = false → use BoxSign (default box position)
        var prasannaMode = settings.UseFixedSignBoxes 
            ? PrasannaCalcMode.JamaGrahaRealDegree 
            : PrasannaCalcMode.JamaGrahaBoxSign;
        result.PrasannaDetails = _prasannaCalculator.Calculate(result.JamaGrahas, result.SpecialPoints, prasannaMode);

        // 8. Calculate Panchanga Details
        // Use the actual ayanamsa value from the chart calculation
        double siderealTime;
        using (var eph = new EphemerisService())
        {
            siderealTime = eph.GetSiderealTime(result.ChartData.JulianDay, birthData.Longitude);
        }

        // Calculate Local Noon for the *Vedic Date* to ensure consistency for Kala Hora
        // Kala Hora depends on "Local Noon" of the current Vedic Day.
        // If it's night (before sunrise), vedicDate is yesterday. We want noon of THAT day.
        DateTime localNoon = _sunriseCalculator.CalculateNoon(
            vedicDate, birthData.Latitude, birthData.Longitude, birthData.TimeZoneOffset);

        result.PanchangaDetails = _panchangaCalculator.Calculate(
            result.ChartData, 
            todaySunrise, 
            todaySunset, 
            localNoon,
            tomorrowSunrise,
            result.ChartData.AyanamsaValue, 
            siderealTime,
            result.VedicDate.DayOfWeek,
            // Pass a fresh EphemerisService instance (or reuse if scoped properly, here we create a temp one inside the calculate method or pass one)
            // Ideally should pass the one from 'using' block above?
            // The existing 'using' block on line 128 is closed at line 131.
            // We should create a new one or extend the scope.
            new EphemerisService() 
        );

        // 9. Calculate Inauspicious Periods (Rahu Kalam, Yamagandam, Gulikai Kalam)
        result.InauspiciousPeriods = _inauspiciousPeriodsCalculator.Calculate(
            todaySunrise, todaySunset, birthData.BirthDateTime, vedicDate.DayOfWeek);

        // 10. Calculate Vimshottari Dasha (sub-levels based on Moon's nakshatra)
        var moonAD = result.ChartData.Planets.FirstOrDefault(p => p.Name == "Moon");
        if (moonAD != null)
        {
            // Calculate starting from birth, find current running dasa for "Now" (or query date)
            DateTime calculationTargetDate = DateTime.Now; 
            // If it's Jamakol (query chart), we might care about the query time
            if (birthData.Location == "Query") calculationTargetDate = birthData.BirthDateTime;
            
            result.DashaResult = _vimshottariDashaCalculator.Calculate(
                moonAD.Longitude, birthData.BirthDateTime, calculationTargetDate, 6); // 6 levels
        }

        return result;
    }

    /// <summary>
    /// Calculate approximate day of week for BC dates using Julian Day
    /// </summary>
    private string GetApproximateDayLord(BirthData birthData)
    {
        // Julian Day can tell us the day of week
        // JD 0 was Monday, so (JD + 1) % 7 gives day of week (0=Sun, 1=Mon, etc)
        using var eph = new EphemerisService();
        var (year, month, day, hour) = birthData.GetUtcComponents();
        double jd = eph.GetJulianDay(year, month, day, hour);
        int dayOfWeekIndex = ((int)Math.Floor(jd + 1.5)) % 7;
        DayOfWeek dow = (DayOfWeek)dayOfWeekIndex;
        return JamaGrahaCalculator.GetDayLord(dow);
    }

    private List<SpecialPoint> CalculateSpecialPoints(
        BirthData birthData, 
        ChartData chartData,
        string dayLord,
        DateTime todaySunrise,
        DateTime todaySunset,
        DateTime tomorrowSunrise)
    {
        var specialPoints = new List<SpecialPoint>();

        // Calculate Aarudam (based on birth minute)
        var aarudam = _specialPointsCalculator.CalculateAarudam(birthData.BirthDateTime);
        specialPoints.Add(aarudam);

        // Get Sun's longitude - Udayam rises with Sun at sunrise and returns to Sun at sunset
        var sun = chartData.Planets.FirstOrDefault(p => p.Name == "Sun");
        double sunLongitude = sun?.Longitude ?? 0;
        
        // Udayam at sunrise = Sun's position
        // Udayam at sunset = Sun's position (completes 360° during day and returns to Sun)
        double sunriseUdayam = sunLongitude;
        double sunsetUdayam = sunLongitude;

        // Calculate Udayam (Udaya Lagna) using Sun's position
        var udayaLagnaCalc = new UdayaLagnaCalculator(
            todaySunrise, todaySunset, tomorrowSunrise, sunriseUdayam, sunsetUdayam);
        var udayam = udayaLagnaCalc.CalculateUdayam(birthData.BirthDateTime);
        specialPoints.Add(udayam);

        // Calculate Kavippu (Sun sign = Tamil month)
        int sunSign = sun?.Sign ?? 1; // 1-12
        var kavippu = _specialPointsCalculator.CalculateKavippu(
            sunSign, udayam.AbsoluteLongitude, aarudam.AbsoluteLongitude);
        specialPoints.Add(kavippu);

        // Calculate Supplementary Points (Rahu Kalam, Yemakandam use portions; Mandhi uses offset)
        // Need to determine if day or night for Mandhi calculation
        bool isDay = birthData.BirthDateTime >= todaySunrise && birthData.BirthDateTime < todaySunset;
        var supplementaryPoints = _supplementaryPlanetsCalculator.Calculate(
            sunLongitude, dayLord, isDay, birthData.BirthDateTime.Date.DayOfWeek);
        specialPoints.AddRange(supplementaryPoints);

        return specialPoints;
    }
}
