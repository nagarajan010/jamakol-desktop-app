using System.Collections.Generic;
using System.Windows.Controls;
using JamakolAstrology.Models;

namespace JamakolAstrology.Controls;

/// <summary>
/// Jamakol Data Grids Panel - container with tabs for Jama Graha and Planetary Positions grids
/// </summary>
public partial class JamakolDataGridsPanel : UserControl
{
    public JamakolDataGridsPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Update the Jama Graha grid
    /// </summary>
    public void UpdateJamaGrahaGrid(List<JamaGrahaPosition> jamaGrahas, List<SpecialPoint> specialPoints)
    {
        JamaGrahaGridControl.UpdateGrid(jamaGrahas, specialPoints);
    }

    /// <summary>
    /// Update the Planetary Positions grid
    /// </summary>
    public void UpdatePlanetGrid(JamakolData jamakolData)
    {
        PlanetGridControl.UpdateGrid(jamakolData);
    }
}
