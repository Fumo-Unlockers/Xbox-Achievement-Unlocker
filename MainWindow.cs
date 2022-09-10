using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
        bool attached = false;
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

        private void MainWindow_Shown(object sender, EventArgs e)
        {
            BGWorker.RunWorkerAsync();
        }

        private void BGWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (attached)
            {
                LBL_Attached.Text = "Attached to: XboxAppServices.exe (" + m.GetProcIdFromName("XboxAppServices").ToString() + ")";
                LBL_Attached.ForeColor = Color.Green;
                BTN_GrabXauth.Enabled = true;
            }
            if (!attached)
            {
                LBL_Attached.Text = "Not attached to Xbox App";
                LBL_Attached.ForeColor = Color.Red;
                BTN_GrabXauth.Enabled = false;
                BTN_SpoofGame.Enabled = false;
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
                TXT_Xauth.Text = "xauth: " + xauthtoken;
            }
            catch
            {
                MessageBox.Show("something with the xauth scan did a fucky wucky");
            }

            LoadInfo();
        }
        static HttpClientHandler handler = new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };
        HttpClient client = new HttpClient(handler);
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
            client.DefaultRequestHeaders.Add("accept-language", "en-GB");
            client.DefaultRequestHeaders.Add("Authorization", xauthtoken);
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
                    MessageBox.Show("Xauth is not correct. Restart this tool and kill xbox app services in task manager before reopening the xbox app", "401 Unauthorised");
                else
                    MessageBox.Show("something did a fucky wucky and I dont have a specific message for the error code", "fucky wucky");
            }


        }

        async void LoadInfo()
        {
            //required headers for a request to go through. (just taken from a legitimate request to profile.xboxlive.com)
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("accept-language", "en-GB");
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
            }
            catch (HttpRequestException ex)
            {
                if ((int)ex.StatusCode == 401)
                    MessageBox.Show("Xauth is not correct. Restart this tool and kill xbox app services in task manager before reopening the xbox app", "401 Unauthorised");
                else
                    MessageBox.Show("something did a fucky wucky and I dont have a specific message for the error code", "fucky wucky");
            }
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("accept-language", "en-GB");
            client.DefaultRequestHeaders.Add("Authorization", xauthtoken);
            client.DefaultRequestHeaders.Add("Host", "titlehub.xboxlive.com");
            client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            try
            {
                var responseString = await client.GetStringAsync("https://titlehub.xboxlive.com/users/xuid(" + xuid + ")/titles/titleHistory/decoration/Achievement,scid");
                var Jsonresponse = (dynamic)(new JObject());
                Jsonresponse = (dynamic)JObject.Parse(responseString);
                var count = 0;
                var newline = 0;
                for (int i = 0; i < Jsonresponse.titles.Count; i++)
                {
                    if (!(Jsonresponse.titles[i].devices.ToString()).Contains("Win32") && !(Jsonresponse.titles[i].devices.ToString().Contains("Xbox360")))
                    {
                        if (count % 6 == 0 && count != 0)
                        {
                            newline = newline + 160;
                            count = 0;
                        }
                        PictureBox GameImage = new PictureBox();
                        GameImage.Location = new System.Drawing.Point(130 * count, 25 + newline);
                        GameImage.Size = new System.Drawing.Size(125, 125);
                        GameImage.SizeMode = PictureBoxSizeMode.StretchImage;
                        GameImage.ImageLocation = Jsonresponse.titles[i].displayImage.ToString();
                        GameImage.Name = Jsonresponse.titles[i].titleId.ToString();
                        GameImage.Click += new System.EventHandler(this.LoadAchievementList);
                        Panel_Recents.Controls.Add(GameImage);
                        //Create the dynamic TextBox.
                        TextBox textbox = new TextBox();
                        textbox.Location = new System.Drawing.Point(130 * count, 150 + newline);
                        textbox.Size = new System.Drawing.Size(125, 20);
                        textbox.Name = "txt_" + (count + 1);
                        textbox.Text = Jsonresponse.titles[i].name;
                        Panel_Recents.Controls.Add(textbox);
                        count = count + 1;
                    }
                    if (Panel_Recents.Controls.OfType<TextBox>().ToList().Count == 60)
                        break;
                }
            }
            catch (HttpRequestException ex)
            {
                if ((int)ex.StatusCode == 401)
                    MessageBox.Show("Xauth is not correct. Restart this tool and kill xbox app services in task manager before reopening the xbox app", "401 Unauthorised");
                else
                    MessageBox.Show("something did a fucky wucky and I dont have a specific message for the error code", "fucky wucky");
            }
        }

        private void BTN_SpoofGame_Click(object sender, EventArgs e)
        {
            Game_Spoofer SpoofForm = new Game_Spoofer();
            SpoofForm.Show();
        }
    }
}
