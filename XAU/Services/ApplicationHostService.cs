using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using XAU.Views.Pages;
using XAU.Views.Windows;

namespace XAU.Services;

public class ApplicationHostService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public ApplicationHostService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await HandleActivationAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    private async Task HandleActivationAsync()
    {
        await Task.CompletedTask;

        if (!Application.Current.Windows.OfType<MainWindow>().Any())
        {
            var navigationWindow = _serviceProvider.GetRequiredService<MainWindow>();
            navigationWindow.Loaded += OnNavigationWindowLoaded;
            navigationWindow.Show();
        }
    }

    private static void OnNavigationWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow navigationWindow)
        {
            return;
        }

        navigationWindow.NavigationView.Navigate(typeof(HomePage));
    }
}