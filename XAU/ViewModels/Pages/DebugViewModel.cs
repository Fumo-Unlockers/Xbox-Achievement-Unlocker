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
            if (!_isInitialized) InitializeViewModel();
        }

        public void OnNavigatedFrom()
        {
        }

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
            string eventsDirectory = Environment.ProcessPath;
            eventsDirectory = eventsDirectory.Substring(0, eventsDirectory.LastIndexOf("\\XAU\\"));
            eventsDirectory = Path.Combine(eventsDirectory, "Events");
            string DataPath = Path.Combine(eventsDirectory, "Data.json");
            var data = JObject.Parse(File.ReadAllText(DataPath));
            JArray SupportedGamesJ = (JArray)data["SupportedTitleIDs"];
            string Achievement = "";
            DateTime timestamp = DateTime.UtcNow;
            foreach (var game in SupportedGamesJ)
            {
                var GamePath = Path.Combine(eventsDirectory, game.ToString());
                var EventsData = (dynamic)JObject.Parse(File.ReadAllText(GamePath + $"\\{game}.json"));
                foreach (var achievement in EventsData.Achievements)
                {
                    try
                    {
                        foreach (var Event in achievement)
                        {
                            // it is extremely stupid that I need to do this but I dont care because it probably doesnt matter when doing it for actual unlocking
                            foreach (var Event2 in Event)
                            {
                                var eventstring = File.ReadAllText(GamePath + $"\\{Event2.Event}");
                                foreach (var replacement in Event2.Replacements)
                                {
                                    switch (replacement.ReplacementType.ToString())
                                    {
                                        case "Replace":
                                        {
                                            eventstring = eventstring.Replace(replacement.Target.ToString(),
                                                replacement.Replacement.ToString());
                                            break;
                                        }
                                        case "RangeInt":
                                        {
                                            int min = replacement.Min;
                                            int max = replacement.Max;
                                            Random random = new Random();
                                            int randomint = random.Next(min, max);
                                            eventstring = eventstring.Replace(replacement.Target.ToString(),
                                                randomint.ToString());
                                            break;
                                        }
                                        case "RangeFloat":
                                        {
                                            float min = replacement.Min;
                                            float max = replacement.Max;
                                            Random random = new Random();
                                            float randomfloat = (float)random.NextDouble() * (max - min) + min;
                                            eventstring = eventstring.Replace(replacement.Target.ToString(),
                                                randomfloat.ToString());
                                            break;
                                        }
                                        case "StupidFuckingLDAPTimestamp":
                                        {
                                            long ldapTimestamp = DateTime.Now.ToFileTime();
                                            eventstring = eventstring.Replace(replacement.Target.ToString(),
                                                ldapTimestamp.ToString());
                                            break;
                                        }
                                        default:
                                        {
                                            return;
                                        }
                                    }
                                }
                                eventstring = eventstring.Replace("REPLACETIME", timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));
                                eventstring = eventstring.Replace("REPLACESEQ", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                                eventstring = eventstring.Replace("REPLACEXUID", HomeViewModel.XUIDOnly);
                                eventstring = JObject.Parse(eventstring).ToString(Formatting.None);
                                var bodyconverted = new StringContent(eventstring, Encoding.UTF8, "application/x-json-stream");

                            }
                        }
                        successfulAchievements++;
                    }
                    catch (Exception e)
                    {
                        failedAchievements++;
                        errors.Add($"Game: {game}, Achievement:{Achievement}, Error: {e.Message}");
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
