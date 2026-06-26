using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using Wpf.Ui.Common;
using Wpf.Ui.Contracts;
using Wpf.Ui.Controls;
using XAU.Services.HttpServer;

namespace XAU.ViewModels.Pages
{
    public partial class SettingsViewModel : ObservableObject, INavigationAware, IDisposable
    {
        private readonly ISnackbarService _snackbar;
        private readonly IContentDialogService _dialogs;
        private readonly TimeSpan _snackDur = TimeSpan.FromSeconds(2);

        public SettingsViewModel(ISnackbarService snackbar, IContentDialogService dialogs)
        {
            _snackbar = snackbar;
            _dialogs = dialogs;
        }

        private bool _isInitialized = false;

        [ObservableProperty]
        private string _appVersion = String.Empty;

        static string ProgramFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU");
        string SettingsFilePath = Path.Combine(ProgramFolderPath, "settings.json");
        [ObservableProperty] private string _settingsVersion;
        [ObservableProperty] private string _toolVersion;
        [ObservableProperty] private bool _unlockAllEnabled;
        [ObservableProperty] private bool _autoSpooferEnabled;
        [ObservableProperty] private bool _autoLaunchXboxAppEnabled;
        [ObservableProperty] private bool _launchHidden;
        [ObservableProperty] private bool _regionOverride;
        [ObservableProperty] private bool _useAcrylic;
        [ObservableProperty] private bool _privacyMode;

        [ObservableProperty] private bool _serverEnabled;
        [ObservableProperty] private string _serverPort = "1337";
        [ObservableProperty] private string _listeningAddress = "http://localhost:1337";

        private HttpServer? _httpServer;
        private bool _disposed;

        [RelayCommand]
        public void SaveSettings()
        {
            var settings = new XAUSettings
            {
                SettingsVersion = SettingsVersion,
                ToolVersion = ToolVersion,
                UnlockAllEnabled = UnlockAllEnabled,
                AutoSpooferEnabled = AutoSpooferEnabled,
                AutoLaunchXboxAppEnabled = AutoLaunchXboxAppEnabled,
                LaunchHidden = LaunchHidden,
                RegionOverride = RegionOverride,
                UseAcrylic = UseAcrylic,
                PrivacyMode = PrivacyMode
            };
            string settingsJson = JsonConvert.SerializeObject(settings);
            File.WriteAllText(SettingsFilePath, settingsJson);
            HomeViewModel.Settings = settings; // atualiza a referência
        }

        [RelayCommand]
        private void ToggleServer()
        {
            if (_httpServer == null)
            {
                var routes = Routes.GetRoutes(
                    getXauthToken: () => HomeViewModel.XAUTH,
                    getXboxRestAPI: () => new XboxRestAPI(HomeViewModel.XAUTH),
                    getXUIDOnly: () => HomeViewModel.XUIDOnly
                ); _httpServer = new HttpServer(ServerPort, routes);
            }

            if (ServerEnabled)
            {
                _httpServer.Start();
                UpdateListeningAddress();
            }
            else
            {
                _httpServer.Stop();
                ListeningAddress = $"http://localhost:{ServerPort}";
            }
            // TODO: salvar status (ligado/desligado) do servidor e número da porta
            //SaveSettings();
        }

        [RelayCommand]
        public void UpdateServerPort()
        {
            if (_httpServer != null)
            {
                _httpServer.UpdatePort(ServerPort);
                UpdateListeningAddress();
            }

            // TODO: salvar status (ligado/desligado) do servidor e número da porta
            //SaveSettings();
        }

        [RelayCommand]
        public void RestartAsAdmin()
        {
            if (_httpServer != null)
            {
                _httpServer.RestartAsAdmin();
            }
        }

        [RelayCommand]
        private void OpenListeningAddress()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(ListeningAddress))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = ListeningAddress,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to open address: {ex.Message}");
            }
        }

        [RelayCommand]
        private void CopyXauth() => CopyToClipboard(HomeViewModel.XAUTH, "xauth token");

        [RelayCommand]
        private void CopyEventToken() => CopyToClipboard(AchievementsViewModel.EventsToken, "event token");

        // Copia o token pro clipboard com feedback (ou avisa que não tem token ainda).
        private void CopyToClipboard(string? value, string label)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _snackbar.Show("Nothing to copy", $"No {label} yet — sign in first.",
                    ControlAppearance.Caution, new SymbolIcon(SymbolRegular.Warning24), _snackDur);
                return;
            }

            // O clipboard pode estar travado por outro processo (comum em VM/clipboard
            // managers). Usa o retry embutido do WPF; se mesmo assim não entrar, mostra o
            // token num diálogo pra copiar na mão (Ctrl+C).
            try
            {
                // WinForms tem o overload com retry embutido (15x a cada 120ms).
                System.Windows.Forms.Clipboard.SetDataObject(value, true, 15, 120);
                _snackbar.Show("Copied", $"The {label} is on your clipboard.",
                    ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackDur);
            }
            catch
            {
                ShowTokenManually(value, label);
            }
        }

        // Fallback: clipboard inacessível -> exibe o token selecionável pra copiar na mão.
        private async void ShowTokenManually(string value, string label)
        {
            var hint = new System.Windows.Controls.TextBlock
            {
                Text = "Couldn't reach the clipboard (it may be locked by a VM clipboard sync or a clipboard manager). Select the text below and press Ctrl+C.",
                TextWrapping = System.Windows.TextWrapping.Wrap,
                Opacity = 0.85,
                Margin = new System.Windows.Thickness(0, 0, 0, 10)
            };
            var box = new System.Windows.Controls.TextBox
            {
                Text = value,
                IsReadOnly = true,
                TextWrapping = System.Windows.TextWrapping.Wrap,
                MinWidth = 440,
                MaxHeight = 180,
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto
            };
            box.Loaded += (_, _) => { box.Focus(); box.SelectAll(); };

            var panel = new System.Windows.Controls.StackPanel();
            panel.Children.Add(hint);
            panel.Children.Add(box);

            await _dialogs.ShowSimpleDialogAsync(new SimpleContentDialogCreateOptions()
            {
                Title = $"Copy the {label}",
                Content = panel,
                CloseButtonText = "Close"
            });
        }

        public void OnNavigatedTo()
        {
            if (!_isInitialized)
            {
                InitializeViewModel();
            }
        }

        public void OnNavigatedFrom()
        { }

        private void InitializeViewModel()
        {
            LoadSettings();
            ToolVersion = $"XAU - {GetAssemblyVersion()}";
            SettingsVersion = "2";
            _isInitialized = true;

            if (_httpServer == null)
            {
                var routes = Routes.GetRoutes(
                    getXauthToken: () => HomeViewModel.XAUTH,
                    getXboxRestAPI: () => new XboxRestAPI(HomeViewModel.XAUTH),
                    getXUIDOnly: () => HomeViewModel.XUIDOnly
                );
                _httpServer = new HttpServer(ServerPort, routes);
            }
            ListeningAddress = $"http://localhost:{ServerPort}";
        }

        public void LoadSettings()
        {
            SettingsVersion = HomeViewModel.Settings.SettingsVersion;
            ToolVersion = HomeViewModel.Settings.ToolVersion;
            UnlockAllEnabled = HomeViewModel.Settings.UnlockAllEnabled;
            AutoSpooferEnabled = HomeViewModel.Settings.AutoSpooferEnabled;
            AutoLaunchXboxAppEnabled = HomeViewModel.Settings.AutoLaunchXboxAppEnabled;
            LaunchHidden = HomeViewModel.Settings.LaunchHidden;
            RegionOverride = HomeViewModel.Settings.RegionOverride;
            UseAcrylic = HomeViewModel.Settings.UseAcrylic;
            PrivacyMode = HomeViewModel.Settings.PrivacyMode;
        }

        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }

        private void UpdateListeningAddress()
        {
            if (_httpServer != null)
            {
                ListeningAddress = _httpServer.GetListeningAddress();
            }
        }
        partial void OnServerPortChanged(string value)
        {
            if (_httpServer != null)
            {
                _httpServer.UpdatePort(value);
                UpdateListeningAddress();
            }
            // TODO: salvar status (ligado/desligado) do servidor e número da porta
            //SaveSettings();
        }
        public void Dispose()
        {
            if (_disposed) return;

            if (_httpServer != null)
            {
                _httpServer.Dispose();
                _httpServer = null;
            }

            _disposed = true;
        }
    }
}
