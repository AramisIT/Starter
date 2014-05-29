using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows;
using AramisStarter.FilesDownloading;
using AramisStarter.Utils;
using System.IO;

namespace AramisStarter
    {
    class Starter
        {
        #region private

        #region consts

        private const string SERVER_NAME_STR = "ServerName";

        private const string DATABASE_NAME_STR = "DatabaseName";

        private const string USER_ID_STR = "User";

        private const string USER_KEY_STR = "UserKey";

        private const string STARTER_PATH_VAR_NAME = "StarterPath";
        
        private const string UPDATE_EXISTS_VAR_NAME = "UpdatesExists";

        private const string LOADED_STR = "AramisSystemLoaded";

        private const string UPDATE_VERSION_VAR_NAME = "DownloadedUpdateNumber";

        private const int EXECUTING_TIME_SEC = 5;

        private string EXIT_STATUS_VAR_NAME = "ExitStatus";

        private const int WAIT_FOR_THREADS_COMPLATING_INTERVAL_MILLISEC = 500;

        private int WAIT_FOR_RUN_INTERVAL_MILLISEC = 400;

        private const int WAIT_FOT_DATABASE_UPDATE_INTERVAL_SEC = 2;

        private const int MAX_TIME_FOR_DB_UPDATING_SEC = 180;

        private static readonly Mutex RUN_SOLUTION_MUTEX = GetRunMutex();

        private static Mutex GetRunMutex()
            {
            string mutexName = string.Format("START_Aramis.Net_{0}", App.SelectedSolution.BuildFullSolutionName());
            return new Mutex(false, mutexName);
            }

        #endregion

        private static volatile bool solutionExecuting;

        private static volatile bool errorStart;

        private static bool starterInitialized;

        private static Starter starter;

        private SolutionInfo solution;
        private Thread solutionThread;
        private string solutionExecutiveFileName;
        private AppDomain solutionDomain;

        #region constructor

        private Starter(SolutionInfo solutionInfo)
            {
            solution = solutionInfo;

            solutionExecutiveFileName = App.SolutionDirPath + solution.SolutionName + ".exe";

            StartExecutorThread();
            }

        private void StartExecutorThread()
            {
            solutionThread = new Thread(SolutionExecutorMethod);
            solutionThread.Name = "Solution executor";
            solutionThread.SetApartmentState(ApartmentState.STA);
            solutionThread.Start();
            }

        #endregion

        private void InitSolutionDomain()
            {
            solutionDomain = AppDomain.CreateDomain("Solution domain", null, new AppDomainSetup
            {
                ApplicationBase = App.SolutionDirPath.Substring(0, App.SolutionDirPath.Length - 1),
                ConfigurationFile = solutionExecutiveFileName + ".config"
            });

            solutionDomain.SetData(LOADED_STR, false);

            solutionDomain.SetData(SERVER_NAME_STR, App.SelectedSolution.SqlServerName);
            solutionDomain.SetData(DATABASE_NAME_STR, App.SelectedSolution.SqlBaseName);

            solutionDomain.SetData(STARTER_PATH_VAR_NAME, SolutionUpdater.STARTER_PATH);

            solutionDomain.SetData(USER_ID_STR, LoginWindow.UserName);
            solutionDomain.SetData(USER_KEY_STR, Encryptor.ConvertToByte(LoginWindow.UserPassword));

            solutionDomain.SetData(EXECUTE_ERROR, "");
            }

        private bool RegistrateInRegisrty()
            {
            Process currentProcess = Process.GetCurrentProcess();

            string processId = currentProcess.Id.ToString();
            string startTime = currentProcess.StartTime.ToString(DATE_TIME_FORMAT);

            RegistryHelper.ProcessesIDsRegistryKey.SetValue(processId, startTime);

            string wrotenValue;
            try
                {
                wrotenValue = RegistryHelper.ProcessesIDsRegistryKey.GetValue(processId) as string;
                return startTime == wrotenValue;
                }
            catch
                {
                solutionDomain.SetData(EXECUTE_ERROR, "Can't registrate process in registry");
                return false;
                }
            }

        private bool RegistrateSolutionExecuting()
            {
            SyncHelper.EnterMutex(RunSolutionMutex);

            solutionExecuting = true;

            if (!RegistrateInRegisrty())
                {
                SyncHelper.ExitMutex(RunSolutionMutex);
                Splash.SplashWindow.HideWindow();
                MessageBox.Show("Не удается получить доступ к реестру. Обратитесь к системному администратору для получения прав.\r\n\r\nВетвь: " + RegistryHelper.RegistryMainNodeName,
                    "Aramis.NET",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                errorStart = true;
                return false;
                }

            SyncHelper.ExitMutex(RunSolutionMutex);

            return true;
            }

        private static void RegistrateSolutionExit()
            {
            Process currentProcess = Process.GetCurrentProcess();

            string processId = currentProcess.Id.ToString();
            try
                {
                RegistryHelper.ProcessesIDsRegistryKey.DeleteValue(processId);
                }
            catch
            { }

            solutionExecuting = false;
            }

        private void SafetySetDomainData(string varName, object newUpdateDownloaded)
            {
            bool valueSetted;
            do
                {
                valueSetted = true;
                try
                    {
                    solutionDomain.SetData(varName, newUpdateDownloaded);
                    }
                catch
                    {
                    valueSetted = false;
                    Thread.Sleep(100);
                    }
                } while (!valueSetted);
            }

        private void ExecuteSolution()
            {
            if (!RegistrateSolutionExecuting())
                {
                return;
                }

            InitSolutionDomain();
            DateTime startTime = DateTime.Now;

            try
                {
                solutionDomain.ExecuteAssembly(solutionExecutiveFileName);
                Log.Append("Executed - OK");
                }
            catch (ThreadAbortException abortExp)
                {
                Log.Append("Executing ThreadAbortException - " + abortExp.Message);
                App.Stop();
                return;
                }

            catch (Exception exp)
                {
                Log.Append("Executing exception - " + exp.Message);
                executionError = string.Format("ExecuteAssembly error: {0}", exp.Message);
                Trace.WriteLine(executionError);

                string fileName = Path.GetDirectoryName(SolutionUpdater.STARTER_PATH) + "\\" + "lastError.txt";
                try
                    {
                    File.WriteAllText(fileName, exp.Message + "\r\n" + exp.StackTrace);
                    }
                catch { }

                if (ExecutingTimeUnreallySmall(startTime))
                    {
                    SolutionUpdater.ResetVersion();
                    // tell to updater to updateVersion
                    return;
                    }
                }

            while (solutionDomain.GetData(EXIT_STATUS_VAR_NAME) == null)
                {
                Thread.Sleep(WAIT_FOR_THREADS_COMPLATING_INTERVAL_MILLISEC);
                }

            //GC.WaitForPendingFinalizers();
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
            //object exitForUpdateObj = solutionDomain.GetData(UPDATE_MODE_VAR_NAME);
            //object forsedUpdateObj = solutionDomain.GetData(FORSED_UPDATE_VAR_NAME);

            //try
            //    {
            //    Log.Append("before domain unload");
            //    AppDomain.Unload(solutionDomain);
            //    Log.Append("domain unloaded");
            //    }
            //catch
            //    {
            //    Log.Append("error domain unloaded");
            //    return false;
            //    }

            //solutionDomain = null;
            //GC.Collect();
            //GC.WaitForPendingFinalizers();

            //RegistrateSolutionExit();
            //Log.Append("RegistrateSolutionExit() solutionExecuting = " + solutionExecuting.ToString());

            //exitForUpdate = (exitForUpdateObj != null) && (exitForUpdateObj is bool) && (bool)exitForUpdateObj;
            //forsedUpdate = (forsedUpdateObj != null) && (forsedUpdateObj is bool) && (bool)forsedUpdateObj;
            //Log.Append("exit ExecuteSolution");
            //return true;
            }

        private bool WaitForPermissionToStart()
            {
            DateTime startTime = DateTime.Now;
            while (DatabaseHelper.IsDatabaseUpdating())
                {
                Thread.Sleep(WAIT_FOT_DATABASE_UPDATE_INTERVAL_SEC * 1000);

                int totalSec = (int)((TimeSpan)(DateTime.Now - startTime)).TotalSeconds;
                if (totalSec > MAX_TIME_FOR_DB_UPDATING_SEC)
                    {
                    return false;
                    }
                }

            return true;
            }

        private static bool ExecutingTimeUnreallySmall(DateTime startTime)
            {
            int executingTimeSec = (int)((TimeSpan)(DateTime.Now - startTime)).TotalSeconds;
            return executingTimeSec < EXECUTING_TIME_SEC;
            }

        private void SolutionExecutorMethod(object state)
            {
            while (!(SolutionUpdater.ReadyToRun && LoginWindow.Authorized))
                {
                Thread.Sleep(WAIT_FOR_RUN_INTERVAL_MILLISEC);
                }

            if (!WaitForPermissionToStart())
                {
                return;
                }

            ExecuteSolution();
            App.Stop();

            //bool exit = false;
            //do
            //    {
            //    if (SolutionUpdater.ReadyToRun && LoginWindow.Authorized)
            //        {
            //        if (!WaitForPermissionToStart())
            //            {
            //            return;
            //            }

            //        bool exitForUpdate, forsedUpdate;
            //        exit = !ExecuteSolution(out exitForUpdate, out forsedUpdate) || !exitForUpdate;

            //        if (exitForUpdate)
            //            {
            //            SolutionUpdater.MakeToUpdate(forsedUpdate);
            //            }
            //        }
            //    else
            //        {
            //        Thread.Sleep(WAIT_FOR_RUN_INTERVAL_MILLISEC);
            //        }
            //    }
            //while (!exit);

            //App.Stop();
            }

        #endregion

        #region public

        internal const string DATE_TIME_FORMAT = "dd.MM.yyyy HH:mm:ss";
        internal const string EXECUTE_ERROR = "ExecutingError";
        internal static string executionError;

        /// <summary>
        /// Возвращает true если были ошибки при старте, из-за которых приложение не смогло запустится
        /// </summary>
        internal static bool ErrorStart
            {
            get
                {
                if (!starterInitialized)
                    {
                    return false;
                    }
                else
                    {
                    return Starter.errorStart;
                    }
                }
            }

        /// <summary>
        /// Возвращает true если запустившееся приложение завершило необходимую инициализацию
        /// </summary>
        internal static bool SolutionLoaded
            {
            get
                {
                if (!starterInitialized || !solutionExecuting)
                    {
                    return false;
                    }
                else
                    {
                    bool solutionLoaded = false;
                    try
                        {
                        solutionLoaded = (bool)starter.solutionDomain.GetData(LOADED_STR);
                        }
                    catch { }

                    return solutionLoaded; //solutionLoaded
                    }
                }
            }

        /// <summary>
        /// Возвращает мьютекс, используемый при запуске приложения
        /// </summary>
        internal static Mutex RunSolutionMutex
            {
            get
                {
                return RUN_SOLUTION_MUTEX;
                }
            }

        /// <summary>
        /// Возвращает true если приложение запущено
        /// </summary>
        internal static bool SolutionExecuting
            {
            get
                {
                if (!starterInitialized)
                    {
                    return false;
                    }
                else
                    {
                    return solutionExecuting;
                    }
                }
            }

        /// <summary>
        /// Выполняет единоразовую инициализацию, создает приватный экземпляр класса
        /// </summary>
        internal static void Init(SolutionInfo solution)
            {
            if (!starterInitialized)
                {
                starter = new Starter(solution);
                starterInitialized = true;
                }
            }

        /// <summary>
        /// Сообщить приложению про доступность обновления и его версию
        /// </summary>
        internal static void SetUpdateExistingStatus(bool newUpdateDownloaded, int version = 0)
            {
            if (!starterInitialized)
                {
                return;
                }

            if (solutionExecuting && starter.solutionDomain != null)
                {
                starter.SafetySetDomainData(UPDATE_EXISTS_VAR_NAME, newUpdateDownloaded);

                if (newUpdateDownloaded)
                    {
                    starter.SafetySetDomainData(UPDATE_VERSION_VAR_NAME, version);
                    }
                }
            }

        #endregion
        }
    }
