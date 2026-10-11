using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wpf.Ui.Contracts;
using Wpf.Ui.Services;
using XAU.Services;
using XAU.Services.LiveCapture;
using XAU.ViewModels.Pages;
using XAU.ViewModels.Windows;
using XAU.Views.Pages;
using XAU.Views.Windows;
using LocalHttpServer = XAU.Services.HttpServer.HttpServer;

namespace XAU;

public partial class App
{
    private LocalHttpServer? _diagnosticHttpServer;

    private static readonly IHost Host = Microsoft.Extensions.Hosting.Host
        .CreateDefaultBuilder()
        .ConfigureServices((_, services) =>
        {
            services.AddHostedService<ApplicationHostService>();
            services.AddSingleton<IAchievementSnapshotSource, XboxAchievementSnapshotSource>();
            services.AddSingleton<IGameProcessResolver, WindowsGameProcessResolver>();
            services.AddSingleton<ILiveCaptureProvider, XboxAchievementCaptureProvider>();
            services.AddSingleton<ILiveCaptureProvider, GameProcessCaptureProvider>();
            services.AddSingleton<LiveCaptureCoordinator>();
            services.AddHostedService(provider => provider.GetRequiredService<LiveCaptureCoordinator>());

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
            services.AddSingleton<AchievementResearchLabPage>();
            services.AddSingleton<AchievementResearchLabViewModel>();
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
        StartupDiagnostics.Begin();
        SetupExceptionHandling();

        try
        {
            Host.Start();
            StartupDiagnostics.Write("Host started and main window shown.");
            StartDiagnosticLocalApi(e.Args);
        }
        catch (Exception exception)
        {
            StartupDiagnostics.WriteException("Host startup failed", exception);
            Shutdown(-1);
        }
    }

    private async void OnExit(object sender, ExitEventArgs e)
    {
        StartupDiagnostics.Write($"Application exiting with code {e.ApplicationExitCode}.");
        _diagnosticHttpServer?.Dispose();
        await Host.StopAsync();
        Host.Dispose();
    }

    private void SetupExceptionHandling()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception)
            {
                StartupDiagnostics.WriteException("Unhandled application exception", exception);
                ReportException(exception);
            }
        };

        DispatcherUnhandledException += (_, e) =>
        {
            StartupDiagnostics.WriteException("Unhandled dispatcher exception", e.Exception);
            ReportException(e.Exception);
            e.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            StartupDiagnostics.WriteException("Unobserved task exception", e.Exception);
            ReportException(e.Exception);
            e.SetObserved();
        };
    }
    private static void ReportException(Exception exception)
    {
        var mainWindowViewModel = GetService<MainWindowViewModel>();
        mainWindowViewModel?.ShowErrorDialog(exception);
    }

    private void StartDiagnosticLocalApi(string[] args)
    {
        if (!args.Contains("--diagnostic-local-api", StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        var port = args
            .FirstOrDefault(arg => arg.StartsWith("--api-port=", StringComparison.OrdinalIgnoreCase))?
            .Split('=', 2)[1] ?? "1337";

        if (!int.TryParse(port, out var portNumber) || portNumber is < 1 or > 65535)
        {
            StartupDiagnostics.Write($"Diagnostic local API not started: invalid port '{port}'.");
            return;
        }

        var routes = Routes.GetDiagnosticRoutes(
            getXboxRestAPI: () => new XboxRestAPI(HomeViewModel.XAUTH),
            getXUIDOnly: () => HomeViewModel.XUIDOnly);

        _diagnosticHttpServer = new LocalHttpServer(
            port,
            routes,
            localOnly: true,
            requestFilter: Routes.IsDiagnosticRequestAllowed);
        _diagnosticHttpServer.Start();
        StartupDiagnostics.Write(_diagnosticHttpServer.IsRunning
            ? $"Diagnostic local API listening on http://localhost:{port}."
            : $"Diagnostic local API failed to listen on http://localhost:{port}: "
              + (_diagnosticHttpServer.LastStartError ?? "no listener error was reported."));
    }
}
