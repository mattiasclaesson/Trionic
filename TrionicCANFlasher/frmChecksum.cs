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
    public partial class frmChecksum : Form
    {
        public frmChecksum()
        {
            InitializeComponent();
        }

        public string Layer
        {
            set
            {
                groupBox1.Text = value;
            }
        }

        public string FileChecksum
        {
            set
            {
                textBox1.Text = value;
            }
        }

        public string RealChecksum
        {
            set
            {
                textBox2.Text = value;
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnIgnore_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
