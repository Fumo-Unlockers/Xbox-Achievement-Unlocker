namespace Xbox_Achievement_Unlocker
{
    partial class StatsEditor
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
            label1 = new System.Windows.Forms.Label();
            LST_Stats = new System.Windows.Forms.ListBox();
            LBL_StatType = new System.Windows.Forms.Label();
            TXT_Stat = new System.Windows.Forms.TextBox();
            LBL_TitleID = new System.Windows.Forms.Label();
            TXT_TitleID = new System.Windows.Forms.TextBox();
            BTN_LoadStats = new System.Windows.Forms.Button();
            BTN_WriteStat = new System.Windows.Forms.Button();
            LBL_WriteStatus = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(12, 9);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(465, 30);
            label1.TabIndex = 0;
            label1.Text = "This functionality is very experimental. If it doesnt work for a game then it doesnt work.\r\nGLHF try not to overdo it or something idk :)";
            // 
            // LST_Stats
            // 
            LST_Stats.FormattingEnabled = true;
            LST_Stats.ItemHeight = 15;
            LST_Stats.Location = new System.Drawing.Point(12, 42);
            LST_Stats.Name = "LST_Stats";
            LST_Stats.Size = new System.Drawing.Size(61, 214);
            LST_Stats.TabIndex = 1;
            LST_Stats.SelectedIndexChanged += LST_Stats_SelectedIndexChanged;
            // 
            // LBL_StatType
            // 
            LBL_StatType.AutoSize = true;
            LBL_StatType.Location = new System.Drawing.Point(79, 70);
            LBL_StatType.Name = "LBL_StatType";
            LBL_StatType.Size = new System.Drawing.Size(78, 15);
            LBL_StatType.TabIndex = 2;
            LBL_StatType.Text = "Variable Type:";
            // 
            // TXT_Stat
            // 
            TXT_Stat.Location = new System.Drawing.Point(79, 88);
            TXT_Stat.Name = "TXT_Stat";
            TXT_Stat.Size = new System.Drawing.Size(193, 23);
            TXT_Stat.TabIndex = 3;
            // 
            // LBL_TitleID
            // 
            LBL_TitleID.AutoSize = true;
            LBL_TitleID.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            LBL_TitleID.Location = new System.Drawing.Point(496, 9);
            LBL_TitleID.Name = "LBL_TitleID";
            LBL_TitleID.Size = new System.Drawing.Size(60, 20);
            LBL_TitleID.TabIndex = 4;
            LBL_TitleID.Text = "Title ID:";
            // 
            // TXT_TitleID
            // 
            TXT_TitleID.Location = new System.Drawing.Point(562, 6);
            TXT_TitleID.Name = "TXT_TitleID";
            TXT_TitleID.Size = new System.Drawing.Size(108, 23);
            TXT_TitleID.TabIndex = 5;
            // 
            // BTN_LoadStats
            // 
            BTN_LoadStats.Location = new System.Drawing.Point(562, 34);
            BTN_LoadStats.Name = "BTN_LoadStats";
            BTN_LoadStats.Size = new System.Drawing.Size(107, 23);
            BTN_LoadStats.TabIndex = 6;
            BTN_LoadStats.Text = "Load Stats";
            BTN_LoadStats.UseVisualStyleBackColor = true;
            BTN_LoadStats.Click += BTN_LoadStats_Click;
            // 
            // BTN_WriteStat
            // 
            BTN_WriteStat.Location = new System.Drawing.Point(79, 117);
            BTN_WriteStat.Name = "BTN_WriteStat";
            BTN_WriteStat.Size = new System.Drawing.Size(107, 23);
            BTN_WriteStat.TabIndex = 7;
            BTN_WriteStat.Text = "Write Stat";
            BTN_WriteStat.UseVisualStyleBackColor = true;
            BTN_WriteStat.Click += BTN_WriteStat_Click;
            // 
            // LBL_WriteStatus
            // 
            LBL_WriteStatus.AutoSize = true;
            LBL_WriteStatus.Location = new System.Drawing.Point(79, 55);
            LBL_WriteStatus.Name = "LBL_WriteStatus";
            LBL_WriteStatus.Size = new System.Drawing.Size(67, 15);
            LBL_WriteStatus.TabIndex = 8;
            LBL_WriteStatus.Text = "Status: N/A";
            // 
            // StatsEditor
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(681, 267);
            Controls.Add(LBL_WriteStatus);
            Controls.Add(BTN_WriteStat);
            Controls.Add(BTN_LoadStats);
            Controls.Add(TXT_TitleID);
            Controls.Add(LBL_TitleID);
            Controls.Add(TXT_Stat);
            Controls.Add(LBL_StatType);
            Controls.Add(LST_Stats);
            Controls.Add(label1);
            Name = "StatsEditor";
            Text = "StatsEditor";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox LST_Stats;
        private System.Windows.Forms.Label LBL_StatType;
        private System.Windows.Forms.TextBox TXT_Stat;
        private System.Windows.Forms.Label LBL_TitleID;
        private System.Windows.Forms.TextBox TXT_TitleID;
        private System.Windows.Forms.Button BTN_LoadStats;
        private System.Windows.Forms.Button BTN_WriteStat;
        private System.Windows.Forms.Label LBL_WriteStatus;
    }
}