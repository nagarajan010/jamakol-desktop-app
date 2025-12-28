using System.Configuration;
using System.Data;
using System.Windows;
using JamakolAstrology.Helpers;
using JamakolAstrology.Models;

namespace JamakolAstrology;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        // Load settings and apply language before any UI is created
        var settings = AppSettings.Load();
        LocalizationHelper.SetLanguage(settings.Language);

        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        string errorMsg = $"An unhandled exception occurred: {e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}";
        System.IO.File.WriteAllText("crash.log", errorMsg);
        MessageBox.Show(errorMsg, 
                        "Application Error", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error);
        e.Handled = true;
    }
}
