using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology.Controls;

public partial class PlaceSearchDialog : Window
{
    private readonly GeoNamesService _geoService;
    public GeoLocation? SelectedLocation { get; private set; }

    public PlaceSearchDialog()
    {
        InitializeComponent();
        _geoService = new GeoNamesService();
        SearchInput.Focus();
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        PerformSearch();
    }

    private void SearchInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            PerformSearch();
        }
    }

    private async void PerformSearch()
    {
        string query = SearchInput.Text.Trim();
        if (string.IsNullOrEmpty(query)) return;

        StatusText.Text = "Searching...";
        SearchButton.IsEnabled = false;

        try
        {
            var results = await _geoService.SearchPlaceAsync(query);
            ResultsList.ItemsSource = results;
            if (results.Count == 0)
            {
                StatusText.Text = "No results found.";
            }
            else
            {
                StatusText.Text = $"Found {results.Count} results.";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "Error searching.";
            MessageBox.Show(ex.Message);
        }
        finally
        {
            SearchButton.IsEnabled = true;
        }
    }

    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Optional: Update status
    }

    private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        SelectAndClose();
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        SelectAndClose();
    }

    private void SelectAndClose()
    {
        if (ResultsList.SelectedItem is GeoLocation loc)
        {
            SelectedLocation = loc;
            DialogResult = true;
            Close();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
