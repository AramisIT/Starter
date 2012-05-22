using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AramisStarter
    {
    class HashGenerator
        {
        internal static string GetFileHash( string filePath, HashType type )
            {
            if ( !File.Exists( filePath ) )
                return string.Empty;

            System.Security.Cryptography.HashAlgorithm hasher;
            switch ( type )
                {
                case HashType.SHA1:
                default:
                    hasher = new SHA1CryptoServiceProvider();
                    break;
                case HashType.SHA256:
                    hasher = new SHA256Managed();
                    break;
                case HashType.SHA384:
                    hasher = new SHA384Managed();
                    break;
                case HashType.SHA512:
                    hasher = new SHA512Managed();
                    break;
                case HashType.MD5:
                    hasher = new MD5CryptoServiceProvider();
                    break;
                case HashType.RIPEMD160:
                    hasher = new RIPEMD160Managed();
                    break;
                }
            StringBuilder buff = new StringBuilder();
            try
                {
                using ( FileStream f = new FileStream( filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192 ) )
                    {
                    hasher.ComputeHash( f );
                    Byte[] hash = hasher.Hash;
                    foreach ( Byte hashByte in hash )
                        {
                        buff.Append( string.Format( "{0:x2}", hashByte ) );
                        }
                    }
                }
            catch ( Exception exp )
                {
                string str = new System.Random( DateTime.Now.Second * DateTime.Now.Millisecond ).Next().ToString();
                // String.Format( "HashGenerator.GetFileHash.\r\n\t{0}", exp.Message ).Log( "Error reading file." + str );
                return "Error reading file." + str;
                }
            return buff.ToString();
            }

        internal enum HashType
            {
            [Description( "SHA-1" )]
            SHA1,
            [Description( "SHA-256" )]
            SHA256,
            [Description( "SHA-384" )]
            SHA384,
            [Description( "SHA-512" )]
            SHA512,
            [Description( "MD5" )]
            MD5,
            [Description( "RIPEMD-160" )]
            RIPEMD160
            }
        }
    }
