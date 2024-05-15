using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Reflection;
using XAU.Services;
using XAU.ViewModels.Pages;
using XAU.ViewModels.Windows;
using XAU.Views.Pages;
using XAU.Views.Windows;


namespace XAU
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)); })
            .ConfigureServices((context, services) =>
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

        /// <summary>
        /// Gets registered service.
        /// </summary>
        /// <typeparam name="T">Type of the service to get.</typeparam>
        /// <returns>Instance of the service or <see langword="null"/>.</returns>
        public static T GetService<T>()
            where T : class
        {
            return _host.Services.GetService(typeof(T)) as T;
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private void OnStartup(object sender, StartupEventArgs e)
        {
            _host.Start();
            SetupExceptionHandling();
        }

        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();

            _host.Dispose();
        }

        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>
        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                ReportException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");
            DispatcherUnhandledException += (_, e) =>
            {
                ReportException(e.Exception, "Application.Current.DispatcherUnhandledException");
                e.Handled = true;
            };
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                ReportException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }
        private async void ReportException(Exception exception, string source)
        {
            var mainWindowViewModel = GetService<MainWindowViewModel>();
            if (mainWindowViewModel != null)
            {
                await mainWindowViewModel.ShowErrorDialog(exception);
            }
        }
    }
}
