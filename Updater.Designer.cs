namespace Xbox_Achievement_Unlocker
{
    partial class Updater
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
            this.BTN_Yes = new System.Windows.Forms.Button();
            this.BTN_No = new System.Windows.Forms.Button();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // BTN_Yes
            // 
            this.BTN_Yes.Location = new System.Drawing.Point(12, 51);
            this.BTN_Yes.Name = "BTN_Yes";
            this.BTN_Yes.Size = new System.Drawing.Size(75, 23);
            this.BTN_Yes.TabIndex = 0;
            this.BTN_Yes.Text = "Yes";
            this.BTN_Yes.UseVisualStyleBackColor = true;
            this.BTN_Yes.Click += new System.EventHandler(this.BTN_Yes_Click);
            // 
            // BTN_No
            // 
            this.BTN_No.Location = new System.Drawing.Point(122, 51);
            this.BTN_No.Name = "BTN_No";
            this.BTN_No.Size = new System.Drawing.Size(75, 23);
            this.BTN_No.TabIndex = 1;
            this.BTN_No.Text = "No";
            this.BTN_No.UseVisualStyleBackColor = true;
            this.BTN_No.Click += new System.EventHandler(this.BTN_No_Click);
            // 
            // richTextBox1
            // 
            this.richTextBox1.BackColor = System.Drawing.SystemColors.Control;
            this.richTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBox1.Location = new System.Drawing.Point(12, 12);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(188, 96);
            this.richTextBox1.TabIndex = 2;
            this.richTextBox1.Text = "There is a new version avaliable.\nWould you like to download?";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(12, 80);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(185, 23);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar1.TabIndex = 3;
            // 
            // Updater
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(209, 115);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.BTN_No);
            this.Controls.Add(this.BTN_Yes);
            this.Controls.Add(this.richTextBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Updater";
            this.Text = "Updater";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button BTN_Yes;
        private System.Windows.Forms.Button BTN_No;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.ProgressBar progressBar1;
    }
}