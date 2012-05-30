using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Win32;

namespace AramisStarter.Utils
    {
    internal class RegistryHelper
        {
        private static RegistryKey processesIDsRegistryKey = InitSubKey( "PIDs" );
       
        #region public

        /// <summary>
        /// Возвращает раздел реестра, в котором хранится перечень запущенных процессов
        /// </summary>
        internal static RegistryKey ProcessesIDsRegistryKey
            {
            get
                {
                return RegistryHelper.processesIDsRegistryKey;
                }
            }

        internal static string RegistryMainNodeName
            {
            get;
            private set;
            }

        internal static void Init( string solutionName, string sqlBaseName )
            {
            RegistryMainNodeName = string.Format( @"Software\Aramis .NET\{0}.{1}\", solutionName, sqlBaseName );
            }

        internal static RegistryKey InitSubKey( string subKeySuffix )
            {
            string subKeyName = RegistryMainNodeName + subKeySuffix;

            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey( subKeyName, RegistryKeyPermissionCheck.ReadWriteSubTree, System.Security.AccessControl.RegistryRights.FullControl );

            if ( registryKey == null )
                {
                Registry.CurrentUser.CreateSubKey( subKeyName, RegistryKeyPermissionCheck.ReadWriteSubTree );
                registryKey = Registry.CurrentUser.OpenSubKey( subKeyName, RegistryKeyPermissionCheck.ReadWriteSubTree, System.Security.AccessControl.RegistryRights.FullControl );
                }

            return registryKey;
            }
        
        #endregion
        }
    }
