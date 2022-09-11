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
    public partial class AchievementList : Form
    {
        public AchievementList()
        {
            InitializeComponent();
        }
        public List<string> AchievementIDs = new List<string>();
        HttpClient client = new HttpClient();
        string SCID;
        string TitleID;
        List<string> UnlockableAchievements = new List<string>();
        public async void PopulateAchievementList(string AchievementData)
        {
            var Jsonresponse = (dynamic)(new JObject());
            Jsonresponse = (dynamic)JObject.Parse(AchievementData);
            var newline = 0;
            var backcolour = Color.Silver;
            if (Jsonresponse.achievements.Count == 0)
            {
                Close();
                MessageBox.Show("this game has no achivements", "fucky wucky");

            }
            else
            {
                SCID = Jsonresponse.achievements[0].serviceConfigId.ToString();
                TitleID = Jsonresponse.achievements[0].titleAssociations[0].id.ToString();
                for (int i = 0; i < Jsonresponse.achievements.Count; i++)
                {
                    if (Jsonresponse.achievements[0].progression.requirements.ToString().Length > 2)
                    {
                        if (Jsonresponse.achievements[0].progression.requirements[0].id != "00000000-0000-0000-0000-000000000000")
                            MessageBox.Show("This game might use event based achivements and if so currently does not work", "Warning");
                        break;
                    }
                }

                for (int i = 0; i < Jsonresponse.achievements.Count; i++)
                {
                    if (Jsonresponse.achievements[i].progressState.ToString() == "Achieved")
                    {
                        try
                        {


                            DGV_AchievementList.Rows.Add(2,
                                Jsonresponse.achievements[i].name.ToString(),
                                Jsonresponse.achievements[i].description.ToString(),
                                "Gamerscore: " + Jsonresponse.achievements[i].rewards[0].value.ToString() +
                                "\nRarity: " + Jsonresponse.achievements[i].rarity.currentCategory.ToString() +
                                "\nPlayer Percentage: " +
                                Jsonresponse.achievements[i].rarity.currentPercentage.ToString() + "%" +
                                "\nSecret: " + Jsonresponse.achievements[i].isSecret.ToString() +
                                "\nProgress State: " + Jsonresponse.achievements[i].progressState.ToString() +
                                "\nUnlock Time: " + Jsonresponse.achievements[i].progression.timeUnlocked.ToString(),
                                Jsonresponse.achievements[i].id.ToString()
                            );
                        }
                        catch
                        {
                            DGV_AchievementList.Rows.Add(0,
                                Jsonresponse.achievements[i].name.ToString(),
                                Jsonresponse.achievements[i].description.ToString(),
                                "There was a problem grabbing stats for this achievement.\n\n\n\n\n",
                                Jsonresponse.achievements[i].id.ToString()
                            );
                        }
                    }
                    else
                    {
                        try
                        {
                            DGV_AchievementList.Rows.Add(0,
                                Jsonresponse.achievements[i].name.ToString(),
                                Jsonresponse.achievements[i].description.ToString(),
                                "Gamerscore: " + Jsonresponse.achievements[i].rewards[0].value.ToString() +
                                "\nRarity: " + Jsonresponse.achievements[i].rarity.currentCategory.ToString() +
                                "\nPlayer Percentage: " +
                                Jsonresponse.achievements[i].rarity.currentPercentage.ToString() +
                                "%" +
                                "\nSecret: " + Jsonresponse.achievements[i].isSecret.ToString() +
                                "\nProgress State: " + Jsonresponse.achievements[i].progressState.ToString() + "\n",
                                Jsonresponse.achievements[i].id.ToString()
                            );
                        }
                        catch
                        {
                            DGV_AchievementList.Rows.Add(0,
                                Jsonresponse.achievements[i].name.ToString(),
                                Jsonresponse.achievements[i].description.ToString(),
                                "There was a problem grabbing stats for this achievement.\n\n\n\n\n",
                                Jsonresponse.achievements[i].id.ToString()
                            );
                        }

                    }
                }
            }
        }
        void SelectAchievement(object sender, EventArgs e)
        {
            CheckBox SelectedAchievement = (sender as CheckBox);
            if (SelectedAchievement.Checked)
            {
                AchievementIDs.Add(SelectedAchievement.Name);
            }
            else
            {
                AchievementIDs.Remove(SelectedAchievement.Name);
            }
        }
        private void CheckBox_Images_CheckedChanged(object sender, EventArgs e)
        {
            //dont even know if I can put images in a data grid view lmao
        }

        void BTN_Unlock_Click(object sender, EventArgs e)
        {
            var requestbody = "{\"action\":\"progressUpdate\",\"serviceConfigId\":\"" + SCID + "\",\"titleId\":\"" + TitleID + "\",\"userId\":\"" + MainWindow.xuid + "\",\"achievements\":[";

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("accept-language", "en-GB");
            client.DefaultRequestHeaders.Add("Authorization", MainWindow.xauthtoken);
            client.DefaultRequestHeaders.Add("Host", "achievements.xboxlive.com");
            client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            client.DefaultRequestHeaders.Add("User-Agent", "XboxServicesAPI/2021.04.20210610.3 c");
            //
            if (AchievementIDs.Count == 0)
            {
                MessageBox.Show("Select Achivements");
            }
            else
            {
                foreach (string id in AchievementIDs)
                {
                    requestbody = requestbody + "{\"id\":\"" + id + "\",\"percentComplete\":\"100\"},";
                }
                requestbody = requestbody.Remove(requestbody.Length - 1) + "]}";
                var bodyconverted = new StringContent(requestbody);
                try
                {
                    client.PostAsync("https://achievements.xboxlive.com/users/xuid(" + MainWindow.xuid + ")/achievements/" + SCID + "/update", bodyconverted);
                }
                catch (HttpRequestException ex)
                {
                    if ((int)ex.StatusCode == 401)
                        MessageBox.Show("Xauth is not correct. Restart this tool and kill xbox app services in task manager before reopening the xbox app", "401 Unauthorised");
                    else
                        MessageBox.Show("something did a fucky wucky and I dont have a specific message for the error code", "fucky wucky");
                }


            }
        }

        private void BTN_UnlockAll_Click(object sender, EventArgs e)
        {

            var Jsonresponse = (dynamic)JObject.Parse(MainWindow.responseString);
            //create list for unlock all
            for (int i = 0; i < Jsonresponse.achievements.Count; i++)
            {
                if (Jsonresponse.achievements[i].progressState.ToString() != "Achieved")
                {
                    UnlockableAchievements.Add(Jsonresponse.achievements[i].id.ToString());
                }
            }

            var requestbody = "{\"action\":\"progressUpdate\",\"serviceConfigId\":\"" + SCID + "\",\"titleId\":\"" + TitleID + "\",\"userId\":\"" + MainWindow.xuid + "\",\"achievements\":[";

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("accept-language", "en-GB");
            client.DefaultRequestHeaders.Add("Authorization", MainWindow.xauthtoken);
            client.DefaultRequestHeaders.Add("Host", "achievements.xboxlive.com");
            client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            client.DefaultRequestHeaders.Add("User-Agent", "XboxServicesAPI/2021.04.20210610.3 c");

            for (int i = 0; i < UnlockableAchievements.Count; i++)
            {
                requestbody = requestbody + "{\"id\":\"" + UnlockableAchievements[i] + "\",\"percentComplete\":\"100\"},";


                if (i % 50 == 0 || i == UnlockableAchievements.Count || i != 0)
                {
                    requestbody = requestbody.Remove(requestbody.Length - 1) + "]}";
                    var bodyconverted = new StringContent(requestbody);
                    try
                    {
                        client.PostAsync("https://achievements.xboxlive.com/users/xuid(" + MainWindow.xuid + ")/achievements/" + SCID + "/update", bodyconverted);
                    }
                    catch (HttpRequestException ex)
                    {
                        if ((int)ex.StatusCode == 401)
                            MessageBox.Show("Xauth is not correct. Restart this tool and kill xbox app services in task manager before reopening the xbox app", "401 Unauthorised");
                        else
                            MessageBox.Show("something did a fucky wucky and I dont have a specific message for the error code", "fucky wucky");
                    }

                    requestbody = "{\"action\":\"progressUpdate\",\"serviceConfigId\":\"" + SCID + "\",\"titleId\":\"" + TitleID + "\",\"userId\":\"" + MainWindow.xuid + "\",\"achievements\":[";
                }
            }

        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if ((int)DGV_AchievementList.Rows[e.RowIndex].Cells[0].Value == 0)
            {
                AchievementIDs.Add(DGV_AchievementList.Rows[e.RowIndex].Cells[4].Value.ToString());
                DGV_AchievementList.Rows[e.RowIndex].Cells[0].Value = 1;
            }
            else if ((int)DGV_AchievementList.Rows[e.RowIndex].Cells[0].Value == 1)
            {
                DGV_AchievementList.Rows[e.RowIndex].Cells[0].Value = 0;
                AchievementIDs.Remove(DGV_AchievementList.Rows[e.RowIndex].Cells[4].Value.ToString());
            }

        }
    }
}
