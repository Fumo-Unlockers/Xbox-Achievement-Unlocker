
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
            dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            CL_Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            CL_Stats = new System.Windows.Forms.DataGridViewTextBoxColumn();
            CL_ID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Check_UnlockAll = new System.Windows.Forms.CheckBox();
            BTN_ALRefresh = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            panel1 = new System.Windows.Forms.Panel();
            groupBox2 = new System.Windows.Forms.GroupBox();
            panel4 = new System.Windows.Forms.Panel();
            LBL_Timer = new System.Windows.Forms.Label();
            BTN_SpoofStop = new System.Windows.Forms.Button();
            BTN_Spoof = new System.Windows.Forms.Button();
            groupBox1 = new System.Windows.Forms.GroupBox();
            gameImage = new System.Windows.Forms.PictureBox();
            CB_titleList = new System.Windows.Forms.ComboBox();
            LBL_TID = new System.Windows.Forms.Label();
            panel2 = new System.Windows.Forms.Panel();
            panel3 = new System.Windows.Forms.Panel();
            SpoofingTime = new System.Windows.Forms.Timer(components);
            ((System.ComponentModel.ISupportInitialize)DGV_AchievementList).BeginInit();
            panel1.SuspendLayout();
            groupBox2.SuspendLayout();
            panel4.SuspendLayout();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gameImage).BeginInit();
            panel2.SuspendLayout();
            panel3.SuspendLayout();
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
            DGV_AchievementList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { dataGridViewCheckBoxColumn1, dataGridViewTextBoxColumn1, CL_Description, CL_Stats, CL_ID });
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
            DGV_AchievementList.Location = new System.Drawing.Point(0, 102);
            DGV_AchievementList.Name = "DGV_AchievementList";
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
            DGV_AchievementList.Size = new System.Drawing.Size(915, 399);
            DGV_AchievementList.TabIndex = 1;
            DGV_AchievementList.CellClick += dataGridView1_CellClick;
            // 
            // dataGridViewCheckBoxColumn1
            // 
            dataGridViewCheckBoxColumn1.FalseValue = "0";
            dataGridViewCheckBoxColumn1.FillWeight = 70F;
            dataGridViewCheckBoxColumn1.HeaderText = "Unlock";
            dataGridViewCheckBoxColumn1.IndeterminateValue = "2";
            dataGridViewCheckBoxColumn1.MinimumWidth = 70;
            dataGridViewCheckBoxColumn1.Name = "dataGridViewCheckBoxColumn1";
            dataGridViewCheckBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            dataGridViewCheckBoxColumn1.TrueValue = "1";
            dataGridViewCheckBoxColumn1.Width = 70;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewTextBoxColumn1.FillWeight = 150F;
            dataGridViewTextBoxColumn1.HeaderText = "Name";
            dataGridViewTextBoxColumn1.MinimumWidth = 6;
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            // 
            // CL_Description
            // 
            CL_Description.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            CL_Description.FillWeight = 300F;
            CL_Description.HeaderText = "Description";
            CL_Description.MinimumWidth = 6;
            CL_Description.Name = "CL_Description";
            // 
            // CL_Stats
            // 
            CL_Stats.FillWeight = 300F;
            CL_Stats.HeaderText = "Stats";
            CL_Stats.MinimumWidth = 6;
            CL_Stats.Name = "CL_Stats";
            CL_Stats.Width = 298;
            // 
            // CL_ID
            // 
            CL_ID.FillWeight = 40F;
            CL_ID.HeaderText = "ID";
            CL_ID.MinimumWidth = 6;
            CL_ID.Name = "CL_ID";
            CL_ID.Width = 40;
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
            BTN_ALRefresh.Location = new System.Drawing.Point(809, 4);
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
            panel1.Controls.Add(groupBox2);
            panel1.Controls.Add(groupBox1);
            panel1.Dock = System.Windows.Forms.DockStyle.Top;
            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(915, 102);
            panel1.TabIndex = 14;
            // 
            // groupBox2
            // 
            groupBox2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            groupBox2.Controls.Add(panel4);
            groupBox2.Controls.Add(BTN_SpoofStop);
            groupBox2.Controls.Add(BTN_Spoof);
            groupBox2.Location = new System.Drawing.Point(506, 12);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(397, 71);
            groupBox2.TabIndex = 11;
            groupBox2.TabStop = false;
            // 
            // panel4
            // 
            panel4.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            panel4.Controls.Add(LBL_Timer);
            panel4.Location = new System.Drawing.Point(231, 19);
            panel4.Name = "panel4";
            panel4.Size = new System.Drawing.Size(151, 40);
            panel4.TabIndex = 7;
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
            // BTN_SpoofStop
            // 
            BTN_SpoofStop.Location = new System.Drawing.Point(114, 22);
            BTN_SpoofStop.Name = "BTN_SpoofStop";
            BTN_SpoofStop.Size = new System.Drawing.Size(98, 34);
            BTN_SpoofStop.TabIndex = 5;
            BTN_SpoofStop.Text = "Stop";
            BTN_SpoofStop.UseVisualStyleBackColor = true;
            // 
            // BTN_Spoof
            // 
            BTN_Spoof.Location = new System.Drawing.Point(10, 22);
            BTN_Spoof.Name = "BTN_Spoof";
            BTN_Spoof.Size = new System.Drawing.Size(98, 34);
            BTN_Spoof.TabIndex = 3;
            BTN_Spoof.Text = "Start";
            BTN_Spoof.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(gameImage);
            groupBox1.Controls.Add(CB_titleList);
            groupBox1.Controls.Add(LBL_TID);
            groupBox1.Location = new System.Drawing.Point(3, 2);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(461, 90);
            groupBox1.TabIndex = 10;
            groupBox1.TabStop = false;
            // 
            // gameImage
            // 
            gameImage.Location = new System.Drawing.Point(12, 13);
            gameImage.Name = "gameImage";
            gameImage.Size = new System.Drawing.Size(68, 68);
            gameImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            gameImage.TabIndex = 8;
            gameImage.TabStop = false;
            // 
            // CB_titleList
            // 
            CB_titleList.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            CB_titleList.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            CB_titleList.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            CB_titleList.FormattingEnabled = true;
            CB_titleList.Location = new System.Drawing.Point(91, 52);
            CB_titleList.Name = "CB_titleList";
            CB_titleList.Size = new System.Drawing.Size(141, 29);
            CB_titleList.TabIndex = 7;
            // 
            // LBL_TID
            // 
            LBL_TID.AutoSize = true;
            LBL_TID.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            LBL_TID.Location = new System.Drawing.Point(91, 22);
            LBL_TID.Name = "LBL_TID";
            LBL_TID.Size = new System.Drawing.Size(251, 21);
            LBL_TID.TabIndex = 0;
            LBL_TID.Text = "Monster Hunter (windows version)";
            // 
            // panel2
            // 
            panel2.Controls.Add(BTN_UnlockAll);
            panel2.Controls.Add(label1);
            panel2.Controls.Add(BTN_ALRefresh);
            panel2.Controls.Add(BTN_Unlock);
            panel2.Controls.Add(Check_UnlockAll);
            panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            panel2.Location = new System.Drawing.Point(0, 501);
            panel2.Name = "panel2";
            panel2.Size = new System.Drawing.Size(915, 54);
            panel2.TabIndex = 15;
            // 
            // panel3
            // 
            panel3.Controls.Add(DGV_AchievementList);
            panel3.Controls.Add(panel1);
            panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            panel3.Location = new System.Drawing.Point(0, 0);
            panel3.Name = "panel3";
            panel3.Size = new System.Drawing.Size(915, 501);
            panel3.TabIndex = 16;
            // 
            // SpoofingTime
            // 
            SpoofingTime.Enabled = true;
            // 
            // AchievementList
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(224, 224, 224);
            ClientSize = new System.Drawing.Size(915, 555);
            Controls.Add(panel3);
            Controls.Add(panel2);
            KeyPreview = true;
            Name = "AchievementList";
            Text = "Achievement List";
            KeyDown += AchievementList_KeyDown;
            ((System.ComponentModel.ISupportInitialize)DGV_AchievementList).EndInit();
            panel1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            panel4.ResumeLayout(false);
            panel4.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gameImage).EndInit();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            panel3.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private System.Windows.Forms.Button BTN_Unlock;
        private System.Windows.Forms.Button BTN_UnlockAll;
        private System.Windows.Forms.DataGridView DGV_AchievementList;
        private System.Windows.Forms.CheckBox Check_UnlockAll;
        private System.Windows.Forms.Button BTN_ALRefresh;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DataGridViewCheckBoxColumn dataGridViewCheckBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn CL_Description;
        private System.Windows.Forms.DataGridViewTextBoxColumn CL_Stats;
        private System.Windows.Forms.DataGridViewTextBoxColumn CL_ID;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Timer SpoofingTime;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.PictureBox gameImage;
        private System.Windows.Forms.ComboBox CB_titleList;
        private System.Windows.Forms.Label LBL_TID;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Label LBL_Timer;
        private System.Windows.Forms.Button BTN_SpoofStop;
        private System.Windows.Forms.Button BTN_Spoof;
    }
}