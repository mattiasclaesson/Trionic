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
            this.label4 = new System.Windows.Forms.Label();
            this.tbE85 = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tbTopSpeed = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.tbVIN = new System.Windows.Forms.TextBox();
            this.tbRPMLimit = new System.Windows.Forms.TextBox();
            this.closeButton = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // cbCab
            // 
            this.cbCab.AutoSize = true;
            this.cbCab.Location = new System.Drawing.Point(114, 122);
            this.cbCab.Name = "cbCab";
            this.cbCab.Size = new System.Drawing.Size(79, 17);
            this.cbCab.TabIndex = 0;
            this.cbCab.Text = "Convertible";
            this.cbCab.UseVisualStyleBackColor = true;
            // 
            // cbSAI
            // 
            this.cbSAI.AutoSize = true;
            this.cbSAI.Location = new System.Drawing.Point(114, 145);
            this.cbSAI.Name = "cbSAI";
            this.cbSAI.Size = new System.Drawing.Size(43, 17);
            this.cbSAI.TabIndex = 1;
            this.cbSAI.Text = "SAI";
            this.cbSAI.UseVisualStyleBackColor = true;
            // 
            // cbOutput
            // 
            this.cbOutput.AutoSize = true;
            this.cbOutput.Location = new System.Drawing.Point(114, 169);
            this.cbOutput.Name = "cbOutput";
            this.cbOutput.Size = new System.Drawing.Size(81, 17);
            this.cbOutput.TabIndex = 2;
            this.cbOutput.Text = "High output";
            this.cbOutput.UseVisualStyleBackColor = true;
            // 
            // btnWriteToECU
            // 
            this.btnWriteToECU.Location = new System.Drawing.Point(88, 216);
            this.btnWriteToECU.Name = "btnWriteToECU";
            this.btnWriteToECU.Size = new System.Drawing.Size(121, 23);
            this.btnWriteToECU.TabIndex = 3;
            this.btnWriteToECU.Text = "Write Fields to ECU";
            this.btnWriteToECU.UseVisualStyleBackColor = true;
            this.btnWriteToECU.Click += new System.EventHandler(this.writeToECU_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.tbE85);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.tbTopSpeed);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.tbVIN);
            this.groupBox1.Controls.Add(this.tbRPMLimit);
            this.groupBox1.Controls.Add(this.cbCab);
            this.groupBox1.Controls.Add(this.cbSAI);
            this.groupBox1.Controls.Add(this.cbOutput);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(278, 198);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Fields";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(15, 73);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(34, 13);
            this.label4.TabIndex = 11;
            this.label4.Text = "E85%";
            // 
            // tbE85
            // 
            this.tbE85.Location = new System.Drawing.Point(114, 70);
            this.tbE85.Name = "tbE85";
            this.tbE85.Size = new System.Drawing.Size(144, 20);
            this.tbE85.TabIndex = 10;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 99);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(94, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Top Speed (km/h)";
            // 
            // tbTopSpeed
            // 
            this.tbTopSpeed.Location = new System.Drawing.Point(114, 96);
            this.tbTopSpeed.Name = "tbTopSpeed";
            this.tbTopSpeed.Size = new System.Drawing.Size(144, 20);
            this.tbTopSpeed.TabIndex = 8;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(99, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "VIN (17 characters)";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 47);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(55, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "RPM Limit";
            // 
            // tbVIN
            // 
            this.tbVIN.Location = new System.Drawing.Point(114, 18);
            this.tbVIN.Name = "tbVIN";
            this.tbVIN.Size = new System.Drawing.Size(144, 20);
            this.tbVIN.TabIndex = 6;
            // 
            // tbRPMLimit
            // 
            this.tbRPMLimit.Location = new System.Drawing.Point(114, 44);
            this.tbRPMLimit.Name = "tbRPMLimit";
            this.tbRPMLimit.Size = new System.Drawing.Size(144, 20);
            this.tbRPMLimit.TabIndex = 3;
            // 
            // closeButton
            // 
            this.closeButton.Location = new System.Drawing.Point(215, 216);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(75, 23);
            this.closeButton.TabIndex = 5;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // PiSelection
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(302, 247);
            this.Controls.Add(this.closeButton);
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
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.TextBox tbVIN;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbTopSpeed;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbRPMLimit;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbE85;
        private System.Windows.Forms.Label label3;
    }
}