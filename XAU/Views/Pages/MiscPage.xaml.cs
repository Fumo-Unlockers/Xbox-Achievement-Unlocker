using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui.Controls;
using XAU.ViewModels.Pages;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace XAU.Views.Pages
{
    public partial class MiscPage : INavigableView<MiscViewModel>
    {
        public MiscViewModel ViewModel { get; }
        private DispatcherTimer AutoUnlock_Timer;
        public MiscPage(MiscViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();

            // Initialise DispatcherTimer for auto unlock
            AutoUnlock_Timer = new DispatcherTimer();
            AutoUnlock_Timer.Tick += Timer_Tick;

            if(ViewModel.SelectedTabIndex > 0)//works once, I haven't managed to get the binding for the TabControl's SelectedIndex to work
            {
                MiscTabControl.SelectedIndex = ViewModel.SelectedTabIndex;
            }
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listbox = sender as ListBox;
            if (listbox.SelectedIndex == -1) return;
            ViewModel.DisplayGameInfo(listbox.SelectedIndex);
        }
        private void TitleIDSearch_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                //for some reason, the search text is not being updated when pressing enter
                ViewModel.TSearchText = TitleIdSearchBox.Text;
                ViewModel.SearchGame();
            }
        }

        private void GamertagSearch_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Fully qualify TextBox to avoid ambiguity
                var textBox = sender as System.Windows.Controls.TextBox;
                var bindingExpression = textBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty);
                bindingExpression?.UpdateSource();

                if (ViewModel.SearchGamertagCommand.CanExecute(null))
                {
                    ViewModel.SearchGamertagCommand.Execute(null);
                }
            }
        }
        private async void ExportToCsvButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ExportToCsvAsync();
        }

        #region AutoUnlocker
        private int TimeBuffSecond = 0;
        private int TimerTickRate = 1;
        private int AproxFinishTime = 0;

        private void UpdateUIListAchivement()
        {
            AutoUnlock_ListAchivement.Items.Clear();

            AutoUnlock_ListAchivement.Items.Add(new ListBoxItem { Content = "List unlock order :" });
            foreach (var item in ViewModel.AutoUnlock_FilteredAchievements)
            {
                AutoUnlock_ListAchivement.Items.Add(new ListBoxItem { Content = item.Name + " / Rarity: " + item.RarityPercentage});
            }
        }

        private void StartTimer(int Time)
        {
            if (Time > 0)
            {
                if (!ViewModel.UpdateRemains())
                {
                    AutoUnlock_TextStatut.Text = "No Remains";
                    AutoUnlock_TextStatut.Foreground = new SolidColorBrush(Colors.Orange);
                    AutoUnlock_TextRemain.Text = $"Remains {ViewModel.ReaminingAchivement}";
                    return;
                }

                ViewModel.IniListAchievements(AutoUnlock_ComboBoxOrder.SelectedIndex);

                UpdateUIListAchivement();

                //update progress bar
                AutoUnlock_ProgressBar.Maximum = ViewModel._achievementsViewModel.Achievements.Count;
                AutoUnlock_ProgressBar.Value = (AutoUnlock_ProgressBar.Maximum - ViewModel.ReaminingAchivement);

                AutoUnlock_NameGameSelect.Text = ViewModel.AutoUnlock_GameName;
                // Définir l'intervalle du timer
                AutoUnlock_Timer.Interval = TimeSpan.FromSeconds(TimerTickRate);
                AutoUnlock_Timer.Start();
                AutoUnlock_TextStatut.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                AutoUnlock_TextStatut.Text = "ERROR 0 MIN";
            }
        }

        private void ResetTimer()
        {
            if (AutoUnlock_Timer.IsEnabled)
                AutoUnlock_Timer.Stop();

            AutoUnlock_TextStatut.Text = "OFF";
            AutoUnlock_TextStatut.Foreground = new SolidColorBrush(Colors.Red);

            ViewModel.AutoUnlocker_IsEnable = false;
            AutoUnlock_EnableCheckBox.IsChecked = false;
            TimeBuffSecond = 0;
            AproxFinishTime = 0;

            ViewModel.AutoUnlock_FilteredAchievements.Clear();
        }

        private int UpdateTimeIntervalInSeconds(int selectedIndex)
        {
            int intervalInMinutes = 0;
            int randomSeconds = 0;
            Random random = new Random();

            switch (selectedIndex)
            {
                case 0: // 5-10 minute
                    randomSeconds = random.Next(5 * 60, 10 * 60);
                    break;
                case 1: // 10-20 minute
                    randomSeconds = random.Next(10 * 60, 20 * 60);
                    break;
                case 2: // 20-40 minutes
                    randomSeconds = random.Next(20 * 60, 40 * 60);
                    break;
                default:
                    AutoUnlock_Timer.Stop(); // Stop the timer if the interval is invalid 
                    AutoUnlock_TextStatut.Text = "ERROR: Invalid interval";
                    AutoUnlock_TextStatut.Foreground = new SolidColorBrush(Colors.Red);
                    return 0; 
            }

            return (randomSeconds + intervalInMinutes * 60);
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
             
            //check if is game
            if (AutoUnlock_NameGameSelect.Text != ViewModel.AutoUnlock_GameName || ViewModel.AutoUnlocker_IsEnable == false)
            {
                Debug.WriteLine("Auto Unlocker Stoped");
                ResetTimer();
                return;
            }

            TimeBuffSecond -= TimerTickRate;

            if (TimeBuffSecond <= 0)
            {
                // Prevent multiple simultaneous executions
                AutoUnlock_Timer.Stop();

                bool result = await ViewModel.AutoUnlocker();

                if (!result)
                {
                    AutoUnlock_TextStatut.Text = "UNLOCK FAILED";
                    AutoUnlock_TextStatut.Foreground = new SolidColorBrush(Colors.Red);

                    return;
                }

                UpdateUIListAchivement();

                if (ViewModel.ReaminingAchivement <= 0)
                {
                    System.Media.SystemSounds.Exclamation.Play();
                    await Task.Delay(1000);
                    System.Media.SystemSounds.Exclamation.Play();
                    await Task.Delay(1000);
                    System.Media.SystemSounds.Exclamation.Play();

                    ResetTimer();
                    ViewModel.AutoUnlocker_IsInfoGet = false;

                    AutoUnlock_TextStatut.Text = "COMPLETED";
                    AutoUnlock_TextStatut.Foreground = new SolidColorBrush(Colors.Green);
                    return;
                }

                AutoUnlock_ProgressBar.Value = AutoUnlock_ProgressBar.Maximum - ViewModel.ReaminingAchivement;

                TimeBuffSecond = UpdateTimeIntervalInSeconds(AutoUnlock_ComboBoxTime.SelectedIndex);

                AproxFinishTime = TimeBuffSecond / 60;

                AutoUnlock_Timer.Start();

                return;
            }
            else
            {
                // Calculer minute and secondes remain
                int minutes = TimeBuffSecond / 60;
                int seconds = TimeBuffSecond % 60;

                AutoUnlock_TextStatut.Text = $"Next in {minutes:D2}:{seconds:D2}";
                AutoUnlock_TextRemain.Text = $"Remains {ViewModel.ReaminingAchivement}" + $" / ~{ViewModel.ReaminingAchivement * AproxFinishTime} Minutes to finish";
            }

        }

        private void AutoUnlock_EnableCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (AutoUnlock_EnableCheckBox.IsChecked == true)
            {

                //check if game is selected
                if(!ViewModel.AutoUnlocker_IsInfoGet)
                {
                    System.Windows.Forms.MessageBox.Show("Please select a game by pressing the \"Auto Unlock\" button on the achievements page.", "Game Not Selected", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    AutoUnlock_EnableCheckBox.IsChecked = false;
                    return;
                }

                //start auto unlocker
                AutoUnlock_TextStatut.Text = "STARTING...";
                AutoUnlock_TextStatut.Foreground = new SolidColorBrush(Colors.Orange);
                ViewModel.AutoUnlocker_IsEnable = true;

                TimeBuffSecond = UpdateTimeIntervalInSeconds(AutoUnlock_ComboBoxTime.SelectedIndex);
                AproxFinishTime = TimeBuffSecond / 60;
                StartTimer(TimeBuffSecond);
            }
            else
                ResetTimer();
        }

        private void AutoUnlock_ComboBoxOrder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MiscTabControl.SelectedIndex != 4)
                return;

            ViewModel.IniListAchievements(AutoUnlock_ComboBoxOrder.SelectedIndex);

            UpdateUIListAchivement();
        }

        private void AutoUnlock_ComboBoxTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MiscTabControl.SelectedIndex != 4)
                return;

            TimeBuffSecond = UpdateTimeIntervalInSeconds(AutoUnlock_ComboBoxTime.SelectedIndex);
            AproxFinishTime = TimeBuffSecond / 60;

        }

        private void AutoUnlock_ListAchivement_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AutoUnlock_ListAchivement.SelectedIndex = -1;
        }

        #endregion


    }
}
