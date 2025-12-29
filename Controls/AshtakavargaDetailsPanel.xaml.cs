using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology.Controls;

public partial class AshtakavargaDetailsPanel : UserControl
{
    public AshtakavargaDetailsPanel()
    {
        InitializeComponent();
        
        // Apply font size from settings
        var settings = AppSettings.Load();
        if (settings != null)
        {
            AVGrid.FontSize = settings.TableFontSize;
            PindaGrid.FontSize = settings.TableFontSize;
            
            // Also update chart styles if needed, but currently charts use UserControl specific sizing.
            // If user meant "tables" as in DataGrids, this is sufficient.
        }
    }

    public void ShowGridView() => SetGridView();
    public void ShowTableView() => SetTableView();
    public void ShowPindaView() => SetPindaView();

    public void UpdateChart(ChartData? chart)
    {
        if (chart == null || chart.Ashtakavarga == null)
        {
            AVGrid.ItemsSource = null;
            return;
        }

        var data = chart.Ashtakavarga;
        
        // Helper to get localized title
        string GetTitle(string en, string ta) => ZodiacUtils.IsTamil ? ta : en;
        string GetPlanetTitle(Planet p) => ZodiacUtils.IsTamil ? ZodiacUtils.GetPlanetName(p) : p.ToString();

        // 1. Update Grid View Charts
        // SAV
        ChartSAV.Update(GetTitle("SAV", "சர்வா"), data.Sarvashtakavarga, 0, chart.AscendantSign); 
        
        // Lagna
        ChartLagna.Update(GetTitle("Asc", "லக்னம்"), data.LagnaAshtakavarga, chart.AscendantSign, chart.AscendantSign);

        // Planets
        UpdatePlanetChart(ChartSun, GetPlanetTitle(Planet.Sun), Planet.Sun, chart, data);
        UpdatePlanetChart(ChartMoon, GetPlanetTitle(Planet.Moon), Planet.Moon, chart, data);
        UpdatePlanetChart(ChartMars, GetPlanetTitle(Planet.Mars), Planet.Mars, chart, data);
        UpdatePlanetChart(ChartMercury, GetPlanetTitle(Planet.Mercury), Planet.Mercury, chart, data);
        UpdatePlanetChart(ChartJupiter, GetPlanetTitle(Planet.Jupiter), Planet.Jupiter, chart, data);
        UpdatePlanetChart(ChartVenus, GetPlanetTitle(Planet.Venus), Planet.Venus, chart, data);
        UpdatePlanetChart(ChartSaturn, GetPlanetTitle(Planet.Saturn), Planet.Saturn, chart, data);

        // 2. Update Table View (Existing Logic)
        var rows = new List<AVRow>();

        for (int i = 0; i < 12; i++)
        {
            // Index i corresponds to sign index 0-11 (Aries-Pisces)
            rows.Add(new AVRow
            {
                SignName = ZodiacUtils.GetSignName(i + 1),
                SunPoints = GetPoints(data.Bhinnashtakavarga, Planet.Sun, i),
                MoonPoints = GetPoints(data.Bhinnashtakavarga, Planet.Moon, i),
                MarsPoints = GetPoints(data.Bhinnashtakavarga, Planet.Mars, i),
                MercuryPoints = GetPoints(data.Bhinnashtakavarga, Planet.Mercury, i),
                JupiterPoints = GetPoints(data.Bhinnashtakavarga, Planet.Jupiter, i),
                VenusPoints = GetPoints(data.Bhinnashtakavarga, Planet.Venus, i),
                SaturnPoints = GetPoints(data.Bhinnashtakavarga, Planet.Saturn, i),
                LagnaPoints = data.LagnaAshtakavarga[i],
                SarvaPoints = data.Sarvashtakavarga[i],
                SarvaWithLagnaPoints = data.SarvashtakavargaWithLagna[i]
            });
        }

        var totalRow = new AVRow
        {
            SignName = ZodiacUtils.IsTamil ? "மொத்தம்" : "TOTAL",
            SunPoints = SumPoints(rows, r => r.SunPoints),
            MoonPoints = SumPoints(rows, r => r.MoonPoints),
            MarsPoints = SumPoints(rows, r => r.MarsPoints),
            MercuryPoints = SumPoints(rows, r => r.MercuryPoints),
            JupiterPoints = SumPoints(rows, r => r.JupiterPoints),
            VenusPoints = SumPoints(rows, r => r.VenusPoints),
            SaturnPoints = SumPoints(rows, r => r.SaturnPoints),
            LagnaPoints = SumPoints(rows, r => r.LagnaPoints),
            SarvaPoints = SumPoints(rows, r => r.SarvaPoints),
            SarvaWithLagnaPoints = SumPoints(rows, r => r.SarvaWithLagnaPoints)
        };
        rows.Add(totalRow);

        AVGrid.ItemsSource = rows;

        // 3. Update Pinda View
        var pindaRows = new List<PindaRow>();
        
        // Lagna
        pindaRows.Add(new PindaRow 
        { 
            PlanetName = ZodiacUtils.IsTamil ? "லக்னம்" : "Lagna", 
            RasiPinda = data.LagnaPinda.RasiPinda, 
            GrahaPinda = data.LagnaPinda.GrahaPinda, 
            SodhyaPinda = data.LagnaPinda.SodhyaPinda 
        });

        // Planets (Sun to Saturn)
        AddPindaRow(pindaRows, Planet.Sun, "Sun", data);
        AddPindaRow(pindaRows, Planet.Moon, "Moon", data);
        AddPindaRow(pindaRows, Planet.Mars, "Mars", data);
        AddPindaRow(pindaRows, Planet.Mercury, "Mercury", data);
        AddPindaRow(pindaRows, Planet.Jupiter, "Jupiter", data);
        AddPindaRow(pindaRows, Planet.Venus, "Venus", data);
        AddPindaRow(pindaRows, Planet.Saturn, "Saturn", data);

        PindaGrid.ItemsSource = pindaRows;
        
        LocalizeHeaders();
    }
    
    private void LocalizeHeaders()
    {
        if (AVGrid.Columns.Count < 11) return;
        
        bool isTa = ZodiacUtils.IsTamil;
        
        // AV Grid Headers
        AVGrid.Columns[0].Header = isTa ? "ராசி" : "Sign";
        AVGrid.Columns[1].Header = isTa ? "சூரி" : "Sun";
        AVGrid.Columns[2].Header = isTa ? "சந்" : "Mon";
        AVGrid.Columns[3].Header = isTa ? "செவ்" : "Mar";
        AVGrid.Columns[4].Header = isTa ? "புத" : "Mer";
        AVGrid.Columns[5].Header = isTa ? "குரு" : "Jup";
        AVGrid.Columns[6].Header = isTa ? "சுக்" : "Ven";
        AVGrid.Columns[7].Header = isTa ? "சனி" : "Sat";
        AVGrid.Columns[8].Header = isTa ? "லக்" : "Lag";
        AVGrid.Columns[9].Header = isTa ? "சர்வா" : "Sarva";
        AVGrid.Columns[10].Header = isTa ? "மொத்தம்(+ல)" : "Total(+Lg)";
        
        // Pinda Grid Headers
        if (PindaGrid.Columns.Count >= 4)
        {
            PindaGrid.Columns[0].Header = isTa ? "கிரகம்" : "Planet";
            PindaGrid.Columns[1].Header = isTa ? "சோத்ய பிண்டம்" : "Sodhya Pinda";
            PindaGrid.Columns[2].Header = isTa ? "ராசி பிண்டம்" : "Rasi Pinda";
            PindaGrid.Columns[3].Header = isTa ? "கிரக பிண்டம்" : "Graha Pinda";
        }
    }

    public void ClearChart()
    {
        ChartSAV.Clear();
        ChartLagna.Clear();
        ChartSun.Clear();
        ChartMoon.Clear();
        ChartMars.Clear();
        ChartMercury.Clear();
        ChartJupiter.Clear();
        ChartVenus.Clear();
        ChartSaturn.Clear();
        
        AVGrid.ItemsSource = null;
        PindaGrid.ItemsSource = null;
    }
    
    private void AddPindaRow(List<PindaRow> rows, Planet p, string name, AshtakavargaData data)
    {
        if (data.Pindas.TryGetValue(p, out var res))
        {
            rows.Add(new PindaRow
            {
                PlanetName = ZodiacUtils.IsTamil ? ZodiacUtils.GetPlanetName(p) : name,
                RasiPinda = res.RasiPinda,
                GrahaPinda = res.GrahaPinda,
                SodhyaPinda = res.SodhyaPinda
            });
        }
    }

    private void UpdatePlanetChart(AshtakavargaChart control, string title, Planet planet, ChartData chart, AshtakavargaData data)
    {
        if (data.Bhinnashtakavarga.TryGetValue(planet, out int[]? points) && points != null)
        {
            // Find planet sign
            var p = chart.Planets.FirstOrDefault(pl => pl.Planet == planet);
            int sign = p?.Sign ?? 0;
            control.Update(title, points, sign, chart.AscendantSign);
        }
    }

    private void SwitchToGridView_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        SetGridView();
    }

    private void SetGridView()
    {
        ChartsScrollViewer.Visibility = System.Windows.Visibility.Visible;
        AVGrid.Visibility = System.Windows.Visibility.Collapsed;
        PindaGrid.Visibility = System.Windows.Visibility.Collapsed;
        
        MenuGridView.IsChecked = true;
        MenuTableView.IsChecked = false;
        MenuPindaView.IsChecked = false;
    }

    private void SwitchToTableView_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        SetTableView();
    }

    private void SetTableView()
    {
        ChartsScrollViewer.Visibility = System.Windows.Visibility.Collapsed;
        AVGrid.Visibility = System.Windows.Visibility.Visible;
        PindaGrid.Visibility = System.Windows.Visibility.Collapsed;
        
        MenuGridView.IsChecked = false;
        MenuTableView.IsChecked = true;
        MenuPindaView.IsChecked = false;
    }

    private void SwitchToPindaView_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        SetPindaView();
    }

    private void SetPindaView()
    {
        ChartsScrollViewer.Visibility = System.Windows.Visibility.Collapsed;
        AVGrid.Visibility = System.Windows.Visibility.Collapsed;
        PindaGrid.Visibility = System.Windows.Visibility.Visible;
        
        MenuGridView.IsChecked = false;
        MenuTableView.IsChecked = false;
        MenuPindaView.IsChecked = true;
    }

    private int GetPoints(Dictionary<Planet, int[]> bhinna, Planet p, int index)
    {
        if (bhinna.TryGetValue(p, out int[]? points) && points != null)
        {
            return points[index];
        }
        return 0;
    }

    private int SumPoints(List<AVRow> rows, System.Func<AVRow, int> selector)
    {
        return rows.Sum(selector);
    }
    
    /// <summary>
    /// Bubble scroll events to parent ScrollViewer so main page can scroll
    /// </summary>
    private void ChartsScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        // Don't handle if this ScrollViewer can actually scroll
        if (ChartsScrollViewer.ScrollableHeight > 0 && ChartsScrollViewer.ComputedVerticalScrollBarVisibility == System.Windows.Visibility.Visible)
        {
            // Let the inner ScrollViewer handle it
            return;
        }
        
        // Bubble the event to parent
        e.Handled = true;
        var eventArgs = new System.Windows.Input.MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = System.Windows.UIElement.MouseWheelEvent,
            Source = sender
        };
        
        var parent = System.Windows.Media.VisualTreeHelper.GetParent(this);
        while (parent != null && !(parent is System.Windows.Controls.ScrollViewer))
        {
            parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
        }
        
        if (parent is System.Windows.Controls.ScrollViewer parentScrollViewer)
        {
            parentScrollViewer.RaiseEvent(eventArgs);
        }
    }
}

public class AVRow
{
    public string SignName { get; set; } = "";
    public int SunPoints { get; set; }
    public int MoonPoints { get; set; }
    public int MarsPoints { get; set; }
    public int MercuryPoints { get; set; }
    public int JupiterPoints { get; set; }
    public int VenusPoints { get; set; }
    public int SaturnPoints { get; set; }
    public int LagnaPoints { get; set; }
    public int SarvaPoints { get; set; }
    public int SarvaWithLagnaPoints { get; set; }
}

public class PindaRow
{
    public string PlanetName { get; set; } = "";
    public int SodhyaPinda { get; set; }
    public int RasiPinda { get; set; }
    public int GrahaPinda { get; set; }
}
