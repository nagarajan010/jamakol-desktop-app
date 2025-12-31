using System.Windows.Controls;
using JamakolAstrology.Models;

namespace JamakolAstrology.Controls;

public partial class SideChartsPanel : UserControl
{
    public SideChartsPanel()
    {
        InitializeComponent();
    }

    public void UpdateCharts(ChartData chartData, double fontSize, bool hideDegrees, ChartStyle style = ChartStyle.SouthIndian)
    {
        bool isSouth = style == ChartStyle.SouthIndian;
        
        // Rasi Chart
        ChartControl.Visibility = isSouth ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        ChartControlNI.Visibility = !isSouth ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        if (isSouth)
        {
            ChartControl.HideDegrees = hideDegrees;
            ChartControl.UpdateChart(chartData, fontSize, hideDegrees);
        }
        else
        {
            ChartControlNI.HideDegrees = hideDegrees;
            ChartControlNI.UpdateChart(chartData, fontSize, hideDegrees);
        }

        // Update Navamsa (D-9)
        var navamsaChart = chartData.GetDivisionalChart(9);
        if (navamsaChart != null)
        {
            NavamsaChartControl.Visibility = isSouth ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            NavamsaChartControlNI.Visibility = !isSouth ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            
            if (isSouth)
            {
                NavamsaChartControl.HideDegrees = hideDegrees;
                NavamsaChartControl.UpdateDivisionalChart(navamsaChart, chartData, chartData.BirthData.Name, fontSize);
            }
            else
            {
                NavamsaChartControlNI.HideDegrees = hideDegrees;
                NavamsaChartControlNI.UpdateDivisionalChart(navamsaChart, chartData, chartData.BirthData.Name, fontSize);
            }
        }
    }

    // Proxy property for HideDegrees if needed separately
    public bool HideDegrees
    {
        set
        {
            ChartControl.HideDegrees = value;
            ChartControlNI.HideDegrees = value;
            NavamsaChartControl.HideDegrees = value;
            NavamsaChartControlNI.HideDegrees = value;
        }
    }
}
