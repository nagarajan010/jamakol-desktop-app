using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JamakolAstrology.Helpers;

/// <summary>
/// Helper methods for UI theming and styling
/// </summary>
public static class UiThemeHelper
{
    /// <summary>
    /// Recursively sets the font size for a control and its children
    /// </summary>
    public static void SetFontSizeRecursive(FrameworkElement element, double size)
    {
        if (element is Control c) c.FontSize = size;
        if (element is TextBlock t) t.FontSize = size;
        
        if (element is Panel panel)
        {
            foreach (UIElement child in panel.Children)
            {
                if (child is FrameworkElement fe)
                    SetFontSizeRecursive(fe, size);
            }
        }
        else if (element is ItemsControl itemsControl)
        {
            // Handle TabControl, ListBox, etc.
            foreach (var item in itemsControl.Items)
            {
                if (item is FrameworkElement fe)
                    SetFontSizeRecursive(fe, size);
                else if (item is ContentControl cc && cc.Content is FrameworkElement contentFe)
                    SetFontSizeRecursive(contentFe, size);
            }
        }
        else if (element is ContentControl cc && cc.Content is FrameworkElement contentParams)
        {
             SetFontSizeRecursive(contentParams, size);
        }
        else if (element is Border border && border.Child is FrameworkElement child)
        {
             SetFontSizeRecursive(child, size);
        }
    }
}
