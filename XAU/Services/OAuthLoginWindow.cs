using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace XAU.Services;

// Login Microsoft interativo minimo. Mostra a pagina OAuth do login.live.com num
// WebView2 e resolve com o `code` quando redireciona para oauth20_desktop.
public class OAuthLoginWindow : Window
{
    private readonly WebView2 _web = new();
    private readonly TaskCompletionSource<string?> _tcs = new();
    private readonly string _authUrl;
    private readonly string _redirectUrl;
    private bool _done;

    public OAuthLoginWindow(string authUrl, string redirectUrl)
    {
        _authUrl = authUrl;
        _redirectUrl = redirectUrl;
        Title = "Sign in to Microsoft / Xbox";
        Width = 480;
        Height = 660;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Content = _web;
        Loaded += OnLoaded;
        Closed += (_, _) => _tcs.TrySetResult(null);
    }

    public Task<string?> GetCodeAsync()
    {
        Show();
        Activate();
        return _tcs.Task;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var udf = Path.Combine(Path.GetTempPath(), "XAU_WebView2");
            var env = await CoreWebView2Environment.CreateAsync(null, udf);
            await _web.EnsureCoreWebView2Async(env);
            _web.CoreWebView2.NavigationStarting += OnNavStarting;
            _web.CoreWebView2.Navigate(_authUrl);
        }
        catch (Exception ex)
        {
            WamAuthService.Diag("oauth window init: " + ex.Message);
            _tcs.TrySetResult(null);
            Close();
        }
    }

    private void OnNavStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (_done || !e.Uri.StartsWith(_redirectUrl, StringComparison.OrdinalIgnoreCase))
            return;

        _done = true;
        var code = ExtractQueryValue(e.Uri, "code");
        var error = ExtractQueryValue(e.Uri, "error");
        if (error != null)
            WamAuthService.Diag($"oauth redirect error: {error}");
        _tcs.TrySetResult(code);
        Dispatcher.BeginInvoke(new Action(Close));
    }

    private static string? ExtractQueryValue(string uri, string key)
    {
        var q = new Uri(uri).Query.TrimStart('?');
        foreach (var pair in q.Split('&'))
        {
            var i = pair.IndexOf('=');
            if (i <= 0) continue;
            if (pair.Substring(0, i) == key)
                return Uri.UnescapeDataString(pair.Substring(i + 1));
        }
        return null;
    }
}
