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
    private readonly PrasannaCalculator _prasannaCalculator;
    private readonly PanchangaCalculator _panchangaCalculator;
    private SunriseCalculator _sunriseCalculator;

    public ChartOrchestratorService()
    {
        _chartCalculator = new ChartCalculator();
        _jamakolCalculator = new JamakolCalculator();
        _jamaGrahaCalculator = new JamaGrahaCalculator();
        _specialPointsCalculator = new SpecialPointsCalculator();
        _prasannaCalculator = new PrasannaCalculator();
        _panchangaCalculator = new PanchangaCalculator();
        _sunriseCalculator = new SunriseCalculator();
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

        // 1. Calculate Basic Chart
        result.ChartData = _chartCalculator.CalculateChart(birthData, settings.Ayanamsha);

        // 2. Determine Vedic Day and Sunrise/Sunset logic
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
        result.SpecialPoints = CalculateSpecialPoints(birthData, result.ChartData, todaySunrise, todaySunset, tomorrowSunrise);

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
        result.PanchangaDetails = _panchangaCalculator.Calculate(result.ChartData, todaySunrise, todaySunset, result.ChartData.AyanamsaValue);

        return result;
    }

    private List<SpecialPoint> CalculateSpecialPoints(
        BirthData birthData, 
        ChartData chartData,
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

        return specialPoints;
    }
}
