
namespace Xbox_Achievement_Unlocker
{
    partial class MainWindow
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            BGWorker = new System.ComponentModel.BackgroundWorker();
            LBL_Attached = new System.Windows.Forms.Label();
            TXT_Xauth = new System.Windows.Forms.TextBox();
            BTN_GrabXauth = new System.Windows.Forms.Button();
            LBL_Gamertag = new System.Windows.Forms.Label();
            LBL_Gamerscore = new System.Windows.Forms.Label();
            Panel_Recents = new System.Windows.Forms.Panel();
            BTN_SpoofGame = new System.Windows.Forms.Button();
            TXT_Xuid = new System.Windows.Forms.TextBox();
            BTN_SaveToFile = new System.Windows.Forms.Button();
            LST_GameFilter = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            Skidbox = new System.Windows.Forms.RichTextBox();
            BTN_fixer = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // BGWorker
            // 
            BGWorker.WorkerReportsProgress = true;
            BGWorker.DoWork += BGWorker_DoWork;
            BGWorker.ProgressChanged += BGWorker_ProgressChanged;
            BGWorker.RunWorkerCompleted += BGWorker_RunWorkerCompleted;
            // 
            // LBL_Attached
            // 
            LBL_Attached.AutoSize = true;
            LBL_Attached.ForeColor = System.Drawing.Color.Red;
            LBL_Attached.Location = new System.Drawing.Point(12, 9);
            LBL_Attached.Name = "LBL_Attached";
            LBL_Attached.Size = new System.Drawing.Size(145, 15);
            LBL_Attached.TabIndex = 0;
            LBL_Attached.Text = "Not attached to Xbox App";
            // 
            // TXT_Xauth
            // 
            TXT_Xauth.BackColor = System.Drawing.SystemColors.Control;
            TXT_Xauth.BorderStyle = System.Windows.Forms.BorderStyle.None;
            TXT_Xauth.Location = new System.Drawing.Point(12, 102);
            TXT_Xauth.Name = "TXT_Xauth";
            TXT_Xauth.Size = new System.Drawing.Size(158, 16);
            TXT_Xauth.TabIndex = 1;
            TXT_Xauth.Text = "xauth:";
            // 
            // BTN_GrabXauth
            // 
            BTN_GrabXauth.Enabled = false;
            BTN_GrabXauth.Location = new System.Drawing.Point(12, 124);
            BTN_GrabXauth.Name = "BTN_GrabXauth";
            BTN_GrabXauth.Size = new System.Drawing.Size(158, 46);
            BTN_GrabXauth.TabIndex = 2;
            BTN_GrabXauth.Text = "Grab All";
            BTN_GrabXauth.UseVisualStyleBackColor = true;
            BTN_GrabXauth.Click += BTN_GrabXauth_Click;
            // 
            // LBL_Gamertag
            // 
            LBL_Gamertag.AutoSize = true;
            LBL_Gamertag.Location = new System.Drawing.Point(173, 121);
            LBL_Gamertag.Name = "LBL_Gamertag";
            LBL_Gamertag.Size = new System.Drawing.Size(65, 15);
            LBL_Gamertag.TabIndex = 6;
            LBL_Gamertag.Text = "Gamertag: ";
            // 
            // LBL_Gamerscore
            // 
            LBL_Gamerscore.AutoSize = true;
            LBL_Gamerscore.Location = new System.Drawing.Point(173, 136);
            LBL_Gamerscore.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            LBL_Gamerscore.Name = "LBL_Gamerscore";
            LBL_Gamerscore.Size = new System.Drawing.Size(76, 15);
            LBL_Gamerscore.TabIndex = 7;
            LBL_Gamerscore.Text = "Gamerscore: ";
            // 
            // Panel_Recents
            // 
            Panel_Recents.AutoScroll = true;
            Panel_Recents.Location = new System.Drawing.Point(0, 176);
            Panel_Recents.Name = "Panel_Recents";
            Panel_Recents.Size = new System.Drawing.Size(797, 336);
            Panel_Recents.TabIndex = 8;
            // 
            // BTN_SpoofGame
            // 
            BTN_SpoofGame.Enabled = false;
            BTN_SpoofGame.Location = new System.Drawing.Point(631, 128);
            BTN_SpoofGame.Name = "BTN_SpoofGame";
            BTN_SpoofGame.Size = new System.Drawing.Size(158, 46);
            BTN_SpoofGame.TabIndex = 9;
            BTN_SpoofGame.Text = "Open Game Spoofer";
            BTN_SpoofGame.UseVisualStyleBackColor = true;
            BTN_SpoofGame.Click += BTN_SpoofGame_Click;
            // 
            // TXT_Xuid
            // 
            TXT_Xuid.BackColor = System.Drawing.SystemColors.Control;
            TXT_Xuid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            TXT_Xuid.Location = new System.Drawing.Point(176, 154);
            TXT_Xuid.Name = "TXT_Xuid";
            TXT_Xuid.Size = new System.Drawing.Size(158, 16);
            TXT_Xuid.TabIndex = 10;
            TXT_Xuid.Text = "XUID:";
            // 
            // BTN_SaveToFile
            // 
            BTN_SaveToFile.Enabled = false;
            BTN_SaveToFile.Location = new System.Drawing.Point(631, 76);
            BTN_SaveToFile.Name = "BTN_SaveToFile";
            BTN_SaveToFile.Size = new System.Drawing.Size(158, 46);
            BTN_SaveToFile.TabIndex = 11;
            BTN_SaveToFile.Text = "Save games list to file";
            BTN_SaveToFile.UseVisualStyleBackColor = true;
            BTN_SaveToFile.Click += BTN_SaveToFile_Click;
            // 
            // LST_GameFilter
            // 
            LST_GameFilter.FormattingEnabled = true;
            LST_GameFilter.Items.AddRange(new object[] { "All", "Console Only", "New Gen", "Win32" });
            LST_GameFilter.Location = new System.Drawing.Point(504, 151);
            LST_GameFilter.Name = "LST_GameFilter";
            LST_GameFilter.Size = new System.Drawing.Size(121, 23);
            LST_GameFilter.TabIndex = 12;
            LST_GameFilter.SelectedIndexChanged += LST_GameFilter_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(402, 154);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(96, 15);
            label1.TabIndex = 13;
            label1.Text = "Filter Games List:";
            // 
            // Skidbox
            // 
            Skidbox.BackColor = System.Drawing.SystemColors.Control;
            Skidbox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            Skidbox.Location = new System.Drawing.Point(163, 6);
            Skidbox.Name = "Skidbox";
            Skidbox.ReadOnly = true;
            Skidbox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            Skidbox.Size = new System.Drawing.Size(625, 96);
            Skidbox.TabIndex = 14;
            Skidbox.Text = resources.GetString("Skidbox.Text");
            Skidbox.LinkClicked += Skidbox_LinkClicked;
            // 
            // BTN_fixer
            // 
            BTN_fixer.Location = new System.Drawing.Point(12, 27);
            BTN_fixer.Name = "BTN_fixer";
            BTN_fixer.Size = new System.Drawing.Size(145, 69);
            BTN_fixer.TabIndex = 15;
            BTN_fixer.Text = "FuckyWucky \r\nFixer";
            BTN_fixer.UseVisualStyleBackColor = true;
            BTN_fixer.Click += BTN_fixer_Click;
            // 
            // MainWindow
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(800, 512);
            Controls.Add(BTN_fixer);
            Controls.Add(label1);
            Controls.Add(LST_GameFilter);
            Controls.Add(BTN_SaveToFile);
            Controls.Add(BTN_SpoofGame);
            Controls.Add(TXT_Xuid);
            Controls.Add(Panel_Recents);
            Controls.Add(LBL_Gamerscore);
            Controls.Add(LBL_Gamertag);
            Controls.Add(BTN_GrabXauth);
            Controls.Add(TXT_Xauth);
            Controls.Add(LBL_Attached);
            Controls.Add(Skidbox);
            Name = "MainWindow";
            Text = "MainWindow";
            Shown += MainWindow_Shown;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.ComponentModel.BackgroundWorker BGWorker;
        private System.Windows.Forms.Label LBL_Attached;
        private System.Windows.Forms.TextBox TXT_Xauth;
        private System.Windows.Forms.Button BTN_GrabXauth;
        private System.Windows.Forms.Label LBL_Gamertag;
        private System.Windows.Forms.Label LBL_Gamerscore;
        private System.Windows.Forms.Panel Panel_Recents;
        private System.Windows.Forms.Button BTN_SpoofGame;
        private System.Windows.Forms.TextBox TXT_Xuid;
        private System.Windows.Forms.Button BTN_SaveToFile;
        private System.Windows.Forms.ComboBox LST_GameFilter;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox Skidbox;
        private System.Windows.Forms.Button BTN_fixer;
    }
}

