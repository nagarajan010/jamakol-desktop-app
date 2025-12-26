using System.Windows.Controls;
using JamakolAstrology.Models;

namespace JamakolAstrology.Controls;

public partial class SideChartsPanel : UserControl
{
    public SideChartsPanel()
    {
        InitializeComponent();
    }

    public void UpdateCharts(ChartData chartData, double fontSize, bool hideDegrees)
    {
        ChartControl.HideDegrees = hideDegrees;
        ChartControl.UpdateChart(chartData, fontSize, hideDegrees);

        // Update Navamsa (D-9)
        var navamsaChart = chartData.GetDivisionalChart(9);
        if (navamsaChart != null)
        {
            NavamsaChartControl.HideDegrees = hideDegrees;
            NavamsaChartControl.UpdateDivisionalChart(navamsaChart, chartData, chartData.BirthData.Name, fontSize);
        }
    }

    // Proxy property for HideDegrees if needed separately
    public bool HideDegrees
    {
        set
        {
            ChartControl.HideDegrees = value;
            NavamsaChartControl.HideDegrees = value;
        }
    }
}
