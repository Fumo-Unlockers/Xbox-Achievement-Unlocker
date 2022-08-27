
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
            this.BGWorker = new System.ComponentModel.BackgroundWorker();
            this.LBL_Attached = new System.Windows.Forms.Label();
            this.TXT_Xauth = new System.Windows.Forms.TextBox();
            this.BTN_GrabXauth = new System.Windows.Forms.Button();
            this.LBL_Gamertag = new System.Windows.Forms.Label();
            this.LBL_Gamerscore = new System.Windows.Forms.Label();
            this.Panel_Recents = new System.Windows.Forms.Panel();
            this.BTN_SpoofGame = new System.Windows.Forms.Button();
            this.TXT_Xuid = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // BGWorker
            // 
            this.BGWorker.WorkerReportsProgress = true;
            this.BGWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BGWorker_DoWork);
            this.BGWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.BGWorker_ProgressChanged);
            this.BGWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BGWorker_RunWorkerCompleted);
            // 
            // LBL_Attached
            // 
            this.LBL_Attached.AutoSize = true;
            this.LBL_Attached.ForeColor = System.Drawing.Color.Red;
            this.LBL_Attached.Location = new System.Drawing.Point(12, 9);
            this.LBL_Attached.Name = "LBL_Attached";
            this.LBL_Attached.Size = new System.Drawing.Size(145, 15);
            this.LBL_Attached.TabIndex = 0;
            this.LBL_Attached.Text = "Not attached to Xbox App";
            // 
            // TXT_Xauth
            // 
            this.TXT_Xauth.BackColor = System.Drawing.SystemColors.Control;
            this.TXT_Xauth.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TXT_Xauth.Location = new System.Drawing.Point(12, 27);
            this.TXT_Xauth.Name = "TXT_Xauth";
            this.TXT_Xauth.Size = new System.Drawing.Size(158, 16);
            this.TXT_Xauth.TabIndex = 1;
            this.TXT_Xauth.Text = "xauth:";
            // 
            // BTN_GrabXauth
            // 
            this.BTN_GrabXauth.Enabled = false;
            this.BTN_GrabXauth.Location = new System.Drawing.Point(12, 53);
            this.BTN_GrabXauth.Name = "BTN_GrabXauth";
            this.BTN_GrabXauth.Size = new System.Drawing.Size(158, 46);
            this.BTN_GrabXauth.TabIndex = 2;
            this.BTN_GrabXauth.Text = "Grab All";
            this.BTN_GrabXauth.UseVisualStyleBackColor = true;
            this.BTN_GrabXauth.Click += new System.EventHandler(this.BTN_GrabXauth_Click);
            // 
            // LBL_Gamertag
            // 
            this.LBL_Gamertag.AutoSize = true;
            this.LBL_Gamertag.Location = new System.Drawing.Point(176, 27);
            this.LBL_Gamertag.Name = "LBL_Gamertag";
            this.LBL_Gamertag.Size = new System.Drawing.Size(65, 15);
            this.LBL_Gamertag.TabIndex = 6;
            this.LBL_Gamertag.Text = "Gamertag: ";
            // 
            // LBL_Gamerscore
            // 
            this.LBL_Gamerscore.AutoSize = true;
            this.LBL_Gamerscore.Location = new System.Drawing.Point(176, 53);
            this.LBL_Gamerscore.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.LBL_Gamerscore.Name = "LBL_Gamerscore";
            this.LBL_Gamerscore.Size = new System.Drawing.Size(76, 15);
            this.LBL_Gamerscore.TabIndex = 7;
            this.LBL_Gamerscore.Text = "Gamerscore: ";
            // 
            // Panel_Recents
            // 
            this.Panel_Recents.AutoScroll = true;
            this.Panel_Recents.Location = new System.Drawing.Point(3, 102);
            this.Panel_Recents.Name = "Panel_Recents";
            this.Panel_Recents.Size = new System.Drawing.Size(797, 336);
            this.Panel_Recents.TabIndex = 8;
            // 
            // BTN_SpoofGame
            // 
            this.BTN_SpoofGame.Enabled = false;
            this.BTN_SpoofGame.Location = new System.Drawing.Point(630, 53);
            this.BTN_SpoofGame.Name = "BTN_SpoofGame";
            this.BTN_SpoofGame.Size = new System.Drawing.Size(158, 46);
            this.BTN_SpoofGame.TabIndex = 9;
            this.BTN_SpoofGame.Text = "Open Game Spoofer";
            this.BTN_SpoofGame.UseVisualStyleBackColor = true;
            this.BTN_SpoofGame.Click += new System.EventHandler(this.BTN_SpoofGame_Click);
            // 
            // TXT_Xuid
            // 
            this.TXT_Xuid.BackColor = System.Drawing.SystemColors.Control;
            this.TXT_Xuid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TXT_Xuid.Location = new System.Drawing.Point(176, 80);
            this.TXT_Xuid.Name = "TXT_Xuid";
            this.TXT_Xuid.Size = new System.Drawing.Size(158, 16);
            this.TXT_Xuid.TabIndex = 10;
            this.TXT_Xuid.Text = "XUID:";
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.BTN_SpoofGame);
            this.Controls.Add(this.TXT_Xuid);
            this.Controls.Add(this.Panel_Recents);
            this.Controls.Add(this.LBL_Gamerscore);
            this.Controls.Add(this.LBL_Gamertag);
            this.Controls.Add(this.BTN_GrabXauth);
            this.Controls.Add(this.TXT_Xauth);
            this.Controls.Add(this.LBL_Attached);
            this.Name = "MainWindow";
            this.Text = "MainWindow";
            this.Shown += new System.EventHandler(this.MainWindow_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

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
    }
}

