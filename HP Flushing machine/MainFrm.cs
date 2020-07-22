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
    public partial class MainFrm : Form
    {
        public MainFrm()
        {
            InitializeComponent();
            Load_winforms();
        }
        private void Load_winforms()
        {
            //Station 1
            FrmStation1 objForm = new FrmStation1();
            objForm.TopLevel = false;
            objForm.MdiParent = this;
            panel2.Controls.Add(objForm);
            objForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            objForm.Dock = DockStyle.Fill;
            objForm.Show();
            
            //Station 2
            FrmStation2 objForms = new FrmStation2();
            objForms.TopLevel = false;
            objForms.MdiParent = this;
            panel3.Controls.Add(objForms);
            objForms.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            objForms.Dock = DockStyle.Fill;
            objForms.Show();
        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //PLC data show
            Frm_PLCData frm = new Frm_PLCData();
            frm.Show();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Frmabout frm = new Frmabout();
            frm.Show();
        }
    }
}
