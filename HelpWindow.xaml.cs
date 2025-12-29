using System.Windows;
using Markdig;

namespace JamakolAstrology
{
    public partial class HelpWindow : Window
    {
        public HelpWindow(string title, string content, string? basePath = null)
        {
            InitializeComponent();
            this.Title = title;
            HeaderTitle.Text = title;

            var pipeline = new Markdig.MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();
            string htmlBody = Markdig.Markdown.ToHtml(content, pipeline);

            // Add basic CSS styling
            string css = @"
                <style>
                    body { font-family: 'Segoe UI', sans-serif; padding: 20px; color: #333; line-height: 1.6; }
                    h1 { color: #cc0000; border-bottom: 2px solid #eee; padding-bottom: 10px; }
                    h2 { color: #cc0000; border-bottom: 1px solid #eee; margin-top: 25px; }
                    h3 { color: #990000; margin-top: 20px; }
                    pre { background: #f5f5f5; padding: 10px; border-radius: 5px; overflow-x: auto; font-family: Consolas, monospace; }
                    code { background: #f5f5f5; padding: 2px 4px; border-radius: 3px; font-family: Consolas, monospace; }
                    blockquote { border-left: 4px solid #cc0000; margin: 0; padding-left: 15px; color: #666; }
                    table { border-collapse: collapse; width: 100%; margin: 15px 0; }
                    th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                    th { background-color: #f9f9f9; }
                    img { max-width: 100%; height: auto; display: block; margin: 10px 0; border: 1px solid #ddd; border-radius: 4px; box-shadow: 0 2px 5px rgba(0,0,0,0.1); }
                    p { margin-bottom: 10px; }
                    ul, ol { margin-bottom: 10px; padding-left: 25px; }
                    li { margin-bottom: 5px; }
                    a { color: #333; text-decoration: none; cursor: default; }
                    a:hover { text-decoration: none; cursor: default; }
                </style>";

            string baseTag = string.IsNullOrEmpty(basePath) ? "" : $"<base href='file:///{basePath.Replace("\\", "/")}/' />";

            string fullHtml = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8' />
                    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
                    {baseTag}
                    {css}
                </head>
                <body>
                    {htmlBody}
                </body>
                </html>";

            ContentBrowser.NavigateToString(fullHtml);
            
            // Hack to suppress script errors if any
            System.Reflection.FieldInfo? fiComWebBrowser = typeof(System.Windows.Controls.WebBrowser).GetField("_axIWebBrowser2", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (fiComWebBrowser != null)
            {
                object? objComWebBrowser = fiComWebBrowser.GetValue(ContentBrowser);
                if (objComWebBrowser != null)
                {
                    objComWebBrowser.GetType().InvokeMember("Silent", System.Reflection.BindingFlags.SetProperty, null, objComWebBrowser, new object[] { true });
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ContentBrowser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            // Cancel any navigation that is not the initial load
            if (e.Uri != null)
            {
                e.Cancel = true;
            }
        }
    }
}
