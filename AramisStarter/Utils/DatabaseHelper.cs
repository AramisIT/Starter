using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;


namespace AramisStarter.Utils
    {
    class DatabaseHelper
        {

        #region private

        private const string ARAMIS_GUEST_ID = "AramisGuest";
        private const string ARAMIS_GUEST_CRED = "vjhrjdysqcjrjcnsdftn";

        private static readonly List<string> SYSTEM_TABLES = new List<string>() { "master", "model", "msdb", "tempdb" };

        private static string SecureToStr( SecureString str )
            {
            IntPtr ptr = new IntPtr();
            ptr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR( str );
            string result = System.Runtime.InteropServices.Marshal.PtrToStringBSTR( ptr );

            System.Runtime.InteropServices.Marshal.ZeroFreeBSTR( ptr );
            return result;
            }

        #endregion

        #region public

        internal static string GetConnectionString( string serverName, string database = null )
            {
            return string.Format( @"Data Source=""{0}"";{1} User ID=""{2}""; Password=""{3}""",
                serverName,
                database == null ? "" : string.Format( @" Initial Catalog=""{0}"";", database ),
                ARAMIS_GUEST_ID,
                ARAMIS_GUEST_CRED );
            }

        internal static SqlConnection GetGuestConnection()
            {
            SqlConnectionStringBuilder connStrBuilder = new SqlConnectionStringBuilder( GetConnectionString( App.SelectedSolution.SqlServerName, App.SelectedSolution.SqlBaseName ) );
            connStrBuilder.ConnectTimeout = 1;
            return new SqlConnection( connStrBuilder.ConnectionString );
            }

        internal static SqlConnection GetUpdateConnection()
            {
            return new SqlConnection( GetConnectionString( App.SelectedSolution.SqlServerName, "AramisUpdate" ) );
           // return new SqlConnection( GetConnectionString( App.SelectedSolution.SqlServerName, App.SelectedSolution.SqlBaseName + "Update" ) );
            }

        internal static bool CheckPassword( string userId, SecureString securePassword )
            {
            try
                {
                using ( SqlConnection conn = GetGuestConnection() )
                    {
                    conn.Open();
                    //conn.Close();
                    //return true;

                    using ( SqlCommand cmd = new SqlCommand( "Registration", conn ) )
                        {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        Guid sessionId = System.Guid.NewGuid();
                        cmd.Parameters.AddWithValue( "@SessionId", sessionId );
                        cmd.Parameters.AddWithValue( "@Login", userId );
                        cmd.Parameters.AddWithValue( "@Password", GetPasswordHash( securePassword ) );

                        SqlParameter resultParameter = new SqlParameter( "@UserId", DbType.Int64 );
                        resultParameter.Direction = ParameterDirection.Output;
                        cmd.Parameters.Add( resultParameter );

                        cmd.ExecuteNonQuery();
                        object resultObj = resultParameter.Value;

                        long id = -1;
                        bool correctResult = ( resultObj != null ) && Int64.TryParse( resultObj.ToString(), out id );

                        bool result = correctResult && id > 0;

                        return result;
                        }
                    }
                }
            catch ( Exception exp )
                {
                Trace.WriteLine( exp.Message );
                }

            return false;
            }

        internal static string GetPasswordHash( SecureString str )
            {
            System.Security.Cryptography.MD5CryptoServiceProvider MD5CryptoProvider = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] data = MD5CryptoProvider.ComputeHash( System.Text.Encoding.ASCII.GetBytes( ( SecureToStr( str ) ) ) );
            string HashResult = System.Text.Encoding.ASCII.GetString( data );

            return HashResult.Trim();
            }

        internal static bool WriteQuickStart( Guid g, byte[] b )
            {
            #region Query text

            String queryText = @"

if (SELECT Count(*) FROM [QuickStart] where [G] = @G) = 0
	begin		
		insert into [QuickStart]([G], [QuickInfo]) values(@G, @QuickInfo);
	end
else 
	begin
		update [QuickStart] Set [QuickInfo] = @QuickInfo where [G] = @G;		
	end
select 1 ok;
";
            #endregion

            SqlConnection conn = GetGuestConnection();

            try
                {
                conn.Open();
                using ( SqlCommand cmd = new SqlCommand( queryText, conn ) )
                    {
                    cmd.Parameters.AddWithValue( "@G", g );
                    cmd.Parameters.AddWithValue( "@QuickInfo", b );
                    object result = cmd.ExecuteScalar();

                    return result != null && Convert.ToInt32( result ) == 1;
                    }
                }
            catch ( Exception exp )
                {
                System.Windows.MessageBox.Show( string.Format( "Не удалось сохранить пароль: {0}", exp.Message ) );
                return false;
                }
            finally
                {
                if ( conn != null )
                    {
                    ( ( IDisposable )conn ).Dispose();
                    }
                }
            }

        internal static bool ReadQuickStart( Guid g, out byte[] b )
            {
            String queryText = @"select Top 1 [QuickInfo] FROM [QuickStart] where [G] = @G";

            SqlConnection conn = GetGuestConnection();
            try
                {
                conn.Open();
                using ( SqlCommand cmd = new SqlCommand( queryText, conn ) )
                    {
                    cmd.Parameters.AddWithValue( "@G", g );
                    object result = cmd.ExecuteScalar();
                    b = ( byte[] )result;
                    }
                }
            catch
                {
                b = new byte[ 0 ];
                }
            finally
                {
                if ( conn != null )
                    {
                    ( ( IDisposable )conn ).Dispose();
                    }
                }

            return b != null && b.Length > 0;
            }

        internal static void EraseQuickStart( Guid id )
            {
            String queryText = @" delete FROM [QuickStart] where [G] = @G";

            using ( SqlConnection conn = GetGuestConnection() )
                {
                conn.Open();
                using ( SqlCommand cmd = new SqlCommand( queryText, conn ) )
                    {
                    cmd.Parameters.AddWithValue( "@G", id );
                    cmd.ExecuteNonQuery();
                    }
                }
            }

        internal static bool IsDatabaseUpdating()
            {
            String queryText = @"select Count(*) CantStart from [isUpdate] where [action] = 999 and LOWER([ApplicationName]) = @SolutionName";

            SqlConnection conn = GetUpdateConnection();
            try
                {
                conn.Open();
                using ( SqlCommand cmd = new SqlCommand( queryText, conn ) )
                    {
                    cmd.Parameters.AddWithValue( "@SolutionName", string.Format( "{0}.{1}", App.SelectedSolution.SolutionName, App.SelectedSolution.SqlBaseName ).ToLower() );
                    object result = cmd.ExecuteScalar();
                    bool can_tStart = Convert.ToInt32( result ) > 0;

                    return can_tStart;
                    }
                }
            catch
                {
                return false;
                }
            finally
                {
                if ( conn != null )
                    {
                    ( ( IDisposable )conn ).Dispose();
                    }
                }
            }

        internal static List<SolutionInfo> GetDatabasesList( string serverName, out string errorMessage )
            {
            errorMessage = null;
            List<string> databases = new List<string>();

            using ( SqlConnection conn = new SqlConnection( DatabaseHelper.GetConnectionString( serverName, "master" ) ) )
                {
                try
                    {
                    conn.Open();
                    }
                catch ( Exception exp )
                    {
                    errorMessage = "Не обнаружен сервер";
                    return null;
                    }

                try
                    {
                    using ( SqlCommand cmd = new SqlCommand( "select RTRIM([Name]) DatabaseName from sys.databases order by DatabaseName", conn ) )
                        {
                        using ( SqlDataReader reader = cmd.ExecuteReader() )
                            {
                            while ( reader.Read() )
                                {
                                string databaseName = reader[ 0 ] as string;
                                if ( !SYSTEM_TABLES.Contains( databaseName.ToLower() ) )
                                    {
                                    databases.Add( databaseName );
                                    }

                                }
                            }
                        }
                    }
                catch ( Exception exp )
                    {
                    Trace.WriteLine( exp.Message );
                    errorMessage = "Не удалось получить список информационных баз";
                    return null;
                    }
                }

            List<SolutionInfo> result = new List<SolutionInfo>();
            SolutionInfo solutionInfo;

            databases.ForEach( databaseName =>
                {
                    //if ( ReadSolutionInfo( serverName, databaseName, out solutionInfo ) )
                    //  {
                    result.Add( new SolutionInfo() { SqlServerName = serverName, SqlBaseName = databaseName } );
                    //  }
                } );

            return result;
            }

        #endregion

        internal static bool ReadSolutionInfo( string serverName, string databaseName, out SolutionInfo solutionInfo )
            {
            string systemNameInfo = GetSolutionNameInfo( serverName, databaseName );
            if ( systemNameInfo == null )
                {
                solutionInfo = null;
                return false;
                }

            solutionInfo = new SolutionInfo() { SqlServerName = serverName, SqlBaseName = databaseName };
            return SetSolutionName( solutionInfo, systemNameInfo );
            }

        private static bool SetSolutionName( SolutionInfo solutionInfo, string systemNameInfo )
            {
            int separatorIndex = systemNameInfo.IndexOf( ';' );

            if ( separatorIndex <= 0 )
                {
                return false;
                }
            else
                {
                solutionInfo.SolutionName = systemNameInfo.Substring( 0, separatorIndex ).Trim();
                solutionInfo.SolutionFriendlyName = systemNameInfo.Substring( separatorIndex + 1, systemNameInfo.Length - ( separatorIndex + 1 ) ).Trim();
                return solutionInfo.SolutionName.Length > 0;
                }
            }

        private static string GetSolutionNameInfo( string serverName, string databaseName )
            {
            try
                {
                using ( SqlConnection conn = new SqlConnection( DatabaseHelper.GetConnectionString( serverName, databaseName ) ) )
                    {
                    conn.Open();
                    using ( SqlCommand cmd = new SqlCommand( "select dbo.GetAramisSystemName()", conn ) )
                        {
                        string result = cmd.ExecuteScalar() as string;
                        return result;
                        }
                    }
                }
            catch ( Exception exp )
                {
                Trace.WriteLine( @"Error of ""select dbo.GetAramisSystemName()"" - " + exp.Message );
                return null;
                }
            }

        }
    }
