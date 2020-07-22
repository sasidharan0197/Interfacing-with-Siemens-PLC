using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace HP_Flushing_SCADA
{
    class Clslogwrite
    {
        public static string m_exePath = string.Empty;

        public string LogWrite(string logMessage)
        {
            try
            {
                m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                using (StreamWriter w = File.AppendText(m_exePath + "\\" + "HP_Flushing_L3_log.txt"))
                {
                    Log(logMessage, w);
                    return "0";
                }
            }
            catch (Exception ex)
            {
                return "-1";
                throw ex;
            }
        }

        public void Log(string logMessage, TextWriter txtWriter)
        {
            try
            {
                txtWriter.Write("\r\nLog Entry : ");
                txtWriter.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                DateTime.Now.ToLongDateString());
                txtWriter.WriteLine("  :");
                txtWriter.WriteLine("  :{0}", logMessage);
                txtWriter.WriteLine("--------------------------------------------------------------------------");
                txtWriter.WriteLine("--------------------------------------------------------------------------");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
