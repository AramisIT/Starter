using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
    {
    static class Program
        {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
            {
            SqlConnection conn = new SqlConnection();

            StringBuilder sb = new StringBuilder();
            List<string> after = GetLoadedAssemblies();
            after.Sort();
            after.ForEach( x => sb.Append( x ) );
            MessageBox.Show( sb.ToString() );          
            }

        private static List<string> GetLoadedAssemblies()
            {
            List<string> sb = new List<string>();
            foreach ( Assembly a in AppDomain.CurrentDomain.GetAssemblies() )
                {
                string fileName = Path.GetFileName( a.Location );
                string filePath = Path.GetDirectoryName( a.Location );
                if ( fileName.Length > 10 && fileName.Substring( 0, 10 ) == "DevExpress" )
                    {
                    continue;
                    }
                sb.Add( string.Format( "{1}\\{0}                                                                                              \r\n", fileName, filePath ) );
                }

            return sb;
            }
        }
    }
