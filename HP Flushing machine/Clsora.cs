using System;
using System.Collections.Generic;
using System.Text;
using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;
using System.Data;
using System.Windows.Forms;
using System.IO;

namespace HP_Flushing_SCADA
{
    class Clsora
    {
        string ConnectionString = " Data Source= (DESCRIPTION =(ADDRESS_LIST =(ADDRESS = (PROTOCOL = TCP)(HOST =192.168.254.134)(PORT = 1521)))(CONNECT_DATA =(SERVICE_NAME =PPLPS)));User Id=PPLPSLIVE;Password=PPLPSLIVE012;";
        OracleConnection conora;

        Clslogwrite objlog = new Clslogwrite();

        public bool Connection()
        {
            conora = new OracleConnection();

            conora.ConnectionString = ConnectionString;

            try
            {
                conora.Open();
                return true;
            }
            catch (Exception ex)
            {
                objlog.LogWrite("Application was not started due to ora error!");
                objlog.LogWrite(ex.ToString());
                return false;
            }

        }
        public void ConnectionClose()
        {
            conora.Close();            
        }
        public string GetSingleValue(string sPName)
        {
            try
            {

                OracleCommand command = new OracleCommand();

                command.Connection = conora;
                command.CommandType = CommandType.Text;
                command.CommandText = sPName;

                object rvalue = command.ExecuteScalar();
                if (rvalue != null && rvalue != DBNull.Value)
                {
                    return rvalue.ToString();
                }
                else
                {
                    return "N";
                }
            }
            catch (Exception eX)
            {
                objlog.LogWrite(eX.ToString());
                throw eX;
            }
            finally
            {

            }
        }
        public DataSet GetData(string sPName)
        {
            OracleDataAdapter dA = new OracleDataAdapter();
            try
            {

                dA.SelectCommand = new OracleCommand(sPName, conora);

                DataSet dS = new DataSet();
                dA.Fill(dS);
                return dS;

            }
            catch (Exception eX)
            {
                objlog.LogWrite(conora + "Error");

                objlog.LogWrite(eX.ToString());
                throw eX;
            }
            finally
            {
                //dA = null;
                //connection.Close();
                //connection.Dispose();
            }
        }

        public int ExecutebulkQuery(string[] mSPName)
        {


            OracleCommand dbCommand = new OracleCommand();
            OracleTransaction mytrans;
            mytrans = conora.BeginTransaction(IsolationLevel.ReadCommitted);

            try
            {
                dbCommand.Connection = conora;

                dbCommand.CommandType = CommandType.Text;
                for (int a = 0; a < mSPName.Length; a++)
                {
                    string rquery = mSPName[a];
                    if (rquery != null)
                    {
                        dbCommand.CommandText = rquery;
                        int returnVal = dbCommand.ExecuteNonQuery();
                    }
                }


                mytrans.Commit();

                return 1;
            }
            catch (Exception e)
            {

                MessageBox.Show(e.ToString());
                mytrans.Rollback();
                return -1;
            }

        }


        public int ExecuteQuery(string mSPName)
        {

            try
            {

                OracleCommand dbCommand = new OracleCommand();

                dbCommand.Connection = conora;
                dbCommand.CommandType = CommandType.Text;
                dbCommand.CommandText = mSPName;
                int returnVal = dbCommand.ExecuteNonQuery();

                return returnVal;
            }
            catch (Exception eX)
            {
                objlog.LogWrite(eX.ToString());
                throw eX;
            }
            finally
            {

            }
        }

        public int Executestroreprocedure(string Procedurename, string VUSERID, string VTWO2D, string VSENSOR)
        {
            try
            {
                OracleCommand cmd = new OracleCommand(Procedurename, conora);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("VUSERID", "VARCHAR2(200);").Value = VUSERID;
                cmd.Parameters.Add("VTWO2D", "VARCHAR2(200);").Value = VTWO2D;
                cmd.Parameters.Add("VSENSOR", "VARCHAR2(200);").Value = VSENSOR;
                cmd.Parameters.Add("VPLV", "VARCHAR2(200);").Value = "";
                cmd.Parameters.Add("VLABELCODE", "VARCHAR2(200);").Value = "";
                cmd.Parameters.Add("VARCH_FLG", "VARCHAR2(200);").Value = "";
                int returnVal = cmd.ExecuteNonQuery();
                return returnVal;
            }
            catch (Exception Ex)
            {
                objlog.LogWrite(Ex.ToString());
                MessageBox.Show(Ex.ToString());
                return -1;
            }
        }
    }
}
