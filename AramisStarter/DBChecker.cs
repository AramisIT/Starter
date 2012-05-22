using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace AramisStarter
    {
    class DBChecker
        {
        internal static string GetConnectionString( string serverName, string database = null, string login = "AramisGuest", string password = null )
            {
            return string.Format( "Data Source={0};{1} User ID={2}; Password={3}",
                serverName,
                database == null ? "" : string.Format( " Initial Catalog={0};", database ),
                login,
                password != null ? password : "vjhrjdysqcjrjcnsdftn" );
            }

        internal static bool CheckPassword( string serverName, string databaseName, string userId, SecureString securePassword )
            {
            try
                {
                //using ( SqlConnection conn = new SqlConnection( GetConnectionString( serverName, databaseName, "sa", "123" ) ) )
                using ( SqlConnection conn = new SqlConnection( GetConnectionString( serverName, databaseName, "AramisUTKGuestId", "universal" ) ) )
                    {
                    conn.Open();
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
                        if ( result )
                            {
                            Starter.CurrentSessionId = sessionId;
                            }
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

        private static string SecureToStr( SecureString str )
            {
            IntPtr ptr = new IntPtr();
            ptr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR( str );
            string result = System.Runtime.InteropServices.Marshal.PtrToStringBSTR( ptr );

            System.Runtime.InteropServices.Marshal.ZeroFreeBSTR( ptr );
            return result;
            }

        }
    }
