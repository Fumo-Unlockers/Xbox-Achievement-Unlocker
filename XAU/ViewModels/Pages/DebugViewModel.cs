using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Http;
using System.Text;
using Wpf.Ui.Controls;
using Newtonsoft.Json;
using Wpf.Ui.Extensions;

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
            var data = JObject.Parse(File.ReadAllText(DataPath));
            JArray SupportedGamesJ = (JArray)data["SupportedTitleIDs"];
            string Achievement = "";
            DateTime timestamp = DateTime.UtcNow;
            foreach (var game in SupportedGamesJ)
            {
                var requestbody = File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU", "Events", $"{game}.json"));

                var EventsData = (dynamic)(JObject)data[game.ToString()];
                foreach (var i in EventsData.Achievements)
                {
                    try
                    {
                        foreach (var j in i)
                        {
                            Achievement = i.Name.ToString();
                            foreach (var k in j)
                            {
                                var ReplacementData = k.Value;
                                switch (ReplacementData.ReplacementType.ToString())
                                {
                                    case "Replace":
                                        {
                                            requestbody = requestbody.Replace(ReplacementData.Target.ToString(), ReplacementData.Replacement.ToString());
                                            break;
                                        }
                                    case "RangeInt":
                                        {
                                            int min = ReplacementData.Min;
                                            int max = ReplacementData.Max;
                                            Random random = new Random();
                                            int randomint = random.Next(min, max);
                                            requestbody = requestbody.Replace(ReplacementData.Target.ToString(), randomint.ToString());
                                            break;
                                        }
                                    case "RangeFloat":
                                        {
                                            float min = ReplacementData.Min;
                                            float max = ReplacementData.Max;
                                            Random random = new Random();
                                            float randomfloat = (float)random.NextDouble() * (max - min) + min;
                                            requestbody = requestbody.Replace(ReplacementData.Target.ToString(), randomfloat.ToString());
                                            break;
                                        }
                                    case "StupidFuckingLDAPTimestamp":
                                        {
                                            long ldapTimestamp = DateTime.Now.ToFileTime();
                                            requestbody = requestbody.Replace(ReplacementData.Target.ToString(), ldapTimestamp.ToString());
                                            break;
                                        }
                                    default:
                                        {
                                            //_snackbarService.Show("Error: Bad Achievement Data", "Something went wrong with the achievement data", ControlAppearance.Danger,
                                            //                                                  new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                                            return;
                                        }

                                }

                            }
                        }
                        requestbody = requestbody.Replace("REPLACETIME", timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));
                        requestbody = requestbody.Replace("REPLACESEQ", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                        requestbody = requestbody.Replace("REPLACEXUID", HomeViewModel.XUIDOnly);
                        requestbody = JObject.Parse(requestbody).ToString(Formatting.None);
                        var bodyconverted = new StringContent(requestbody, Encoding.UTF8, "application/x-json-stream");
                        successfulAchievements++;
                    }
                    catch (Exception ex)
                    {
                        failedAchievements++;
                        errors.Add($"Game: {game}, Achievement:{Achievement}, Error: {ex.Message}");
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
