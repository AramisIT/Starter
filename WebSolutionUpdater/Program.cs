using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using WebSolutionUpdater.Helpers;

namespace WebSolutionUpdater
    {
    class Program
        {
        private static readonly string PATH = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string ERROR_FILE_NAME = string.Format(@"{0}\error.txt", PATH);

        static void Main(string[] args)
            {
            if (args.Length > 0)
                {
                if (tryExit(args[0])) return;
                }

            if (!checkThisProcessIsSingle()) return;

            waitInCaseOfTesting(ConfigurationManager.AppSettings["TestMutexFilePath"]);

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

                logError(errorDescription);

                timeout = 15000;
                }

            Thread.Sleep(timeout);
            }

        private static void waitInCaseOfTesting(string testMutexFilePath)
            {
            if (string.IsNullOrEmpty(testMutexFilePath)) return;

            while (true)
                {
                try
                    {
                    if (!File.Exists(testMutexFilePath)) return;

                    var fileInfo = new FileInfo(testMutexFilePath);
                    if (fileInfo.Length == 0) return;

                    Thread.Sleep(500);
                    }
                catch
                    {
                    return;
                    }
                }
            }

        private static bool logError(string message)
            {
            message = string.Format("{0}\t{1}", DateTime.Now.ToString("MM-dd HH:mm:ss"), message);

            try
                {
                if (File.Exists(ERROR_FILE_NAME))
                    {
                    File.AppendAllLines(ERROR_FILE_NAME, new List<string>() { message });
                    }
                else
                    {
                    File.WriteAllText(ERROR_FILE_NAME, message);
                    }
                }
            catch
                {
                return false;
                }
            return true;
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
