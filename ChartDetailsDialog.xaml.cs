using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology;

/// <summary>
/// Dialog to display saved chart metadata (category, tags, result, prediction)
/// </summary>
public partial class ChartDetailsDialog : Window
{
    public ChartDetailsDialog(SavedJamakolChart chart, ChartStorageService storageService)
    {
        InitializeComponent();
        LoadChartDetails(chart, storageService);
    }

    private void LoadChartDetails(SavedJamakolChart chart, ChartStorageService storageService)
    {
        ChartNameText.Text = chart.Name;
        
        // Result with color coding
        ResultText.Text = chart.Result.ToString();
        ResultBorder.Background = chart.Result switch
        {
            ChartResult.Success => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28A745")),
            ChartResult.Failure => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dc3545")),
            _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA500"))
        };
        
        // Category
        if (chart.CategoryId.HasValue)
        {
            var category = storageService.GetCategory(chart.CategoryId.Value);
            CategoryText.Text = category?.Name ?? "(None)";
        }
        else
        {
            CategoryText.Text = "(None)";
        }
        
        // Tags
        TagsPanel.Children.Clear();
        if (chart.TagIds.Count > 0)
        {
            var allTags = storageService.GetAllTags();
            foreach (var tagId in chart.TagIds)
            {
                var tag = allTags.FirstOrDefault(t => t.Id == tagId);
                if (tag != null)
                {
                    var tagBorder = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(tag.Color)),
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(8, 3, 8, 3),
                        Margin = new Thickness(0, 0, 5, 5)
                    };
                    tagBorder.Child = new TextBlock
                    {
                        Text = tag.Name,
                        Foreground = Brushes.White,
                        FontSize = 11
                    };
                    TagsPanel.Children.Add(tagBorder);
                }
            }
        }
        else
        {
            TagsPanel.Children.Add(new TextBlock { Text = "(No tags)", Foreground = Brushes.Gray, FontSize = 12 });
        }
        
        // Prediction
        PredictionText.Text = string.IsNullOrWhiteSpace(chart.Prediction) ? "(No prediction notes)" : chart.Prediction;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
