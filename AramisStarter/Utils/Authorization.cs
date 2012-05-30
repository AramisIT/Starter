using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace AramisStarter.Utils
    {
    internal class Authorization
        {
        #region private

        private static RegistryKey registryKey;
        private const string DISTORTION = "Aramis.Net";
        private const string LAST_LOGIN = "DefaultUser";
        private const string L_REG_KEY = "QuickLogin";
        private const string A_REG_KEY = "A";
        private const string C_REG_KEY = "AA";

        static Authorization()
            {
            InitRegistryKey();
            }

        private static void InitRegistryKey()
            {
            string currentUserSID = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;

            registryKey = RegistryHelper.InitSubKey( string.Format( @"SIDs\{0}", Encryptor.Distort( currentUserSID, DISTORTION + currentUserSID + DISTORTION ) ) );
            }

        private static byte[] ConvertToByte( SecureString sStr )
            {
            int strLenght = sStr.Length;
            byte[] result = new byte[ strLenght * 2 ];
            sStr.MakeReadOnly();

            // Allocate HGlobal memory for source and destination strings
            IntPtr strPtr = Marshal.SecureStringToGlobalAllocUnicode( sStr );

            unsafe
                {
                ushort val;

                char* src = ( char* )strPtr.ToPointer();

                int resultIndex = 0;

                while ( strLenght > 0 )
                    {
                    strLenght--;

                    val = ( ushort )Convert.ToUInt16( *src );
                    byte[] bytes = BitConverter.GetBytes( val );

                    result[ resultIndex ] = ( byte )( bytes[ 0 ] );
                    resultIndex++;

                    result[ resultIndex ] = ( byte )( bytes[ 1 ] );
                    resultIndex++;

                    Array.Clear( bytes, 0, bytes.Length );
                    *src = Convert.ToChar( 0 );

                    src++;
                    }

                Marshal.ZeroFreeGlobalAllocUnicode( strPtr );
                }
            return result;
            }

        private static void SaveQuickStartData( string l, byte[] a, byte[] b, byte[] c )
            {
            if ( WriteQSToDatabase( c, b ) )
                {
                WriteQSToRegistry( l, a, c );
                }
            }

        private static bool ReadQS( string l, out byte[] a, out byte[] b, out byte[] c )
            {
            if ( ReadQSFromRegistry( l, out a, out c ) )
                {
                return ReadQSFromDatabase( c, out b );
                }
            else
                {
                a = new byte[ 0 ];
                b = a;
                c = a;
                return false;
                }
            }

        private static void WriteQSToRegistry( string l, byte[] a, byte[] c )
            {
            registryKey.SetValue( L_REG_KEY, l );
            registryKey.SetValue( A_REG_KEY, a );
            registryKey.SetValue( C_REG_KEY, c );
            }

        private static bool ReadQSFromRegistry( string l, out byte[] a, out byte[] c )
            {
            string savedL = null;
            a = new byte[ 0 ];
            c = a;

            try
                {
                savedL = registryKey.GetValue( L_REG_KEY ) as string;
                if ( savedL != l )
                    {
                    return false;
                    }
                a = ( byte[] )registryKey.GetValue( A_REG_KEY );
                c = ( byte[] )registryKey.GetValue( C_REG_KEY );
                }
            catch
                {
                return false;
                }

            return true;
            }

        private static bool WriteQSToDatabase( byte[] c, byte[] b )
            {
            return DatabaseHelper.WriteQuickStart( GetQuickUser( c ), b );
            }

        private static bool ReadQSFromDatabase( byte[] c, out byte[] b )
            {
            return DatabaseHelper.ReadQuickStart( GetQuickUser( c ), out b );
            }

        private static byte[] GetQuickUser( byte[] c )
            {
            string currentUserSID = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;
            string distortUserSID = Encryptor.Distort( currentUserSID, currentUserSID + DISTORTION );
            return ( new UTF8Encoding() ).GetBytes( distortUserSID );
            }

        private static void FillSStr( SecureString secureString, byte[] dataArray )
            {
            secureString.Clear();

            byte[] bytes = new byte[ 2 ];

            for ( int index = 0; index < dataArray.Length; index += 2 )
                {
                bytes[ 0 ] = ( byte )( dataArray[ index ] );
                bytes[ 1 ] = ( byte )( dataArray[ index + 1 ] );
                secureString.AppendChar( BitConverter.ToChar( bytes, 0 ) );
                }

            Array.Clear( dataArray, 0, dataArray.Length );
            Array.Clear( bytes, 0, bytes.Length );
            }

        #endregion

        #region public interface

        internal static void SaveLastLogin( string userName )
            {
            registryKey.SetValue( LAST_LOGIN, userName );
            }

        internal static string GetLastLogin()
            {
            return registryKey.GetValue( LAST_LOGIN ) as string;
            }

        internal static void SaveQuickStart( string userName, SecureString userPassword )
            {
            byte[] data = ConvertToByte( userPassword ), key, IV;
            byte[] encryptedData = Encryptor.EncryptData( data, out key, out IV );
            Array.Clear( data, 0, data.Length );

            SaveQuickStartData( userName, encryptedData, key, IV );
            }

        internal static SecureString TryToRestoreQuickStart( string userName )
            {
            byte[] data, key, IV;
            if ( !ReadQS( userName, out data, out key, out IV ) )
                {
                return null;
                }

            byte[] decryptData = Encryptor.DecryptData( data, key, IV );
            bool lengthIsEven = decryptData.Length % 2 == 0;

            if ( decryptData.Length == 0 || !lengthIsEven )
                {
                return null;
                }

            SecureString secureString = new SecureString();
            FillSStr( secureString, decryptData );
            secureString.MakeReadOnly();

            return secureString;
            }
        
        internal static void EraseQuickStartData( string userName )
            {
            string savedL = registryKey.GetValue( L_REG_KEY ) as string;
            if ( savedL != userName )
                {
                return;
                }

            byte[] c = ( byte[] )registryKey.GetValue( C_REG_KEY );
            if ( c != null )
                {
                DatabaseHelper.EraseQuickStart( GetQuickUser( c ) );
                }

            #region delete from registry

            try
                {
                registryKey.DeleteValue( L_REG_KEY );
                registryKey.DeleteValue( A_REG_KEY );
                registryKey.DeleteValue( C_REG_KEY );
                }
            catch { }

            #endregion
            }
        
        #endregion

        }
    }
