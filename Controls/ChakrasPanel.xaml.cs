using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology.Controls;

public partial class ChakrasPanel : UserControl
{
    private readonly List<SouthIndianChart> _chartControls = new();
    private readonly int[] _divisions = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 16, 20, 24, 27, 30, 40, 45, 60 };
    private readonly DivisionalChartService _divService = new();

    public ChakrasPanel()
    {
        InitializeComponent();
        InitializeCharts();
    }

    private void InitializeCharts()
    {
        foreach (var div in _divisions)
        {
            var chart = new SouthIndianChart
            {
                Width = 280,
                Height = 280,
                Margin = new Thickness(1),
                HideDegrees = true // Hide degrees by default to save space in small view
            };
            
            // Set initial titles directly if possible, or wait for data update
            // Since SouthIndianChart title is dynamic based on data, we wait.
            
            _chartControls.Add(chart);
            ChartsContainer.Children.Add(chart);
        }
    }

    public void UpdateChart(ChartData? chartData)
    {
        if (chartData == null)
        {
            foreach (var chart in _chartControls)
            {
                chart.ClearChart();
            }
            return;
        }

        for (int i = 0; i < _divisions.Length; i++)
        {
            int div = _divisions[i];
            SouthIndianChart control = _chartControls[i];
            
            // Calculate on the fly
            if (div == 1)
            {
                 control.UpdateChart(chartData, fontSize: 10, hideDegrees: true);
                 // Force title update if needed, but UpdateChart handles D1 title
            }
            else
            {
                var divData = _divService.CalculateDivisionalChart(chartData, div);
                control.UpdateDivisionalChart(divData, chartData, fontSize: 10);
            }
        }
    }

    public void ClearChart()
    {
        UpdateChart(null);
    }
}
