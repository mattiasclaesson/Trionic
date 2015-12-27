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
    public partial class EditParameters : Form
    {
        public EditParameters()
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

        public bool Biopower
        {
            get
            {
                return cbBiopower.Checked;
            }
            set
            {
                cbBiopower.Checked = value;
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

        public float Oil
        {
            get
            {
                float oil;
                float.TryParse(tbOilQuality.Text, out oil);
                return oil;
            }
            set
            {
                tbOilQuality.Text = value.ToString();
            }
        }
        
        public bool ClutchStart
        {
            get
            {
                return cbClutchStart.Checked;
            }
            set
            {
                cbClutchStart.Checked = value;
            }
        }

        public DiagnosticType DiagnosticType
        {
            get
            {
                return (DiagnosticType)comboBoxDiag.SelectedIndex;
            }
            set
            {
                comboBoxDiag.SelectedIndex = (int)value;
            }
        }

        public TankType TankType
        {
            get
            {
                return (TankType)comboBoxTank.SelectedIndex;
            }
            set
            {
                comboBoxTank.SelectedIndex = (int)value;
            }
        }

        public void setECU(ECU ecu) 
        {
            if(ecu == ECU.TRIONIC8)
            {
                tbVIN.Show();
                cbCab.Show();
                cbSAI.Show();
                cbOutput.Show();
                tbTopSpeed.Show();
                tbE85.Show();
                tbOilQuality.Show();
                cbBiopower.Show();
                cbClutchStart.Show();
                comboBoxDiag.Show();
                comboBoxTank.Show();
            }
            else if(ecu == ECU.MOTRONIC96)
            {
                tbVIN.Hide();
                cbCab.Hide();
                cbSAI.Hide();
                cbOutput.Hide();
                tbTopSpeed.Show();
                tbE85.Hide();
                tbOilQuality.Hide();
                cbBiopower.Hide();
                cbClutchStart.Hide();
                comboBoxDiag.Hide();
                comboBoxTank.Hide();
            }
            else if(ecu == ECU.TRIONIC7)
            {
                tbVIN.Hide();
                cbCab.Hide();
                cbSAI.Hide();
                cbOutput.Hide();
                tbTopSpeed.Hide();
                tbE85.Show();
                tbOilQuality.Hide();
                cbBiopower.Hide();
                cbClutchStart.Hide();
                comboBoxDiag.Hide();
                comboBoxTank.Hide();
            }
        }

        private void btnWriteToECU_Click(object sender, EventArgs e)
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
