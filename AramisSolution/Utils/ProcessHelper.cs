using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;

namespace AramisStarter.Utils
    {
    public static class ProcessHelper
        {
        public static List<Process> GetOtherSameProcessesList(bool forCurrentWinUserOnly = false)
            {
            Process currentProcess = Process.GetCurrentProcess();
            string currentProcessName = currentProcess.ProcessName;
            int currentProcessID = currentProcess.Id;
            string currentUserSID = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;
            bool returnAllSameProcess = !forCurrentWinUserOnly;

            return (
                from process in Process.GetProcessesByName(currentProcessName)
                where ((returnAllSameProcess || GetUserSIDByProcessID(process.Id) == currentUserSID)) && (process.Id != currentProcessID)
                select process).ToList<Process>();
            }

        private static string GetUserSIDByProcessID(int processID)
            {
            System.Management.ObjectQuery sq = new System.Management.ObjectQuery(string.Format("Select * from Win32_Process Where ProcessID = '{0}'", processID));
            System.Management.ManagementObjectSearcher searcher = new System.Management.ManagementObjectSearcher(sq);
            if (searcher.Get().Count == 0)
                {
                return string.Empty;
                }
            else
                {
                foreach (System.Management.ManagementObject searchResult in searcher.Get())
                    {
                    string[] sid = new String[1];
                    searchResult.InvokeMethod("GetOwnerSid", (object[])sid);
                    return sid[0];
                    }
                return string.Empty;
                }
            }

        public static void SafetyKill(this Process process)
            {
            try
                {
                process.Kill();
                }
            catch (Exception exp)
                {
                Trace.WriteLine(string.Format("process kill error: {0}", exp.Message));
                }
            }

        internal static void TerminateOldProcess(int processId)
            {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            const int MAX_TIME_TO_WAIT_MILLISEC = 4000;
            Process process = null;
            while (stopWatch.ElapsedMilliseconds < MAX_TIME_TO_WAIT_MILLISEC)
                {
                const int TIME_TO_SLEEP_MILLISEC = 500;
                Thread.Sleep(TIME_TO_SLEEP_MILLISEC);

                try
                    {
                    process = Process.GetProcessById(processId);
                    }
                catch (System.ArgumentException)
                    {
                    // process doesn't exist
                    return;
                    }
                }

            if (process != null)
                {
                process.SafetyKill();
                }
            }
        }
    }
