
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
            ((System.ComponentModel.ISupportInitialize)DGV_AchievementList).BeginInit();
            SuspendLayout();
            // 
            // BTN_Unlock
            // 
            BTN_Unlock.Location = new System.Drawing.Point(0, 395);
            BTN_Unlock.Margin = new System.Windows.Forms.Padding(0);
            BTN_Unlock.Name = "BTN_Unlock";
            BTN_Unlock.Size = new System.Drawing.Size(105, 46);
            BTN_Unlock.TabIndex = 6;
            BTN_Unlock.Text = "Unlock Selected Achievements";
            BTN_Unlock.UseVisualStyleBackColor = true;
            BTN_Unlock.Click += BTN_Unlock_Click;
            // 
            // BTN_UnlockAll
            // 
            BTN_UnlockAll.Enabled = false;
            BTN_UnlockAll.Location = new System.Drawing.Point(105, 395);
            BTN_UnlockAll.Margin = new System.Windows.Forms.Padding(0);
            BTN_UnlockAll.Name = "BTN_UnlockAll";
            BTN_UnlockAll.Size = new System.Drawing.Size(105, 46);
            BTN_UnlockAll.TabIndex = 8;
            BTN_UnlockAll.Text = "Unlock All Achievements";
            BTN_UnlockAll.UseVisualStyleBackColor = true;
            BTN_UnlockAll.Click += BTN_UnlockAll_Click;
            // 
            // DGV_AchievementList
            // 
            DGV_AchievementList.AllowUserToAddRows = false;
            DGV_AchievementList.AllowUserToDeleteRows = false;
            DGV_AchievementList.AllowUserToResizeColumns = false;
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
            DGV_AchievementList.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnF2;
            DGV_AchievementList.Location = new System.Drawing.Point(-41, -3);
            DGV_AchievementList.Name = "DGV_AchievementList";
            DGV_AchievementList.RowTemplate.Height = 25;
            DGV_AchievementList.RowTemplate.ReadOnly = true;
            DGV_AchievementList.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            DGV_AchievementList.ShowCellErrors = false;
            DGV_AchievementList.ShowCellToolTips = false;
            DGV_AchievementList.ShowEditingIcon = false;
            DGV_AchievementList.ShowRowErrors = false;
            DGV_AchievementList.Size = new System.Drawing.Size(898, 395);
            DGV_AchievementList.TabIndex = 10;
            DGV_AchievementList.CellClick += dataGridView1_CellClick;
            // 
            // dataGridViewCheckBoxColumn1
            // 
            dataGridViewCheckBoxColumn1.FalseValue = "0";
            dataGridViewCheckBoxColumn1.HeaderText = "Unlock";
            dataGridViewCheckBoxColumn1.IndeterminateValue = "2";
            dataGridViewCheckBoxColumn1.Name = "dataGridViewCheckBoxColumn1";
            dataGridViewCheckBoxColumn1.TrueValue = "1";
            dataGridViewCheckBoxColumn1.Width = 50;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.HeaderText = "Name";
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            dataGridViewTextBoxColumn1.Width = 150;
            // 
            // CL_Description
            // 
            CL_Description.HeaderText = "Description";
            CL_Description.Name = "CL_Description";
            CL_Description.ReadOnly = true;
            CL_Description.Width = 300;
            // 
            // CL_Stats
            // 
            CL_Stats.HeaderText = "Stats";
            CL_Stats.Name = "CL_Stats";
            CL_Stats.ReadOnly = true;
            CL_Stats.Width = 300;
            // 
            // CL_ID
            // 
            CL_ID.HeaderText = "ID";
            CL_ID.Name = "CL_ID";
            CL_ID.ReadOnly = true;
            CL_ID.Width = 40;
            // 
            // Check_UnlockAll
            // 
            Check_UnlockAll.AutoSize = true;
            Check_UnlockAll.Location = new System.Drawing.Point(213, 410);
            Check_UnlockAll.Name = "Check_UnlockAll";
            Check_UnlockAll.Size = new System.Drawing.Size(113, 19);
            Check_UnlockAll.TabIndex = 11;
            Check_UnlockAll.Text = "Allow Unlock All";
            Check_UnlockAll.UseVisualStyleBackColor = true;
            Check_UnlockAll.CheckedChanged += Check_UnlockAll_CheckedChanged;
            // 
            // BTN_ALRefresh
            // 
            BTN_ALRefresh.Location = new System.Drawing.Point(752, 395);
            BTN_ALRefresh.Margin = new System.Windows.Forms.Padding(0);
            BTN_ALRefresh.Name = "BTN_ALRefresh";
            BTN_ALRefresh.Size = new System.Drawing.Size(105, 46);
            BTN_ALRefresh.TabIndex = 12;
            BTN_ALRefresh.Text = "Refresh";
            BTN_ALRefresh.UseVisualStyleBackColor = true;
            BTN_ALRefresh.Click += BTN_ALRefresh_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Segoe UI", 17F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            label1.Location = new System.Drawing.Point(325, 400);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(430, 31);
            label1.TabIndex = 13;
            label1.Text = "THIS IS EVENT BASED. IT WONT WORK";
            label1.Visible = false;
            // 
            // AchievementList
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(224, 224, 224);
            ClientSize = new System.Drawing.Size(856, 440);
            Controls.Add(BTN_ALRefresh);
            Controls.Add(Check_UnlockAll);
            Controls.Add(DGV_AchievementList);
            Controls.Add(BTN_UnlockAll);
            Controls.Add(BTN_Unlock);
            Controls.Add(label1);
            Name = "AchievementList";
            Text = "Achievement List";
            ((System.ComponentModel.ISupportInitialize)DGV_AchievementList).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.Button BTN_Unlock;
        private System.Windows.Forms.Button BTN_UnlockAll;
        private System.Windows.Forms.DataGridView DGV_AchievementList;
        private System.Windows.Forms.DataGridViewCheckBoxColumn dataGridViewCheckBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn CL_Description;
        private System.Windows.Forms.DataGridViewTextBoxColumn CL_Stats;
        private System.Windows.Forms.DataGridViewTextBoxColumn CL_ID;
        private System.Windows.Forms.CheckBox Check_UnlockAll;
        private System.Windows.Forms.Button BTN_ALRefresh;
        private System.Windows.Forms.Label label1;
    }
}