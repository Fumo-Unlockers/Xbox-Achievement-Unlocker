
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
            AchievementList.Panel_AchievementList = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // Panel_AchievementList
            // 
            AchievementList.Panel_AchievementList.Location = new System.Drawing.Point(0, 0);
            AchievementList.Panel_AchievementList.Name = "Panel_AchievementList";
            AchievementList.Panel_AchievementList.Size = new System.Drawing.Size(775, 450);
            AchievementList.Panel_AchievementList.TabIndex = 0;
            // 
            // AchievementList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(AchievementList.Panel_AchievementList);
            this.Name = "AchievementList";
            this.Text = "AchievementList";
            this.ResumeLayout(false);

        }

        #endregion

        public static System.Windows.Forms.Panel Panel_AchievementList;
    }
}