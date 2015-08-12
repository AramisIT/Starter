﻿using System;
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
            if (args.Length > 0)
                {
                if (tryExit(args[0])) return;
                }

            if (!checkThisProcessIsSingle()) return;

            var publishDirectory = ConfigurationManager.AppSettings["PublishDirectory"];
            var updateTempFolder = ConfigurationManager.AppSettings["UpdateTempFolder"];
            var checkUrl = ConfigurationManager.AppSettings["CheckUrl"];

            if (string.IsNullOrEmpty(publishDirectory)
                || string.IsNullOrEmpty(updateTempFolder)
                || string.IsNullOrEmpty(checkUrl)) return;

            Thread.Sleep(3000); // allow to the web app to send last reply 

            string errorDescription;
            var result = new FilesUpdater(publishDirectory, updateTempFolder, checkUrl).PerformUpdate(out errorDescription);
            Console.WriteLine();

            var timeout = 4000;
            if (result)
                {
                Console.WriteLine("Application has been succsessfully updated!".ToUpper());
                }
            else
                {
                Console.WriteLine(string.Format(@"An error occured while application were updating:

{0}", errorDescription));

                timeout = 15000;
                }

            Thread.Sleep(timeout);
            }

        private static bool tryExit(string parameter)
            {
            int parameterInt;
            if (!Int32.TryParse(parameter, out parameterInt)) return false;

            if (parameterInt > 0)
                {
                parameterInt = Math.Min(parameterInt, 200);
                Thread.Sleep(parameterInt * 1000);
                }

            return true;
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
