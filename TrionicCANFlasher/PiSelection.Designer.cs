namespace TrionicCANFlasher
{
    partial class PiSelection
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
            this.cbCab = new System.Windows.Forms.CheckBox();
            this.cbSAI = new System.Windows.Forms.CheckBox();
            this.cbOutput = new System.Windows.Forms.CheckBox();
            this.btnWriteToECU = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // cbCab
            // 
            this.cbCab.AutoSize = true;
            this.cbCab.Location = new System.Drawing.Point(6, 19);
            this.cbCab.Name = "cbCab";
            this.cbCab.Size = new System.Drawing.Size(45, 17);
            this.cbCab.TabIndex = 0;
            this.cbCab.Text = "Cab";
            this.cbCab.UseVisualStyleBackColor = true;
            // 
            // cbSAI
            // 
            this.cbSAI.AutoSize = true;
            this.cbSAI.Location = new System.Drawing.Point(6, 42);
            this.cbSAI.Name = "cbSAI";
            this.cbSAI.Size = new System.Drawing.Size(43, 17);
            this.cbSAI.TabIndex = 1;
            this.cbSAI.Text = "SAI";
            this.cbSAI.UseVisualStyleBackColor = true;
            // 
            // cbOutput
            // 
            this.cbOutput.AutoSize = true;
            this.cbOutput.Location = new System.Drawing.Point(6, 66);
            this.cbOutput.Name = "cbOutput";
            this.cbOutput.Size = new System.Drawing.Size(81, 17);
            this.cbOutput.TabIndex = 2;
            this.cbOutput.Text = "High output";
            this.cbOutput.UseVisualStyleBackColor = true;
            // 
            // btnWriteToECU
            // 
            this.btnWriteToECU.Location = new System.Drawing.Point(101, 121);
            this.btnWriteToECU.Name = "btnWriteToECU";
            this.btnWriteToECU.Size = new System.Drawing.Size(121, 23);
            this.btnWriteToECU.TabIndex = 3;
            this.btnWriteToECU.Text = "Write Fields to ECU";
            this.btnWriteToECU.UseVisualStyleBackColor = true;
            this.btnWriteToECU.Click += new System.EventHandler(this.writeToECU_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.cbCab);
            this.groupBox1.Controls.Add(this.cbSAI);
            this.groupBox1.Controls.Add(this.cbOutput);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(98, 89);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Fields";
            // 
            // PiSelection
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(234, 156);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnWriteToECU);
            this.Name = "PiSelection";
            this.Text = "ProgramInformation";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckBox cbCab;
        private System.Windows.Forms.CheckBox cbSAI;
        private System.Windows.Forms.CheckBox cbOutput;
        private System.Windows.Forms.Button btnWriteToECU;
        private System.Windows.Forms.GroupBox groupBox1;
    }
}