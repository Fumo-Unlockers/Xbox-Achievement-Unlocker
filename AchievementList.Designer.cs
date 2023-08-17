
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
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
            Sorting_Box = new System.Windows.Forms.ComboBox();
            label2 = new System.Windows.Forms.Label();
            searchbox = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)DGV_AchievementList).BeginInit();
            SuspendLayout();
            // 
            // BTN_Unlock
            // 
            BTN_Unlock.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            BTN_Unlock.Location = new System.Drawing.Point(9, 389);
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
            BTN_UnlockAll.Location = new System.Drawing.Point(124, 389);
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
            DGV_AchievementList.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            DGV_AchievementList.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            DGV_AchievementList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            DGV_AchievementList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            DGV_AchievementList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { dataGridViewCheckBoxColumn1, dataGridViewTextBoxColumn1, CL_Description, CL_Stats, CL_ID });
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            DGV_AchievementList.DefaultCellStyle = dataGridViewCellStyle2;
            DGV_AchievementList.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnF2;
            DGV_AchievementList.Location = new System.Drawing.Point(0, 0);
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
            DGV_AchievementList.Size = new System.Drawing.Size(1160, 388);
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
            dataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // CL_Description
            // 
            CL_Description.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            CL_Description.FillWeight = 300F;
            CL_Description.HeaderText = "Description";
            CL_Description.MinimumWidth = 6;
            CL_Description.Name = "CL_Description";
            CL_Description.ReadOnly = true;
            // 
            // CL_Stats
            // 
            CL_Stats.FillWeight = 300F;
            CL_Stats.HeaderText = "Stats";
            CL_Stats.MinimumWidth = 6;
            CL_Stats.Name = "CL_Stats";
            CL_Stats.ReadOnly = true;
            CL_Stats.Width = 300;
            // 
            // CL_ID
            // 
            CL_ID.FillWeight = 40F;
            CL_ID.HeaderText = "ID";
            CL_ID.MinimumWidth = 6;
            CL_ID.Name = "CL_ID";
            CL_ID.ReadOnly = true;
            CL_ID.Width = 40;
            // 
            // Check_UnlockAll
            // 
            Check_UnlockAll.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            Check_UnlockAll.AutoSize = true;
            Check_UnlockAll.Location = new System.Drawing.Point(225, 396);
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
            BTN_ALRefresh.Location = new System.Drawing.Point(1050, 394);
            BTN_ALRefresh.Margin = new System.Windows.Forms.Padding(0);
            BTN_ALRefresh.Name = "BTN_ALRefresh";
            BTN_ALRefresh.Size = new System.Drawing.Size(105, 40);
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
            label1.Location = new System.Drawing.Point(617, 399);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(430, 31);
            label1.TabIndex = 13;
            label1.Text = "THIS IS EVENT BASED. IT WONT WORK";
            label1.Visible = false;
            // 
            // Sorting_Box
            // 
            Sorting_Box.FormattingEnabled = true;
            Sorting_Box.Items.AddRange(new object[] { "all", "unlockable", "unlocked" });
            Sorting_Box.Location = new System.Drawing.Point(303, 410);
            Sorting_Box.Name = "Sorting_Box";
            Sorting_Box.Size = new System.Drawing.Size(121, 23);
            Sorting_Box.TabIndex = 14;
            Sorting_Box.TabStop = false;
            Sorting_Box.SelectedIndex = 0;
            Sorting_Box.SelectedIndexChanged += Sorting_Box_SelectedIndexChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(341, 393);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(45, 15);
            label2.TabIndex = 15;
            label2.Text = "Sorting";
            // 
            // searchbox
            // 
            searchbox.Location = new System.Drawing.Point(447, 411);
            searchbox.Name = "searchbox";
            searchbox.PlaceholderText = "search box";
            searchbox.Size = new System.Drawing.Size(164, 23);
            searchbox.TabIndex = 16;
            searchbox.TextChanged += searchbox_TextChanged;
            // 
            // AchievementList
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(224, 224, 224);
            ClientSize = new System.Drawing.Size(1161, 440);
            Controls.Add(searchbox);
            Controls.Add(label2);
            Controls.Add(Sorting_Box);
            Controls.Add(BTN_ALRefresh);
            Controls.Add(Check_UnlockAll);
            Controls.Add(DGV_AchievementList);
            Controls.Add(BTN_UnlockAll);
            Controls.Add(BTN_Unlock);
            Controls.Add(label1);
            KeyPreview = true;
            Name = "AchievementList";
            Text = "Achievement List";
            KeyDown += AchievementList_KeyDown;
            ((System.ComponentModel.ISupportInitialize)DGV_AchievementList).EndInit();
            ResumeLayout(false);
            PerformLayout();
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
        private System.Windows.Forms.ComboBox Sorting_Box;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox searchbox;
    }
}