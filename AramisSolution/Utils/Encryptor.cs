using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace AramisStarter.Utils
    {
    internal class Encryptor
        {
        internal static byte[] EncryptData(byte[] dataToSign, out byte[] Key, out byte[] IV)
            {
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(Guid.NewGuid().ToString(), null);
            //генерируем ключ экспорта
            pdb.HashName = "SHA512";

            Rijndael rijndael = (Rijndael)new RijndaelManaged();
            rijndael.KeySize = 256;
            rijndael.Key = pdb.GetBytes(256 / 8);
            rijndael.IV = pdb.GetBytes(rijndael.BlockSize / 8);
            IV = rijndael.IV;
            Key = rijndael.Key;

            MemoryStream memoryStream = new MemoryStream(dataToSign);
            byte[] arr = memoryStream.ToArray();
            ICryptoTransform cryptoTransform = rijndael.CreateEncryptor();
            arr = cryptoTransform.TransformFinalBlock(arr, 0, arr.Length);

            MemoryStream encryptData = new MemoryStream();
            encryptData.Write(arr, 0, arr.Length);
            rijndael.Clear();
            memoryStream.Close();
            encryptData.Flush();
            byte[] result = encryptData.ToArray();
            encryptData.Close();

            return result;
            }

        internal static byte[] DecryptData(byte[] data, byte[] Key, byte[] IV)
            {
            MemoryStream memoryStream = new MemoryStream(data);

            // Create a new Rijndael object.
            Rijndael RijndaelAlg = Rijndael.Create();

            // Create a CryptoStream using the FileStream 
            // and the passed key and initialization vector (IV).
            CryptoStream cStream = new CryptoStream(memoryStream,
                RijndaelAlg.CreateDecryptor(Key, IV),
                CryptoStreamMode.Read);

            // Create a StreamReader using the CryptoStream.
            StreamReader sReader = new StreamReader(cStream);

            using (MemoryStream memstream = new MemoryStream())
                {
                sReader.BaseStream.CopyTo(memstream);
                return memstream.ToArray();
                }
            }

        internal static string Distort(string data, string key)
            {
            byte[] dataBytes = new UTF8Encoding().GetBytes(data);
            byte[] keyBytes = new UTF8Encoding().GetBytes(key);

            long k = 0;
            for (int i = 0; i < keyBytes.Length; i++)
                {
                k += keyBytes[i] * i;
                }

            byte shortk = (byte)(k % byte.MaxValue);

            for (int i = 0; i < dataBytes.Length; i++)
                {
                dataBytes[i] = (byte)(dataBytes[i] ^ shortk);
                }

            return new UTF8Encoding().GetString(dataBytes);
            }

        internal static byte[] ConvertToByte(SecureString sStr)
            {
            int strLenght = sStr.Length;
            byte[] result = new byte[strLenght * 2];

            // Allocate HGlobal memory for source and destination strings
            IntPtr strPtr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(sStr);

            unsafe
                {
                ushort val;

                ushort key = (ushort)(ushort.MaxValue - DateTime.Now.Year * DateTime.Now.Month);

                char* src = (char*)strPtr.ToPointer();

                int resultIndex = 0;

                while (strLenght > 0)
                    {
                    strLenght--;

                    val = (ushort)(Convert.ToUInt16(*src) ^ key);
                    byte[] bytes = BitConverter.GetBytes(val);

                    byte rand = 86;
                    result[resultIndex] = (byte)(bytes[0] ^ rand);
                    resultIndex++;

                    result[resultIndex] = (byte)(bytes[1] ^ rand);
                    resultIndex++;

                    Array.Clear(bytes, 0, bytes.Length);
                    *src = Convert.ToChar(0);

                    src++;
                    }

                System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(strPtr);
                }

            return result;
            }

        }
    }
