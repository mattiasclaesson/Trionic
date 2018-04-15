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
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.btnRestoreT8 = new System.Windows.Forms.Button();
            this.btnFlashECU = new System.Windows.Forms.Button();
            this.listBoxLog = new System.Windows.Forms.ListBox();
            this.btnGetECUInfo = new System.Windows.Forms.Button();
            this.btnReadSRAM = new System.Windows.Forms.Button();
            this.btnRecoverECU = new System.Windows.Forms.Button();
            this.btnReadDTC = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.cbxEcuType = new System.Windows.Forms.ComboBox();
            this.documentation = new System.Windows.Forms.LinkLabel();
            this.btnEditParameters = new System.Windows.Forms.Button();
            this.btnReadECUcalibration = new System.Windows.Forms.Button();
            this.linkLabelLogging = new System.Windows.Forms.LinkLabel();
            this.btnLogData = new System.Windows.Forms.Button();
            this.btnSettings = new System.Windows.Forms.Button();
            this.btnCollapse = new System.Windows.Forms.Button();
            this.btnExpand = new System.Windows.Forms.Button();
            this.Minilog = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnReadECU
            // 
            this.btnReadECU.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnReadECU.Location = new System.Drawing.Point(906, 12);
            this.btnReadECU.Name = "btnReadECU";
            this.btnReadECU.Size = new System.Drawing.Size(107, 50);
            this.btnReadECU.TabIndex = 29;
            this.btnReadECU.Text = "Read ECU";
            this.btnReadECU.UseVisualStyleBackColor = true;
            this.btnReadECU.Click += new System.EventHandler(this.btnReadECU_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar1.Location = new System.Drawing.Point(676, 351);
            this.progressBar1.MaximumSize = new System.Drawing.Size(336, 23);
            this.progressBar1.MinimumSize = new System.Drawing.Size(336, 23);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(336, 23);
            this.progressBar1.TabIndex = 23;
            // 
            // btnRestoreT8
            // 
            this.btnRestoreT8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRestoreT8.Location = new System.Drawing.Point(906, 124);
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
            this.btnFlashECU.Location = new System.Drawing.Point(788, 12);
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
            this.listBoxLog.Size = new System.Drawing.Size(657, 368);
            this.listBoxLog.TabIndex = 20;
            // 
            // btnGetECUInfo
            // 
            this.btnGetECUInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnGetECUInfo.Location = new System.Drawing.Point(788, 68);
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
            this.btnReadSRAM.Location = new System.Drawing.Point(906, 68);
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
            this.btnRecoverECU.Location = new System.Drawing.Point(788, 124);
            this.btnRecoverECU.Name = "btnRecoverECU";
            this.btnRecoverECU.Size = new System.Drawing.Size(112, 50);
            this.btnRecoverECU.TabIndex = 32;
            this.btnRecoverECU.Text = "Recover ECU";
            this.btnRecoverECU.UseVisualStyleBackColor = true;
            this.btnRecoverECU.Click += new System.EventHandler(this.btnRecoverECU_Click);
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
            this.label2.Location = new System.Drawing.Point(785, 246);
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
            "Trionic 5",
            "Trionic 7",
            "Trionic 8",
            "Bosch ME9.6",
            "Trionic 8: MCP (Experimental)",
            "Z22SE (Experimental)",
            "Z22SE: MCP (Experimental)"});
            this.cbxEcuType.Location = new System.Drawing.Point(862, 243);
            this.cbxEcuType.Name = "cbxEcuType";
            this.cbxEcuType.Size = new System.Drawing.Size(150, 21);
            this.cbxEcuType.TabIndex = 56;
            this.cbxEcuType.SelectedIndexChanged += new System.EventHandler(this.cbxEcuType_SelectedIndexChanged);
            // 
            // documentation
            // 
            this.documentation.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.documentation.AutoSize = true;
            this.documentation.Location = new System.Drawing.Point(674, 317);
            this.documentation.Name = "documentation";
            this.documentation.Size = new System.Drawing.Size(103, 13);
            this.documentation.TabIndex = 61;
            this.documentation.TabStop = true;
            this.documentation.Text = "View documentation";
            this.documentation.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.documentation_LinkClicked);
            // 
            // btnEditParameters
            // 
            this.btnEditParameters.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnEditParameters.Location = new System.Drawing.Point(675, 68);
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
            // linkLabelLogging
            // 
            this.linkLabelLogging.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.linkLabelLogging.AutoSize = true;
            this.linkLabelLogging.Location = new System.Drawing.Point(675, 334);
            this.linkLabelLogging.Name = "linkLabelLogging";
            this.linkLabelLogging.Size = new System.Drawing.Size(113, 13);
            this.linkLabelLogging.TabIndex = 67;
            this.linkLabelLogging.TabStop = true;
            this.linkLabelLogging.Text = "Open logging directory";
            this.linkLabelLogging.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelLogging_LinkClicked);
            // 
            // btnLogData
            // 
            this.btnLogData.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLogData.Location = new System.Drawing.Point(788, 180);
            this.btnLogData.Name = "btnLogData";
            this.btnLogData.Size = new System.Drawing.Size(112, 50);
            this.btnLogData.TabIndex = 68;
            this.btnLogData.Text = "Log data";
            this.btnLogData.UseVisualStyleBackColor = true;
            this.btnLogData.Click += new System.EventHandler(this.btnLogData_Click);
            // 
            // btnSettings
            // 
            this.btnSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSettings.Location = new System.Drawing.Point(906, 180);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(107, 50);
            this.btnSettings.TabIndex = 73;
            this.btnSettings.Text = "Settings";
            this.btnSettings.UseVisualStyleBackColor = true;
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
            // 
            // btnCollapse
            // 
            this.btnCollapse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCollapse.Location = new System.Drawing.Point(675, 180);
            this.btnCollapse.Name = "btnCollapse";
            this.btnCollapse.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnCollapse.Size = new System.Drawing.Size(20, 50);
            this.btnCollapse.TabIndex = 74;
            this.btnCollapse.Text = ">";
            this.btnCollapse.UseVisualStyleBackColor = true;
            this.btnCollapse.Click += new System.EventHandler(this.btnCollapse_Click);
            // 
            // btnExpand
            // 
            this.btnExpand.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExpand.Enabled = false;
            this.btnExpand.Location = new System.Drawing.Point(675, 180);
            this.btnExpand.Name = "btnExpand";
            this.btnExpand.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnExpand.Size = new System.Drawing.Size(20, 50);
            this.btnExpand.TabIndex = 75;
            this.btnExpand.Text = "<";
            this.btnExpand.UseVisualStyleBackColor = true;
            this.btnExpand.Visible = false;
            this.btnExpand.Click += new System.EventHandler(this.btnExpand_Click);
            // 
            // Minilog
            // 
            this.Minilog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Minilog.AutoSize = true;
            this.Minilog.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.25F);
            this.Minilog.Location = new System.Drawing.Point(785, 331);
            this.Minilog.Name = "Minilog";
            this.Minilog.Size = new System.Drawing.Size(56, 17);
            this.Minilog.TabIndex = 76;
            this.Minilog.Text = "Mini log";
            this.Minilog.Visible = false;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1023, 382);
            this.Controls.Add(this.Minilog);
            this.Controls.Add(this.btnExpand);
            this.Controls.Add(this.btnCollapse);
            this.Controls.Add(this.btnSettings);
            this.Controls.Add(this.btnLogData);
            this.Controls.Add(this.linkLabelLogging);
            this.Controls.Add(this.btnReadECUcalibration);
            this.Controls.Add(this.btnEditParameters);
            this.Controls.Add(this.documentation);
            this.Controls.Add(this.cbxEcuType);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnReadDTC);
            this.Controls.Add(this.btnRecoverECU);
            this.Controls.Add(this.btnReadSRAM);
            this.Controls.Add(this.btnGetECUInfo);
            this.Controls.Add(this.btnReadECU);
            this.Controls.Add(this.btnRestoreT8);
            this.Controls.Add(this.btnFlashECU);
            this.Controls.Add(this.listBoxLog);
            this.Controls.Add(this.progressBar1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(600, 360);
            this.Name = "frmMain";
            this.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Trionic CAN flasher";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.Shown += new System.EventHandler(this.frmMain_Shown);
            this.SizeChanged += new System.EventHandler(this.frmMainResized);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnReadECU;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button btnRestoreT8;
        private System.Windows.Forms.Button btnFlashECU;
        private System.Windows.Forms.ListBox listBoxLog;
        private System.Windows.Forms.Button btnGetECUInfo;
        private System.Windows.Forms.Button btnReadSRAM;
        private System.Windows.Forms.Button btnRecoverECU;
        private System.Windows.Forms.Button btnReadDTC;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbxEcuType;
        private System.Windows.Forms.LinkLabel documentation;
        private System.Windows.Forms.Button btnEditParameters;
        private System.Windows.Forms.Button btnReadECUcalibration;
        private System.Windows.Forms.LinkLabel linkLabelLogging;
        private System.Windows.Forms.Button btnLogData;
        private System.Windows.Forms.Button btnSettings;
        private System.Windows.Forms.Button btnCollapse;
        private System.Windows.Forms.Button btnExpand;
        private System.Windows.Forms.Label Minilog;
    }
}

