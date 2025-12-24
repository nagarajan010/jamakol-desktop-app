using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology;

/// <summary>
/// Dialog for saving a Jamakol chart with metadata
/// </summary>
public partial class SaveChartDialog : Window
{
    private readonly ChartStorageService _storageService;
    private readonly SavedJamakolChart _chart;
    private List<Guid> _selectedTagIds = new();

    public bool IsSaved { get; private set; }

    public SaveChartDialog(ChartStorageService storageService, SavedJamakolChart chart)
    {
        InitializeComponent();
        _storageService = storageService;
        _chart = chart;

        LoadCategories();
        LoadTags();
        LoadChartData();
    }

    private void LoadChartData()
    {
        NameInput.Text = _chart.Name;
        PredictionInput.Text = _chart.Prediction;
        
        // Set result
        ResultCombo.SelectedIndex = (int)_chart.Result;
        
        // Set category
        if (_chart.CategoryId.HasValue)
        {
            foreach (ComboBoxItem item in CategoryCombo.Items)
            {
                if (item.Tag is Guid id && id == _chart.CategoryId.Value)
                {
                    CategoryCombo.SelectedItem = item;
                    break;
                }
            }
        }
        
        // Set tags
        _selectedTagIds = _chart.TagIds.ToList();
    }

    private void LoadCategories()
    {
        CategoryCombo.Items.Clear();
        CategoryCombo.Items.Add(new ComboBoxItem { Content = "(None)", Tag = null });
        
        foreach (var category in _storageService.GetAllCategories())
        {
            CategoryCombo.Items.Add(new ComboBoxItem 
            { 
                Content = category.Name, 
                Tag = category.Id 
            });
        }
        
        CategoryCombo.SelectedIndex = 0;
    }

    private void LoadTags()
    {
        TagsList.Items.Clear();
        foreach (var tag in _storageService.GetAllTags())
        {
            TagsList.Items.Add(tag);
        }
    }

    private void TagCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.Tag is Guid tagId)
        {
            if (checkBox.IsChecked == true)
            {
                if (!_selectedTagIds.Contains(tagId))
                    _selectedTagIds.Add(tagId);
            }
            else
            {
                _selectedTagIds.Remove(tagId);
            }
        }
    }

    private void ManageCategories_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ManageCategoriesDialog(_storageService);
        dialog.Owner = this;
        dialog.ShowDialog();
        LoadCategories();
    }

    private void ManageTags_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ManageTagsDialog(_storageService);
        dialog.Owner = this;
        dialog.ShowDialog();
        LoadTags();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameInput.Text))
        {
            MessageBox.Show("Please enter a chart name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _chart.Name = NameInput.Text.Trim();
        _chart.Prediction = PredictionInput.Text;
        _chart.Result = (ChartResult)ResultCombo.SelectedIndex;
        
        if (CategoryCombo.SelectedItem is ComboBoxItem catItem && catItem.Tag is Guid catId)
        {
            _chart.CategoryId = catId;
        }
        else
        {
            _chart.CategoryId = null;
        }
        
        _chart.TagIds = _selectedTagIds.ToList();
        
        _storageService.SaveChart(_chart);
        IsSaved = true;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        IsSaved = false;
        DialogResult = false;
        Close();
    }
}
