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
    public partial class frmUpdateAvailable : Form
    {
        public frmUpdateAvailable()
        {
            InitializeComponent();
        }

        public void SetVersionNumber(string version)
        {
            label1.Text = "Available version: " + version;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("IEXPLORE.EXE", "http://develop.trionictuning.com/TrionicCANFlasher/Notes.xml");
        }
    }
}
