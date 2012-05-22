using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using ConnectionGetterInterface;

namespace WpfApplication1
    {
    class ConnectionCreator
        {

         //SqlConnection conn = connectionsGetter.ConnectionToSQL;
                //StringBuilder sb = new StringBuilder();
                //List<string> after = GetLoadedAssemblies();
                //after.Sort();
                //after.ForEach( x => sb.Append( x ) );
                //MessageBox.Show( sb.ToString() );

        private static IConnectionGetter connectionsGetter;

        public static SqlConnection ConnectionToSQL
            {
            get
                {
                if ( connectionsGetter == null )
                    {                   
                    connectionsGetter = CreateConnectionsGetter();
                    }

                SqlConnection conn = connectionsGetter.ConnectionToSQL;
               
                StringBuilder sb = new StringBuilder();
                List<string> after = GetLoadedAssemblies();
                after.Sort();
                after.ForEach( x => sb.Append( x ) );
                MessageBox.Show( sb.ToString() );

                return conn;
                }
            }

        private static IConnectionGetter CreateConnectionsGetter()
            {
            IConnectionGetter result;

            result = TryLoad45();

            if ( result == null || !Check45( result ) )
                {
                result = new Connection40();
                }

            return result;
            }

        private static bool Check45( IConnectionGetter result )
            {
            try
                {
                result.CheckAccessibility();
                return true;
                }
            catch ( Exception exp )
                {
                MessageBox.Show( string.Format( "Check45 error: {0}", exp.Message ) );
                return false;
                }
            }

        private static IConnectionGetter TryLoad45()
            {
            Assembly assembly45;

            try
                {
                string assemblyName = "Connection45.dll";
                assemblyName = @"X:\SOFTWARE\Starter\Тест подключения к SQL в 4.0 и 4.5\ConnectionGetter\bin\Debug\Connection45.dll";

                assembly45 = Assembly.LoadFrom( assemblyName );
                Type type = assembly45.GetType( "ConnectionGetter.Connection45" );
                object obj = Activator.CreateInstance( type );
                //(from x in types where x.IsSubclassOf //typeof(IConnectionGetter).
                return obj as IConnectionGetter;
                }
            catch ( Exception exp )
                {
                Trace.WriteLine( exp.Message );
                return null;
                }

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
