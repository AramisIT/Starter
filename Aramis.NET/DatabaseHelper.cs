using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Aramis.NET
    {
    class DatabaseHelper
        {
        private static readonly string CONNECTION_STRING = GetConnectionString();
        private const string DATABASE_NAME = "AramisUpdate";
        private const string DATABASE_LOGIN = "AramisUpdateFilesGetter";
        private const string DATABASE_PASSWORD = "vjhrjdysqcjrjcnsdftn";  

        internal static SqlConnection GetOpenedConnection()
            {
            SqlConnection conn = new SqlConnection( CONNECTION_STRING );
            try
                {
                conn.Open();
                }
            catch
                {
                return null;
                }

            if ( conn.State == ConnectionState.Open )
                {
                return conn;
                }
            else
                {
                return null;
                }
            }

        private static string GetConnectionString()
            {
            return string.Format( "Data Source={0};Initial Catalog={1};User ID={2};Password={3}",
                GetDataSource(),
                DATABASE_NAME,
                DATABASE_LOGIN,
                DATABASE_PASSWORD );
            }

        private static string GetDataSource()
            {
            return System.Configuration.ConfigurationManager.AppSettings[ "ServerName" ] ?? "localhost";
            }
        }
    }
