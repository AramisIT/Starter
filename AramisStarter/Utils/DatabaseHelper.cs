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

        private const string ARAMIS_GUEST_ID = "AramisUTKGuestId";
        private const string ARAMIS_GUEST_CRED = "universal";

        private const string ARAMIS_UPDATE_ID = "AramisUpdateFilesGetter";
        private const string ARAMIS_UPDATE_CRED = "vjhrjdysqcjrjcnsdftn";


        private static SqlConnection GetGuestConnection()
            {
            return new SqlConnection( GetConnectionString( App.SelectedSolution.SqlServerName,
                App.SelectedSolution.SqlBaseName,
                ARAMIS_GUEST_ID,
                ARAMIS_GUEST_CRED ) );
            }

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

        internal static string GetConnectionString( string serverName, string database = null, string login = "AramisGuest", string password = null )
            {
            return string.Format( "Data Source={0};{1} User ID={2}; Password={3}",
                serverName,
                database == null ? "" : string.Format( " Initial Catalog={0};", database ),
                login,
                password != null ? password : "vjhrjdysqcjrjcnsdftn" );
            }

        internal static bool CheckPassword( string userId, SecureString securePassword )
            {
            try
                {
                //using ( SqlConnection conn = new SqlConnection( GetConnectionString( serverName, databaseName, "sa", "123" ) ) )
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

        internal static bool WriteQuickStart( byte[] user, byte[] b )
            {
            #region Query text

            String queryText = @"

if (SELECT Count(*) FROM [QuickStart]) = 0
	begin		
		insert into [QuickStart]([User], [QuickInfo]) values(@User, @QuickInfo);
	end
else 
	begin
		update [QuickStart] Set [QuickInfo] = @QuickInfo where [User] = @User;		
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
                    cmd.Parameters.AddWithValue( "@User", user );
                    cmd.Parameters.AddWithValue( "@QuickInfo", b );
                    object result = cmd.ExecuteScalar();

                    return result != null && Convert.ToInt32( result ) == 1;
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

        internal static bool ReadQuickStart( byte[] user, out byte[] b )
            {
            String queryText = @" select Top 1 [QuickInfo] FROM [QuickStart] where [User] = @User";

            SqlConnection conn = GetGuestConnection();
            try
                {
                conn.Open();
                using ( SqlCommand cmd = new SqlCommand( queryText, conn ) )
                    {
                    cmd.Parameters.AddWithValue( "@User", user );
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

        internal static void EraseQuickStart( byte[] user )
            {
            String queryText = @" delete FROM [QuickStart] where [User] = @User";

            using ( SqlConnection conn = GetGuestConnection() )
                {
                conn.Open();
                using ( SqlCommand cmd = new SqlCommand( queryText, conn ) )
                    {
                    cmd.Parameters.AddWithValue( "@User", user );
                    cmd.ExecuteNonQuery();
                    }
                }
            }

        internal static SqlConnection GetUpdateConnection()
            {
            string updateConnectionString = string.Format( "Data Source={0}; Initial Catalog={1}; User ID={2}; Password={3}",
               App.SelectedSolution.SqlServerName,
               "AramisUpdate",
               ARAMIS_UPDATE_ID,
               ARAMIS_UPDATE_CRED );

            return new SqlConnection( updateConnectionString );
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

        #endregion


        }
    }
