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

        public bool Convertible
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

        public int RPMLimit
        {
            get
            {
                int rpm;
                int.TryParse(tbRPMLimit.Text, out rpm);
                return rpm;
            }
            set
            {
                tbRPMLimit.Text = value.ToString();
            }
        }

        public string VIN
        {
            get
            {
                return tbVIN.Text;
            }
            set
            {
                tbVIN.Text = value;
            }
        }

        public int TopSpeed
        {
            get
            {
                int speed;
                int.TryParse(tbTopSpeed.Text, out speed);
                return speed;
            }
            set
            {
                tbTopSpeed.Text = value.ToString();
            }
        }

        public float E85
        {
            get
            {
                float e85;
                float.TryParse(tbE85.Text, out e85);
                return e85;
            }
            set
            {
                tbE85.Text = value.ToString();
            }
        } 

        private void writeToECU_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
