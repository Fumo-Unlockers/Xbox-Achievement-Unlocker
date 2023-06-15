using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Memory;
using System.Threading;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace Xbox_Achievement_Unlocker
{
    public partial class MainWindow : Form
    {
        public Mem m = new Mem();
        Game_Spoofer SpoofForm = new Game_Spoofer();
        StatsEditor StatsForm = new StatsEditor();
        bool attached = false;
        string filter1;
        string filter2;
        string filter3;
        string filter4;
        string currentSystemLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;
        static HttpClientHandler handler = new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };
        HttpClient client = new HttpClient(handler);

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BGWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (!m.OpenProcess("XboxAppServices"))
                {
                    attached = false;
                    Thread.Sleep(1000);
                    continue;
                }
                attached = true;
                Thread.Sleep(1000);
                BGWorker.ReportProgress(0);
            }
        }

        async void MainWindow_Shown(object sender, EventArgs e)
        {
            BGWorker.RunWorkerAsync();
            LST_GameFilterType.SelectedIndex = 2;
            try
            {
                Updater Updater = new Updater();
                //update checker
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Host", "api.github.com");
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:108.0) Gecko/20100101 Firefox/108.0");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                client.DefaultRequestHeaders.Add("Accept",
                    "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
                var responseString =
                    await client.GetStringAsync("https://api.github.com/repos/ItsLogic/Xbox-Achievement-unlocker/releases");
                var Jsonresponse = (dynamic)(new JArray());
                Jsonresponse = (dynamic)JArray.Parse(responseString);
                if (Jsonresponse[0].tag_name.ToString() != "1.7")
                    Updater.Show();
            }
            catch
            {
                MessageBox.Show("Failed to check for updates");
            }
        }

        private void BGWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (attached)
            {
                LBL_Attached.Text = "Attached to: " + m.GetProcIdFromName("XboxAppServices").ToString();
                LBL_Attached.ForeColor = Color.Green;
                BTN_GrabXauth.Enabled = true;
            }
            if (!attached)
            {
                LBL_Attached.Text = "Not attached to Xbox App";
                LBL_Attached.ForeColor = Color.Red;
                BTN_GrabXauth.Enabled = false;
                BTN_SpoofGame.Enabled = false;
                BTN_SaveToFile.Enabled = false;
                BTN_StatsEditor.Enabled = false;
            }
        }

        private void BGWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BGWorker.RunWorkerAsync();
        }
        //define some xauth variables
        long XauthStartAddress;
        long XauthEndAddress;
        string XauthStartAddressHex;
        long XauthLength;
        public static string xauthtoken;
        private async void BTN_GrabXauth_Click(object sender, EventArgs e)
        {
            if (TXT_Xauth.Text.Length > 10)
            {
                try
                {
                    xauthtoken = TXT_Xauth.Text;
                    LoadInfo();
                    BTN_GrabXauth.Text = "Refresh Info";
                }
                catch
                {
                    MessageBox.Show("There is an issue with the xauth token provided.");
                }
            }
            else
            {
                //scan for first part of xauth "Authorization: XBL3.0 x="
                XauthStartAddress = (await (m.AoBScan("41 75 74 68 6F 72 69 7A 61 74 69 6F 6E 3A 20 58 42 4C 33 2E 30 20 78 3D", true, true))).FirstOrDefault();
                XauthStartAddressHex = (XauthStartAddress + 15).ToString("X");
                //scan for the end of xauth "Content - Length: "
                IEnumerable<long> XauthEndScanList = await m.AoBScan("0D 0A 43 6F 6E 74 65 6E 74 2D 4C 65 6E 67 74 68 3A 20", true, true);
                foreach (var endaddr in XauthEndScanList.ToArray())
                {
                    if (endaddr > XauthStartAddress)
                    {
                        //find the closest end to the start of xauth to use as length
                        XauthEndAddress = endaddr;
                        XauthLength = (XauthEndAddress - XauthStartAddress - 15);
                        break;
                    }
                }
                //read the bytes into string
                try
                {
                    xauthtoken = Encoding.ASCII.GetString(m.ReadBytes(XauthStartAddressHex, XauthLength));
                    TXT_Xauth.Text = xauthtoken;
                    LoadInfo();
                    BTN_GrabXauth.Text = "Refresh Info";
                }
                catch
                {
                    MessageBox.Show("Couldnt find xauth. Go click the FuckyWucky Fixer button until this doesnt happen.");
                }
            }
        }


        public static string xuid;
        public static string responseString;
        AchievementList ALForm = new AchievementList();
        public async void LoadAchievementList(object sender, EventArgs e)
        {
            PictureBox SelectedGame = (sender as PictureBox);

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "4");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
            try
            {
                client.DefaultRequestHeaders.Add("Authorization", xauthtoken);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "Couldnt find xauth. Go click the FuckyWucky Fixer button until this doesnt happen.");
                return;
            }
            client.DefaultRequestHeaders.Add("Host", "achievements.xboxlive.com");
            client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            try
            {
                responseString = await client.GetStringAsync("https://achievements.xboxlive.com/users/xuid(" + xuid + ")/achievements?titleId=" + SelectedGame.Name.ToString() + "&maxItems=1000");
                AchievementList ALForm = new AchievementList();
                ALForm.Show();
                ALForm.PopulateAchievementList(responseString);
            }
            catch (HttpRequestException ex)
            {

                if ((int)ex.StatusCode == 401)
                    MessageBox.Show("Couldnt find xauth. Go click the FuckyWucky Fixer button until this doesnt happen.", "401 Unauthorised");
                else
                    MessageBox.Show("something did a fucky wucky and I dont have a specific message for the error code", "fucky wucky");
            }


        }

        async void LoadInfo()
        {
            Panel_Recents.Controls.Clear();
            //required headers for a request to go through. (just taken from a legitimate request to profile.xboxlive.com)
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
            try
            {
                client.DefaultRequestHeaders.Add("Authorization", xauthtoken);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "Couldnt find xauth. Go click the FuckyWucky Fixer button until this doesnt happen.",
                    "Xauth not found");
                return;
            }
            client.DefaultRequestHeaders.Add("Host", "profile.xboxlive.com");
            client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            try
            {
                //query the users own profile using their xauth to find out profile information for future queries
                var responseString = await client.GetStringAsync("https://profile.xboxlive.com/users/me/profile/settings?settings=Gamertag,Gamerscore");
                var Jsonresponse = (dynamic)(new JObject());
                Jsonresponse = (dynamic)JObject.Parse(responseString);
                LBL_Gamertag.Text = "Gamertag: " + Jsonresponse.profileUsers[0].settings[0].value;
                LBL_Gamerscore.Text = "Gamerscore: " + Jsonresponse.profileUsers[0].settings[1].value;
                TXT_Xuid.Text = "XUID: " + Jsonresponse.profileUsers[0].id;
                xuid = Jsonresponse.profileUsers[0].id;
                BTN_SpoofGame.Enabled = true;
                BTN_SaveToFile.Enabled = true;
                BTN_StatsEditor.Enabled = true;
            }
            catch (HttpRequestException ex)
            {
                if ((int)ex.StatusCode == 401)
                    MessageBox.Show("Couldnt find xauth. Go click the FuckyWucky Fixer button until this doesnt happen.", "401 Unauthorised");
                else
                    MessageBox.Show("something did a fucky wucky and I dont have a specific message for the error code", "fucky wucky");
            }
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
            try
            {
                client.DefaultRequestHeaders.Add("Authorization", xauthtoken);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "Couldnt find xauth. Go click the FuckyWucky Fixer button until this doesnt happen.",
                    "Xauth not found");
                return;
            }
            client.DefaultRequestHeaders.Add("Host", "titlehub.xboxlive.com");
            client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            try
            {
                var responseString = await client.GetStringAsync("https://titlehub.xboxlive.com/users/xuid(" + xuid + ")/titles/titleHistory/decoration/Achievement,scid");
                var Jsonresponse = (dynamic)(new JObject());
                Jsonresponse = (dynamic)JObject.Parse(responseString);
                var count = 0;
                const int itemWidth = 150;
                const int rowHeight = 205;
                int itemCountPerRow = 6;
                int newline = 0;
                int itemWidthWithMargin = 0;
                for (int i = 0; i < Jsonresponse.titles.Count; i++)
                {
                    dynamic title = Jsonresponse.titles[i];
                    string devices = title.devices.ToString();
                    string titles = title.name.ToString() + " " + title.titleId.ToString();
                    if (!devices.Contains(filter1)
                        && !devices.Contains(filter2)
                        && !devices.Contains(filter3)
                        && !devices.Contains(filter4)
                        && titles.ToLower().Contains(TXT_GameFilterTitle.Text.ToLower()))
                    {
                        if (count % itemCountPerRow == 0 && count != 0)
                        {
                            newline = newline + rowHeight;
                            count = 0;
                        }
                        PictureBox GameImage = new PictureBox();
                        GameImage.Location = new Point(itemWidthWithMargin * count, newline);
                        GameImage.Size = new Size(itemWidth, 150);
                        if (count == 0)
                            itemWidthWithMargin = GameImage.Width + GameImage.Margin.Left + GameImage.Margin.Right;
                        GameImage.SizeMode = PictureBoxSizeMode.StretchImage;
                        GameImage.ImageLocation = Jsonresponse.titles[i].displayImage.ToString() + "?w=512&h=512&format=jpg";
                        GameImage.Name = Jsonresponse.titles[i].titleId.ToString();
                        GameImage.Cursor = Cursors.Hand;
                        GameImage.Click += new EventHandler(this.LoadAchievementList);
                        Panel_Recents.Controls.Add(GameImage);
                        //Create the dynamic TextBox.
                        TextBox textbox = new TextBox();
                        textbox.Location = new Point(itemWidthWithMargin * count, 150 + newline);
                        textbox.Size = new Size(itemWidth, 20);
                        textbox.BorderStyle = BorderStyle.None;
                        textbox.Margin = new Padding(textbox.Margin.Left + 2, 0, textbox.Margin.Right + 2, 0);
                        textbox.ReadOnly = true;
                        textbox.Name = "txt_" + (count + 1);
                        textbox.Text = Jsonresponse.titles[i].name;
                        Panel_Recents.Controls.Add(textbox);
                        TextBox titleidBox = new TextBox();
                        titleidBox.Location = new Point(itemWidthWithMargin * count, 170 + newline);
                        titleidBox.Size = new Size(itemWidth, 20);
                        titleidBox.BorderStyle = BorderStyle.None;
                        titleidBox.ReadOnly = true;
                        titleidBox.Name = "txt_" + Jsonresponse.titles[i].modernTitleId;
                        titleidBox.Text = "TitleID: " + Jsonresponse.titles[i].modernTitleId;
                        Panel_Recents.Controls.Add(titleidBox);

                        if (count == 0)
                            // calculate how many items will fit the current width
                            itemCountPerRow = Convert.ToInt32(Math.Floor(Convert.ToDouble(Panel_Recents.Width)
                                / (itemWidth + GameImage.Margin.Left + GameImage.Margin.Right)));

                        count = count + 1;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                if ((int)ex.StatusCode == 401)
                    MessageBox.Show("Couldnt find xauth. Go click the FuckyWucky Fixer button until this doesn't happen.", "401 Unauthorised");
                else
                    MessageBox.Show("something did a fucky wucky and I dont have a specific message for the error code", "fucky wucky");
            }
        }

        private void BTN_SpoofGame_Click(object sender, EventArgs e)
        {
            try { SpoofForm.Show(this); }
            catch { SpoofForm.Focus(); }
        }
        private void BTN_StatsEditor_Click(object sender, EventArgs e)
        {
            try { StatsForm.Show(this); }
            catch { StatsForm.Focus(); }
        }

        private void LST_GameFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (LST_GameFilterType.SelectedIndex == 0)
            {
                //All
                filter1 = "commands";
                filter2 = "commands";
                filter3 = "commands";
                filter4 = "commands";
            }
            else if (LST_GameFilterType.SelectedIndex == 1)
            {
                //Console Only
                filter1 = "Win32";
                filter2 = "commands";
                filter3 = "commands";
                filter4 = "commands";
            }
            else if (LST_GameFilterType.SelectedIndex == 2)
            {
                //New Gen
                filter1 = "Win32";
                filter2 = "Xbox360";
                filter3 = "commands";
                filter4 = "commands";
            }
            else if (LST_GameFilterType.SelectedIndex == 3)
            {
                //Win32
                filter1 = "Xbox360";
                filter2 = "XboxOne";
                filter3 = "XboxSeries";
                filter4 = "commands";
            }
        }

        async void BTN_SaveToFile_Click(object sender, EventArgs e)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
            client.DefaultRequestHeaders.Add("Authorization", xauthtoken);
            client.DefaultRequestHeaders.Add("Host", "profile.xboxlive.com");
            client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            try
            {
                //query the users own profile using their xauth to find out profile information for future queries
                var responseString = await client.GetStringAsync("https://profile.xboxlive.com/users/me/profile/settings?settings=Gamertag,Gamerscore");
                var Jsonresponse = (dynamic)(new JObject());
                Jsonresponse = (dynamic)JObject.Parse(responseString);
                LBL_Gamertag.Text = "Gamertag: " + Jsonresponse.profileUsers[0].settings[0].value;
                LBL_Gamerscore.Text = "Gamerscore: " + Jsonresponse.profileUsers[0].settings[1].value;
                TXT_Xuid.Text = "XUID: " + Jsonresponse.profileUsers[0].id;
                xuid = Jsonresponse.profileUsers[0].id;
                BTN_SpoofGame.Enabled = true;
                BTN_StatsEditor.Enabled = true;
            }
            catch (HttpRequestException ex)
            {
                if ((int)ex.StatusCode == 401)
                    MessageBox.Show("Couldnt find xauth. Go click the FuckyWucky Fixer button until this doesnt happen.", "401 Unauthorised");
                else
                    MessageBox.Show("something did a fucky wucky and I dont have a specific message for the error code", "fucky wucky");
            }
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
            try
            {
                client.DefaultRequestHeaders.Add("Authorization", xauthtoken);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "Couldnt find xauth. Go click the FuckyWucky Fixer button until this doesnt happen.",
                    "Xauth not found");
                return;
            }

            client.DefaultRequestHeaders.Add("Host", "titlehub.xboxlive.com");
            client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            try
            {
                var responseString = await client.GetStringAsync("https://titlehub.xboxlive.com/users/xuid(" + xuid + ")/titles/titleHistory/decoration/Achievement,scid");
                var Jsonresponse = (dynamic)(new JObject());
                Jsonresponse = (dynamic)JObject.Parse(responseString);
                using (StreamWriter writer = new StreamWriter("GameList.txt"))
                {
                    for (int i = 0; i < Jsonresponse.titles.Count; i++)
                    {
                        if (Jsonresponse.titles[i].modernTitleId.ToString().Length > 0)
                            writer.WriteLine(Jsonresponse.titles[i].modernTitleId.ToString() + "," + Jsonresponse.titles[i].name.ToString());
                    }
                }

            }
            catch (HttpRequestException ex)
            {
                if ((int)ex.StatusCode == 401)
                    MessageBox.Show("Couldnt find xauth. Go click the FuckyWucky Fixer button until this doesnt happen.", "401 Unauthorised");
                else
                    MessageBox.Show("something did a fucky wucky and I dont have a specific message for the error code", "fucky wucky");
            }
        }

        private void Skidbox_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", e.LinkText);
        }

        private void BTN_fixer_Click(object sender, EventArgs e)
        {
            //kill xbox app services
            Process[] processes = Process.GetProcessesByName("XboxAppServices");
            foreach (Process process in processes)
            {
                process.Kill();
            }
            //kill xboxapp.exe
            processes = Process.GetProcessesByName("XboxPcApp");
            foreach (Process process in processes)
            {
                process.Kill();
            }
            processes = Process.GetProcessesByName("GameBar");
            foreach (Process process in processes)
            {
                process.Kill();
            }
            Thread.Sleep(5000);
            //open the uwp xbox app
            Process p = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.FileName = startInfo.FileName = @"shell:appsFolder\Microsoft.GamingApp_8wekyb3d8bbwe!Microsoft.Xbox.App";
            p.StartInfo = startInfo;
            p.Start();
            p = new Process();
            startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.FileName = startInfo.FileName = @"shell:appsFolder\Microsoft.XboxGamingOverlay_8wekyb3d8bbwe!App";
            p.StartInfo = startInfo;
            p.Start();
            //
            MessageBox.Show("This might not work first time. Click this button and refresh until it does.", "Fixer");
        }

        private void TXT_Xauth_TextChanged(object sender, EventArgs e)
        {
            BTN_GrabXauth.Enabled = true;
        }
    }
}
