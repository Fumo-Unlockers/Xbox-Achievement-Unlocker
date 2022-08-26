
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
            this.Panel_AchievementList = new System.Windows.Forms.Panel();
            this.Header3 = new System.Windows.Forms.TextBox();
            this.Header2 = new System.Windows.Forms.TextBox();
            this.Header1 = new System.Windows.Forms.TextBox();
            this.Header4 = new System.Windows.Forms.TextBox();
            this.BTN_Unlock = new System.Windows.Forms.Button();
            this.CheckBox_Images = new System.Windows.Forms.CheckBox();
            this.BTN_UnlockAll = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // Panel_AchievementList
            // 
            this.Panel_AchievementList.AutoScroll = true;
            this.Panel_AchievementList.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.Panel_AchievementList.Location = new System.Drawing.Point(0, 0);
            this.Panel_AchievementList.Margin = new System.Windows.Forms.Padding(0);
            this.Panel_AchievementList.Name = "Panel_AchievementList";
            this.Panel_AchievementList.Size = new System.Drawing.Size(800, 405);
            this.Panel_AchievementList.TabIndex = 0;
            // 
            // Header3
            // 
            this.Header3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.Header3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Header3.Location = new System.Drawing.Point(198, 0);
            this.Header3.Margin = new System.Windows.Forms.Padding(0);
            this.Header3.Name = "Header3";
            this.Header3.ReadOnly = true;
            this.Header3.Size = new System.Drawing.Size(293, 23);
            this.Header3.TabIndex = 5;
            this.Header3.Text = "Description";
            this.Header3.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // Header2
            // 
            this.Header2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.Header2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Header2.Location = new System.Drawing.Point(47, 0);
            this.Header2.Margin = new System.Windows.Forms.Padding(0);
            this.Header2.Name = "Header2";
            this.Header2.ReadOnly = true;
            this.Header2.Size = new System.Drawing.Size(151, 23);
            this.Header2.TabIndex = 3;
            this.Header2.Text = "Name";
            this.Header2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // Header1
            // 
            this.Header1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.Header1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Header1.Location = new System.Drawing.Point(0, 0);
            this.Header1.Margin = new System.Windows.Forms.Padding(0);
            this.Header1.Name = "Header1";
            this.Header1.ReadOnly = true;
            this.Header1.Size = new System.Drawing.Size(47, 23);
            this.Header1.TabIndex = 2;
            this.Header1.Text = "Unlock";
            this.Header1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // Header4
            // 
            this.Header4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.Header4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Header4.Location = new System.Drawing.Point(491, 0);
            this.Header4.Margin = new System.Windows.Forms.Padding(0);
            this.Header4.Name = "Header4";
            this.Header4.ReadOnly = true;
            this.Header4.Size = new System.Drawing.Size(292, 23);
            this.Header4.TabIndex = 4;
            this.Header4.Text = "Stats";
            this.Header4.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // BTN_Unlock
            // 
            this.BTN_Unlock.Location = new System.Drawing.Point(0, 405);
            this.BTN_Unlock.Margin = new System.Windows.Forms.Padding(0);
            this.BTN_Unlock.Name = "BTN_Unlock";
            this.BTN_Unlock.Size = new System.Drawing.Size(105, 46);
            this.BTN_Unlock.TabIndex = 6;
            this.BTN_Unlock.Text = "Unlock Selected Achievements";
            this.BTN_Unlock.UseVisualStyleBackColor = true;
            this.BTN_Unlock.Click += new System.EventHandler(this.BTN_Unlock_Click);
            // 
            // CheckBox_Images
            // 
            this.CheckBox_Images.AutoSize = true;
            this.CheckBox_Images.Enabled = false;
            this.CheckBox_Images.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.CheckBox_Images.Location = new System.Drawing.Point(435, 416);
            this.CheckBox_Images.Name = "CheckBox_Images";
            this.CheckBox_Images.Size = new System.Drawing.Size(365, 24);
            this.CheckBox_Images.TabIndex = 7;
            this.CheckBox_Images.Text = "Show images instead of stats (disabled until I fix it)";
            this.CheckBox_Images.UseVisualStyleBackColor = true;
            this.CheckBox_Images.CheckedChanged += new System.EventHandler(this.CheckBox_Images_CheckedChanged);
            // 
            // BTN_UnlockAll
            // 
            this.BTN_UnlockAll.Location = new System.Drawing.Point(105, 405);
            this.BTN_UnlockAll.Margin = new System.Windows.Forms.Padding(0);
            this.BTN_UnlockAll.Name = "BTN_UnlockAll";
            this.BTN_UnlockAll.Size = new System.Drawing.Size(105, 46);
            this.BTN_UnlockAll.TabIndex = 8;
            this.BTN_UnlockAll.Text = "Unlock All Achievements";
            this.BTN_UnlockAll.UseVisualStyleBackColor = true;
            this.BTN_UnlockAll.Click += new System.EventHandler(this.BTN_UnlockAll_Click);
            // 
            // AchievementList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.BTN_UnlockAll);
            this.Controls.Add(this.Header1);
            this.Controls.Add(this.CheckBox_Images);
            this.Controls.Add(this.BTN_Unlock);
            this.Controls.Add(this.Header3);
            this.Controls.Add(this.Header4);
            this.Controls.Add(this.Header2);
            this.Controls.Add(this.Panel_AchievementList);
            this.Name = "AchievementList";
            this.Text = "AchievementList";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel Panel_AchievementList;
        private System.Windows.Forms.TextBox Header1;
        private System.Windows.Forms.TextBox Header2;
        private System.Windows.Forms.TextBox Header4;
        private System.Windows.Forms.TextBox Header3;
        private System.Windows.Forms.Button BTN_Unlock;
        private System.Windows.Forms.CheckBox CheckBox_Images;
        private System.Windows.Forms.Button BTN_UnlockAll;
    }
}