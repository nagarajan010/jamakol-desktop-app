using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JamakolAstrology.Models;

namespace JamakolAstrology.Controls;

/// <summary>
/// Jama Graha Positions Grid - displays special points and planet positions for Jamakol
/// </summary>
public partial class JamaGrahaGrid : UserControl
{
    public JamaGrahaGrid()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handle mouse wheel to bubble scroll up to parent ScrollViewer
    /// </summary>
    private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled) return;
        
        e.Handled = true;
        var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = UIElement.MouseWheelEvent,
            Source = sender
        };
        
        var parent = VisualTreeHelper.GetParent((DependencyObject)sender);
        while (parent != null && !(parent is ScrollViewer))
        {
            parent = VisualTreeHelper.GetParent(parent);
        }
        
        if (parent is ScrollViewer scrollViewer)
        {
            scrollViewer.RaiseEvent(eventArg);
        }
    }
    
    /// <summary>
    /// Clear selection when DataGrid loads
    /// </summary>
    private void DataGrid_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is DataGrid dataGrid)
        {
            dataGrid.UnselectAll();
            dataGrid.SelectedIndex = -1;
        }
    }

    /// <summary>
    /// Update the grid with Jama Graha data
    /// </summary>
    public void UpdateGrid(List<JamaGrahaPosition> jamaGrahas, List<SpecialPoint> specialPoints)
    {
        var allItems = new List<JamaGrahaGridItem>();
        
        // Add Special Points
        foreach (var point in specialPoints)
        {
            double deg = point.DegreeInSign;
            string dms = $"{(int)deg}°{(int)((deg % 1) * 60)}'{(int)(((deg % 1) * 60 % 1) * 60)}\"";
            string signShort = point.Sign.Length > 2 ? point.Sign.Substring(0, 2) : point.Sign;
            allItems.Add(new JamaGrahaGridItem 
            { 
                Name = point.Name, 
                DegreeDisplay = dms, 
                NakshatraName = point.NakshatraName, 
                Pada = point.Pada.ToString(),
                SignName = signShort 
            });
        }
        
        // Add Planets
        foreach (var graha in jamaGrahas)
        {
            string signShort = graha.SignName.Length > 2 ? graha.SignName.Substring(0, 2) : graha.SignName;
            allItems.Add(new JamaGrahaGridItem 
            { 
                Name = graha.Name, 
                DegreeDisplay = graha.DegreeDisplay, 
                NakshatraName = graha.NakshatraName, 
                Pada = graha.Pada.ToString(),
                SignName = signShort 
            });
        }

        // Define priority order
        var priorityNames = new[] { "Udayam", "Aarudam", "Kavippu" };
        var orderedItems = new List<JamaGrahaGridItem>();
        
        foreach (var name in priorityNames)
        {
             var item = allItems.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
             if (item != null)
             {
                 orderedItems.Add(item);
                 allItems.Remove(item);
             }
        }
        
        orderedItems.AddRange(allItems);
        DataGridControl.ItemsSource = orderedItems;
    }
}

/// <summary>
/// Display item for Jama Graha grid
/// </summary>
public class JamaGrahaGridItem
{
    public string Name { get; set; } = "";
    public string DegreeDisplay { get; set; } = "";
    public string NakshatraName { get; set; } = "";
    public string Pada { get; set; } = "";
    public string SignName { get; set; } = "";
}
