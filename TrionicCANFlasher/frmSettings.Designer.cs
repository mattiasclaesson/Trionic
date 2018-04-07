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
            this.SuspendLayout();
            // 
            // cbEnableLogging
            // 
            this.cbEnableLogging.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbEnableLogging.AutoSize = true;
            this.cbEnableLogging.Location = new System.Drawing.Point(12, 35);
            this.cbEnableLogging.Name = "cbEnableLogging";
            this.cbEnableLogging.Size = new System.Drawing.Size(96, 17);
            this.cbEnableLogging.TabIndex = 25;
            this.cbEnableLogging.Text = "Enable logging";
            this.cbEnableLogging.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            this.btnSave.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnSave.Location = new System.Drawing.Point(199, 153);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(140, 23);
            this.btnSave.TabIndex = 27;
            this.btnSave.Text = "Close";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // cbOnboardFlasher
            // 
            this.cbOnboardFlasher.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbOnboardFlasher.AutoSize = true;
            this.cbOnboardFlasher.Location = new System.Drawing.Point(210, 12);
            this.cbOnboardFlasher.Name = "cbOnboardFlasher";
            this.cbOnboardFlasher.Size = new System.Drawing.Size(129, 17);
            this.cbOnboardFlasher.TabIndex = 68;
            this.cbOnboardFlasher.Tag = "";
            this.cbOnboardFlasher.Text = "Use flasher on device";
            this.cbOnboardFlasher.UseVisualStyleBackColor = true;
            // 
            // cbOnlyPBus
            // 
            this.cbOnlyPBus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbOnlyPBus.AutoSize = true;
            this.cbOnlyPBus.Location = new System.Drawing.Point(12, 12);
            this.cbOnlyPBus.Name = "cbOnlyPBus";
            this.cbOnlyPBus.Size = new System.Drawing.Size(134, 17);
            this.cbOnlyPBus.TabIndex = 67;
            this.cbOnlyPBus.Text = "Only P-Bus connection";
            this.cbOnlyPBus.UseVisualStyleBackColor = true;
            // 
            // cbAutoChecksum
            // 
            this.cbAutoChecksum.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbAutoChecksum.AutoSize = true;
            this.cbAutoChecksum.Location = new System.Drawing.Point(12, 159);
            this.cbAutoChecksum.Name = "cbAutoChecksum";
            this.cbAutoChecksum.Size = new System.Drawing.Size(136, 17);
            this.cbAutoChecksum.TabIndex = 76;
            this.cbAutoChecksum.Text = "Auto update checksum";
            this.cbAutoChecksum.UseVisualStyleBackColor = true;
            // 
            // cbUnlockSys
            // 
            this.cbUnlockSys.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbUnlockSys.AutoSize = true;
            this.cbUnlockSys.Location = new System.Drawing.Point(12, 113);
            this.cbUnlockSys.Name = "cbUnlockSys";
            this.cbUnlockSys.Size = new System.Drawing.Size(140, 17);
            this.cbUnlockSys.TabIndex = 75;
            this.cbUnlockSys.Text = "Unlock system partitions";
            this.cbUnlockSys.UseVisualStyleBackColor = true;
            this.cbUnlockSys.CheckedChanged += new System.EventHandler(this.cbUnlockSys_CheckedChanged);
            // 
            // cbUseLegion
            // 
            this.cbUseLegion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbUseLegion.AutoSize = true;
            this.cbUseLegion.Location = new System.Drawing.Point(210, 35);
            this.cbUseLegion.Name = "cbUseLegion";
            this.cbUseLegion.Size = new System.Drawing.Size(133, 17);
            this.cbUseLegion.TabIndex = 74;
            this.cbUseLegion.Text = "Use Legion bootloader";
            this.cbUseLegion.UseVisualStyleBackColor = true;
            this.cbUseLegion.CheckedChanged += new System.EventHandler(this.cbUseLegion_CheckedChanged);
            // 
            // cbUnlockBoot
            // 
            this.cbUnlockBoot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbUnlockBoot.AutoSize = true;
            this.cbUnlockBoot.Location = new System.Drawing.Point(12, 136);
            this.cbUnlockBoot.Name = "cbUnlockBoot";
            this.cbUnlockBoot.Size = new System.Drawing.Size(124, 17);
            this.cbUnlockBoot.TabIndex = 73;
            this.cbUnlockBoot.Text = "Unlock boot partition";
            this.cbUnlockBoot.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Black;
            this.label2.Location = new System.Drawing.Point(9, 88);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(294, 13);
            this.label2.TabIndex = 77;
            this.label2.Text = "Only enable these if you know what you are doing!";
            // 
            // frmSettings
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnSave;
            this.ClientSize = new System.Drawing.Size(351, 185);
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
            this.Name = "frmSettings";
            this.Text = "Settings";
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
    }
}