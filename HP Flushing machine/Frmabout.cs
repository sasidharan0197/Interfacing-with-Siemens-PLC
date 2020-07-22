using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HP_Flushing_SCADA
{
    public partial class Frmabout : Form
    {
        public Frmabout()
        {
            InitializeComponent();
        }

        private void okaybtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
