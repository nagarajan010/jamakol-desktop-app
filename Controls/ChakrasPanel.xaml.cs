using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology.Controls;

public partial class ChakrasPanel : UserControl
{
    // Store as UserControl base class
    private readonly List<UserControl> _chartControls = new();
    private readonly int[] _divisions = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 16, 20, 24, 27, 30, 40, 45, 60 };
    private readonly DivisionalChartService _divService = new();
    private ChartStyle _currentStyle = ChartStyle.SouthIndian;
    private bool _initialized = false;

    public ChakrasPanel()
    {
        InitializeComponent();
        // Lazily initialize on first update or with default
        InitializeCharts(ChartStyle.SouthIndian);
    }

    private void InitializeCharts(ChartStyle style)
    {
        ChartsContainer.Children.Clear();
        _chartControls.Clear();
        _currentStyle = style;

        foreach (var div in _divisions)
        {
            UserControl chart;
            if (style == ChartStyle.NorthIndian)
            {
                chart = new NorthIndianChart
                {
                    Width = 400,
                    Height = 400,
                    HideDegrees = true
                };
            }
            else
            {
                chart = new SouthIndianChart
                {
                    Width = 400,
                    Height = 400,
                    HideDegrees = true
                };
            }
            
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
        _initialized = true;
    }

    public void UpdateChart(ChartData? chartData, double fontSize = 14, ChartStyle style = ChartStyle.SouthIndian)
    {
        // Re-initialize if style changed or not initialized
        if (!_initialized || _currentStyle != style)
        {
            InitializeCharts(style);
        }

        if (chartData == null)
        {
            foreach (dynamic chart in _chartControls)
            {
                chart.ClearChart();
            }
            return;
        }

        for (int i = 0; i < _divisions.Length; i++)
        {
            int div = _divisions[i];
            
            // Use dynamic to call methods present on both NorthIndianChart and SouthIndianChart
            dynamic control = _chartControls[i];
            
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
        foreach (dynamic chart in _chartControls)
        {
            chart.ClearChart();
        }
    }
}
