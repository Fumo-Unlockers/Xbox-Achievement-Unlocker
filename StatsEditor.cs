using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Xbox_Achievement_Unlocker
{
    public partial class StatsEditor : Form
    {
        public StatsEditor()
        {
            InitializeComponent();
        }
        static HttpClientHandler handler = new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };
        HttpClient client = new HttpClient(handler);
        dynamic jsonresponse;
        private void ResizeListBox(ListBox listBox, TextBox textBox, Label label,Label label2, Button button)
        {
            int maxWidth = 0;

            foreach (var item in listBox.Items)
            {
                var itemWidth = TextRenderer.MeasureText(item.ToString(), listBox.Font).Width;

                if (maxWidth < itemWidth)
                {
                    maxWidth = itemWidth;
                }
            }
            listBox.Width = maxWidth + (SystemInformation.VerticalScrollBarWidth * 2);
            textBox.Location = new Point(listBox.Location.X + listBox.Width + 10, textBox.Location.Y);
            label.Location = new Point(listBox.Location.X + listBox.Width + 10, label.Location.Y);
            label2.Location = new Point(listBox.Location.X + listBox.Width + 10, label2.Location.Y);
            button.Location = new Point(listBox.Location.X + listBox.Width + 10, button.Location.Y);

        }

        private async void BTN_LoadStats_Click(object sender, EventArgs e)
        {
            LST_Stats.Items.Clear();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("Authorization", MainWindow.xauthtoken);
            client.DefaultRequestHeaders.Add("accept-language", "en-GB");
            StringContent requestbody = new StringContent("{\"arrangebyfield\":\"xuid\",\"xuids\":[\"" + MainWindow.xuid + "\"],\"groups\":[{\"name\":\"Hero\",\"titleId\":\"" + TXT_TitleID.Text + "\"}]}");
            jsonresponse = (dynamic)JObject.Parse(await client.PostAsync("https://userstats.xboxlive.com/batch", requestbody).Result.Content.ReadAsStringAsync());
            try
            {
                foreach (var stat in jsonresponse.groups[0].statlistscollection[0].stats)
                {
                    LST_Stats.Items.Add(stat.groupproperties.DisplayName.ToString());
                }
            }
            catch
            {
                MessageBox.Show("Error loading stats, the TitleID may be wrong or the game has no stats.");
            }
            
            ResizeListBox(LST_Stats, TXT_Stat, LBL_StatType, LBL_WriteStatus, BTN_WriteStat);
        }

        private void LST_Stats_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedStat = jsonresponse.groups[0].statlistscollection[0].stats[LST_Stats.SelectedIndex];
            if (selectedStat.ContainsKey("type"))
            {
                LBL_StatType.Text = "Variable Type: " + selectedStat.type.ToString();
            }
            else
            {
                LBL_StatType.Text = "Variable Type: Unknown";
            }
            if (selectedStat.ContainsKey("value"))
            {
                TXT_Stat.Text = selectedStat.value.ToString();
            }
            else
            {
                TXT_Stat.Text = string.Empty;
            }
        }

        private async void BTN_WriteStat_Click(object sender, EventArgs e)
        {
            var selectedStat = jsonresponse.groups[0].statlistscollection[0].stats[LST_Stats.SelectedIndex];
            var currentTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ");
            long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            StringContent requestbody = new StringContent("{\"$schema\":\"http://stats.xboxlive.com/2017-1/schema#\",\"previousRevision\":" + unixTime + ",\"revision\":" + (unixTime + 1) + ",\"stats\":{\"title\":{\"" + selectedStat.name + "\":{\"value\":" + TXT_Stat.Text + "}}},\"timestamp\":\"" + currentTime + "\"}", Encoding.UTF8, "application/json");
            var response = client.PostAsync("https://statswrite.xboxlive.com/stats/users/" + MainWindow.xuid + "/scids/" + selectedStat.scid, requestbody);
            if (response.Result.IsSuccessStatusCode)
            {
                LBL_WriteStatus.Text = "Stat Write Successful!";
            }
            else
            {
                LBL_WriteStatus.Text = "Stat Write Failed!";
            }


        }
    }
}
