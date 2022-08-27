namespace Xbox_Achievement_Unlocker
{
    partial class Game_Spoofer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.LBL_TID = new System.Windows.Forms.Label();
            this.TXT_TID = new System.Windows.Forms.TextBox();
            this.BTN_Spoof = new System.Windows.Forms.Button();
            this.TXT_SpoofedGame = new System.Windows.Forms.RichTextBox();
            this.BTN_SpoofStop = new System.Windows.Forms.Button();
            this.LBL_Timer = new System.Windows.Forms.Label();
            this.SpoofingTime = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // LBL_TID
            // 
            this.LBL_TID.AutoSize = true;
            this.LBL_TID.Location = new System.Drawing.Point(3, 9);
            this.LBL_TID.Name = "LBL_TID";
            this.LBL_TID.Size = new System.Drawing.Size(43, 15);
            this.LBL_TID.TabIndex = 0;
            this.LBL_TID.Text = "TitleID:";
            // 
            // TXT_TID
            // 
            this.TXT_TID.Location = new System.Drawing.Point(52, 6);
            this.TXT_TID.Name = "TXT_TID";
            this.TXT_TID.Size = new System.Drawing.Size(168, 23);
            this.TXT_TID.TabIndex = 1;
            // 
            // BTN_Spoof
            // 
            this.BTN_Spoof.Location = new System.Drawing.Point(226, 5);
            this.BTN_Spoof.Name = "BTN_Spoof";
            this.BTN_Spoof.Size = new System.Drawing.Size(98, 34);
            this.BTN_Spoof.TabIndex = 3;
            this.BTN_Spoof.Text = "Start";
            this.BTN_Spoof.UseVisualStyleBackColor = true;
            this.BTN_Spoof.Click += new System.EventHandler(this.BTN_Spoof_Click);
            // 
            // TXT_SpoofedGame
            // 
            this.TXT_SpoofedGame.BackColor = System.Drawing.SystemColors.Control;
            this.TXT_SpoofedGame.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TXT_SpoofedGame.Location = new System.Drawing.Point(3, 35);
            this.TXT_SpoofedGame.Margin = new System.Windows.Forms.Padding(0);
            this.TXT_SpoofedGame.Name = "TXT_SpoofedGame";
            this.TXT_SpoofedGame.Size = new System.Drawing.Size(217, 44);
            this.TXT_SpoofedGame.TabIndex = 4;
            this.TXT_SpoofedGame.Text = "Currently Spoofing: N/A";
            // 
            // BTN_SpoofStop
            // 
            this.BTN_SpoofStop.Location = new System.Drawing.Point(226, 45);
            this.BTN_SpoofStop.Name = "BTN_SpoofStop";
            this.BTN_SpoofStop.Size = new System.Drawing.Size(98, 34);
            this.BTN_SpoofStop.TabIndex = 5;
            this.BTN_SpoofStop.Text = "Stop";
            this.BTN_SpoofStop.UseVisualStyleBackColor = true;
            this.BTN_SpoofStop.Click += new System.EventHandler(this.BTN_SpoofStop_Click);
            // 
            // LBL_Timer
            // 
            this.LBL_Timer.AutoSize = true;
            this.LBL_Timer.Location = new System.Drawing.Point(72, 64);
            this.LBL_Timer.Name = "LBL_Timer";
            this.LBL_Timer.Size = new System.Drawing.Size(72, 15);
            this.LBL_Timer.TabIndex = 6;
            this.LBL_Timer.Text = "For: 00:00:00";
            // 
            // SpoofingTime
            // 
            this.SpoofingTime.Enabled = true;
            this.SpoofingTime.Tick += new System.EventHandler(this.SpoofingTime_Tick);
            // 
            // Game_Spoofer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(336, 84);
            this.Controls.Add(this.LBL_Timer);
            this.Controls.Add(this.BTN_SpoofStop);
            this.Controls.Add(this.TXT_SpoofedGame);
            this.Controls.Add(this.BTN_Spoof);
            this.Controls.Add(this.TXT_TID);
            this.Controls.Add(this.LBL_TID);
            this.Name = "Game_Spoofer";
            this.Text = "Game Spoofer";
            this.Load += new System.EventHandler(this.Game_Spoofer_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label LBL_TID;
        private System.Windows.Forms.TextBox TXT_TID;
        private System.Windows.Forms.Button BTN_Spoof;
        private System.Windows.Forms.RichTextBox TXT_SpoofedGame;
        private System.Windows.Forms.Button BTN_SpoofStop;
        private System.Windows.Forms.Label LBL_Timer;
        private System.Windows.Forms.Timer SpoofingTime;
    }
}