using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;
using JamakolAstrology.Controls;

namespace JamakolAstrology;

/// <summary>
/// Dialog for saving a Jamakol chart with metadata
/// </summary>
public partial class SaveChartDialog : Window
{
    private readonly ChartStorageService _storageService;
    private readonly SavedJamakolChart _chart;
    private List<Guid> _selectedTagIds = new();
    private readonly GeoNamesService _geoService;

    public bool IsSaved { get; private set; }

    public SaveChartDialog(ChartStorageService storageService, SavedJamakolChart chart)
    {
        InitializeComponent();
        _storageService = storageService;
        _chart = chart;
        _geoService = new GeoNamesService();

        // Set title and hide Result based on chart type
        if (chart.ChartType == "BirthChart")
        {
            this.Title = "Save Birth Chart";
            ResultPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            this.Title = "Save Jamakol Chart";
        }

        LoadCategories();
        LoadTags();
        LoadChartData();
    }

    private void LoadChartData()
    {
        NameInput.Text = _chart.Name;
        PredictionInput.Text = _chart.Prediction;
        
        // Populate Date/Time/Location with BC date support
        YearInput.Text = _chart.Year.ToString();
        MonthInput.Text = _chart.Month.ToString();
        DayInput.Text = _chart.Day.ToString();
        TimeInput.Text = $"{_chart.Hour:D2}:{_chart.Minute:D2}:{_chart.Second:D2}";
        LatInput.Text = _chart.Latitude.ToString();
        LongInput.Text = _chart.Longitude.ToString();
        TzInput.Text = _chart.Timezone.ToString();
        
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
    
    private void SearchPlace_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PlaceSearchDialog();
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() == true && dialog.SelectedLocation != null)
        {
            var loc = dialog.SelectedLocation;
            LatInput.Text = loc.Lat;
            LongInput.Text = loc.Lng;
            
            if (loc.Timezone != null && !string.IsNullOrEmpty(loc.Timezone.TimeZoneId))
            {
                var offset = _geoService.GetTimezoneOffset(loc.Timezone.TimeZoneId);
                TzInput.Text = offset.ToString("0.##");
            }
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameInput.Text))
        {
            MessageBox.Show("Please enter a chart name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Parse Date and Time with BC date support
            int year = int.Parse(YearInput.Text);
            int month = int.Parse(MonthInput.Text);
            int day = int.Parse(DayInput.Text);
            var timeParts = TimeInput.Text.Split(':');
            if (timeParts.Length < 2) throw new Exception("Invalid time format. Use HH:mm:ss");
            
            int hour = int.Parse(timeParts[0]);
            int minute = int.Parse(timeParts[1]);
            int second = timeParts.Length > 2 ? int.Parse(timeParts[2]) : 0;

            // Update Chart with BC date support
            _chart.Name = NameInput.Text.Trim();
            _chart.Year = year;
            _chart.Month = month;
            _chart.Day = day;
            _chart.Hour = hour;
            _chart.Minute = minute;
            _chart.Second = second;
            _chart.Latitude = double.Parse(LatInput.Text);
            _chart.Longitude = double.Parse(LongInput.Text);
            _chart.Timezone = double.Parse(TzInput.Text);
            
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
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving: {ex.Message}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        IsSaved = false;
        DialogResult = false;
        Close();
    }
}
