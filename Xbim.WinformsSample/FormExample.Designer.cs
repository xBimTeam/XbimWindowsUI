namespace Xbim.WinformsSample
{
    partial class FormExample
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.controlHost = new System.Windows.Forms.Integration.ElementHost();
            this.panel1 = new System.Windows.Forms.Panel();
            this.button2 = new System.Windows.Forms.Button();
            this.txtEntityLabel = new System.Windows.Forms.Label();
            this.button3 = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(112, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Load Bim file";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // controlHost
            // 
            this.controlHost.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.controlHost.Location = new System.Drawing.Point(130, 12);
            this.controlHost.Name = "controlHost";
            this.controlHost.Size = new System.Drawing.Size(545, 533);
            this.controlHost.TabIndex = 1;
            this.controlHost.Text = "elementHost1";
            this.controlHost.Child = null;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.button3);
            this.panel1.Controls.Add(this.button2);
            this.panel1.Controls.Add(this.txtEntityLabel);
            this.panel1.Controls.Add(this.button1);
            this.panel1.Controls.Add(this.controlHost);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(687, 557);
            this.panel1.TabIndex = 2;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(12, 71);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(112, 23);
            this.button2.TabIndex = 3;
            this.button2.Text = "Select Next Enity";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // txtEntityLabel
            // 
            this.txtEntityLabel.AutoSize = true;
            this.txtEntityLabel.Location = new System.Drawing.Point(12, 38);
            this.txtEntityLabel.Name = "txtEntityLabel";
            this.txtEntityLabel.Size = new System.Drawing.Size(33, 13);
            this.txtEntityLabel.TabIndex = 2;
            this.txtEntityLabel.Text = "Entity";
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(12, 100);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(112, 23);
            this.button3.TabIndex = 4;
            this.button3.Text = "Select Walls";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // FormExample
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(687, 557);
            this.Controls.Add(this.panel1);
            this.Name = "FormExample";
            this.Text = "Embedding viewer in winform";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Integration.ElementHost controlHost;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label txtEntityLabel;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
    }
}
