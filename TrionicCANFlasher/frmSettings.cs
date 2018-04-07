using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TrionicCANLib.API;

namespace TrionicCANFlasher
{
    public partial class frmSettings : Form
    {
        public bool enableCombiflash
        {
            get { return cbOnboardFlasher.Checked;  }
            set { cbOnboardFlasher.Checked = value; }
        }

        public bool onboardFlasher
        {
            get { return cbOnboardFlasher.Checked;  }
            set { cbOnboardFlasher.Checked = value; }
        }

        public bool onlyPBus
        {
            get { return cbOnlyPBus.Checked;  }
            set { cbOnlyPBus.Checked = value; }
        }

        public bool enableLogging
        {
            get { return cbEnableLogging.Checked;  }
            set { cbEnableLogging.Checked = value; }
        }

        public bool autoChecksum
        {
            get { return cbAutoChecksum.Checked;  }
            set { cbAutoChecksum.Checked = value; }
        }

        public bool useLegion
        {
            get { return cbUseLegion.Checked;  }
            set { cbUseLegion.Checked = value; }
        }

        public bool unlockSys
        {
            get { return cbUnlockSys.Checked;  }
            set { cbUnlockSys.Checked = value; }
        }

        public bool unlockBoot
        {
            get { return cbUnlockBoot.Checked;  }
            set { cbUnlockBoot.Checked = value; }
        }

        public void setECU(int ecu)
        {
            cbOnboardFlasher.Enabled = false;
            cbUseLegion.Enabled = false;
            cbUnlockSys.Enabled = false;
            cbUnlockBoot.Enabled = false;

            cbEnableLogging.Enabled = true;
            cbAutoChecksum.Enabled = true;
            cbOnlyPBus.Enabled = true;

            if (ecu == (int)ECU.TRIONIC5)
            {
                cbOnlyPBus.Enabled = false;
            }

            else if (ecu == (int)ECU.TRIONIC7)
            {
                cbOnboardFlasher.Enabled = cbOnlyPBus.Checked;
            }

            else if (ecu == (int)ECU.TRIONIC8 ||
                     ecu == (int)ECU.TRIONIC8_MCP)
            {
                cbUseLegion.Enabled = true;
                cbUnlockSys.Enabled = cbUseLegion.Checked;
                cbUnlockBoot.Enabled = (cbUnlockSys.Checked && cbUnlockSys.Enabled);

                if (ecu == (int)ECU.TRIONIC8_MCP)
                {
                    cbAutoChecksum.Enabled = false;
                }
            }

            else if (ecu == (int)ECU.MOTRONIC96)
            {
                cbUnlockSys.Enabled = true;
                cbAutoChecksum.Enabled = false;
            }

            // Catch lesser ECU's
            else
            {
                cbAutoChecksum.Enabled = false;
            }

        }

        private void cbUseLegion_CheckedChanged(object sender, EventArgs e)
        {
            cbUnlockSys.Enabled = cbUseLegion.Checked;
            cbUnlockBoot.Enabled = (cbUnlockSys.Checked && cbUnlockSys.Enabled);
        }

        private void cbUnlockSys_CheckedChanged(object sender, EventArgs e)
        {
            cbUnlockSys.Enabled = cbUseLegion.Checked;
            cbUnlockBoot.Enabled = cbUnlockSys.Checked;
        }
        
        public frmSettings()
        {
            InitializeComponent();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
