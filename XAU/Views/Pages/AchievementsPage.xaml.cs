using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Wpf.Ui.Controls;
using XAU.ViewModels.Pages;
using System.Text.RegularExpressions;
using System.Windows.Threading;

namespace XAU.Views.Pages
{
    /// <summary>
    /// Interaction logic for AchievementsPage.xaml
    /// </summary>
    public partial class AchievementsPage : INavigableView<AchievementsViewModel>
    {
        public AchievementsViewModel ViewModel { get; }
        private DispatcherTimer timer;
        private Action periodicAction;
        
        public AchievementsPage(AchievementsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
            
            // Initialiser le DispatcherTimer
            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
        }

        private void UnlockButton(object sender, RoutedEventArgs e)
        {
            ButtonBase SelectedAchievement = sender as ButtonBase;
            ViewModel.UnlockAchievement(Convert.ToInt32(SelectedAchievement.Tag));
        }

        private void FilterBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {


        }

        private async void SearchBox_OnKeyDownAsync(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                //for some reason, the search text is not being updated when pressing enter
                ViewModel.SearchText = SearchBox.Text;
                await ViewModel.SearchAndFilterAchievements();

            }
        }
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
{

}

private void checkBox_Checked(object sender, RoutedEventArgs e)
{

}
// AUTO ACHIVEMENT
private int TimeBuffSecond = 0;
private int TimerTickRate = 1;
private string TitleIDBoxBuff = "";
private void ResetAuto() 
{
    if (EtaAutoAchivement.Visibility == Visibility.Visible)
    {
        if (timer.IsEnabled)
            timer.Stop();


        ViewModel.AutoAchivementEnable = false;
        EnableAutoUnlocker.IsChecked = false;
        RemainLabel.Visibility = Visibility.Hidden;
        textBoxAuto.Visibility = Visibility.Hidden;
        UAtext.Visibility = Visibility.Hidden;
        comboBoxUA.Visibility = Visibility.Hidden;
        comboBoxUA.Items.Clear();
        MinuteText.Visibility = Visibility.Hidden;
        EtaAutoAchivement.Visibility = Visibility.Hidden;

        if(GridMain.RowDefinitions.Count == 3)
            GridMain.RowDefinitions.RemoveAt(2);
        GridMain.Height = 72.0f;
    }
}
private void StartTimer(int Time)
{
    if (Time > 0)
    {
        if(!ViewModel.UpdateRemains())
        {
            EtaAutoAchivement.Content = "0 Remains";
            EtaAutoAchivement.Foreground = new SolidColorBrush(Colors.Orange);
            RemainLabel.Content = $"Remains {ViewModel.ReaminingAchivement}";
            return;
        }

        // Définir l'intervalle du timer
        timer.Interval = TimeSpan.FromSeconds(TimerTickRate);
        TimeBuffSecond = Time * 60;
        TitleIDBoxBuff = TitleIDBox.Text;
        timer.Start();
        EtaAutoAchivement.Foreground = new SolidColorBrush(Colors.Green);
    }
    else
    {
        EtaAutoAchivement.Content = "ERROR 0 MIN";
    }
}

private void Timer_Tick(object sender, EventArgs e)
{
    //check if is game
    if (TitleIDBoxBuff != TitleIDBox.Text || ViewModel.AutoAchivementEnable == false)
    {
        Debug.WriteLine("Auto Achivement Stoped");
        ResetAuto();
        return;
    }

    TimeBuffSecond -= TimerTickRate;

    if (TimeBuffSecond <= 0)
    {
        
        if(!ViewModel.AutoUnlocker(comboBoxUA.SelectedIndex))
        {
            //stop unlocker
            timer.Stop();
        }

        //check Remains
        if (ViewModel.ReaminingAchivement == 0)
        {
            EtaAutoAchivement.Content = "0 Remains";
            RemainLabel.Content = $"Remains {ViewModel.ReaminingAchivement}";
            EtaAutoAchivement.Foreground = new SolidColorBrush(Colors.Orange);
            //its noun to alert
            System.Media.SystemSounds.Exclamation.Play();
            System.Threading.Thread.Sleep(1000);
            System.Media.SystemSounds.Exclamation.Play();
            System.Threading.Thread.Sleep(1000);
            System.Media.SystemSounds.Exclamation.Play();
            timer.Stop();
            return;
        }

        if (int.TryParse(textBoxAuto.Text, out int intervalInMinutes) && intervalInMinutes > 0)
        {
            //aletoire for not same time unlock 60/120 second
            Random random = new Random();
            int randomSeconds = random.Next(60, 120);
            TimeBuffSecond = randomSeconds + intervalInMinutes * 60; // Réinitialiser le temps restant
        }
        else
        {
            timer.Stop(); // Arrêter le timer si l'intervalle est invalide  
            EtaAutoAchivement.Content = "ERROR: Invalid interval";
            EtaAutoAchivement.Foreground = new SolidColorBrush(Colors.Red);
        }
    }
    else
    {
        // Calculer minute and secondes remain
        int minutes = TimeBuffSecond / 60;
        int seconds = TimeBuffSecond % 60;

        EtaAutoAchivement.Content = $"Next in {minutes:D2}:{seconds:D2}";
        EtaAutoAchivement.Content = $"Next in {minutes:D2}:{seconds:D2}";
        RemainLabel.Content = $"Remains {ViewModel.ReaminingAchivement}";
    }

}

private void checkBox_Checked_1(object sender, RoutedEventArgs e)
{
    // Cast l'objet sender en CheckBox
    System.Windows.Controls.CheckBox checkBox = sender as System.Windows.Controls.CheckBox;

    if (checkBox.IsChecked == true)
    {
        GridMain.Height = 165.0f;
        TitleIDBox.Height = 32.62f;
        TitleIDRefreshButton.Height = 30.62f;
        SearchBox.Height = 32.62f;
        SearchButton.Height = 30.62f;

        EtaAutoAchivement.Content = "STOPED";
        EtaAutoAchivement.Foreground = new SolidColorBrush(Colors.Red);

        RemainLabel.Visibility = Visibility.Visible;
        textBoxAuto.Visibility = Visibility.Visible;
        UAtext.Visibility = Visibility.Visible;
        comboBoxUA.Visibility = Visibility.Visible;
        MinuteText.Visibility = Visibility.Visible;
        EtaAutoAchivement.Visibility = Visibility.Visible;

        GridMain.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
        comboBoxUA.Items.Add(new ComboBoxItem() { Content = "Descending" });
        comboBoxUA.Items.Add(new ComboBoxItem() { Content = "Ascending" });
        comboBoxUA.SelectedIndex = 0;

        ViewModel.AutoAchivementEnable = true;

        int.TryParse(textBoxAuto.Text, out int intervalInMinutes);
        StartTimer(intervalInMinutes);
    }
    else
        ResetAuto();

    
}

private void textBoxAuto_TextChanged(object sender, TextChangedEventArgs e)
{
}

private void textBoxAuto_TextInput(object sender, TextCompositionEventArgs e)
{
    e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
}
    }
}
