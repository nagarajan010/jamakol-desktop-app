using System;
using System.Windows;
using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology;

public partial class ManageTagsDialog : Window
{
    private readonly ChartStorageService _storageService;
    private ChartTag? _selectedTag;

    public ManageTagsDialog(ChartStorageService storageService)
    {
        InitializeComponent();
        _storageService = storageService;
        LoadTags();
    }

    private void LoadTags()
    {
        TagsList.ItemsSource = _storageService.GetAllTags();
    }

    private void TagsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedTag = TagsList.SelectedItem as ChartTag;
        if (_selectedTag != null)
        {
            TagNameInput.Text = _selectedTag.Name;
            foreach (ComboBoxItem item in ColorPicker.Items)
            {
                if (item.Tag?.ToString() == _selectedTag.Color)
                {
                    ColorPicker.SelectedItem = item;
                    break;
                }
            }
            AddUpdateBtn.Content = "Update";
            DeleteBtn.IsEnabled = true;
        }
        else
        {
            ClearForm();
        }
    }

    private void ClearForm()
    {
        TagNameInput.Text = "";
        ColorPicker.SelectedIndex = 0;
        AddUpdateBtn.Content = "Add";
        DeleteBtn.IsEnabled = false;
        _selectedTag = null;
        TagsList.SelectedItem = null;
    }

    private void AddUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TagNameInput.Text))
        {
            MessageBox.Show("Please enter a tag name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var color = (ColorPicker.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "#28A745";
        
        if (_selectedTag != null)
        {
            _selectedTag.Name = TagNameInput.Text.Trim();
            _selectedTag.Color = color;
            _storageService.SaveTag(_selectedTag);
        }
        else
        {
            var tag = new ChartTag
            {
                Name = TagNameInput.Text.Trim(),
                Color = color
            };
            _storageService.SaveTag(tag);
        }

        LoadTags();
        ClearForm();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTag != null)
        {
            var result = MessageBox.Show($"Delete tag '{_selectedTag.Name}'?", "Confirm", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _storageService.DeleteTag(_selectedTag.Id);
                LoadTags();
                ClearForm();
            }
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
