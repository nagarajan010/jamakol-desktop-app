using System.Configuration;
using System.Data;
using System.Windows;

namespace JamakolAstrology;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
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

