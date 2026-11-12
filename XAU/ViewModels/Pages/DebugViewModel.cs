using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Http;
using System.Text;
using Wpf.Ui.Controls;
using Newtonsoft.Json;
using Wpf.Ui.Contracts;

namespace XAU.ViewModels.Pages
{
    public partial class DebugViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        public void OnNavigatedTo()
        {
            if (!_isInitialized)
                InitializeViewModel();
        }

        public void OnNavigatedFrom() { }

        private void InitializeViewModel()
        {
            _isInitialized = true;
        }
        public DebugViewModel(ISnackbarService snackbarService, IContentDialogService contentDialogService)
        {
            _snackbarService = snackbarService;
            _contentDialogService = contentDialogService;
        }
        private readonly ISnackbarService _snackbarService;
        private TimeSpan _snackbarDuration = TimeSpan.FromSeconds(2);
        private readonly IContentDialogService _contentDialogService;

        [RelayCommand]
        public void TestEventReplacements()
        {
            int failedAchievements = 0;
            int successfulAchievements = 0;
            List<string> errors = new List<string>();
            string DataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU", "Events", "Data.json");
            var data = XAU.Services.EventCatalog.Load(Path.GetDirectoryName(DataPath)!);
            JArray SupportedGamesJ = (JArray)data["SupportedTitleIDs"];
            DateTime timestamp = DateTime.UtcNow;
            foreach (var game in SupportedGamesJ)
            {
                var title = data[game.ToString()] as JObject;
                var achievements = title?["Achievements"] as JObject;
                if (achievements == null || !achievements.HasValues)
                {
                    failedAchievements++;
                    errors.Add($"Game: {game}, Error: No achievement mappings.");
                    continue;
                }
                foreach (var achievement in achievements.Properties())
                {
                    try
                    {
                        string template = File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                            "XAU", "Events", $"{game}.json"));
                        XAU.Services.EventPayloadRenderer.Render(template, (JObject)achievement.Value,
                            HomeViewModel.XUIDOnly, timestamp);
                        successfulAchievements++;
                    }
                    catch (Exception ex)
                    {
                        failedAchievements++;
                        errors.Add($"Game: {game}, Achievement: {achievement.Name}, Error: {ex.Message}");
                    }
                }
            }

            // Write errors to a file
            File.WriteAllLines(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU", "Events", "Errors.log"), errors);

            // Show message box with the summary
            _contentDialogService.ShowSimpleDialogAsync(
                        new SimpleContentDialogCreateOptions()
                        {
                            Title = "Test Results",
                            Content = $"Successful Achievements: {successfulAchievements}\nUnsuccessful Achievements: {failedAchievements}",
                            CloseButtonText = "Close"
                        });
        }

    }
}
