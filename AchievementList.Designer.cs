
namespace Xbox_Achievement_Unlocker
{
    partial class AchievementList
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            BTN_Unlock = new System.Windows.Forms.Button();
            BTN_UnlockAll = new System.Windows.Forms.Button();
            DGV_AchievementList = new System.Windows.Forms.DataGridView();
            dataGridViewCheckBoxColumn1 = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            CL_Rarity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            CL_Name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            CL_Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            CL_Stats = new System.Windows.Forms.DataGridViewTextBoxColumn();
            CL_ID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Check_UnlockAll = new System.Windows.Forms.CheckBox();
            BTN_ALRefresh = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            panel1 = new System.Windows.Forms.Panel();
            LBL_TID_UIXD = new System.Windows.Forms.Label();
            LBL_TID = new System.Windows.Forms.Label();
            lblLink_completationTime = new System.Windows.Forms.LinkLabel();
            groupBox1 = new System.Windows.Forms.GroupBox();
            gameImage = new System.Windows.Forms.PictureBox();
            panel4 = new System.Windows.Forms.Panel();
            LBL_Timer = new System.Windows.Forms.Label();
            BTN_SpoofStop = new System.Windows.Forms.Button();
            BTN_Spoof = new System.Windows.Forms.Button();
            panel6 = new System.Windows.Forms.Panel();
            panel2 = new System.Windows.Forms.Panel();
            panel3 = new System.Windows.Forms.Panel();
            panel7 = new System.Windows.Forms.Panel();
            panel5 = new System.Windows.Forms.Panel();
            SpoofingTime = new System.Windows.Forms.Timer(components);
            ((System.ComponentModel.ISupportInitialize)DGV_AchievementList).BeginInit();
            panel1.SuspendLayout();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gameImage).BeginInit();
            panel4.SuspendLayout();
            panel6.SuspendLayout();
            panel2.SuspendLayout();
            panel3.SuspendLayout();
            panel7.SuspendLayout();
            panel5.SuspendLayout();
            SuspendLayout();
            // 
            // BTN_Unlock
            // 
            BTN_Unlock.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            BTN_Unlock.Location = new System.Drawing.Point(9, 4);
            BTN_Unlock.Margin = new System.Windows.Forms.Padding(0);
            BTN_Unlock.Name = "BTN_Unlock";
            BTN_Unlock.Size = new System.Drawing.Size(113, 46);
            BTN_Unlock.TabIndex = 2;
            BTN_Unlock.Text = "Unlock Selected Achievements";
            BTN_Unlock.UseVisualStyleBackColor = true;
            BTN_Unlock.Click += BTN_Unlock_Click;
            // 
            // BTN_UnlockAll
            // 
            BTN_UnlockAll.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            BTN_UnlockAll.Enabled = false;
            BTN_UnlockAll.Location = new System.Drawing.Point(124, 4);
            BTN_UnlockAll.Margin = new System.Windows.Forms.Padding(0);
            BTN_UnlockAll.Name = "BTN_UnlockAll";
            BTN_UnlockAll.Size = new System.Drawing.Size(97, 46);
            BTN_UnlockAll.TabIndex = 3;
            BTN_UnlockAll.Text = "Unlock All Achievements";
            BTN_UnlockAll.UseVisualStyleBackColor = true;
            BTN_UnlockAll.Click += BTN_UnlockAll_Click;
            // 
            // DGV_AchievementList
            // 
            DGV_AchievementList.AllowUserToAddRows = false;
            DGV_AchievementList.AllowUserToDeleteRows = false;
            DGV_AchievementList.AllowUserToResizeRows = false;
            DGV_AchievementList.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            DGV_AchievementList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            DGV_AchievementList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            DGV_AchievementList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { dataGridViewCheckBoxColumn1, CL_Rarity, CL_Name, CL_Description, CL_Stats, CL_ID });
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            DGV_AchievementList.DefaultCellStyle = dataGridViewCellStyle1;
            DGV_AchievementList.Dock = System.Windows.Forms.DockStyle.Fill;
            DGV_AchievementList.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnF2;
            DGV_AchievementList.Location = new System.Drawing.Point(0, 0);
            DGV_AchievementList.Name = "DGV_AchievementList";
            DGV_AchievementList.ReadOnly = true;
            DGV_AchievementList.RowHeadersVisible = false;
            DGV_AchievementList.RowHeadersWidth = 51;
            DGV_AchievementList.RowTemplate.Height = 25;
            DGV_AchievementList.RowTemplate.ReadOnly = true;
            DGV_AchievementList.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            DGV_AchievementList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            DGV_AchievementList.ShowCellErrors = false;
            DGV_AchievementList.ShowCellToolTips = false;
            DGV_AchievementList.ShowEditingIcon = false;
            DGV_AchievementList.ShowRowErrors = false;
            DGV_AchievementList.Size = new System.Drawing.Size(652, 463);
            DGV_AchievementList.TabIndex = 1;
            DGV_AchievementList.CellClick += dataGridView1_CellClick;
            // 
            // dataGridViewCheckBoxColumn1
            // 
            dataGridViewCheckBoxColumn1.FalseValue = "0";
            dataGridViewCheckBoxColumn1.FillWeight = 70F;
            dataGridViewCheckBoxColumn1.HeaderText = "";
            dataGridViewCheckBoxColumn1.IndeterminateValue = "2";
            dataGridViewCheckBoxColumn1.MinimumWidth = 40;
            dataGridViewCheckBoxColumn1.Name = "dataGridViewCheckBoxColumn1";
            dataGridViewCheckBoxColumn1.ReadOnly = true;
            dataGridViewCheckBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            dataGridViewCheckBoxColumn1.TrueValue = "1";
            dataGridViewCheckBoxColumn1.Width = 40;
            // 
            // CL_Rarity
            // 
            CL_Rarity.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            CL_Rarity.HeaderText = "%";
            CL_Rarity.MinimumWidth = 20;
            CL_Rarity.Name = "CL_Rarity";
            CL_Rarity.ReadOnly = true;
            // 
            // CL_Name
            // 
            CL_Name.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            CL_Name.FillWeight = 150F;
            CL_Name.HeaderText = "Name";
            CL_Name.MinimumWidth = 50;
            CL_Name.Name = "CL_Name";
            CL_Name.ReadOnly = true;
            // 
            // CL_Description
            // 
            CL_Description.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            CL_Description.FillWeight = 300F;
            CL_Description.HeaderText = "Description";
            CL_Description.MinimumWidth = 100;
            CL_Description.Name = "CL_Description";
            CL_Description.ReadOnly = true;
            // 
            // CL_Stats
            // 
            CL_Stats.FillWeight = 300F;
            CL_Stats.HeaderText = "Stats";
            CL_Stats.MinimumWidth = 100;
            CL_Stats.Name = "CL_Stats";
            CL_Stats.ReadOnly = true;
            CL_Stats.Width = 200;
            // 
            // CL_ID
            // 
            CL_ID.FillWeight = 40F;
            CL_ID.HeaderText = "ID";
            CL_ID.MinimumWidth = 6;
            CL_ID.Name = "CL_ID";
            CL_ID.ReadOnly = true;
            CL_ID.Width = 20;
            // 
            // Check_UnlockAll
            // 
            Check_UnlockAll.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            Check_UnlockAll.AutoSize = true;
            Check_UnlockAll.Location = new System.Drawing.Point(224, 11);
            Check_UnlockAll.Name = "Check_UnlockAll";
            Check_UnlockAll.Size = new System.Drawing.Size(80, 34);
            Check_UnlockAll.TabIndex = 4;
            Check_UnlockAll.Text = "Allow\r\nUnlock All";
            Check_UnlockAll.UseVisualStyleBackColor = true;
            Check_UnlockAll.CheckedChanged += Check_UnlockAll_CheckedChanged;
            // 
            // BTN_ALRefresh
            // 
            BTN_ALRefresh.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            BTN_ALRefresh.Location = new System.Drawing.Point(798, 4);
            BTN_ALRefresh.Margin = new System.Windows.Forms.Padding(0);
            BTN_ALRefresh.Name = "BTN_ALRefresh";
            BTN_ALRefresh.Size = new System.Drawing.Size(97, 46);
            BTN_ALRefresh.TabIndex = 5;
            BTN_ALRefresh.Text = "Refresh (F5)";
            BTN_ALRefresh.UseVisualStyleBackColor = true;
            BTN_ALRefresh.Click += BTN_ALRefresh_Click;
            // 
            // label1
            // 
            label1.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Segoe UI", 17F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            label1.Location = new System.Drawing.Point(310, 11);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(430, 31);
            label1.TabIndex = 13;
            label1.Text = "THIS IS EVENT BASED. IT WONT WORK";
            label1.Visible = false;
            // 
            // panel1
            // 
            panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            panel1.Controls.Add(LBL_TID_UIXD);
            panel1.Controls.Add(LBL_TID);
            panel1.Dock = System.Windows.Forms.DockStyle.Top;
            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(904, 44);
            panel1.TabIndex = 14;
            // 
            // LBL_TID_UIXD
            // 
            LBL_TID_UIXD.Anchor = System.Windows.Forms.AnchorStyles.Right;
            LBL_TID_UIXD.AutoSize = true;
            LBL_TID_UIXD.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            LBL_TID_UIXD.Location = new System.Drawing.Point(740, 6);
            LBL_TID_UIXD.Name = "LBL_TID_UIXD";
            LBL_TID_UIXD.Size = new System.Drawing.Size(157, 30);
            LBL_TID_UIXD.TabIndex = 1;
            LBL_TID_UIXD.Text = "123493827832";
            // 
            // LBL_TID
            // 
            LBL_TID.AutoSize = true;
            LBL_TID.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            LBL_TID.Location = new System.Drawing.Point(7, 6);
            LBL_TID.Name = "LBL_TID";
            LBL_TID.Size = new System.Drawing.Size(347, 30);
            LBL_TID.TabIndex = 0;
            LBL_TID.Text = "Monster Hunter (windows version)";
            // 
            // lblLink_completationTime
            // 
            lblLink_completationTime.AutoSize = true;
            lblLink_completationTime.Location = new System.Drawing.Point(43, 2);
            lblLink_completationTime.Name = "lblLink_completationTime";
            lblLink_completationTime.Size = new System.Drawing.Size(145, 15);
            lblLink_completationTime.TabIndex = 8;
            lblLink_completationTime.TabStop = true;
            lblLink_completationTime.Text = "Completation Time: 4 - 6h";
            lblLink_completationTime.Visible = false;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom;
            groupBox1.Controls.Add(gameImage);
            groupBox1.Controls.Add(panel4);
            groupBox1.Controls.Add(BTN_SpoofStop);
            groupBox1.Controls.Add(BTN_Spoof);
            groupBox1.Location = new System.Drawing.Point(14, 18);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(227, 415);
            groupBox1.TabIndex = 10;
            groupBox1.TabStop = false;
            // 
            // gameImage
            // 
            gameImage.Location = new System.Drawing.Point(57, 18);
            gameImage.Name = "gameImage";
            gameImage.Size = new System.Drawing.Size(120, 120);
            gameImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            gameImage.TabIndex = 8;
            gameImage.TabStop = false;
            // 
            // panel4
            // 
            panel4.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            panel4.Controls.Add(LBL_Timer);
            panel4.Location = new System.Drawing.Point(6, 148);
            panel4.Name = "panel4";
            panel4.Size = new System.Drawing.Size(215, 43);
            panel4.TabIndex = 7;
            // 
            // LBL_Timer
            // 
            LBL_Timer.AutoSize = true;
            LBL_Timer.Font = new System.Drawing.Font("Segoe UI", 30F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            LBL_Timer.Location = new System.Drawing.Point(12, -9);
            LBL_Timer.Name = "LBL_Timer";
            LBL_Timer.Size = new System.Drawing.Size(183, 54);
            LBL_Timer.TabIndex = 0;
            LBL_Timer.Text = "00:00:00";
            // 
            // BTN_SpoofStop
            // 
            BTN_SpoofStop.Location = new System.Drawing.Point(6, 247);
            BTN_SpoofStop.Name = "BTN_SpoofStop";
            BTN_SpoofStop.Size = new System.Drawing.Size(215, 43);
            BTN_SpoofStop.TabIndex = 5;
            BTN_SpoofStop.Text = "Stop";
            BTN_SpoofStop.UseVisualStyleBackColor = true;
            BTN_SpoofStop.Click += BTN_SpoofStop_Click;
            // 
            // BTN_Spoof
            // 
            BTN_Spoof.Location = new System.Drawing.Point(6, 198);
            BTN_Spoof.Name = "BTN_Spoof";
            BTN_Spoof.Size = new System.Drawing.Size(215, 43);
            BTN_Spoof.TabIndex = 3;
            BTN_Spoof.Text = "Start";
            BTN_Spoof.UseVisualStyleBackColor = true;
            BTN_Spoof.Click += BTN_Spoof_Click;
            // 
            // panel6
            // 
            panel6.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            panel6.Controls.Add(lblLink_completationTime);
            panel6.Dock = System.Windows.Forms.DockStyle.Bottom;
            panel6.Location = new System.Drawing.Point(0, 439);
            panel6.Name = "panel6";
            panel6.Size = new System.Drawing.Size(252, 24);
            panel6.TabIndex = 9;
            // 
            // panel2
            // 
            panel2.Controls.Add(BTN_UnlockAll);
            panel2.Controls.Add(label1);
            panel2.Controls.Add(BTN_ALRefresh);
            panel2.Controls.Add(BTN_Unlock);
            panel2.Controls.Add(Check_UnlockAll);
            panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            panel2.Location = new System.Drawing.Point(0, 507);
            panel2.Name = "panel2";
            panel2.Size = new System.Drawing.Size(904, 54);
            panel2.TabIndex = 15;
            // 
            // panel3
            // 
            panel3.Controls.Add(panel7);
            panel3.Controls.Add(panel5);
            panel3.Controls.Add(panel1);
            panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            panel3.Location = new System.Drawing.Point(0, 0);
            panel3.Name = "panel3";
            panel3.Size = new System.Drawing.Size(904, 507);
            panel3.TabIndex = 16;
            // 
            // panel7
            // 
            panel7.Controls.Add(DGV_AchievementList);
            panel7.Dock = System.Windows.Forms.DockStyle.Fill;
            panel7.Location = new System.Drawing.Point(0, 44);
            panel7.Name = "panel7";
            panel7.Size = new System.Drawing.Size(652, 463);
            panel7.TabIndex = 16;
            // 
            // panel5
            // 
            panel5.Controls.Add(panel6);
            panel5.Controls.Add(groupBox1);
            panel5.Dock = System.Windows.Forms.DockStyle.Right;
            panel5.Location = new System.Drawing.Point(652, 44);
            panel5.Name = "panel5";
            panel5.Size = new System.Drawing.Size(252, 463);
            panel5.TabIndex = 15;
            // 
            // SpoofingTime
            // 
            SpoofingTime.Enabled = true;
            SpoofingTime.Tick += SpoofingTime_Tick;
            // 
            // AchievementList
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(224, 224, 224);
            ClientSize = new System.Drawing.Size(904, 561);
            Controls.Add(panel3);
            Controls.Add(panel2);
            KeyPreview = true;
            MinimumSize = new System.Drawing.Size(920, 600);
            Name = "AchievementList";
            Text = "Monster Hunter (windows version)";
            KeyDown += AchievementList_KeyDown;
            ((System.ComponentModel.ISupportInitialize)DGV_AchievementList).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)gameImage).EndInit();
            panel4.ResumeLayout(false);
            panel4.PerformLayout();
            panel6.ResumeLayout(false);
            panel6.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            panel3.ResumeLayout(false);
            panel7.ResumeLayout(false);
            panel5.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private System.Windows.Forms.Button BTN_Unlock;
        private System.Windows.Forms.Button BTN_UnlockAll;
        private System.Windows.Forms.DataGridView DGV_AchievementList;
        private System.Windows.Forms.CheckBox Check_UnlockAll;
        private System.Windows.Forms.Button BTN_ALRefresh;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.PictureBox gameImage;
        private System.Windows.Forms.Label LBL_TID;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Button BTN_SpoofStop;
        private System.Windows.Forms.Button BTN_Spoof;
        private System.Windows.Forms.LinkLabel lblLink_completationTime;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Panel panel7;
        private System.Windows.Forms.Timer SpoofingTime;
        private System.Windows.Forms.Label LBL_Timer;
        private System.Windows.Forms.Label LBL_TID_UIXD;
        private System.Windows.Forms.DataGridViewCheckBoxColumn dataGridViewCheckBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn CL_Rarity;
        private System.Windows.Forms.DataGridViewTextBoxColumn CL_Name;
        private System.Windows.Forms.DataGridViewTextBoxColumn CL_Description;
        private System.Windows.Forms.DataGridViewTextBoxColumn CL_Stats;
        private System.Windows.Forms.DataGridViewTextBoxColumn CL_ID;
    }
}