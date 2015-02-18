namespace TrionicCANFlasher
{
    partial class frmMain
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.btnReadECU = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.cbEnableLogging = new System.Windows.Forms.CheckBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.btnExit = new System.Windows.Forms.Button();
            this.btnFlashECU = new System.Windows.Forms.Button();
            this.listBoxLog = new System.Windows.Forms.ListBox();
            this.btnGetECUInfo = new System.Windows.Forms.Button();
            this.btnReadSRAM = new System.Windows.Forms.Button();
            this.btnRecoverECU = new System.Windows.Forms.Button();
            this.cbxAdapterType = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.cbxComPort = new System.Windows.Forms.ComboBox();
            this.btnReadDTC = new System.Windows.Forms.Button();
            this.btnSetECUVIN = new System.Windows.Forms.Button();
            this.tbParameter = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.btnSetE85 = new System.Windows.Forms.Button();
            this.btnSetSpeed = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.cbxEcuType = new System.Windows.Forms.ComboBox();
            this.cbOnlyPBus = new System.Windows.Forms.CheckBox();
            this.cbDisableConnectionCheck = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.cbxComSpeed = new System.Windows.Forms.ComboBox();
            this.documentation = new System.Windows.Forms.LinkLabel();
            this.cbELM327Kline = new System.Windows.Forms.CheckBox();
            this.btnCabSAIHo = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnReadECU
            // 
            this.btnReadECU.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnReadECU.Location = new System.Drawing.Point(909, 11);
            this.btnReadECU.Name = "btnReadECU";
            this.btnReadECU.Size = new System.Drawing.Size(107, 50);
            this.btnReadECU.TabIndex = 29;
            this.btnReadECU.Text = "Read ECU";
            this.btnReadECU.UseVisualStyleBackColor = true;
            this.btnReadECU.Click += new System.EventHandler(this.btnReadECU_Click);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(531, 341);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(0, 13);
            this.label1.TabIndex = 25;
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cbEnableLogging
            // 
            this.cbEnableLogging.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbEnableLogging.AutoSize = true;
            this.cbEnableLogging.Checked = true;
            this.cbEnableLogging.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbEnableLogging.Location = new System.Drawing.Point(866, 301);
            this.cbEnableLogging.Name = "cbEnableLogging";
            this.cbEnableLogging.Size = new System.Drawing.Size(96, 17);
            this.cbEnableLogging.TabIndex = 24;
            this.cbEnableLogging.Text = "Enable logging";
            this.cbEnableLogging.UseVisualStyleBackColor = true;
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar1.Location = new System.Drawing.Point(12, 357);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(544, 23);
            this.progressBar1.TabIndex = 23;
            // 
            // btnExit
            // 
            this.btnExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExit.Location = new System.Drawing.Point(909, 123);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(107, 50);
            this.btnExit.TabIndex = 22;
            this.btnExit.Text = "Exit";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // btnFlashECU
            // 
            this.btnFlashECU.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFlashECU.Location = new System.Drawing.Point(791, 11);
            this.btnFlashECU.Name = "btnFlashECU";
            this.btnFlashECU.Size = new System.Drawing.Size(112, 50);
            this.btnFlashECU.TabIndex = 21;
            this.btnFlashECU.Text = "Flash ECU";
            this.btnFlashECU.UseVisualStyleBackColor = true;
            this.btnFlashECU.Click += new System.EventHandler(this.btnFlashEcu_Click);
            // 
            // listBoxLog
            // 
            this.listBoxLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxLog.Font = new System.Drawing.Font("Courier New", 8.25F);
            this.listBoxLog.FormattingEnabled = true;
            this.listBoxLog.ItemHeight = 14;
            this.listBoxLog.Location = new System.Drawing.Point(12, 11);
            this.listBoxLog.Name = "listBoxLog";
            this.listBoxLog.Size = new System.Drawing.Size(544, 312);
            this.listBoxLog.TabIndex = 20;
            // 
            // btnGetECUInfo
            // 
            this.btnGetECUInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnGetECUInfo.Location = new System.Drawing.Point(791, 67);
            this.btnGetECUInfo.Name = "btnGetECUInfo";
            this.btnGetECUInfo.Size = new System.Drawing.Size(112, 50);
            this.btnGetECUInfo.TabIndex = 30;
            this.btnGetECUInfo.Text = "Get ECU info";
            this.btnGetECUInfo.UseVisualStyleBackColor = true;
            this.btnGetECUInfo.Click += new System.EventHandler(this.btnGetEcuInfo_Click);
            // 
            // btnReadSRAM
            // 
            this.btnReadSRAM.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnReadSRAM.Location = new System.Drawing.Point(909, 67);
            this.btnReadSRAM.Name = "btnReadSRAM";
            this.btnReadSRAM.Size = new System.Drawing.Size(107, 50);
            this.btnReadSRAM.TabIndex = 31;
            this.btnReadSRAM.Text = "Read SRAM";
            this.btnReadSRAM.UseVisualStyleBackColor = true;
            this.btnReadSRAM.Click += new System.EventHandler(this.btnReadSRAM_Click);
            // 
            // btnRecoverECU
            // 
            this.btnRecoverECU.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRecoverECU.Location = new System.Drawing.Point(791, 123);
            this.btnRecoverECU.Name = "btnRecoverECU";
            this.btnRecoverECU.Size = new System.Drawing.Size(112, 50);
            this.btnRecoverECU.TabIndex = 32;
            this.btnRecoverECU.Text = "Recover ECU";
            this.btnRecoverECU.UseVisualStyleBackColor = true;
            this.btnRecoverECU.Click += new System.EventHandler(this.btnRecoverECU_Click);
            // 
            // cbxAdapterType
            // 
            this.cbxAdapterType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxAdapterType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxAdapterType.FormattingEnabled = true;
            this.cbxAdapterType.Items.AddRange(new object[] {
            "Lawicel CANUSB",
            "CombiAdapter",
            "ELM327 v1.3 or higher",
            "Just4Trionic"});
            this.cbxAdapterType.Location = new System.Drawing.Point(866, 220);
            this.cbxAdapterType.Name = "cbxAdapterType";
            this.cbxAdapterType.Size = new System.Drawing.Size(150, 21);
            this.cbxAdapterType.TabIndex = 33;
            this.cbxAdapterType.SelectedIndexChanged += new System.EventHandler(this.cbxAdapterType_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(793, 223);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(67, 13);
            this.label4.TabIndex = 34;
            this.label4.Text = "Adapter type";
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(793, 250);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(49, 13);
            this.label5.TabIndex = 36;
            this.label5.Text = "Com port";
            // 
            // cbxComPort
            // 
            this.cbxComPort.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxComPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxComPort.FormattingEnabled = true;
            this.cbxComPort.Location = new System.Drawing.Point(866, 247);
            this.cbxComPort.Name = "cbxComPort";
            this.cbxComPort.Size = new System.Drawing.Size(150, 21);
            this.cbxComPort.TabIndex = 35;
            // 
            // btnReadDTC
            // 
            this.btnReadDTC.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnReadDTC.Location = new System.Drawing.Point(678, 12);
            this.btnReadDTC.Name = "btnReadDTC";
            this.btnReadDTC.Size = new System.Drawing.Size(107, 50);
            this.btnReadDTC.TabIndex = 39;
            this.btnReadDTC.Text = "Read ECU DTC";
            this.btnReadDTC.UseVisualStyleBackColor = true;
            this.btnReadDTC.Click += new System.EventHandler(this.btnReadDTC_Click);
            // 
            // btnSetECUVIN
            // 
            this.btnSetECUVIN.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSetECUVIN.Location = new System.Drawing.Point(565, 67);
            this.btnSetECUVIN.Name = "btnSetECUVIN";
            this.btnSetECUVIN.Size = new System.Drawing.Size(107, 50);
            this.btnSetECUVIN.TabIndex = 46;
            this.btnSetECUVIN.Text = "Set ECU VIN";
            this.btnSetECUVIN.UseVisualStyleBackColor = true;
            this.btnSetECUVIN.Click += new System.EventHandler(this.btnSetECUVIN_Click);
            // 
            // tbParameter
            // 
            this.tbParameter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.tbParameter.Location = new System.Drawing.Point(642, 194);
            this.tbParameter.Name = "tbParameter";
            this.tbParameter.Size = new System.Drawing.Size(143, 20);
            this.tbParameter.TabIndex = 47;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(562, 197);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(74, 13);
            this.label3.TabIndex = 48;
            this.label3.Text = "Set Parameter";
            // 
            // btnSetE85
            // 
            this.btnSetE85.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSetE85.Location = new System.Drawing.Point(565, 12);
            this.btnSetE85.Name = "btnSetE85";
            this.btnSetE85.Size = new System.Drawing.Size(107, 50);
            this.btnSetE85.TabIndex = 52;
            this.btnSetE85.Text = "Set E85 percent";
            this.btnSetE85.UseVisualStyleBackColor = true;
            this.btnSetE85.Click += new System.EventHandler(this.btnSetE85_Click);
            // 
            // btnSetSpeed
            // 
            this.btnSetSpeed.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSetSpeed.Location = new System.Drawing.Point(565, 124);
            this.btnSetSpeed.Name = "btnSetSpeed";
            this.btnSetSpeed.Size = new System.Drawing.Size(107, 50);
            this.btnSetSpeed.TabIndex = 53;
            this.btnSetSpeed.Text = "Set speed limiter";
            this.btnSetSpeed.UseVisualStyleBackColor = true;
            this.btnSetSpeed.Click += new System.EventHandler(this.btnSetSpeed_Click);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(793, 196);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(56, 13);
            this.label2.TabIndex = 55;
            this.label2.Text = "ECU Type";
            // 
            // cbxEcuType
            // 
            this.cbxEcuType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxEcuType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxEcuType.FormattingEnabled = true;
            this.cbxEcuType.Items.AddRange(new object[] {
            "Trionic 7",
            "Trionic 8",
            "Motronic 9.6"});
            this.cbxEcuType.Location = new System.Drawing.Point(866, 193);
            this.cbxEcuType.Name = "cbxEcuType";
            this.cbxEcuType.Size = new System.Drawing.Size(150, 21);
            this.cbxEcuType.TabIndex = 56;
            this.cbxEcuType.SelectedIndexChanged += new System.EventHandler(this.cbxEcuType_SelectedIndexChanged);
            // 
            // cbOnlyPBus
            // 
            this.cbOnlyPBus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbOnlyPBus.AutoSize = true;
            this.cbOnlyPBus.Checked = true;
            this.cbOnlyPBus.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbOnlyPBus.Location = new System.Drawing.Point(866, 324);
            this.cbOnlyPBus.Name = "cbOnlyPBus";
            this.cbOnlyPBus.Size = new System.Drawing.Size(134, 17);
            this.cbOnlyPBus.TabIndex = 57;
            this.cbOnlyPBus.Text = "Only P-Bus connection";
            this.cbOnlyPBus.UseVisualStyleBackColor = true;
            // 
            // cbDisableConnectionCheck
            // 
            this.cbDisableConnectionCheck.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbDisableConnectionCheck.AutoSize = true;
            this.cbDisableConnectionCheck.Checked = true;
            this.cbDisableConnectionCheck.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbDisableConnectionCheck.Location = new System.Drawing.Point(866, 347);
            this.cbDisableConnectionCheck.Name = "cbDisableConnectionCheck";
            this.cbDisableConnectionCheck.Size = new System.Drawing.Size(150, 17);
            this.cbDisableConnectionCheck.TabIndex = 58;
            this.cbDisableConnectionCheck.Text = "Disable connection check";
            this.cbDisableConnectionCheck.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(793, 277);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(60, 13);
            this.label6.TabIndex = 60;
            this.label6.Text = "Com speed";
            // 
            // cbxComSpeed
            // 
            this.cbxComSpeed.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxComSpeed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxComSpeed.FormattingEnabled = true;
            this.cbxComSpeed.Items.AddRange(new object[] {
            "115200",
            "230400",
            "1Mbit",
            "2Mbit"});
            this.cbxComSpeed.Location = new System.Drawing.Point(866, 274);
            this.cbxComSpeed.Name = "cbxComSpeed";
            this.cbxComSpeed.Size = new System.Drawing.Size(150, 21);
            this.cbxComSpeed.TabIndex = 59;
            // 
            // documentation
            // 
            this.documentation.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.documentation.AutoSize = true;
            this.documentation.Location = new System.Drawing.Point(675, 370);
            this.documentation.Name = "documentation";
            this.documentation.Size = new System.Drawing.Size(103, 13);
            this.documentation.TabIndex = 61;
            this.documentation.TabStop = true;
            this.documentation.Text = "View documentation";
            this.documentation.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.documentation_LinkClicked);
            // 
            // cbELM327Kline
            // 
            this.cbELM327Kline.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbELM327Kline.AutoSize = true;
            this.cbELM327Kline.Location = new System.Drawing.Point(866, 370);
            this.cbELM327Kline.Name = "cbELM327Kline";
            this.cbELM327Kline.Size = new System.Drawing.Size(99, 17);
            this.cbELM327Kline.TabIndex = 62;
            this.cbELM327Kline.Text = "ELM327 K-Line";
            this.cbELM327Kline.UseVisualStyleBackColor = true;
            // 
            // btnCabSAIHo
            // 
            this.btnCabSAIHo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCabSAIHo.Location = new System.Drawing.Point(678, 67);
            this.btnCabSAIHo.Name = "btnCabSAIHo";
            this.btnCabSAIHo.Size = new System.Drawing.Size(107, 50);
            this.btnCabSAIHo.TabIndex = 63;
            this.btnCabSAIHo.Text = "Set Cab SAI HO";
            this.btnCabSAIHo.UseVisualStyleBackColor = true;
            this.btnCabSAIHo.Click += new System.EventHandler(this.btnCabSAIHo_Click);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1026, 392);
            this.Controls.Add(this.btnCabSAIHo);
            this.Controls.Add(this.cbELM327Kline);
            this.Controls.Add(this.documentation);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.cbxComSpeed);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cbDisableConnectionCheck);
            this.Controls.Add(this.cbOnlyPBus);
            this.Controls.Add(this.cbxEcuType);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnSetSpeed);
            this.Controls.Add(this.btnSetE85);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tbParameter);
            this.Controls.Add(this.btnSetECUVIN);
            this.Controls.Add(this.btnReadDTC);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.cbxComPort);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.cbxAdapterType);
            this.Controls.Add(this.btnRecoverECU);
            this.Controls.Add(this.btnReadSRAM);
            this.Controls.Add(this.btnGetECUInfo);
            this.Controls.Add(this.btnReadECU);
            this.Controls.Add(this.cbEnableLogging);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.btnFlashECU);
            this.Controls.Add(this.listBoxLog);
            this.Controls.Add(this.progressBar1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Trionic CAN flasher";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.Shown += new System.EventHandler(this.frmMain_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnReadECU;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox cbEnableLogging;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Button btnFlashECU;
        private System.Windows.Forms.ListBox listBoxLog;
        private System.Windows.Forms.Button btnGetECUInfo;
        private System.Windows.Forms.Button btnReadSRAM;
        private System.Windows.Forms.Button btnRecoverECU;
        private System.Windows.Forms.ComboBox cbxAdapterType;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cbxComPort;
        private System.Windows.Forms.Button btnReadDTC;
        private System.Windows.Forms.Button btnSetECUVIN;
        private System.Windows.Forms.TextBox tbParameter;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnSetE85;
        private System.Windows.Forms.Button btnSetSpeed;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbxEcuType;
        private System.Windows.Forms.CheckBox cbOnlyPBus;
        private System.Windows.Forms.CheckBox cbDisableConnectionCheck;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cbxComSpeed;
        private System.Windows.Forms.LinkLabel documentation;
        private System.Windows.Forms.CheckBox cbELM327Kline;
        private System.Windows.Forms.Button btnCabSAIHo;
    }
}

