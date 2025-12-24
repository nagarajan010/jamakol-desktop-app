using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JamakolAstrology.Controls;

/// <summary>
/// Panel to display details of a loaded saved chart
/// </summary>
public partial class SavedChartInfoPanel : UserControl
{
    public SavedChartInfoPanel()
    {
        InitializeComponent();
        this.Visibility = Visibility.Collapsed; // Default to hidden
    }

    /// <summary>
    /// Displays the chart information
    /// </summary>
    public void SetChartInfo(string result, string category, List<string> tags, string prediction)
    {
        // Result
        ResultText.Text = result;
        if (result.Equals("Success", StringComparison.OrdinalIgnoreCase))
        {
            ResultBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")); // Green
        }
        else if (result.Equals("Failure", StringComparison.OrdinalIgnoreCase))
        {
            ResultBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")); // Red
        }
        else
        {
            ResultBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")); // Orange/Pending
        }

        // Category
        CategoryText.Text = string.IsNullOrEmpty(category) ? "-" : category;

        // Tags
        TagsPanel.Children.Clear();
        if (tags != null && tags.Count > 0)
        {
            foreach (var tag in tags)
            {
                var border = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(6, 2, 6, 2),
                    Margin = new Thickness(0, 0, 5, 5)
                };
                var text = new TextBlock
                {
                    Text = tag,
                    FontSize = 10,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333"))
                };
                border.Child = text;
                TagsPanel.Children.Add(border);
            }
        }
        else
        {
            TagsPanel.Children.Add(new TextBlock { Text = "-", FontSize = 11, Foreground = Brushes.Gray });
        }

        // Prediction
        PredictionText.Text = string.IsNullOrEmpty(prediction) ? "No notes/prediction." : prediction;

        // Show self
        this.Visibility = Visibility.Visible;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Visibility = Visibility.Collapsed;
    }

    public void Hide()
    {
        this.Visibility = Visibility.Collapsed;
    }
}
