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

namespace Xbox_Achievement_Unlocker
{
    public partial class Game_Spoofer : Form
    {
        private Stopwatch stopwatch;
        public Game_Spoofer()
        {
            InitializeComponent();
        }
        static HttpClientHandler handler = new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };
        HttpClient client = new HttpClient(handler);

        bool active;
        async void BTN_Spoof_Click(object sender, EventArgs e)
        {
            BTN_Spoof.Enabled = false;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("Authorization", MainWindow.xauthtoken);
            client.DefaultRequestHeaders.Add("accept-language", "en-GB");

            //
            StringContent requestbody = new StringContent("{\"pfns\":null,\"titleIds\":[\"" + TXT_TID.Text + "\"]}");
            var jsonresponse = (dynamic)JObject.Parse(await client.PostAsync("https://titlehub.xboxlive.com/users/xuid(" + MainWindow.xuid + ")/titles/batch/decoration/GamePass,Achievement,Stats", requestbody).Result.Content.ReadAsStringAsync());

            TXT_SpoofedGame.Text = "Currently Spoofing: " + jsonresponse.titles[0].name.ToString();
            BTN_SpoofStop.Enabled = true;
            Task.Run(() => Spoofing());
        }

        public async Task Spoofing()
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "3");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("Authorization", MainWindow.xauthtoken);

            var requestbody = new StringContent("{\"titles\":[{\"expiration\":600,\"id\":" + TXT_TID.Text + ",\"state\":\"active\",\"sandbox\":\"RETAIL\"}]}", encoding: Encoding.UTF8, "application/json");
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
            LBL_Timer.Text = "For: N/A";
            TXT_SpoofedGame.Text = "Currently Spoofing: N/A";
        }

        private void Game_Spoofer_Load(object sender, EventArgs e)
        {
            stopwatch = new Stopwatch();
        }

        private void SpoofingTime_Tick(object sender, EventArgs e)
        {
            LBL_Timer.Text = "For: " + string.Format("{0:hh\\:mm\\:ss}", stopwatch.Elapsed);

        }
    }
}
