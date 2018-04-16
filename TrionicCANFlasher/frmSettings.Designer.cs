namespace TrionicCANFlasher
{
    partial class frmSettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmSettings));
            this.cbEnableLogging = new System.Windows.Forms.CheckBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.cbOnboardFlasher = new System.Windows.Forms.CheckBox();
            this.cbOnlyPBus = new System.Windows.Forms.CheckBox();
            this.cbAutoChecksum = new System.Windows.Forms.CheckBox();
            this.cbUnlockSys = new System.Windows.Forms.CheckBox();
            this.cbUseLegion = new System.Windows.Forms.CheckBox();
            this.cbUnlockBoot = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cbxAdapterItem = new System.Windows.Forms.ComboBox();
            this.ComBaudLabel = new System.Windows.Forms.Label();
            this.cbxComSpeed = new System.Windows.Forms.ComboBox();
            this.AdapterLabel = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.cbxAdapterType = new System.Windows.Forms.ComboBox();
            this.cbPowerUser = new System.Windows.Forms.CheckBox();
            this.bntDiscard = new System.Windows.Forms.Button();
            this.cbUseLastPointer = new System.Windows.Forms.CheckBox();
            this.cbVerifyChecksum = new System.Windows.Forms.CheckBox();
            this.cbFaster = new System.Windows.Forms.CheckBox();
            this.InterframeLabel = new System.Windows.Forms.Label();
            this.cbxInterFrame = new System.Windows.Forms.ComboBox();
            this.cbRemember = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // cbEnableLogging
            // 
            this.cbEnableLogging.AutoSize = true;
            this.cbEnableLogging.Checked = true;
            this.cbEnableLogging.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbEnableLogging.Location = new System.Drawing.Point(12, 69);
            this.cbEnableLogging.Name = "cbEnableLogging";
            this.cbEnableLogging.Size = new System.Drawing.Size(96, 17);
            this.cbEnableLogging.TabIndex = 25;
            this.cbEnableLogging.Text = "Enable logging";
            this.cbEnableLogging.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnSave.Location = new System.Drawing.Point(194, 292);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(151, 44);
            this.btnSave.TabIndex = 27;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // cbOnboardFlasher
            // 
            this.cbOnboardFlasher.AutoSize = true;
            this.cbOnboardFlasher.Checked = true;
            this.cbOnboardFlasher.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbOnboardFlasher.Location = new System.Drawing.Point(12, 115);
            this.cbOnboardFlasher.Name = "cbOnboardFlasher";
            this.cbOnboardFlasher.Size = new System.Drawing.Size(129, 17);
            this.cbOnboardFlasher.TabIndex = 68;
            this.cbOnboardFlasher.Tag = "";
            this.cbOnboardFlasher.Text = "Use flasher on device";
            this.cbOnboardFlasher.UseVisualStyleBackColor = true;
            // 
            // cbOnlyPBus
            // 
            this.cbOnlyPBus.AutoSize = true;
            this.cbOnlyPBus.Checked = true;
            this.cbOnlyPBus.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbOnlyPBus.Location = new System.Drawing.Point(12, 92);
            this.cbOnlyPBus.Name = "cbOnlyPBus";
            this.cbOnlyPBus.Size = new System.Drawing.Size(134, 17);
            this.cbOnlyPBus.TabIndex = 67;
            this.cbOnlyPBus.Text = "Only P-Bus connection";
            this.cbOnlyPBus.UseVisualStyleBackColor = true;
            this.cbOnlyPBus.CheckedChanged += new System.EventHandler(this.cbOnlyPBus_Checkchanged);
            // 
            // cbAutoChecksum
            // 
            this.cbAutoChecksum.AutoSize = true;
            this.cbAutoChecksum.Location = new System.Drawing.Point(12, 235);
            this.cbAutoChecksum.Name = "cbAutoChecksum";
            this.cbAutoChecksum.Size = new System.Drawing.Size(136, 17);
            this.cbAutoChecksum.TabIndex = 76;
            this.cbAutoChecksum.Text = "Auto update checksum";
            this.cbAutoChecksum.UseVisualStyleBackColor = true;
            // 
            // cbUnlockSys
            // 
            this.cbUnlockSys.AutoSize = true;
            this.cbUnlockSys.Location = new System.Drawing.Point(12, 189);
            this.cbUnlockSys.Name = "cbUnlockSys";
            this.cbUnlockSys.Size = new System.Drawing.Size(140, 17);
            this.cbUnlockSys.TabIndex = 75;
            this.cbUnlockSys.Text = "Unlock system partitions";
            this.cbUnlockSys.UseVisualStyleBackColor = true;
            this.cbUnlockSys.CheckedChanged += new System.EventHandler(this.cbUnlockSys_CheckedChanged);
            // 
            // cbUseLegion
            // 
            this.cbUseLegion.AutoSize = true;
            this.cbUseLegion.Checked = true;
            this.cbUseLegion.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbUseLegion.Location = new System.Drawing.Point(194, 92);
            this.cbUseLegion.Name = "cbUseLegion";
            this.cbUseLegion.Size = new System.Drawing.Size(133, 17);
            this.cbUseLegion.TabIndex = 74;
            this.cbUseLegion.Text = "Use Legion bootloader";
            this.cbUseLegion.UseVisualStyleBackColor = true;
            this.cbUseLegion.CheckedChanged += new System.EventHandler(this.cbUseLegion_CheckedChanged);
            // 
            // cbUnlockBoot
            // 
            this.cbUnlockBoot.AutoSize = true;
            this.cbUnlockBoot.Location = new System.Drawing.Point(12, 212);
            this.cbUnlockBoot.Name = "cbUnlockBoot";
            this.cbUnlockBoot.Size = new System.Drawing.Size(124, 17);
            this.cbUnlockBoot.TabIndex = 73;
            this.cbUnlockBoot.Text = "Unlock boot partition";
            this.cbUnlockBoot.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Black;
            this.label2.Location = new System.Drawing.Point(9, 146);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(114, 13);
            this.label2.TabIndex = 77;
            this.label2.Text = "Advanced features";
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // cbxAdapterItem
            // 
            this.cbxAdapterItem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxAdapterItem.FormattingEnabled = true;
            this.cbxAdapterItem.Location = new System.Drawing.Point(194, 25);
            this.cbxAdapterItem.Name = "cbxAdapterItem";
            this.cbxAdapterItem.Size = new System.Drawing.Size(150, 21);
            this.cbxAdapterItem.TabIndex = 83;
            // 
            // ComBaudLabel
            // 
            this.ComBaudLabel.AutoSize = true;
            this.ComBaudLabel.Location = new System.Drawing.Point(191, 49);
            this.ComBaudLabel.Name = "ComBaudLabel";
            this.ComBaudLabel.Size = new System.Drawing.Size(60, 13);
            this.ComBaudLabel.TabIndex = 82;
            this.ComBaudLabel.Text = "Com speed";
            // 
            // cbxComSpeed
            // 
            this.cbxComSpeed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxComSpeed.FormattingEnabled = true;
            this.cbxComSpeed.Items.AddRange(new object[] {
            "115200",
            "230400",
            "1Mbit",
            "2Mbit"});
            this.cbxComSpeed.Location = new System.Drawing.Point(194, 65);
            this.cbxComSpeed.Name = "cbxComSpeed";
            this.cbxComSpeed.Size = new System.Drawing.Size(150, 21);
            this.cbxComSpeed.TabIndex = 81;
            // 
            // AdapterLabel
            // 
            this.AdapterLabel.AutoSize = true;
            this.AdapterLabel.Location = new System.Drawing.Point(191, 9);
            this.AdapterLabel.Name = "AdapterLabel";
            this.AdapterLabel.Size = new System.Drawing.Size(44, 13);
            this.AdapterLabel.TabIndex = 80;
            this.AdapterLabel.Text = "Adapter";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(9, 9);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(67, 13);
            this.label4.TabIndex = 79;
            this.label4.Text = "Adapter type";
            // 
            // cbxAdapterType
            // 
            this.cbxAdapterType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxAdapterType.FormattingEnabled = true;
            this.cbxAdapterType.Location = new System.Drawing.Point(12, 25);
            this.cbxAdapterType.Name = "cbxAdapterType";
            this.cbxAdapterType.Size = new System.Drawing.Size(150, 21);
            this.cbxAdapterType.TabIndex = 78;
            this.cbxAdapterType.SelectedIndexChanged += new System.EventHandler(this.cbxAdapterType_SelectedIndexChanged);
            // 
            // cbPowerUser
            // 
            this.cbPowerUser.AutoSize = true;
            this.cbPowerUser.Location = new System.Drawing.Point(12, 166);
            this.cbPowerUser.Name = "cbPowerUser";
            this.cbPowerUser.Size = new System.Drawing.Size(110, 17);
            this.cbPowerUser.TabIndex = 84;
            this.cbPowerUser.Text = "I am a power user";
            this.cbPowerUser.UseVisualStyleBackColor = true;
            this.cbPowerUser.CheckedChanged += new System.EventHandler(this.cbPowerUser_CheckedChanged);
            // 
            // bntDiscard
            // 
            this.bntDiscard.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bntDiscard.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bntDiscard.Location = new System.Drawing.Point(10, 292);
            this.bntDiscard.Name = "bntDiscard";
            this.bntDiscard.Size = new System.Drawing.Size(151, 44);
            this.bntDiscard.TabIndex = 85;
            this.bntDiscard.Text = "Cancel";
            this.bntDiscard.UseVisualStyleBackColor = true;
            this.bntDiscard.Click += new System.EventHandler(this.bntDiscard_Click);
            // 
            // cbUseLastPointer
            // 
            this.cbUseLastPointer.AutoSize = true;
            this.cbUseLastPointer.Checked = true;
            this.cbUseLastPointer.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbUseLastPointer.Enabled = false;
            this.cbUseLastPointer.Location = new System.Drawing.Point(194, 189);
            this.cbUseLastPointer.Name = "cbUseLastPointer";
            this.cbUseLastPointer.Size = new System.Drawing.Size(139, 17);
            this.cbUseLastPointer.TabIndex = 86;
            this.cbUseLastPointer.Text = "Use last address pointer";
            this.cbUseLastPointer.UseVisualStyleBackColor = true;
            // 
            // cbVerifyChecksum
            // 
            this.cbVerifyChecksum.AutoSize = true;
            this.cbVerifyChecksum.Checked = true;
            this.cbVerifyChecksum.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbVerifyChecksum.Enabled = false;
            this.cbVerifyChecksum.Location = new System.Drawing.Point(12, 258);
            this.cbVerifyChecksum.Name = "cbVerifyChecksum";
            this.cbVerifyChecksum.Size = new System.Drawing.Size(162, 17);
            this.cbVerifyChecksum.TabIndex = 87;
            this.cbVerifyChecksum.Text = "Verify checksum before flash";
            this.cbVerifyChecksum.UseVisualStyleBackColor = true;
            // 
            // cbFaster
            // 
            this.cbFaster.AutoSize = true;
            this.cbFaster.Enabled = false;
            this.cbFaster.Location = new System.Drawing.Point(194, 212);
            this.cbFaster.Name = "cbFaster";
            this.cbFaster.Size = new System.Drawing.Size(115, 17);
            this.cbFaster.TabIndex = 88;
            this.cbFaster.Text = "Skip certain delays";
            this.cbFaster.UseVisualStyleBackColor = true;
            // 
            // InterframeLabel
            // 
            this.InterframeLabel.AutoSize = true;
            this.InterframeLabel.Enabled = false;
            this.InterframeLabel.Location = new System.Drawing.Point(191, 146);
            this.InterframeLabel.Name = "InterframeLabel";
            this.InterframeLabel.Size = new System.Drawing.Size(85, 13);
            this.InterframeLabel.TabIndex = 90;
            this.InterframeLabel.Text = "Inter-frame delay";
            // 
            // cbxInterFrame
            // 
            this.cbxInterFrame.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxInterFrame.Enabled = false;
            this.cbxInterFrame.FormattingEnabled = true;
            this.cbxInterFrame.Items.AddRange(new object[] {
            "300",
            "400",
            "500",
            "600",
            "700",
            "800",
            "900",
            "1000 (Lowest safe delay)",
            "1100",
            "1200 (Default)",
            "1300",
            "1400",
            "1500",
            "1600",
            "1700",
            "1800",
            "1900",
            "2000"});
            this.cbxInterFrame.Location = new System.Drawing.Point(194, 162);
            this.cbxInterFrame.Name = "cbxInterFrame";
            this.cbxInterFrame.Size = new System.Drawing.Size(150, 21);
            this.cbxInterFrame.TabIndex = 89;
            // 
            // cbRemember
            // 
            this.cbRemember.AutoSize = true;
            this.cbRemember.Location = new System.Drawing.Point(194, 115);
            this.cbRemember.Name = "cbRemember";
            this.cbRemember.Size = new System.Drawing.Size(132, 17);
            this.cbRemember.TabIndex = 91;
            this.cbRemember.Text = "Remember dimensions";
            this.cbRemember.UseVisualStyleBackColor = true;
            // 
            // frmSettings
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bntDiscard;
            this.ClientSize = new System.Drawing.Size(356, 348);
            this.Controls.Add(this.cbRemember);
            this.Controls.Add(this.InterframeLabel);
            this.Controls.Add(this.cbxInterFrame);
            this.Controls.Add(this.cbFaster);
            this.Controls.Add(this.cbVerifyChecksum);
            this.Controls.Add(this.cbUseLastPointer);
            this.Controls.Add(this.bntDiscard);
            this.Controls.Add(this.cbPowerUser);
            this.Controls.Add(this.cbxAdapterItem);
            this.Controls.Add(this.ComBaudLabel);
            this.Controls.Add(this.cbxComSpeed);
            this.Controls.Add(this.AdapterLabel);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.cbxAdapterType);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cbAutoChecksum);
            this.Controls.Add(this.cbUnlockSys);
            this.Controls.Add(this.cbUseLegion);
            this.Controls.Add(this.cbUnlockBoot);
            this.Controls.Add(this.cbOnboardFlasher);
            this.Controls.Add(this.cbOnlyPBus);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.cbEnableLogging);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmSettings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings";
            this.Shown += new System.EventHandler(this.DialogShown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox cbEnableLogging;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.CheckBox cbOnboardFlasher;
        private System.Windows.Forms.CheckBox cbOnlyPBus;
        private System.Windows.Forms.CheckBox cbAutoChecksum;
        private System.Windows.Forms.CheckBox cbUnlockSys;
        private System.Windows.Forms.CheckBox cbUseLegion;
        private System.Windows.Forms.CheckBox cbUnlockBoot;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbxAdapterItem;
        private System.Windows.Forms.Label ComBaudLabel;
        private System.Windows.Forms.ComboBox cbxComSpeed;
        private System.Windows.Forms.Label AdapterLabel;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cbxAdapterType;
        private System.Windows.Forms.CheckBox cbPowerUser;
        private System.Windows.Forms.Button bntDiscard;
        private System.Windows.Forms.CheckBox cbUseLastPointer;
        private System.Windows.Forms.CheckBox cbVerifyChecksum;
        private System.Windows.Forms.CheckBox cbFaster;
        private System.Windows.Forms.Label InterframeLabel;
        private System.Windows.Forms.ComboBox cbxInterFrame;
        private System.Windows.Forms.CheckBox cbRemember;
    }
}