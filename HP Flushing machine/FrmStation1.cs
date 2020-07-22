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
    public partial class FrmStation1 : Form
    {
        Clsora objora = new Clsora();

        // S7 PLC Connection 
        List<int> readIntList;
        List<int> readIntList2;
        LibnoDaveClass libnoInstance = new LibnoDaveClass(102, "172.16.24.126", 0, 2, false, false);
        LibnoDaveClass.AddressType address = LibnoDaveClass.AddressType.DB;
        LibnoDaveClass.PLCDataType dataType;
        string[] alarmlists;

        //Station 1 Reader     
        Byte[] StorageByte = new byte[1024];
        int _dataManPort = 23;
        string _sDataManIP = "172.10.10.71";
        TcpClient dmSocket = new TcpClient();
        Stream stream;
        BinaryReader SocketReader;
        string readdata = string.Empty;
        string glbbypass = string.Empty;
        string barref1 = string.Empty;

        public FrmStation1()
        {
            InitializeComponent();
            if(!objora.Connection())
            {
                opmsglbl.Text = "CONNECTION TO DATABASE FAILED";
                opmsglbl.ForeColor = Color.Red;
            }
            libnoInstance.connect_plc();

            if (libnoInstance.IsConnected)
            {
                lifebitlbl.Text = "CONNECTED";
                lifebitlbl.ForeColor = Color.LawnGreen;                
                Alarm_Timer.Enabled = true;
            }
            else
            {
                lifebitlbl.Text = "NOT CONNECTED";
                lifebitlbl.ForeColor = Color.Red;                
            }
            DataManInitialize_Stn1();
            fillcombo1();
            get_alarmaddress();
        }
        private void S7_Device_Initialize()
        {
            libnoInstance.write_bit_value(address, 61, 0, 1, true); //enable 2d matrix read in STN1           
            
            if (libnoInstance.read_bit_value(address, 61, 2, 5).ToString().ToUpper() != "FALSE")
            {
                libnoInstance.write_bit_value(address, 61, 2, 5, true); //Enable traceability server communication on Stn 1
                fillcombo1();
            }     

            libnoInstance.write_bit_value(address, 61, 2, 7, true); //Select english language in WINCC
        }
        public void DataManInitialize_Stn1()
        {
            try
            {
                dmSocket = new TcpClient(_sDataManIP, _dataManPort);
                Stream xstream = dmSocket.GetStream();
                stream = Stream.Synchronized(xstream);
                SocketReader = new BinaryReader(stream);
                stream.BeginRead(StorageByte, 0, 0, new AsyncCallback(OnDmReceive), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("DataManInit() : {0}", ex.Message));
            }
        }
        void OnDmReceive(IAsyncResult ar)                                                           // Data receive for station 1 DM Reader
        {
            if (!dmSocket.Connected) return;
            try
            {
                byte[] RecData = new byte[30];
                RecData = SocketReader.ReadBytes(dmSocket.Available);
                string RecStr = ASCIIEncoding.ASCII.GetString(RecData);
                readdata = RecStr;
                readdata = readdata.TrimEnd('\r', '\n', ' '); // Dataman include \r\n in result                
                stream.BeginRead(StorageByte, 0, 0, new AsyncCallback(OnDmReceive), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("READER 1 DATA READ ERROR !");
            }
        }
        private void fillcombo1()
        {
            string getsql = "SELECT APP_NAME FROM APP_MASTER_LANCE3 UNION SELECT '--SELECT--' AS APP_NAME FROM DUAL";
            DataSet DS = objora.GetData(getsql);
            cmbapp1.DataSource = DS.Tables[0];
            cmbapp1.DisplayMember = "APP_NAME";
            cmbapp1.ValueMember = "APP_NAME";
        }
        private void get_alarmaddress()
        {
            string getsql = "SELECT * FROM LANCE3_PLCCONFIG WHERE DATABLOCK = 'DB100' ORDER BY SERIAL_NO ASC";
            DataSet DS2 = objora.GetData(getsql);
            if (DS2.Tables[0].Rows.Count > 0)
            {
                alarmlists = new string[DS2.Tables[0].Rows.Count];
                for (int i = 0; i < DS2.Tables[0].Rows.Count; i++)
                {
                    string alarm = string.Empty;
                    alarm = DS2.Tables[0].Rows[i]["ADDRESS"].ToString();
                    alarmlists[i] = alarm;
                }
            }
        }

        private void Alarm_Timer_Tick(object sender, EventArgs e)
        {
            check_alarm();
            if (DM_Datastn1txt.Text == "" && readdata != "" && opmsglbl.Text != "CYCLE IN PROGRESS IN STATION 1 ")
            {
                DM_Datastn1txt.Text = readdata;    //ASSIGN THE READ DATA TO READER 1 TEXT BOX
            }
            if (libnoInstance.read_bit_value(address, 100, 8, 1).ToString().ToUpper() == "TRUE")
            {
                libnoInstance.write_bit_value(address, 100, 8, 1, false); // PC reset CODE READ TIMEOUT IN STN 1
            }
            check_trace_Bypass();
        }
        private void check_trace_Bypass()
        {
            LibnoDaveClass.PLCDataType dataType;

            if (libnoInstance.read_bit_value(address, 61, 2, 5).ToString().ToUpper() == "TRUE")                  //check traceability bypass in the PLC
            {
                return;
            }
            else
            {
                DM_Datastn1txt.Text = "BYPASSED";
                DM_Datastn1txt.ForeColor = Color.Red;
                DM_Datastn1txt.ReadOnly = true;
                glbbypass = "1";
            }
        }
        private void check_alarm()
        {
            alarmlist.Rows.Clear();
            int dbNo = 100;
            int byteno;
            int bitno;
            for (int j = 0; j < alarmlists.Count(); j++)
            {
                if (alarmlists[j].Length == 12)
                {
                    byteno = Int32.Parse(alarmlists[j].Substring(9, 1));
                    bitno = Int32.Parse(alarmlists[j].Substring(11, 1));
                }
                else
                {
                    byteno = Int32.Parse(alarmlists[j].Substring(9, 2));
                    bitno = Int32.Parse(alarmlists[j].Substring(12, 1));
                }
                if (libnoInstance.read_bit_value(address, dbNo, byteno, bitno).ToString().ToUpper() == "TRUE")
                {
                    if (check_duplicate_alarm(alarmlists[j]))
                    {
                        string getssql = "SELECT DESCRIPTION FROM LANCE3_PLCCONFIG WHERE ADDRESS = '" + alarmlists[j] + "'";
                        string message = objora.GetSingleValue(getssql);
                        alarmlist.Rows.Add(alarmlists[j], message);
                    }
                }

            }
            alarmlist.Refresh();
            alarmlist.AutoResizeColumns();
        }
        private bool check_duplicate_alarm(string DBAddress)
        {
            if (alarmlist.Rows[0].Cells[0].Value == null || alarmlist.Rows.Count == 0)
            {
                return true;
            }
            else
            {
                for (int i = 0; i < alarmlist.Rows.Count - 1; i++)
                {
                    if (alarmlist.Rows[i].Cells[0].Value.ToString() == DBAddress)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void alramresetbtn_Click(object sender, EventArgs e)
        {
            alarmlist.Rows.Clear();
        }

        private void cmbapp1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbapp1.SelectedIndex > 0)
            {
                string getapcode = "SELECT APP_CODE FROM APP_MASTER_LANCE3 WHERE APP_NAME = '" + cmbapp1.Text + "'";
                barref1 = objora.GetSingleValue(getapcode);
                DM_Datastn1txt.ReadOnly = false;
                DM_Datastn1txt.Focus();
            }
        }

        private void DM_Datastn1txt_TextChanged(object sender, EventArgs e)
        {
            if (DM_Datastn1txt.Text.Length > 10)
            {
                DM_Datastn1txt.Text = DM_Datastn1txt.Text.ToUpper();
                if (check_appref())
                {
                    if (check_previousstation_result(DM_Datastn1txt.Text))
                    {
                        libnoInstance.write_bit_value(address, 61, 0, 4, true);   //ok to flush the component in station 1     
                        libnoInstance.write_bit_value(address, 61, 0, 6, true);   //cycle in progress set to false

                        opmsglbl.Text = "PREVIOUS STATION RESULT OK !& LOAD THE COMPONENT";
                        opmsglbl.ForeColor = Color.Yellow;
                        Cyltimer_1.Enabled = true;
                    }
                    else
                    {
                        libnoInstance.write_bit_value(address, 61, 0, 4, false);   //nok to flush the component in station 1
                        opmsglbl.Text = "CYCLE ABORTED DUE TO PREVIOUS STATION RESULT FAIL";
                        opmsglbl.ForeColor = Color.Red;
                        Cyltimer_1.Enabled = false;
                        readdata = "";
                        DM_Datastn1txt.Focus();
                        DM_Datastn1txt.Text = "";
                    }

                }
                else
                {
                    opmsglbl.Text = "APPLICATION REFERENCE MISMATCH !";
                    opmsglbl.ForeColor = Color.Red;
                }
            }            
        }
        private bool check_appref()
        {
                if (DM_Datastn1txt.Text.Substring(0, 2) == barref1)
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
                    opmsglbl.Text = "NO PREVIOUS STATION DATA FOUND ! IN STN 1 ";
                    opmsglbl.ForeColor = Color.Red;
                    return false;
                }
                else if (DS.Tables[0].Rows[0]["STATUS_CODE"].ToString() == "FAIL")
                {
                    opmsglbl.Text = DS.Tables[0].Rows[0]["STATUS_MSG"].ToString();
                    opmsglbl.ForeColor = Color.Yellow;
                    return false;
                }
                else
                {
                    opmsglbl.Text = "PREVIOUS STATION RESULT OK ! (STN 1)";
                    opmsglbl.ForeColor = Color.LawnGreen;
                    return true;
                }                        
        }
        private void save_results(string Status, int stnid)
        {
            List<float> readprocess_Values;
            libnoInstance.read_real_values(address, 61, 56, 13, out readprocess_Values);
            string insertsql = "INSERT INTO LANCE3_RESULTS VALUES('" + DM_Datastn1txt.Text + "',SYSDATE,'" + stnid + "','"+readprocess_Values[0]+"'";
            insertsql += ",'" + readprocess_Values[1] + "','" + readprocess_Values[2] + "','" + readprocess_Values[3] + "','" + readprocess_Values[4] + "','" + readprocess_Values[5] + "'";
            insertsql += ",'" + readIntList[4] + "','" + readIntList[5] + "','" + readIntList[6] + "','" + readIntList[7] + "','" + readIntList[8] + "','" + Status + "','" + readprocess_Values[12] + "')";
            int res = objora.ExecuteQuery(insertsql);
            if (res == 1)
            {
                opmsglbl.Text = "DATA SAVED SUCCESSFULLY ! & SCAN THE NEXT COMPONENT";
                opmsglbl.ForeColor = Color.LawnGreen;

            }
        }
        private void filldatagrid(int stationid)
        {
            string filsql = "SELECT * FROM LANCE3_RESULTS WHERE TRUNC(DATE_TIME)>=TRUNC(SYSDATE-1) AND STN_ID = " + stationid + " ORDER BY DATE_TIME DESC";
            DataSet ds = objora.GetData(filsql);
            
                if (ds.Tables[0].Rows.Count > 0)
                {
                    dataGridView1.DataSource = ds.Tables[0];
                    dataGridView1.Refresh();
                    dataGridView1.AutoResizeColumns();
                }            
        }

        private void Cyltimer_1_Tick(object sender, EventArgs e)
        {
            if (libnoInstance.read_bit_value(address, 101, 0, 0).ToString().ToUpper() == "TRUE") //trigger message of closing guard in station 2
            {
                opmsglbl.Text = "CYCLE IN PROGRESS IN STATION 1 ";
                opmsglbl.ForeColor = Color.Yellow;                
            }

            if (libnoInstance.read_bit_value(address, 61, 0, 6).ToString().ToUpper() == "FALSE") //flushing in progress in station 1
            {
                dataType = LibnoDaveClass.PLCDataType.Integer;
                readIntList = null;
                if (libnoInstance.read_integer_values(address, 61, 8, 9, out readIntList, dataType, true))
                {
                    if (readIntList[0].ToString() == "0")
                    {
                        save_results("PASS", 1);
                        libnoInstance.write_bit_value(address, 61, 1, 0, true); //release componnet after datalogging
                        filldatagrid(1);
                        Cyltimer_1.Enabled = false;
                        libnoInstance.write_bit_value(address, 61, 0, 4, false);
                        readdata = "";
                        DM_Datastn1txt.Text = "";
                        DM_Datastn1txt.Focus();
                    }
                    else
                    {
                        save_results("FAIL", 1);
                        libnoInstance.write_bit_value(address, 61, 1, 0, true); //release componnet after datalogging
                        filldatagrid(1);
                        Cyltimer_1.Enabled = false;
                        libnoInstance.write_bit_value(address, 61, 0, 4, false);
                        readdata = "";
                        DM_Datastn1txt.Focus();
                        DM_Datastn1txt.Text = "";
                    }
                }
            }
            else
            {
                return;
            }
        }

        private void opmsglbl_Click(object sender, EventArgs e)
        {

        }

        private void alarmlist_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void panel27_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }
    }
}
