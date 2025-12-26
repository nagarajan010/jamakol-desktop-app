using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;

namespace JamakolAstrology.Controls;

public partial class AshtakavargaChart : UserControl
{
    private Dictionary<int, TextBlock> _textBlocks;
    private Dictionary<int, Border> _borders;

    public AshtakavargaChart()
    {
        InitializeComponent();
        _textBlocks = new Dictionary<int, TextBlock>
        {
            {1, T1}, {2, T2}, {3, T3}, {4, T4}, {5, T5}, {6, T6},
            {7, T7}, {8, T8}, {9, T9}, {10, T10}, {11, T11}, {12, T12}
        };
        _borders = new Dictionary<int, Border>
        {
            {1, B1}, {2, B2}, {3, B3}, {4, B4}, {5, B5}, {6, B6},
            {7, B7}, {8, B8}, {9, B9}, {10, B10}, {11, B11}, {12, B12}
        };
    }

    public void Update(string title, int[] points, int highlightedSign = 0, int lagnaSign = 0)
    {
        TitleText.Text = title;

        for (int i = 0; i < 12; i++)
        {
            int sign = i + 1;
            int val = points[i];

            if (_textBlocks.TryGetValue(sign, out var tb))
            {
                tb.Text = val.ToString();
            }

            if (_borders.TryGetValue(sign, out var border))
            {
                // Highlight logic
                // If this is the "HighlightedSign" (planet position), use grey background (as per image description "Positions in D-1 are highlighted")
                // If it is Lagna, maybe bold text or green border?
                
                // Reset
                border.Background = Brushes.White;
                // border.BorderBrush = new SolidColorBrush(Color.FromRgb(136, 136, 136)); // #888

                if (sign == highlightedSign)
                {
                    border.Background = new SolidColorBrush(Color.FromRgb(220, 220, 220)); // Light Grey
                }
                
                if (sign == lagnaSign)
                {
                    // Maybe show "As" text or just highlight?
                    // The image shows "As" in the center of the Lagna chart.
                    // But for other charts, Lagna sign might be marked?
                    // I will just thicken the border for Lagna sign in all charts if needed, or leave it.
                    // The user image description says "Positions in D-1 are highlighted".
                    // For the Lagna chart, the "Position" is Lagna itself.
                }
            }
        }
    }
}
