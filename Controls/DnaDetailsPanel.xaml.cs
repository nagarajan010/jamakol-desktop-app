using System.Collections.Generic;
using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;
using JamakolAstrology.Resources;

namespace JamakolAstrology.Controls;

public partial class DnaDetailsPanel : UserControl
{
    private DnaAstrologyService _dnaService = new DnaAstrologyService();

    public DnaDetailsPanel()
    {
        InitializeComponent();
        LocalizeHeaders();
    }

    public void UpdateChart(ChartData? chart)
    {
        if (chart == null)
        {
            DnaGrid.ItemsSource = null;
            return;
        }

        var dnaDetails = _dnaService.CalculateDnaDetails(chart);
        
        // Post-process for UI-specific "Lagna" string if needed, 
        // though Service now returns "Lagna" in English or handled via ZodiacUtils?
        // Service returns "Lagna" string.
        // If Tamil, we might want to change "Lagna" to "லக்னம்".
        
        if (ZodiacUtils.IsTamil)
        {
            foreach (var item in dnaDetails)
            {
                if (item.IsLagna)
                {
                    item.Body = "லக்னம் (Asc)";
                }
                // Planets and others are already localized by Service using ZodiacUtils
            }
        }

        DnaGrid.ItemsSource = dnaDetails;
        
        // Calculate and set House Cusp details
        var houseDetails = _dnaService.CalculateHouseDetails(chart);
        if (DnaHouseGrid != null)
        {
            DnaHouseGrid.ItemsSource = houseDetails;
        }

        LocalizeHeaders();
    }

    private void LocalizeHeaders()
    {
        // x:Static handles most, but if we need dynamic updates without restart:
        // (Currently app requires restart for lang change, so x:Static is fine)
        // This method is kept for consistency with other panels or if we switch to dynamic loading later.
    }
}
