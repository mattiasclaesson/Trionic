using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TrionicCANFlasher
{
    public partial class PiSelection : Form
    {
        public PiSelection()
        {
            InitializeComponent();
        }

        public bool Cab
        {
            get
            {
                return cbCab.Checked;
            }
            set
            {
                cbCab.Checked = value;
            }
        }

        public bool SAI
        {
            get
            {
                return cbSAI.Checked;
            }
            set
            {
                cbSAI.Checked = value;
            }
        }

        public bool Highoutput
        {
            get
            {
                return cbOutput.Checked;
            }
            set
            {
                cbOutput.Checked = value;
            }
        }

        private void writeToECU_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
