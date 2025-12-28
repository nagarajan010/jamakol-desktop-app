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
                SignName = ZodiacUtils.SignNames[i + 1],
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
            SignName = "TOTAL",
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
            PlanetName = "Lagna", 
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
                PlanetName = name,
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
        ChartsScrollViewer.Visibility = System.Windows.Visibility.Visible;
        AVGrid.Visibility = System.Windows.Visibility.Collapsed;
        PindaGrid.Visibility = System.Windows.Visibility.Collapsed;
        
        MenuGridView.IsChecked = true;
        MenuTableView.IsChecked = false;
        MenuPindaView.IsChecked = false;
    }

    private void SwitchToTableView_Click(object sender, System.Windows.RoutedEventArgs e)
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
