using System;
using System.Windows;
using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology;

public partial class ManageCategoriesDialog : Window
{
    private readonly ChartStorageService _storageService;
    private ChartCategory? _selectedCategory;

    public ManageCategoriesDialog(ChartStorageService storageService)
    {
        InitializeComponent();
        _storageService = storageService;
        LoadCategories();
    }

    private void LoadCategories()
    {
        CategoriesList.ItemsSource = _storageService.GetAllCategories();
    }

    private void CategoriesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedCategory = CategoriesList.SelectedItem as ChartCategory;
        if (_selectedCategory != null)
        {
            CategoryNameInput.Text = _selectedCategory.Name;
            // Select matching color
            foreach (ComboBoxItem item in ColorPicker.Items)
            {
                if (item.Tag?.ToString() == _selectedCategory.Color)
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
        CategoryNameInput.Text = "";
        ColorPicker.SelectedIndex = 0;
        AddUpdateBtn.Content = "Add";
        DeleteBtn.IsEnabled = false;
        _selectedCategory = null;
        CategoriesList.SelectedItem = null;
    }

    private void AddUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CategoryNameInput.Text))
        {
            MessageBox.Show("Please enter a category name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var color = (ColorPicker.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "#4A90D9";
        
        if (_selectedCategory != null)
        {
            _selectedCategory.Name = CategoryNameInput.Text.Trim();
            _selectedCategory.Color = color;
            _storageService.SaveCategory(_selectedCategory);
        }
        else
        {
            var category = new ChartCategory
            {
                Name = CategoryNameInput.Text.Trim(),
                Color = color
            };
            _storageService.SaveCategory(category);
        }

        LoadCategories();
        ClearForm();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCategory != null)
        {
            var result = MessageBox.Show($"Delete category '{_selectedCategory.Name}'?", "Confirm", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _storageService.DeleteCategory(_selectedCategory.Id);
                LoadCategories();
                ClearForm();
            }
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
