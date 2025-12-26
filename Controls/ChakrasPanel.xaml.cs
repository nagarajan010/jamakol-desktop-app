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
                Width = 400,  // Fixed size for proper scaling
                Height = 400,
                HideDegrees = true
            };
            
            // Wrap in Viewbox to scale while maintaining aspect ratio
            var viewbox = new Viewbox
            {
                Stretch = System.Windows.Media.Stretch.Uniform,
                Child = chart,
                Margin = new Thickness(5)
            };
            
            _chartControls.Add(chart);
            ChartsContainer.Children.Add(viewbox);
        }
    }

    public void UpdateChart(ChartData? chartData, double fontSize = 14)
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
            
            // Calculate on the fly using provided font size
            if (div == 1)
            {
                 control.UpdateChart(chartData, fontSize: fontSize, hideDegrees: true);
            }
            else
            {
                var divData = _divService.CalculateDivisionalChart(chartData, div);
                control.UpdateDivisionalChart(divData, chartData, fontSize: fontSize);
            }
        }
    }

    public void ClearChart()
    {
        UpdateChart(null);
    }
}
