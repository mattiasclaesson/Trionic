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
            this.cbEnableLogging = new System.Windows.Forms.CheckBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.btnRestoreT8 = new System.Windows.Forms.Button();
            this.btnFlashECU = new System.Windows.Forms.Button();
            this.listBoxLog = new System.Windows.Forms.ListBox();
            this.btnGetECUInfo = new System.Windows.Forms.Button();
            this.btnReadSRAM = new System.Windows.Forms.Button();
            this.btnRecoverECU = new System.Windows.Forms.Button();
            this.cbxAdapterType = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.btnReadDTC = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.cbxEcuType = new System.Windows.Forms.ComboBox();
            this.cbOnlyPBus = new System.Windows.Forms.CheckBox();
            this.cbDisableConnectionCheck = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.cbxComSpeed = new System.Windows.Forms.ComboBox();
            this.documentation = new System.Windows.Forms.LinkLabel();
            this.cbELM327Kline = new System.Windows.Forms.CheckBox();
            this.btnEditParameters = new System.Windows.Forms.Button();
            this.btnReadECUcalibration = new System.Windows.Forms.Button();
            this.cbAdapter = new System.Windows.Forms.ComboBox();
            this.cbUseFlasherOnDevice = new System.Windows.Forms.CheckBox();
            this.linkLabelLogging = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // btnReadECU
            // 
            this.btnReadECU.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnReadECU.Location = new System.Drawing.Point(906, 11);
            this.btnReadECU.Name = "btnReadECU";
            this.btnReadECU.Size = new System.Drawing.Size(107, 50);
            this.btnReadECU.TabIndex = 29;
            this.btnReadECU.Text = "Read ECU";
            this.btnReadECU.UseVisualStyleBackColor = true;
            this.btnReadECU.Click += new System.EventHandler(this.btnReadECU_Click);
            // 
            // cbEnableLogging
            // 
            this.cbEnableLogging.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbEnableLogging.AutoSize = true;
            this.cbEnableLogging.Checked = true;
            this.cbEnableLogging.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbEnableLogging.Location = new System.Drawing.Point(863, 301);
            this.cbEnableLogging.Name = "cbEnableLogging";
            this.cbEnableLogging.Size = new System.Drawing.Size(96, 17);
            this.cbEnableLogging.TabIndex = 24;
            this.cbEnableLogging.Text = "Enable logging";
            this.cbEnableLogging.UseVisualStyleBackColor = true;
            this.cbEnableLogging.CheckedChanged += new System.EventHandler(this.cbEnableLogging_CheckedChanged);
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar1.Location = new System.Drawing.Point(12, 357);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(648, 23);
            this.progressBar1.TabIndex = 23;
            // 
            // btnRestoreT8
            // 
            this.btnRestoreT8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRestoreT8.Location = new System.Drawing.Point(906, 123);
            this.btnRestoreT8.Name = "btnRestoreT8";
            this.btnRestoreT8.Size = new System.Drawing.Size(107, 50);
            this.btnRestoreT8.TabIndex = 22;
            this.btnRestoreT8.Text = "Restore T8";
            this.btnRestoreT8.UseVisualStyleBackColor = true;
            this.btnRestoreT8.Click += new System.EventHandler(this.btnRestoreT8_Click);
            // 
            // btnFlashECU
            // 
            this.btnFlashECU.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFlashECU.Location = new System.Drawing.Point(788, 11);
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
            this.listBoxLog.Location = new System.Drawing.Point(12, 6);
            this.listBoxLog.Name = "listBoxLog";
            this.listBoxLog.Size = new System.Drawing.Size(648, 340);
            this.listBoxLog.TabIndex = 20;
            // 
            // btnGetECUInfo
            // 
            this.btnGetECUInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnGetECUInfo.Location = new System.Drawing.Point(788, 67);
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
            this.btnReadSRAM.Location = new System.Drawing.Point(906, 67);
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
            this.btnRecoverECU.Location = new System.Drawing.Point(788, 123);
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
            "Just4Trionic",
            "Kvaser",
            "OBDLink MX WiFi"});
            this.cbxAdapterType.Location = new System.Drawing.Point(863, 220);
            this.cbxAdapterType.Name = "cbxAdapterType";
            this.cbxAdapterType.Size = new System.Drawing.Size(150, 21);
            this.cbxAdapterType.TabIndex = 33;
            this.cbxAdapterType.SelectedIndexChanged += new System.EventHandler(this.cbxAdapterType_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(790, 223);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(67, 13);
            this.label4.TabIndex = 34;
            this.label4.Text = "Adapter type";
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(790, 250);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(44, 13);
            this.label5.TabIndex = 36;
            this.label5.Text = "Adapter";
            // 
            // btnReadDTC
            // 
            this.btnReadDTC.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnReadDTC.Location = new System.Drawing.Point(675, 12);
            this.btnReadDTC.Name = "btnReadDTC";
            this.btnReadDTC.Size = new System.Drawing.Size(107, 50);
            this.btnReadDTC.TabIndex = 39;
            this.btnReadDTC.Text = "Read ECU DTC";
            this.btnReadDTC.UseVisualStyleBackColor = true;
            this.btnReadDTC.Click += new System.EventHandler(this.btnReadDTC_Click);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(790, 196);
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
            this.cbxEcuType.Location = new System.Drawing.Point(863, 193);
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
            this.cbOnlyPBus.Location = new System.Drawing.Point(863, 324);
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
            this.cbDisableConnectionCheck.Location = new System.Drawing.Point(863, 347);
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
            this.label6.Location = new System.Drawing.Point(790, 277);
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
            this.cbxComSpeed.Location = new System.Drawing.Point(863, 274);
            this.cbxComSpeed.Name = "cbxComSpeed";
            this.cbxComSpeed.Size = new System.Drawing.Size(150, 21);
            this.cbxComSpeed.TabIndex = 59;
            // 
            // documentation
            // 
            this.documentation.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.documentation.AutoSize = true;
            this.documentation.Location = new System.Drawing.Point(718, 325);
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
            this.cbELM327Kline.Location = new System.Drawing.Point(863, 370);
            this.cbELM327Kline.Name = "cbELM327Kline";
            this.cbELM327Kline.Size = new System.Drawing.Size(99, 17);
            this.cbELM327Kline.TabIndex = 62;
            this.cbELM327Kline.Text = "ELM327 K-Line";
            this.cbELM327Kline.UseVisualStyleBackColor = true;
            this.cbELM327Kline.Visible = false;
            // 
            // btnEditParameters
            // 
            this.btnEditParameters.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnEditParameters.Location = new System.Drawing.Point(675, 67);
            this.btnEditParameters.Name = "btnEditParameters";
            this.btnEditParameters.Size = new System.Drawing.Size(107, 50);
            this.btnEditParameters.TabIndex = 63;
            this.btnEditParameters.Text = "Edit Parameters";
            this.btnEditParameters.UseVisualStyleBackColor = true;
            this.btnEditParameters.Click += new System.EventHandler(this.btnEditParameters_Click);
            // 
            // btnReadECUcalibration
            // 
            this.btnReadECUcalibration.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnReadECUcalibration.Location = new System.Drawing.Point(675, 124);
            this.btnReadECUcalibration.Name = "btnReadECUcalibration";
            this.btnReadECUcalibration.Size = new System.Drawing.Size(107, 50);
            this.btnReadECUcalibration.TabIndex = 64;
            this.btnReadECUcalibration.Text = "Read ECU calibration";
            this.btnReadECUcalibration.UseVisualStyleBackColor = true;
            this.btnReadECUcalibration.Click += new System.EventHandler(this.btnReadECUcalibration_Click);
            // 
            // cbAdapter
            // 
            this.cbAdapter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbAdapter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbAdapter.FormattingEnabled = true;
            this.cbAdapter.Location = new System.Drawing.Point(863, 247);
            this.cbAdapter.Name = "cbAdapter";
            this.cbAdapter.Size = new System.Drawing.Size(150, 21);
            this.cbAdapter.TabIndex = 65;
            // 
            // cbUseFlasherOnDevice
            // 
            this.cbUseFlasherOnDevice.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbUseFlasherOnDevice.AutoSize = true;
            this.cbUseFlasherOnDevice.Checked = true;
            this.cbUseFlasherOnDevice.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbUseFlasherOnDevice.Location = new System.Drawing.Point(721, 301);
            this.cbUseFlasherOnDevice.Name = "cbUseFlasherOnDevice";
            this.cbUseFlasherOnDevice.Size = new System.Drawing.Size(129, 17);
            this.cbUseFlasherOnDevice.TabIndex = 66;
            this.cbUseFlasherOnDevice.Text = "Use flasher on device";
            this.cbUseFlasherOnDevice.UseVisualStyleBackColor = true;
            // 
            // linkLabelLogging
            // 
            this.linkLabelLogging.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.linkLabelLogging.AutoSize = true;
            this.linkLabelLogging.Location = new System.Drawing.Point(718, 347);
            this.linkLabelLogging.Name = "linkLabelLogging";
            this.linkLabelLogging.Size = new System.Drawing.Size(113, 13);
            this.linkLabelLogging.TabIndex = 67;
            this.linkLabelLogging.TabStop = true;
            this.linkLabelLogging.Text = "Open logging directory";
            this.linkLabelLogging.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelLogging_LinkClicked);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1023, 392);
            this.Controls.Add(this.linkLabelLogging);
            this.Controls.Add(this.cbUseFlasherOnDevice);
            this.Controls.Add(this.cbAdapter);
            this.Controls.Add(this.btnReadECUcalibration);
            this.Controls.Add(this.btnEditParameters);
            this.Controls.Add(this.cbELM327Kline);
            this.Controls.Add(this.documentation);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.cbxComSpeed);
            this.Controls.Add(this.cbDisableConnectionCheck);
            this.Controls.Add(this.cbOnlyPBus);
            this.Controls.Add(this.cbxEcuType);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnReadDTC);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.cbxAdapterType);
            this.Controls.Add(this.btnRecoverECU);
            this.Controls.Add(this.btnReadSRAM);
            this.Controls.Add(this.btnGetECUInfo);
            this.Controls.Add(this.btnReadECU);
            this.Controls.Add(this.cbEnableLogging);
            this.Controls.Add(this.btnRestoreT8);
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
        private System.Windows.Forms.CheckBox cbEnableLogging;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button btnRestoreT8;
        private System.Windows.Forms.Button btnFlashECU;
        private System.Windows.Forms.ListBox listBoxLog;
        private System.Windows.Forms.Button btnGetECUInfo;
        private System.Windows.Forms.Button btnReadSRAM;
        private System.Windows.Forms.Button btnRecoverECU;
        private System.Windows.Forms.ComboBox cbxAdapterType;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btnReadDTC;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbxEcuType;
        private System.Windows.Forms.CheckBox cbOnlyPBus;
        private System.Windows.Forms.CheckBox cbDisableConnectionCheck;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cbxComSpeed;
        private System.Windows.Forms.LinkLabel documentation;
        private System.Windows.Forms.CheckBox cbELM327Kline;
        private System.Windows.Forms.Button btnEditParameters;
        private System.Windows.Forms.Button btnReadECUcalibration;
        private System.Windows.Forms.ComboBox cbAdapter;
        private System.Windows.Forms.CheckBox cbUseFlasherOnDevice;
        private System.Windows.Forms.LinkLabel linkLabelLogging;
    }
}

