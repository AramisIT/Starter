using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace WebSolutionUpdater
    {
    class Program
        {
        static void Main(string[] args)
            {
            if (!checkThisProcessIsSingle()) return;

            var publishDirectory = ConfigurationManager.AppSettings["PublishDirectory"];
            var updateTempFolder = ConfigurationManager.AppSettings["UpdateTempFolder"];
            var checkUrl = ConfigurationManager.AppSettings["CheckUrl"];

            if (string.IsNullOrEmpty(publishDirectory)
                || string.IsNullOrEmpty(updateTempFolder)
                || string.IsNullOrEmpty(checkUrl)) return;

            var result = new FilesUpdater(publishDirectory, updateTempFolder, checkUrl).PerformUpdate();
            Console.WriteLine();

            if (result)
                {
                Console.WriteLine("Application has been succsessfully updated!".ToUpper());
                }
            else
                {
                Console.WriteLine("An error occured while application were updating!");
                }

            Thread.Sleep(4000);
            }

        private static bool checkThisProcessIsSingle()
            {
            var processes = getProcesses();

            const int MIN_TIME_FOR_RUNNING_SEC = 120;
            processes.ForEach(process =>
                {
                    if ((DateTime.Now - process.StartTime).TotalSeconds > MIN_TIME_FOR_RUNNING_SEC)
                        {
                        try
                            {
                            process.Kill();
                            }
                        catch { }
                        }
                });

            return getProcesses().Count == 0;
            }

        private static List<Process> getProcesses()
            {
            Process currentProcess = Process.GetCurrentProcess();
            string currentProcessName = currentProcess.ProcessName;
            int currentProcessID = currentProcess.Id;

            List<Process> processes = (from process in Process.GetProcessesByName(currentProcessName)
                                       where process.Id != currentProcessID
                                       select process).ToList<Process>();

            return processes;
            }
        }
    }
