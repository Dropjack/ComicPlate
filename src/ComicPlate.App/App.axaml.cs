using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ComicPlate.App.Services;

namespace ComicPlate.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(GetStartupPath(desktop.Args));
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static string? GetStartupPath(string[]? args)
    {
        return args?
            .FirstOrDefault(arg => !string.IsNullOrWhiteSpace(arg) && !arg.StartsWith("-", StringComparison.Ordinal));
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            CrashReportWriter.Write(exception, "AppDomain");
        }
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        CrashReportWriter.Write(e.Exception, "Dispatcher");
        e.Handled = false;
    }
}
