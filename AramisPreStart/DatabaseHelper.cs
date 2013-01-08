using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using AramisStarter;
using System.Diagnostics;

namespace AramisPreStart
{
    class DatabaseHelper
    {
        private const string DATABASE_LOGIN = "AramisGuest";
        private const string DATABASE_PASSWORD = "vjhrjdysqcjrjcnsdftn";

        internal static SqlConnection GetOpenedUpdateConnection()
        {
            string connectionString = GetUpdateConnectionString();
            if (connectionString == null)
            {
                return null;
            }

            SqlConnectionStringBuilder connStrBuilder = new SqlConnectionStringBuilder(connectionString);
            connStrBuilder.ConnectTimeout = 5;
            SqlConnection conn = new SqlConnection(connStrBuilder.ConnectionString);

            try
            {
                conn.Open();
            }
            catch (Exception exp)
            {
                Trace.WriteLine(exp.Message);
                return null;
            }

            if (conn.State == ConnectionState.Open)
            {
                return conn;
            }
            else
            {
                return null;
            }
        }

        private static string GetUpdateConnectionString()
        {
            StarterUpdateDatabasePath starterUpdateDatabasePath;
            if (!StarterUpdateDatabasePath.GetStarterDatabasePath(out starterUpdateDatabasePath))
            {
                return null;
            }

            return string.Format("Data Source={0};Initial Catalog={1};User ID={2};Password={3}",
                starterUpdateDatabasePath.ServerName,
                starterUpdateDatabasePath.DatabaseName + "Update",
                DATABASE_LOGIN,
                DATABASE_PASSWORD);
        }




    }


}
