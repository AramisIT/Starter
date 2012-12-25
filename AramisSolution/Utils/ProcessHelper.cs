using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AramisStarter.Utils
    {
    public static class ProcessHelper
        {
        public static List<Process> GetOtherSameProcessesList( bool forCurrentWinUserOnly = false )
            {
            Process currentProcess = Process.GetCurrentProcess();
            string currentProcessName = currentProcess.ProcessName;
            int currentProcessID = currentProcess.Id;
            string currentUserSID = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;
            bool returnAllSameProcess = !forCurrentWinUserOnly;

            return (
                from process in Process.GetProcessesByName( currentProcessName )
                where ( ( returnAllSameProcess || GetUserSIDByProcessID( process.Id ) == currentUserSID ) ) && ( process.Id != currentProcessID )
                select process ).ToList<Process>();
            }


        private static string GetUserSIDByProcessID( int processID )
            {
            System.Management.ObjectQuery sq = new System.Management.ObjectQuery( string.Format( "Select * from Win32_Process Where ProcessID = '{0}'", processID ) );
            System.Management.ManagementObjectSearcher searcher = new System.Management.ManagementObjectSearcher( sq );
            if ( searcher.Get().Count == 0 )
                {
                return string.Empty;
                }
            else
                {
                foreach ( System.Management.ManagementObject searchResult in searcher.Get() )
                    {
                    string[] sid = new String[ 1 ];
                    searchResult.InvokeMethod( "GetOwnerSid", ( object[] )sid );
                    return sid[ 0 ];
                    }
                return string.Empty;
                }
            }

        }
    }
