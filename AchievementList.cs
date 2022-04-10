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

        public static async void PopulateAchievementList(string AchievementData)
        {
            Form form = new AchievementList();
            form.Show();
            var Jsonresponse = (dynamic)(new JObject());
            Jsonresponse = (dynamic)JObject.Parse(AchievementData);
            var count = 0;
            var newline = 0;
            for (int i = 0; i < Jsonresponse.achievements.Count; i++)
            {
                if (Panel_AchievementList.Height < newline+55)
                    Panel_AchievementList.Height = Panel_AchievementList.Height + 35;
                RichTextBox AchievementName = new RichTextBox();
                AchievementName.Location = new System.Drawing.Point(12, 12 + newline);
                AchievementName.Size = new System.Drawing.Size(150, 50);
                AchievementName.Name = "name_"+Jsonresponse.achievements[i].id.ToString();
                AchievementName.Text = Jsonresponse.achievements[i].name.ToString();
                Panel_AchievementList.Controls.Add(AchievementName);
                RichTextBox AchievementUnlocked = new RichTextBox();
                AchievementUnlocked.Location = new System.Drawing.Point(167, 12 + newline);
                AchievementUnlocked.Size = new System.Drawing.Size(80, 50);
                AchievementUnlocked.Name = "Unlocked_"+Jsonresponse.achievements[i].id.ToString();
                AchievementUnlocked.Text = Jsonresponse.achievements[i].progressState.ToString();
                Panel_AchievementList.Controls.Add(AchievementUnlocked);
                RichTextBox AchievementDescription = new RichTextBox();
                AchievementDescription.Location = new System.Drawing.Point(252, 12 + newline);
                AchievementDescription.Size = new System.Drawing.Size(300, 50);
                AchievementDescription.Name = "Description_" + Jsonresponse.achievements[i].id.ToString();
                AchievementDescription.Text = Jsonresponse.achievements[i].description.ToString();
                Panel_AchievementList.Controls.Add(AchievementDescription);
                newline = newline + 55;
            }


        }
    }
}
