using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology;

/// <summary>
/// View model for displaying saved charts in the grid
/// </summary>
public class SavedChartViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime QueryDateTime { get; set; }
    public ChartResult Result { get; set; }
    public string CategoryName { get; set; } = "";
    public string PredictionPreview { get; set; } = "";
}

/// <summary>
/// Dialog for browsing and managing saved charts
/// </summary>
public partial class SavedChartsDialog : Window
{
    private readonly ChartStorageService _storageService;
    private SavedJamakolChart? _selectedChart;

    public SavedJamakolChart? ChartToLoad { get; private set; }
    public bool ShouldLoadChart { get; private set; }

    private string? _chartTypeFilter;

    public SavedChartsDialog(ChartStorageService storageService, string? chartTypeFilter = null)
    {
        InitializeComponent();
        _storageService = storageService;
        _chartTypeFilter = chartTypeFilter;
        
        // Set title based on chart type
        if (chartTypeFilter == "BirthChart")
        {
            this.Title = "Saved Birth Charts";
            ResultColumn.Visibility = Visibility.Collapsed;
        }
        else if (chartTypeFilter == "Jamakol")
        {
            this.Title = "Saved Jamakol Charts";
        }
        
        LoadCharts();
    }

    private void LoadCharts()
    {
        var charts = _storageService.GetAllCharts();
        
        // Filter by chart type if specified
        if (!string.IsNullOrEmpty(_chartTypeFilter))
        {
            charts = charts.Where(c => c.ChartType == _chartTypeFilter).ToList();
        }
        
        var categories = _storageService.GetAllCategories();

        var viewModels = charts.OrderByDescending(c => c.CreatedAt).Select(c => new SavedChartViewModel
        {
            Id = c.Id,
            Name = c.Name,
            QueryDateTime = c.QueryDateTime,
            Result = c.Result,
            CategoryName = c.CategoryId.HasValue 
                ? categories.FirstOrDefault(cat => cat.Id == c.CategoryId.Value)?.Name ?? "" 
                : "",
            PredictionPreview = c.Prediction.Length > 50 
                ? c.Prediction.Substring(0, 50) + "..." 
                : c.Prediction
        }).ToList();

        ChartsGrid.ItemsSource = viewModels;
    }

    private void ChartsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ChartsGrid.SelectedItem is SavedChartViewModel vm)
        {
            _selectedChart = _storageService.GetChart(vm.Id);
            LoadBtn.IsEnabled = true;
            EditBtn.IsEnabled = true;
            DeleteBtn.IsEnabled = true;
        }
        else
        {
            _selectedChart = null;
            LoadBtn.IsEnabled = false;
            EditBtn.IsEnabled = false;
            DeleteBtn.IsEnabled = false;
        }
    }

    private void Load_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedChart != null)
        {
            ChartToLoad = _selectedChart;
            ShouldLoadChart = true;
            DialogResult = true;
            Close();
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedChart != null)
        {
            var dialog = new SaveChartDialog(_storageService, _selectedChart);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                LoadCharts(); // Refresh the list
            }
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedChart != null)
        {
            var result = MessageBox.Show($"Delete chart '{_selectedChart.Name}'?\n\nThis cannot be undone.", 
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                _storageService.DeleteChart(_selectedChart.Id);
                _selectedChart = null;
                LoadCharts();
            }
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
