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
    public partial class Frm_PLCData : Form
    {
        Clsora objora = new Clsora();
        public Frm_PLCData()
        {
            InitializeComponent();
            objora.Connection();
            filldatagrid1();
            filldatagrid2();
        }
        private void filldatagrid1()
        {
            string getysql = "SELECT * FROM LANCE3_PLCCONFIG WHERE DATABLOCK = 'DB61' ORDER BY SERIAL_NO ASC";
            DataSet ds = objora.GetData(getysql);
            if(ds.Tables[0].Rows.Count >0)
            {
                dataGridView1.DataSource = ds.Tables[0];
                dataGridView1.Refresh();
                dataGridView1.AutoResizeColumns();
                dataGridView1.AutoResizeRows();
            }
        }
        private void filldatagrid2()
        {
            string getysql = "SELECT * FROM LANCE3_PLCCONFIG WHERE DATABLOCK = 'DB100' ORDER BY SERIAL_NO ASC";
            DataSet DS = objora.GetData(getysql);
            if (DS.Tables[0].Rows.Count > 0)
            {
                dataGridView2.DataSource = DS.Tables[0];
                dataGridView2.Refresh();
                dataGridView2.AutoResizeColumns();
                dataGridView2.AutoResizeRows();
            }
        }

        private void Frm_PLCData_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            objora.ConnectionClose();
        }
    }
}
