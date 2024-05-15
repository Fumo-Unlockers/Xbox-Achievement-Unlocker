using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using XAU.Services;
using XAU.ViewModels.Pages;
using XAU.ViewModels.Windows;
using XAU.Views.Pages;
using XAU.Views.Windows;

namespace XAU;

public partial class App
{
    private static readonly IHost Host = Microsoft.Extensions.Hosting.Host
        .CreateDefaultBuilder()
        .ConfigureServices((_, services) =>
        {
            services.AddHostedService<ApplicationHostService>();

            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<ISnackbarService, SnackbarService>();
            services.AddSingleton<IContentDialogService, ContentDialogService>();

            services.AddSingleton<HomePage>();
            services.AddSingleton<HomeViewModel>();
            services.AddSingleton<SettingsPage>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<GamesPage>();
            services.AddSingleton<GamesViewModel>();
            services.AddSingleton<AchievementsPage>();
            services.AddSingleton<AchievementsViewModel>();
            services.AddSingleton<PlaceholderPage>();
            services.AddSingleton<StatsPage>();
            services.AddSingleton<StatsViewModel>();
            services.AddSingleton<MiscPage>();
            services.AddSingleton<MiscViewModel>();
            services.AddSingleton<InfoPage>();
            services.AddSingleton<InfoViewModel>();
            services.AddSingleton<DebugPage>();
            services.AddSingleton<DebugViewModel>();
        }).Build();

    private static T? GetService<T>() where T : class
    {
        return Host.Services.GetService(typeof(T)) as T;
    }

    private void OnStartup(object sender, StartupEventArgs e)
    {
        Host.Start();
        SetupExceptionHandling();
    }

    private async void OnExit(object sender, ExitEventArgs e)
    {
        await Host.StopAsync();
        Host.Dispose();
    }

    private void SetupExceptionHandling()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            ReportException((Exception)e.ExceptionObject);
        };

        DispatcherUnhandledException += (_, e) =>
        {
            ReportException(e.Exception);
            e.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            ReportException(e.Exception);
            e.SetObserved();
        };
    }
    private static void ReportException(Exception exception)
    {
        var mainWindowViewModel = GetService<MainWindowViewModel>();
        mainWindowViewModel?.ShowErrorDialog(exception);
    }
}
