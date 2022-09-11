
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
            this.BTN_Unlock = new System.Windows.Forms.Button();
            this.BTN_UnlockAll = new System.Windows.Forms.Button();
            this.DGV_AchievementList = new System.Windows.Forms.DataGridView();
            this.dataGridViewCheckBoxColumn1 = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CL_Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CL_Stats = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CL_ID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.DGV_AchievementList)).BeginInit();
            this.SuspendLayout();
            // 
            // BTN_Unlock
            // 
            this.BTN_Unlock.Location = new System.Drawing.Point(0, 395);
            this.BTN_Unlock.Margin = new System.Windows.Forms.Padding(0);
            this.BTN_Unlock.Name = "BTN_Unlock";
            this.BTN_Unlock.Size = new System.Drawing.Size(105, 46);
            this.BTN_Unlock.TabIndex = 6;
            this.BTN_Unlock.Text = "Unlock Selected Achievements";
            this.BTN_Unlock.UseVisualStyleBackColor = true;
            this.BTN_Unlock.Click += new System.EventHandler(this.BTN_Unlock_Click);
            // 
            // BTN_UnlockAll
            // 
            this.BTN_UnlockAll.Location = new System.Drawing.Point(105, 395);
            this.BTN_UnlockAll.Margin = new System.Windows.Forms.Padding(0);
            this.BTN_UnlockAll.Name = "BTN_UnlockAll";
            this.BTN_UnlockAll.Size = new System.Drawing.Size(105, 46);
            this.BTN_UnlockAll.TabIndex = 8;
            this.BTN_UnlockAll.Text = "Unlock All Achievements";
            this.BTN_UnlockAll.UseVisualStyleBackColor = true;
            this.BTN_UnlockAll.Click += new System.EventHandler(this.BTN_UnlockAll_Click);
            // 
            // DGV_AchievementList
            // 
            this.DGV_AchievementList.AllowUserToAddRows = false;
            this.DGV_AchievementList.AllowUserToDeleteRows = false;
            this.DGV_AchievementList.AllowUserToResizeColumns = false;
            this.DGV_AchievementList.AllowUserToResizeRows = false;
            this.DGV_AchievementList.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.DGV_AchievementList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.DGV_AchievementList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DGV_AchievementList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewCheckBoxColumn1,
            this.dataGridViewTextBoxColumn1,
            this.CL_Description,
            this.CL_Stats,
            this.CL_ID});
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.DGV_AchievementList.DefaultCellStyle = dataGridViewCellStyle1;
            this.DGV_AchievementList.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnF2;
            this.DGV_AchievementList.Location = new System.Drawing.Point(-41, -3);
            this.DGV_AchievementList.Name = "DGV_AchievementList";
            this.DGV_AchievementList.RowTemplate.Height = 25;
            this.DGV_AchievementList.RowTemplate.ReadOnly = true;
            this.DGV_AchievementList.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.DGV_AchievementList.ShowCellErrors = false;
            this.DGV_AchievementList.ShowCellToolTips = false;
            this.DGV_AchievementList.ShowEditingIcon = false;
            this.DGV_AchievementList.ShowRowErrors = false;
            this.DGV_AchievementList.Size = new System.Drawing.Size(898, 395);
            this.DGV_AchievementList.TabIndex = 10;
            this.DGV_AchievementList.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellClick);
            // 
            // dataGridViewCheckBoxColumn1
            // 
            this.dataGridViewCheckBoxColumn1.FalseValue = "0";
            this.dataGridViewCheckBoxColumn1.HeaderText = "Unlock";
            this.dataGridViewCheckBoxColumn1.IndeterminateValue = "2";
            this.dataGridViewCheckBoxColumn1.Name = "dataGridViewCheckBoxColumn1";
            this.dataGridViewCheckBoxColumn1.TrueValue = "1";
            this.dataGridViewCheckBoxColumn1.Width = 50;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.HeaderText = "Name";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Width = 150;
            // 
            // CL_Description
            // 
            this.CL_Description.HeaderText = "Description";
            this.CL_Description.Name = "CL_Description";
            this.CL_Description.ReadOnly = true;
            this.CL_Description.Width = 300;
            // 
            // CL_Stats
            // 
            this.CL_Stats.HeaderText = "Stats";
            this.CL_Stats.Name = "CL_Stats";
            this.CL_Stats.ReadOnly = true;
            this.CL_Stats.Width = 300;
            // 
            // CL_ID
            // 
            this.CL_ID.HeaderText = "ID";
            this.CL_ID.Name = "CL_ID";
            this.CL_ID.ReadOnly = true;
            this.CL_ID.Width = 40;
            // 
            // AchievementList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.ClientSize = new System.Drawing.Size(856, 440);
            this.Controls.Add(this.DGV_AchievementList);
            this.Controls.Add(this.BTN_UnlockAll);
            this.Controls.Add(this.BTN_Unlock);
            this.Name = "AchievementList";
            this.Text = "Achievement List";
            ((System.ComponentModel.ISupportInitialize)(this.DGV_AchievementList)).EndInit();
            this.ResumeLayout(false);

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
    }
}