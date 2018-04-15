namespace TrionicCANFlasher
{
    partial class EditParameters
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditParameters));
            this.cbCab = new System.Windows.Forms.CheckBox();
            this.cbSAI = new System.Windows.Forms.CheckBox();
            this.cbOutput = new System.Windows.Forms.CheckBox();
            this.btnWriteToECU = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label7 = new System.Windows.Forms.Label();
            this.comboBoxTank = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.comboBoxDiag = new System.Windows.Forms.ComboBox();
            this.cbClutchStart = new System.Windows.Forms.CheckBox();
            this.cbBiopower = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tbOilQuality = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tbE85 = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tbTopSpeed = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tbVIN = new System.Windows.Forms.TextBox();
            this.closeButton = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // cbCab
            // 
            this.cbCab.AutoSize = true;
            this.cbCab.Location = new System.Drawing.Point(114, 176);
            this.cbCab.Name = "cbCab";
            this.cbCab.Size = new System.Drawing.Size(79, 17);
            this.cbCab.TabIndex = 0;
            this.cbCab.Text = "Convertible";
            this.cbCab.UseVisualStyleBackColor = true;
            // 
            // cbSAI
            // 
            this.cbSAI.AutoSize = true;
            this.cbSAI.Location = new System.Drawing.Point(114, 199);
            this.cbSAI.Name = "cbSAI";
            this.cbSAI.Size = new System.Drawing.Size(43, 17);
            this.cbSAI.TabIndex = 1;
            this.cbSAI.Text = "SAI";
            this.cbSAI.UseVisualStyleBackColor = true;
            // 
            // cbOutput
            // 
            this.cbOutput.AutoSize = true;
            this.cbOutput.Location = new System.Drawing.Point(114, 223);
            this.cbOutput.Name = "cbOutput";
            this.cbOutput.Size = new System.Drawing.Size(81, 17);
            this.cbOutput.TabIndex = 2;
            this.cbOutput.Text = "High output";
            this.cbOutput.UseVisualStyleBackColor = true;
            // 
            // btnWriteToECU
            // 
            this.btnWriteToECU.Location = new System.Drawing.Point(88, 312);
            this.btnWriteToECU.Name = "btnWriteToECU";
            this.btnWriteToECU.Size = new System.Drawing.Size(121, 23);
            this.btnWriteToECU.TabIndex = 3;
            this.btnWriteToECU.Text = "Write Fields to ECU";
            this.btnWriteToECU.UseVisualStyleBackColor = true;
            this.btnWriteToECU.Click += new System.EventHandler(this.btnWriteToECU_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.comboBoxTank);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.comboBoxDiag);
            this.groupBox1.Controls.Add(this.cbClutchStart);
            this.groupBox1.Controls.Add(this.cbBiopower);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.tbOilQuality);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.tbE85);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.tbTopSpeed);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.tbVIN);
            this.groupBox1.Controls.Add(this.cbCab);
            this.groupBox1.Controls.Add(this.cbSAI);
            this.groupBox1.Controls.Add(this.cbOutput);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(278, 294);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Fields";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(15, 152);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(59, 13);
            this.label7.TabIndex = 19;
            this.label7.Text = "Tank Type";
            // 
            // comboBoxTank
            // 
            this.comboBoxTank.FormattingEnabled = true;
            this.comboBoxTank.Items.AddRange(new object[] {
            "US",
            "EU",
            "AWD"});
            this.comboBoxTank.Location = new System.Drawing.Point(114, 149);
            this.comboBoxTank.Name = "comboBoxTank";
            this.comboBoxTank.Size = new System.Drawing.Size(144, 21);
            this.comboBoxTank.TabIndex = 18;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(15, 125);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(84, 13);
            this.label6.TabIndex = 17;
            this.label6.Text = "Diagnostic Type";
            // 
            // comboBoxDiag
            // 
            this.comboBoxDiag.FormattingEnabled = true;
            this.comboBoxDiag.Items.AddRange(new object[] {
            "None",
            "OBD2",
            "EOBD",
            "LOBD"});
            this.comboBoxDiag.Location = new System.Drawing.Point(114, 122);
            this.comboBoxDiag.Name = "comboBoxDiag";
            this.comboBoxDiag.Size = new System.Drawing.Size(144, 21);
            this.comboBoxDiag.TabIndex = 16;
            // 
            // cbClutchStart
            // 
            this.cbClutchStart.AutoSize = true;
            this.cbClutchStart.Location = new System.Drawing.Point(114, 269);
            this.cbClutchStart.Name = "cbClutchStart";
            this.cbClutchStart.Size = new System.Drawing.Size(81, 17);
            this.cbClutchStart.TabIndex = 15;
            this.cbClutchStart.Text = "Clutch Start";
            this.cbClutchStart.UseVisualStyleBackColor = true;
            // 
            // cbBiopower
            // 
            this.cbBiopower.AutoSize = true;
            this.cbBiopower.Location = new System.Drawing.Point(114, 246);
            this.cbBiopower.Name = "cbBiopower";
            this.cbBiopower.Size = new System.Drawing.Size(70, 17);
            this.cbBiopower.TabIndex = 14;
            this.cbBiopower.Text = "Biopower";
            this.cbBiopower.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(15, 99);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(62, 13);
            this.label5.TabIndex = 13;
            this.label5.Text = "Oil Quality%";
            // 
            // tbOilQuality
            // 
            this.tbOilQuality.Location = new System.Drawing.Point(114, 96);
            this.tbOilQuality.Name = "tbOilQuality";
            this.tbOilQuality.Size = new System.Drawing.Size(144, 20);
            this.tbOilQuality.TabIndex = 12;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(15, 47);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(34, 13);
            this.label4.TabIndex = 11;
            this.label4.Text = "E85%";
            // 
            // tbE85
            // 
            this.tbE85.Location = new System.Drawing.Point(114, 44);
            this.tbE85.Name = "tbE85";
            this.tbE85.Size = new System.Drawing.Size(144, 20);
            this.tbE85.TabIndex = 10;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 73);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(94, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Top Speed (km/h)";
            // 
            // tbTopSpeed
            // 
            this.tbTopSpeed.Location = new System.Drawing.Point(114, 70);
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
            // tbVIN
            // 
            this.tbVIN.Location = new System.Drawing.Point(114, 18);
            this.tbVIN.Name = "tbVIN";
            this.tbVIN.Size = new System.Drawing.Size(144, 20);
            this.tbVIN.TabIndex = 6;
            // 
            // closeButton
            // 
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeButton.Location = new System.Drawing.Point(215, 312);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(75, 23);
            this.closeButton.TabIndex = 5;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // EditParameters
            // 
            this.AcceptButton = this.btnWriteToECU;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(303, 346);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnWriteToECU);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "EditParameters";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Edit Parameters";
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
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbE85;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbOilQuality;
        private System.Windows.Forms.CheckBox cbBiopower;
        private System.Windows.Forms.CheckBox cbClutchStart;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox comboBoxTank;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox comboBoxDiag;
    }
}