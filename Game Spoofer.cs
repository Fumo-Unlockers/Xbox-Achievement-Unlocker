using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.AxHost;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.IO;
using System.Reflection.Emit;

namespace Xbox_Achievement_Unlocker
{
    public partial class Game_Spoofer : Form
    {
        private Stopwatch stopwatch;
        public Game_Spoofer()
        {
            InitializeComponent();
            fill_Cb_GameList();
        }
        string currentSystemLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;
        static HttpClientHandler handler = new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };
        HttpClient client = new HttpClient(handler);

        bool active;
        async void BTN_Spoof_Click(object sender, EventArgs e)
        {
            if (CB_titleList.SelectedValue == null)
            {
                MessageBox.Show("You must select a game.\n Do you want to cause a bug?", "Game not selected", MessageBoxButtons.OK, MessageBoxIcon.Question);
                return;
            }

            BTN_Spoof.Enabled = false;
            BTN_SpoofStop.Enabled = true;
            string uuiGame = CB_titleList.SelectedValue.ToString();
            CB_titleList.Enabled = false;
            this.Text = "Game Spoofer:: " + CB_titleList.Text;
            Task.Run(() => Spoofing(uuiGame));
        }

        private void fill_Cb_GameList()
        {
            String line;
            try
            {

                //Pass the file path and file name to the StreamReader constructor
                StreamReader sr = new StreamReader("GamesListAll.txt");
                //Read the first line of text
                line = sr.ReadLine();

                List<object> items = new List<object>();
                while (line != null)
                {
                    string[] row = line.Split(",");

                    if (row[0] != "")
                        items.Add(new { Text = Convert.ToString(row[1]), Value = Convert.ToString(row[0]) });
                    line = sr.ReadLine();
                }
                //close the file
                sr.Close();
                CB_titleList.ValueMember = "Value";
                CB_titleList.DisplayMember = "Text";
                CB_titleList.DataSource = items;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
            finally
            {
                Console.WriteLine("Executing finally block.");
            }
        }
        public async Task Spoofing(string uuiGame)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "3");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("Authorization", MainWindow.xauthtoken);

            var requestbody = new StringContent("{\"titles\":[{\"expiration\":600,\"id\":" + uuiGame + ",\"state\":\"active\",\"sandbox\":\"RETAIL\"}]}", encoding: Encoding.UTF8, "application/json");
            stopwatch.Start();
            await client.PostAsync("https://presence-heartbeat.xboxlive.com/users/xuid(" + MainWindow.xuid + ")/devices/current", requestbody);
            var i = 0;
            active = true;
            while (active)
            {
                if (i == 60)
                {
                    await client.PostAsync("https://presence-heartbeat.xboxlive.com/users/xuid(" + MainWindow.xuid + ")/devices/current", requestbody);
                    i = 0;
                }
                else
                {
                    if (!active)
                    {
                        break;
                    }
                    i++;
                }
                Thread.Sleep(1000);

            }
            BTN_Spoof.Invoke(new Action(() => BTN_Spoof.Enabled = true));
            BTN_SpoofStop.Invoke(new Action(() => BTN_SpoofStop.Enabled = false));
        }

        private void BTN_SpoofStop_Click(object sender, EventArgs e)
        {
            active = false;
            stopwatch.Stop();
            stopwatch.Reset();
            CB_titleList.Enabled = true;
            LBL_Timer.Text = "";
        }

        private void Game_Spoofer_Load(object sender, EventArgs e)
        {
            stopwatch = new Stopwatch();
        }

        private void SpoofingTime_Tick(object sender, EventArgs e)
        {
            LBL_Timer.Text = string.Format("{0:hh\\:mm\\:ss}", stopwatch.Elapsed);

        }
    }
}
