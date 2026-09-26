using System.Linq;
using System.Windows.Controls;
using JamakolAstrology.Services.Shadbala;

namespace JamakolAstrology.Controls;

public partial class ShadbalaPanel : UserControl
{
    private ShadbalaContext? _context;

    private static readonly string[] HoraNotes =
    {
        "Equal sixty-minute hours from sunrise. Reproduces the hora deva.guru prints on 98 of 100 reference charts.",
        "The day and the night each divided into twelve from real sunrise and sunset - the classical natal hora. Matches deva.guru on 34 of 100.",
        "Equal hours from 6 AM local mean time, the muhurta convention. Matches deva.guru on 25 of 100."
    };

    public ShadbalaPanel()
    {
        InitializeComponent();
        MethodNote.Text =
            "Sthana = uccha + saptavargiya + oja-yugma + kendradi + drekkana; Kala = natonnata + paksha + tribhaga + varsesadi. " +
            "Ayana follows Raman (his Example 33 reproduces 7 of 7), not deva.guru. The Sun's chesta is half its ayana and " +
            "the Moon's half its paksha, the classical double count. Yuddha bala is not included.";
    }

    public void UpdateChart(ShadbalaContext? context)
    {
        _context = context;
        Recompute();
    }

    private void HoraChanged(object sender, SelectionChangedEventArgs e) => Recompute();

    private void Recompute()
    {
        // Fires once during InitializeComponent, before the grids below exist.
        if (TotalsGrid == null || PartsGrid == null || HoraNote == null) return;

        int index = HoraPicker.SelectedIndex < 0 ? 0 : HoraPicker.SelectedIndex;
        HoraNote.Text = HoraNotes[index];

        if (_context == null)
        {
            TotalsGrid.ItemsSource = null;
            PartsGrid.ItemsSource = null;
            return;
        }

        var hora = index switch
        {
            1 => VarsesadiHora.Kalahora,
            2 => VarsesadiHora.MahakalahoraLmt,
            _ => VarsesadiHora.EqualFromSunrise
        };

        var results = _context.Compute(hora);
        // Strongest first by ratio, which is what ranks them; the parts in the usual graha order.
        TotalsGrid.ItemsSource = results.OrderByDescending(r => r.Ratio).ToList();
        PartsGrid.ItemsSource = results;
    }
}
