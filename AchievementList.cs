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

        public async void PopulateAchievementList(string AchievementData)
        {
            AchievementList ALForm= new AchievementList();
            ALForm.Show();
            var Jsonresponse = (dynamic)(new JObject());
            Jsonresponse = (dynamic)JObject.Parse(AchievementData);
            var newline = 0;
            var backcolour = Color.Silver;
            for (int i = 0; i < Jsonresponse.achievements.Count; i++)
            {
                //hacky thing because im kinda retarded and dont want to do proper ui design
                Panel PanelLine = new Panel();
                PanelLine.Height = 144;
                PanelLine.Width = 782;
                PanelLine.BackColor = Color.Transparent;
                PanelLine.BorderStyle = BorderStyle.FixedSingle;
                PanelLine.Location = new System.Drawing.Point(0, 23+newline);
                PanelLine.Name = "Line_" + i.ToString();
                ALForm.Panel_AchievementList.Controls.Add(PanelLine);
                var PanelLineElement = ALForm.Panel_AchievementList.Controls.OfType<Panel>().Last();
                //background for unlocked section
                Panel AchievementUnlockedBack = new Panel();
                AchievementUnlockedBack.Enabled = false;
                AchievementUnlockedBack.BorderStyle = BorderStyle.None;
                AchievementUnlockedBack.BackColor = backcolour;
                AchievementUnlockedBack.Location = new System.Drawing.Point(0, 0);
                AchievementUnlockedBack.Size = new System.Drawing.Size(45, 144);
                AchievementUnlockedBack.Name = "UnlockedBack_" + Jsonresponse.achievements[i].id.ToString();
                PanelLineElement.Controls.Add(AchievementUnlockedBack);
                //foreground for unlock section
                CheckBox AchievementUnlocked = new CheckBox();
                AchievementUnlocked.Location = new System.Drawing.Point(15, 64);
                AchievementUnlocked.Text = " ";
                AchievementUnlocked.Name = Jsonresponse.achievements[i].id.ToString();
                AchievementUnlocked.Width = 15;
                AchievementUnlocked.Height = 14;
                AchievementUnlocked.BackColor = backcolour;
                AchievementUnlocked.Click += new System.EventHandler(this.SelectAchievement);
                PanelLineElement.Controls.Add(AchievementUnlocked);
                AchievementUnlocked.BringToFront();
                //background for name section
                Panel AchievementNameBack = new Panel();
                AchievementNameBack.Enabled = false;
                AchievementNameBack.BackColor = backcolour;
                AchievementNameBack.BorderStyle = BorderStyle.None;
                AchievementNameBack.Location = new System.Drawing.Point(47, 0);
                AchievementNameBack.Size = new System.Drawing.Size(149, 144);
                AchievementNameBack.Name = "NameBack_"+Jsonresponse.achievements[i].id.ToString();
                PanelLineElement.Controls.Add(AchievementNameBack);
                //foreground for name section
                Label AchievementName = new Label();
                AchievementName.BackColor = backcolour;
                AchievementName.Location = new System.Drawing.Point(47, 0);
                AchievementName.MinimumSize = new System.Drawing.Size(149, 144);
                AchievementName.MaximumSize = new System.Drawing.Size(149, 144);
                AchievementName.Name = "Name_" + Jsonresponse.achievements[i].id.ToString();
                AchievementName.Text = Jsonresponse.achievements[i].name.ToString();
                PanelLineElement.Controls.Add(AchievementName);
                AchievementName.BringToFront();
                //background for description section
                Panel AchievementDescriptionBack = new Panel();
                AchievementDescriptionBack.Enabled = false;
                AchievementDescriptionBack.BackColor = backcolour;
                AchievementDescriptionBack.BorderStyle = BorderStyle.None;
                AchievementDescriptionBack.Location = new System.Drawing.Point(198, 0);
                AchievementDescriptionBack.Size = new System.Drawing.Size(291, 144);
                AchievementDescriptionBack.Name = "DescriptionBack_" + Jsonresponse.achievements[i].id.ToString();
                PanelLineElement.Controls.Add(AchievementDescriptionBack);
                //foreground for description section
                Label AchievementDescription = new Label();
                AchievementDescription.BackColor = backcolour;
                AchievementDescription.Location = new System.Drawing.Point(198, 0);
                AchievementDescription.MinimumSize = new System.Drawing.Size(291, 144);
                AchievementDescription.MaximumSize = new System.Drawing.Size(291, 144);
                AchievementDescription.Name = "Description_" + Jsonresponse.achievements[i].id.ToString();
                AchievementDescription.Text = Jsonresponse.achievements[i].description.ToString();
                PanelLineElement.Controls.Add(AchievementDescription);
                AchievementDescription.BringToFront();
                //background for stats/image section
                Panel StatsBack = new Panel();
                StatsBack.Enabled = false;
                StatsBack.BackColor = backcolour;
                StatsBack.BorderStyle = BorderStyle.None;
                StatsBack.Location = new System.Drawing.Point(491, 0);
                StatsBack.Size = new System.Drawing.Size(292, 144);
                StatsBack.Name = "StatsBack_" + Jsonresponse.achievements[i].id.ToString();
                PanelLineElement.Controls.Add(StatsBack);
                //foreground for stats section
                Label Stats = new Label();
                Stats.BackColor = backcolour;
                Stats.BorderStyle = BorderStyle.None;
                Stats.Location = new System.Drawing.Point(491, 0);
                Stats.MinimumSize = new System.Drawing.Size(292, 144);
                Stats.MaximumSize = new System.Drawing.Size(292, 144);
                Stats.Name = "Stats_" + Jsonresponse.achievements[i].id.ToString();
                Stats.Text = "Gamerscore: " + Jsonresponse.achievements[i].rewards[0].value.ToString() +
                             "\nRarity: " + Jsonresponse.achievements[i].rarity.currentCategory.ToString() +
                             "\nPlayer Percentage: " + Jsonresponse.achievements[i].rarity.currentPercentage.ToString() + "%" +
                             "\nSecret: " + Jsonresponse.achievements[i].isSecret.ToString() +
                             "\nProgress State: " + Jsonresponse.achievements[i].progressState.ToString();
                PanelLineElement.Controls.Add(Stats);
                Stats.BringToFront();
                /*foreground for images section
                PictureBox AchievementImage = new PictureBox();
                AchievementImage.BackColor = backcolour;
                AchievementImage.BorderStyle = BorderStyle.None;
                AchievementImage.Location = new System.Drawing.Point(491, 0);
                AchievementImage.Size = new System.Drawing.Size(292, 144);
                AchievementImage.Name = "Image_" + Jsonresponse.achievements[i].id.ToString();
                AchievementImage.LoadAsync(Jsonresponse.achievements[i].mediaAssets[0].url.ToString()+ "&w=280&h=280&format=jpg");
                AchievementImage.Visible = false;*/
                //change some ui elements if achievement is already unlocked
                if (Jsonresponse.achievements[i].progressState.ToString()== "Achieved")
                {
                    Stats.Text = "Gamerscore: " + Jsonresponse.achievements[i].rewards[0].value.ToString() +
                                 "\nRarity: " + Jsonresponse.achievements[i].rarity.currentCategory.ToString() +
                                 "\nPlayer Percentage: " + Jsonresponse.achievements[i].rarity.currentPercentage.ToString() + "%" +
                                 "\nSecret: " + Jsonresponse.achievements[i].isSecret.ToString() +
                                 "\nProgress State: " + Jsonresponse.achievements[i].progressState.ToString() +
                                 "\nUnlock Time: " + Jsonresponse.achievements[i].progression.timeUnlocked.ToString();
                    AchievementUnlocked.Enabled = false;
                    AchievementUnlocked.Checked = true;
                }
                newline = newline + 144;
                backcolour = Color.Silver;
                if (i % 2 == 0)
                {
                    backcolour = Color.FromArgb(224, 224, 224);
                }
            }
        }
        void SelectAchievement(object sender,EventArgs e)
        {
            CheckBox SelectedAchievement = (sender as CheckBox);
            if (SelectedAchievement.Checked)
            {
                AchievementIDs.Add(SelectedAchievement.Name);
                foreach (string id in AchievementIDs)
                {
                    MessageBox.Show(id);
                }

            }
            else
            {
                AchievementIDs.Remove(SelectedAchievement.Name.ToString());
                foreach (string id in AchievementIDs)
                {
                    MessageBox.Show(id);
                }

            }
        }
        private void CheckBox_Images_CheckedChanged(object sender, EventArgs e)
        {
            //man idk this doesnt work and I dont really care that much about images. leaving it for now
            AchievementList ALForm = new AchievementList();
            if (CheckBox_Images.Checked)
            {
                ALForm.Header4.Text = "Images";
                foreach (PictureBox box in ALForm.Panel_AchievementList.Controls.OfType<PictureBox>())
                {
                    box.Visible = true;
                    box.BringToFront();
                }
            }
            else
            {
                ALForm.Header4.Text = "Stats";
                foreach (PictureBox box in ALForm.Panel_AchievementList.Controls.OfType<PictureBox>())
                {
                    box.Visible = false;
                    box.SendToBack();
                }
            }
            
        }

        void BTN_Unlock_Click(object sender, EventArgs e)
        {
            foreach (string id in AchievementIDs)
            {
                MessageBox.Show(id);
            }
            
        }
    }
}
