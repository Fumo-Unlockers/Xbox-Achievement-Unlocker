
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
            this.LBL_XUID = new System.Windows.Forms.Label();
            this.BTN_XUID = new System.Windows.Forms.Button();
            this.LBL_Gamertag = new System.Windows.Forms.Label();
            this.LBL_Gamerscore = new System.Windows.Forms.Label();
            this.Panel_Recents = new System.Windows.Forms.Panel();
            this.BTN_Recents = new System.Windows.Forms.Button();
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
            this.TXT_Xauth.Location = new System.Drawing.Point(12, 27);
            this.TXT_Xauth.Name = "TXT_Xauth";
            this.TXT_Xauth.Size = new System.Drawing.Size(158, 23);
            this.TXT_Xauth.TabIndex = 1;
            this.TXT_Xauth.Text = "xauth:";
            // 
            // BTN_GrabXauth
            // 
            this.BTN_GrabXauth.Location = new System.Drawing.Point(12, 56);
            this.BTN_GrabXauth.Name = "BTN_GrabXauth";
            this.BTN_GrabXauth.Size = new System.Drawing.Size(108, 23);
            this.BTN_GrabXauth.TabIndex = 2;
            this.BTN_GrabXauth.Text = "Grab XAUTH";
            this.BTN_GrabXauth.UseVisualStyleBackColor = true;
            this.BTN_GrabXauth.Click += new System.EventHandler(this.BTN_GrabXauth_Click);
            // 
            // LBL_XUID
            // 
            this.LBL_XUID.AutoSize = true;
            this.LBL_XUID.Location = new System.Drawing.Point(176, 84);
            this.LBL_XUID.Name = "LBL_XUID";
            this.LBL_XUID.Size = new System.Drawing.Size(39, 15);
            this.LBL_XUID.TabIndex = 4;
            this.LBL_XUID.Text = "XUID: ";
            // 
            // BTN_XUID
            // 
            this.BTN_XUID.Location = new System.Drawing.Point(666, 5);
            this.BTN_XUID.Name = "BTN_XUID";
            this.BTN_XUID.Size = new System.Drawing.Size(122, 23);
            this.BTN_XUID.TabIndex = 5;
            this.BTN_XUID.Text = "Grab Info";
            this.BTN_XUID.UseVisualStyleBackColor = true;
            this.BTN_XUID.Click += new System.EventHandler(this.BTN_XUID_Click);
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
            this.LBL_Gamerscore.Location = new System.Drawing.Point(176, 56);
            this.LBL_Gamerscore.Name = "LBL_Gamerscore";
            this.LBL_Gamerscore.Size = new System.Drawing.Size(76, 15);
            this.LBL_Gamerscore.TabIndex = 7;
            this.LBL_Gamerscore.Text = "Gamerscore: ";
            // 
            // Panel_Recents
            // 
            this.Panel_Recents.Location = new System.Drawing.Point(12, 102);
            this.Panel_Recents.Name = "Panel_Recents";
            this.Panel_Recents.Size = new System.Drawing.Size(776, 336);
            this.Panel_Recents.TabIndex = 8;
            // 
            // BTN_Recents
            // 
            this.BTN_Recents.Location = new System.Drawing.Point(666, 56);
            this.BTN_Recents.Name = "BTN_Recents";
            this.BTN_Recents.Size = new System.Drawing.Size(122, 23);
            this.BTN_Recents.TabIndex = 9;
            this.BTN_Recents.Text = "Grab Recent Games";
            this.BTN_Recents.UseVisualStyleBackColor = true;
            this.BTN_Recents.Click += new System.EventHandler(this.BTN_Recents_Click);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.BTN_Recents);
            this.Controls.Add(this.Panel_Recents);
            this.Controls.Add(this.LBL_Gamerscore);
            this.Controls.Add(this.LBL_Gamertag);
            this.Controls.Add(this.BTN_XUID);
            this.Controls.Add(this.LBL_XUID);
            this.Controls.Add(this.BTN_GrabXauth);
            this.Controls.Add(this.TXT_Xauth);
            this.Controls.Add(this.LBL_Attached);
            this.Name = "MainWindow";
            this.Text = "Form1";
            this.Shown += new System.EventHandler(this.MainWindow_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.ComponentModel.BackgroundWorker BGWorker;
        private System.Windows.Forms.Label LBL_Attached;
        private System.Windows.Forms.TextBox TXT_Xauth;
        private System.Windows.Forms.Button BTN_GrabXauth;
        private System.Windows.Forms.Label LBL_XUID;
        private System.Windows.Forms.Button BTN_XUID;
        private System.Windows.Forms.Label LBL_Gamertag;
        private System.Windows.Forms.Label LBL_Gamerscore;
        private System.Windows.Forms.Panel Panel_Recents;
        private System.Windows.Forms.Button BTN_Recents;
    }
}

