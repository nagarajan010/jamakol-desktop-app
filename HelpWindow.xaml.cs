using System.Windows;

namespace JamakolAstrology
{
    public partial class HelpWindow : Window
    {
        public HelpWindow(string title, string content)
        {
            InitializeComponent();
            this.Title = title;
            HeaderTitle.Text = title;
            ContentText.Text = content;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
