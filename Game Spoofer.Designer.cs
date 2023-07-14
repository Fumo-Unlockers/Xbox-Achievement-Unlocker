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
            components = new System.ComponentModel.Container();
            LBL_TID = new System.Windows.Forms.Label();
            BTN_Spoof = new System.Windows.Forms.Button();
            BTN_SpoofStop = new System.Windows.Forms.Button();
            LBL_Timer = new System.Windows.Forms.Label();
            SpoofingTime = new System.Windows.Forms.Timer(components);
            CB_titleList = new System.Windows.Forms.ComboBox();
            gameImage = new System.Windows.Forms.PictureBox();
            groupBox1 = new System.Windows.Forms.GroupBox();
            groupBox2 = new System.Windows.Forms.GroupBox();
            panel1 = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)gameImage).BeginInit();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // LBL_TID
            // 
            LBL_TID.AutoSize = true;
            LBL_TID.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            LBL_TID.Location = new System.Drawing.Point(83, 19);
            LBL_TID.Name = "LBL_TID";
            LBL_TID.Size = new System.Drawing.Size(114, 25);
            LBL_TID.TabIndex = 0;
            LBL_TID.Text = "Search Title:";
            // 
            // BTN_Spoof
            // 
            BTN_Spoof.Location = new System.Drawing.Point(10, 22);
            BTN_Spoof.Name = "BTN_Spoof";
            BTN_Spoof.Size = new System.Drawing.Size(98, 34);
            BTN_Spoof.TabIndex = 3;
            BTN_Spoof.Text = "Start";
            BTN_Spoof.UseVisualStyleBackColor = true;
            BTN_Spoof.Click += BTN_Spoof_Click;
            // 
            // BTN_SpoofStop
            // 
            BTN_SpoofStop.Location = new System.Drawing.Point(114, 22);
            BTN_SpoofStop.Name = "BTN_SpoofStop";
            BTN_SpoofStop.Size = new System.Drawing.Size(98, 34);
            BTN_SpoofStop.TabIndex = 5;
            BTN_SpoofStop.Text = "Stop";
            BTN_SpoofStop.UseVisualStyleBackColor = true;
            BTN_SpoofStop.Click += BTN_SpoofStop_Click;
            // 
            // LBL_Timer
            // 
            LBL_Timer.AutoSize = true;
            LBL_Timer.Font = new System.Drawing.Font("Segoe UI", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            LBL_Timer.Location = new System.Drawing.Point(14, -1);
            LBL_Timer.Name = "LBL_Timer";
            LBL_Timer.Size = new System.Drawing.Size(127, 37);
            LBL_Timer.TabIndex = 6;
            LBL_Timer.Text = "00:00:00";
            // 
            // SpoofingTime
            // 
            SpoofingTime.Enabled = true;
            SpoofingTime.Tick += SpoofingTime_Tick;
            // 
            // CB_titleList
            // 
            CB_titleList.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            CB_titleList.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            CB_titleList.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            CB_titleList.FormattingEnabled = true;
            CB_titleList.Location = new System.Drawing.Point(83, 50);
            CB_titleList.Name = "CB_titleList";
            CB_titleList.Size = new System.Drawing.Size(294, 29);
            CB_titleList.TabIndex = 7;
            CB_titleList.SelectedValueChanged += CB_titleList_SelectedValueChanged;
            // 
            // gameImage
            // 
            gameImage.Location = new System.Drawing.Point(6, 19);
            gameImage.Name = "gameImage";
            gameImage.Size = new System.Drawing.Size(71, 71);
            gameImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            gameImage.TabIndex = 8;
            gameImage.TabStop = false;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(gameImage);
            groupBox1.Controls.Add(CB_titleList);
            groupBox1.Controls.Add(LBL_TID);
            groupBox1.Location = new System.Drawing.Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(393, 100);
            groupBox1.TabIndex = 9;
            groupBox1.TabStop = false;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(panel1);
            groupBox2.Controls.Add(BTN_SpoofStop);
            groupBox2.Controls.Add(BTN_Spoof);
            groupBox2.Location = new System.Drawing.Point(12, 118);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(393, 63);
            groupBox2.TabIndex = 10;
            groupBox2.TabStop = false;
            // 
            // panel1
            // 
            panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            panel1.Controls.Add(LBL_Timer);
            panel1.Location = new System.Drawing.Point(236, 17);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(151, 40);
            panel1.TabIndex = 7;
            // 
            // Game_Spoofer
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(424, 190);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Name = "Game_Spoofer";
            Text = "Game Spoofer";
            Load += Game_Spoofer_Load;
            ((System.ComponentModel.ISupportInitialize)gameImage).EndInit();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label LBL_TID;
        private System.Windows.Forms.Button BTN_Spoof;
        private System.Windows.Forms.RichTextBox TXT_SpoofedGame;
        private System.Windows.Forms.Button BTN_SpoofStop;
        private System.Windows.Forms.Label LBL_Timer;
        private System.Windows.Forms.Timer SpoofingTime;
        private System.Windows.Forms.ComboBox CB_titleList;
        private System.Windows.Forms.PictureBox gameImage;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
    }
}