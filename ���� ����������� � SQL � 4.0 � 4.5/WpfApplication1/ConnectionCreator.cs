using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using ConnectionGetterInterface;

namespace WpfApplication1
    {
    class ConnectionCreator
        {
        private static IConnectionGetter connectionsGetter;

        public static SqlConnection ConnectionToSQL
            {
            get
                {
                if ( connectionsGetter == null )
                    {
                    connectionsGetter = CreateConnectionsGetter();
                    }
                return connectionsGetter.ConnectionToSQL;
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
            catch (Exception exp)
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
                //assemblyName = @"C:\Users\D\Desktop\WpfApplication1\ConnectionGetter\bin\Debug\Connection45.dll";

                assembly45 = Assembly.LoadFrom( assemblyName );
                Type type = assembly45.GetType( "ConnectionGetter.Connection45" );
                object obj = Activator.CreateInstance( type );
                //(from x in types where x.IsSubclassOf //typeof(IConnectionGetter).
                return obj as IConnectionGetter;
                }
            catch
                {
                return null;
                }

            }
        }
    }
