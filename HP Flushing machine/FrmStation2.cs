using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.IO;

namespace HP_Flushing_SCADA
{
    public partial class FrmStation2 : Form
    {
        Clsora objora = new Clsora();

        // Connection for S7 - PLC.
        List<int> readIntList;
        List<int> readIntList2;
        LibnoDaveClass libnoInstance = new LibnoDaveClass(102, "172.16.24.126", 0, 2, false, false);
        LibnoDaveClass.AddressType address = LibnoDaveClass.AddressType.DB;
        LibnoDaveClass.PLCDataType dataType;
        string[] alarmlists;

        //station 2 Reader
        Byte[] StorageBytes = new byte[1024];
        string sDataManIP = "172.10.10.72";
        TcpClient dmSockets = new TcpClient();
        Stream streams;
        BinaryReader SocketReaders;
        string readdatas = string.Empty;
        string barref2 = string.Empty;
        public FrmStation2()
        {
            InitializeComponent();            

            if (objora.Connection())
            {
                dbstatuslbl.Text = "CONNECTED";
                dbstatuslbl.ForeColor = Color.LawnGreen;
            }
            else
            {
                dbstatuslbl.Text = "NOT CONNECTED";
                dbstatuslbl.ForeColor = Color.Red;
            }
            libnoInstance.connect_plc();
            if (libnoInstance.IsConnected)
            {                
                Life_timer.Enabled = true;                
            }
            else
            {
                opmsglbl2.Text = "CONNECTION TO PLC WAS FAILED";
                opmsglbl2.ForeColor = Color.Red;                
            }            
            DataManInitialize_Stn2();
            S7_Device_Initialize();
        }
        private void S7_Device_Initialize()
        {                    
            libnoInstance.write_bit_value(address, 61, 1, 3, true); //enable 2d matrix read in STN2   DB61.DBX1.3
     
            if (libnoInstance.read_bit_value(address, 61, 2, 6).ToString().ToUpper() != "FALSE")
            {
                libnoInstance.write_bit_value(address, 61, 2, 6, true); //Enable traceability server communication on Stn 2
                fillcombo2();
            }
            
        }
        public void DataManInitialize_Stn2()
        {
            try
            {
                dmSockets = new TcpClient(sDataManIP, 23);
                Stream xstreams = dmSockets.GetStream();
                streams = Stream.Synchronized(xstreams);
                SocketReaders = new BinaryReader(streams);
                streams.BeginRead(StorageBytes, 0, 0, new AsyncCallback(OnDmReceives), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("DataManInit() : {0}", ex.Message));
            }
        }
        void OnDmReceives(IAsyncResult ars)                                                             //Data receive for station 2 DM Reader
        {
            if (!dmSockets.Connected) return;
            try
            {
                byte[] RecDatas = new byte[30];
                RecDatas = SocketReaders.ReadBytes(dmSockets.Available);
                string RecStrs = ASCIIEncoding.ASCII.GetString(RecDatas);
                readdatas = RecStrs;
                readdatas = readdatas.TrimEnd('\r', '\n', ' '); // Dataman include \r\n in result                
                streams.BeginRead(StorageBytes, 0, 0, new AsyncCallback(OnDmReceives), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("READER 2 DATA READ ERROR !");
            }
        }
        private void fillcombo2()
        {
            string getsql = "SELECT APP_NAME FROM APP_MASTER_LANCE3 UNION SELECT '--SELECT--' AS APP_NAME FROM DUAL";
            DataSet DS = objora.GetData(getsql);
            cmbapp2.DataSource = DS.Tables[0];
            cmbapp2.DisplayMember = "APP_NAME";
            cmbapp2.ValueMember = "APP_NAME";
        }
        private void life_bit_enable()
        {
            LibnoDaveClass.PLCDataType dataType;
            int dbNo, byteNo, bitNo;
            bool readBit;
            string dbNumber3 = "61";
            string byteNumber3 = "2";
            string bitNumber3 = "4";


            int.TryParse(dbNumber3, out dbNo);
            int.TryParse(byteNumber3, out byteNo);
            int.TryParse(bitNumber3, out bitNo);

            readBit = libnoInstance.read_bit_value(address, dbNo, byteNo, bitNo);
            if (readBit.ToString().ToUpper() == "TRUE")                  //if life bit set by plc to true
            {
                heartbeatlbl.Text = "TRUE";
                heartbeatlbl.ForeColor = Color.LawnGreen;
                libnoInstance.write_bit_value(address, dbNo, byteNo, bitNo, false); // PC reset by false
            }
            else
            {
                heartbeatlbl.Text = "FALSE";
                heartbeatlbl.ForeColor = Color.Red;
                libnoInstance.write_bit_value(address, dbNo, byteNo, bitNo, true); // PC reset by false
            }

        }
        private void check_trace_Bypass()
        {
            LibnoDaveClass.PLCDataType dataType;
          
            if (libnoInstance.read_bit_value(address, 61, 2, 6).ToString().ToUpper() == "TRUE")                  //check traceability bypass in the PLC (STN 2)
            {
                return;
            }
            else
            {
                readdatas = "BYPASSED";
                DM_Datastn2txt.ForeColor = Color.Red;
                DM_Datastn2txt.ReadOnly = true;                
            }
        }

        private void Life_timer_Tick(object sender, EventArgs e)
        {
            life_bit_enable();
            check_trace_Bypass();
            if (DM_Datastn2txt.Text == "" && readdatas != "" && opmsglbl2.Text != "CYCLE IN PROGRESS IN STATION 2 ")
            {
                DM_Datastn2txt.Text = readdatas;    //ASSIGN THE READ DATA TO READER 2 TEXT BOX            
            }
        }

        private void cmbapp2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbapp2.SelectedIndex > 0)
            {
                string getapcode = "SELECT APP_CODE FROM APP_MASTER_LANCE3 WHERE APP_NAME = '" + cmbapp2.Text + "'";
                barref2 = objora.GetSingleValue(getapcode);
                DM_Datastn2txt.ReadOnly = false;
                DM_Datastn2txt.Focus();
            }
        }
        private bool check_appref()
        {            
                if (DM_Datastn2txt.Text.Substring(0, 2) == barref2)
                {
                    return true;
                }
                else
                {
                    return false;
                }            
        }
        private bool check_previousstation_result(string barcode)
        {
            string getsql = "SELECT * FROM VW_LANCE_PRV_STN_CHECK WHERE BARCODE = '" + barcode + "' ORDER BY SCANDATE DESC";
            DataSet DS = objora.GetData(getsql);
            
                if (DS.Tables[0].Rows.Count == 0)
                {
                    opmsglbl2.Text = "NO PREVIOUS STATION DATA FOUND ! (STN 2)";
                    opmsglbl2.ForeColor = Color.Red;
                    return false;
                }
                else if (DS.Tables[0].Rows[0]["STATUS_CODE"].ToString() == "FAIL")
                {
                    opmsglbl2.Text = DS.Tables[0].Rows[0]["STATUS_MSG"].ToString();
                    opmsglbl2.ForeColor = Color.Yellow;
                    return false;
                }
                else
                {
                    opmsglbl2.Text = "PREVIOUS STATION RESULT OK ! (STN 2)";
                    opmsglbl2.ForeColor = Color.LawnGreen;
                    return true;
                }            
        }

        private void save_results(string Status, int stnid)
        {
            List<float> readprocess_Values1;
            libnoInstance.read_real_values(address, 61, 80, 8, out readprocess_Values1);
            string insertsql = "INSERT INTO LANCE3_RESULTS VALUES('" + DM_Datastn2txt.Text + "',SYSDATE,'" + stnid + "','" + readprocess_Values1[0] + "'";
            insertsql += ",'" + readprocess_Values1[1] + "','" + readprocess_Values1[2] + "','" + readprocess_Values1[3] + "','" + readprocess_Values1[4] + "','" + readprocess_Values1[5] + "'";
            insertsql += ",'" + readIntList[6] + "','" + readIntList[7] + "','" + readIntList[8] + "','" + readIntList[9] + "','" + readIntList[10] + "','" + Status + "','" + readprocess_Values1[7] + "')";
            int res = objora.ExecuteQuery(insertsql);
            if (res == 1)
            {
                opmsglbl2.Text = "DATA SAVED SUCCESSFULLY ! & SCAN THE NEXT COMPONENT";
                opmsglbl2.ForeColor = Color.LawnGreen;

            }
        }
        private void filldatagrid(int stationid)
        {
            string filsql = "SELECT * FROM LANCE3_RESULTS WHERE TRUNC(DATE_TIME)>=TRUNC(SYSDATE-1) AND STN_ID = " + stationid + " ORDER BY DATE_TIME DESC";
            DataSet ds = objora.GetData(filsql);
                     
                if (ds.Tables[0].Rows.Count > 0)
                {
                    dataGridView2.DataSource = ds.Tables[0];
                    dataGridView2.Refresh();
                    dataGridView2.AutoResizeColumns();
                }            
        }

        private void Cyltimer_2_Tick(object sender, EventArgs e)
        {
            if (libnoInstance.read_bit_value(address, 101, 0, 1).ToString().ToUpper() == "TRUE")  //trigger message of closing guard in station 2
            {
                opmsglbl2.Text = "CYCLE IN PROGRESS IN STATION 2 ";
                opmsglbl2.ForeColor = Color.Yellow;
            }

            if (libnoInstance.read_bit_value(address, 61, 2, 0).ToString().ToUpper() == "FALSE") //flushing in progress in station 2
            {
                dataType = LibnoDaveClass.PLCDataType.Integer;
                readIntList = null;
                if (libnoInstance.read_integer_values(address, 61, 14, 11, out readIntList2, dataType, true))
                {
                    if (readIntList2[0].ToString() == "0")
                    {
                        save_results("PASS", 2);
                        libnoInstance.write_bit_value(address, 61, 2, 2, true); //release componnet after datalogging in station 2
                        filldatagrid(2);
                        Cyltimer_2.Enabled = false;
                        libnoInstance.write_bit_value(address, 61, 1, 6, false);   //reset the value in the ok bit (station 2)
                        readdatas = "";
                        DM_Datastn2txt.Focus();
                        DM_Datastn2txt.Text = "";
                    }
                    else
                    {
                        save_results("FAIL", 2);
                        libnoInstance.write_bit_value(address, 61, 2, 2, true); //release componnet after datalogging in station 2
                        filldatagrid(2);
                        Cyltimer_2.Enabled = false;
                        libnoInstance.write_bit_value(address, 61, 1, 6, false); //reset the value in the ok bit (station 2)
                        readdatas = "";
                        DM_Datastn2txt.Focus();
                        DM_Datastn2txt.Text = "";
                    }
                }
            }
            else
            {
                return;
            }
        }

        private void DM_Datastn2txt_TextChanged(object sender, EventArgs e)
        {
            if (DM_Datastn2txt.Text.Length > 10)
            {
                DM_Datastn2txt.Text = DM_Datastn2txt.Text.ToUpper();
                if (check_appref())
                {
                    if (check_previousstation_result(DM_Datastn2txt.Text))
                    {
                        libnoInstance.write_bit_value(address, 61, 1, 6, true);   //ok to flush the component in station 2     
                        libnoInstance.write_bit_value(address, 61, 2, 0, true);   //cycle in progress set to false in station 2

                        opmsglbl2.Text = "PREVIOUS STATION RESULT OK !& LOAD THE COMPONENT IN STN 2";
                        opmsglbl2.ForeColor = Color.Yellow;
                        Cyltimer_2.Enabled = true;
                    }
                    else
                    {
                        libnoInstance.write_bit_value(address, 61, 1, 6, false);   //nok to flush the component in station 2
                        opmsglbl2.Text = "CYCLE ABORTED DUE TO PREVIOUS STATION RESULT FAIL IN STN 2";
                        opmsglbl2.ForeColor = Color.Red;
                        Cyltimer_2.Enabled = false;
                        readdatas = "";
                        DM_Datastn2txt.Focus();
                        DM_Datastn2txt.Text = "";
                    }

                }
                else
                {
                    opmsglbl2.Text = "APPLICATION REFERENCE MISMATCH ! IN STN 2";
                    opmsglbl2.ForeColor = Color.Red;
                    readdatas = "";
                }
            }
        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void heartbeatlbl_Click(object sender, EventArgs e)
        {

        }
    }
}
