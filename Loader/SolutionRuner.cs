using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Aramis.Loader.SolutionUpdate;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

namespace Aramis.Loader
    {

    enum RunResult
        {
        Exit,
        Update,
        Restart,
        Error
        }

    static class SolutionRuner
        {
        private static string SOLUTION_PATH
            {
            get
                {
                return //@"C:\Documents and Settings\D\Application Data\Aramis .NET\GreenHouse\GreenHouse.exe";
                    //@"X:\My work\Projects\UTK\SOFTWARE\Aramis.NET\PlatformTest\bin\Release\PlatformTest.exe";//Program.SolutionPath + Program.SolutionEXEFileName;
                    @"X:\My work\Projects\UTK\SOFTWARE\GreenHouse\GreenHouse\bin\Release\GreenHouse.exe"; 
                 //    @"X:\My work\Projects\UTK\SOFTWARE\Aramis.NET\PlatformTest\bin\Release\PlatformTest.exe";
                //@"C:\Users\D\Desktop\ThreadsTesting\ThreadsTesting\bin\Debug\ThreadsTesting.exe";
                }
            }
        private const int UPDATES_CHECKING_INTERVAL = 5000;
        private static Thread updatesCheckingThread;
        public static string ApplicationDirectory
            {
            get;
            set;
            }
        private static bool error = false;
        private static AppDomain solutionDomain = null;
        private static int i = 0;

        /// <summary>
        /// Запускает в потоке процесс проверки обновлений и решение в основном потоке
        /// </summary>
        public static void Start()
            {
            RunResult runResult;
            int okQuantity = 0;
            do
                {
                //StartSolutionUpdateChecking();

                runResult = StartSolution();
                okQuantity++;
                Trace.WriteLine( string.Format( "Количество успешных запусков - {0}", okQuantity ) );
                //StopSolutionUpdateChecking();
                //Thread.Sleep(10000);
                //AcceptSolutionUpdates();
                } while ( true );//runResult == RunResult.Update || runResult == RunResult.Restart );
            //Program.ExitWithResult(LoaderResult.Restart);
            }

        /// <summary>
        /// Запускает проверку обновлений в потоке
        /// </summary>
        private static void StartSolutionUpdateChecking()
            {
            updatesCheckingThread = new Thread( () =>
            {
                while ( true )
                    {
                    CheckSolutionUpdates();
                    Thread.Sleep( UPDATES_CHECKING_INTERVAL );
                    }
            } );

            updatesCheckingThread.IsBackground = true;
            updatesCheckingThread.Start();
            }

        /// <summary>
        /// Останавливает процесс загрузки обновлений
        /// </summary>
        private static void StopSolutionUpdateChecking()
            {
            updatesCheckingThread.Abort();
            }

        /// <summary>
        /// Запускает приложение в отдельном домене приложения
        /// </summary>
        /// <returns></returns>
        private static RunResult StartSolution()
            {
            // подождать пока можно будет войти в систему, здесь показать окно загрузки системы
            i++;

            solutionDomain = AppDomain.CreateDomain( "SolutionDomain" + i.ToString(), null, new AppDomainSetup
                {
                    ApplicationBase = Path.GetDirectoryName( SOLUTION_PATH),//Program.SolutionPath ),
                    ConfigurationFile = SOLUTION_PATH + ".config"
                } );

            //  solutionDomain.DomainUnload += solutionDomain_DomainUnload;
            MessageBox.Show( "Готов к запуску" );
            solutionDomain.ExecuteAssembly( SOLUTION_PATH );

            while ( solutionDomain.GetData( "ExitStatus" ) == null )
                {
                Thread.Sleep( 500 );
                }

            object startResult = solutionDomain.GetData( "Result" );
            try
                {
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                AppDomain.Unload( solutionDomain );
                }
            catch ( Exception exp )
                {
                Trace.WriteLine( string.Format( "Ошибка выгрузки домена: {0}", exp.Message ) );
                }
            solutionDomain = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            RunResult result;
            if ( startResult != null )
                {
                TryResultConvert( startResult, out result );
                }
            else
                {
                result = RunResult.Error;
                }
            MessageBox.Show("Работа завершена");
            return result;
            }

        static void solutionDomain_DomainUnload( object sender, EventArgs e )
            {
            string trace = e.ToString();
            }

        static void solutionDomain_UnhandledException( object sender, UnhandledExceptionEventArgs e )
            {
            Trace.WriteLine( "solutionDomain_UnhandledException: " + e.ExceptionObject.ToString() );
            }

        /// <summary>
        /// Возвращает true если доступны обновления
        /// </summary>
        /// <returns></returns>
        public static bool SolutionUpdatesExists()
            {
            return Update.FilesCount > 0;
            }

        /// <summary>
        /// Применяет обновления
        /// </summary>
        public static void AcceptSolutionUpdates()
            {
            Update.ApplyUpdate();
            }

        /// <summary>
        /// Проверяет наличие обновлений в базе данных и загружает их
        /// </summary>
        public static void CheckSolutionUpdates()
            {
            error = false;
            // Ждем окончания загрузки файлов в БД
            if ( !Waiting.Wait( WaitingCodes.DOWNLOADING_FILES_DB_CODE ) )
                {
                // В процессе проверки возникла ошибка. Продолжать обновление нельзя
                error = true;
                return;
                }
            // Проставляем в домен приложения флаг о наличии обновлений

            // Загружаем список обновлений из БД
            Update.GetUpdateFilesList();
            if ( SolutionUpdatesExists() )
                {
                if ( solutionDomain != null )
                    {
                    solutionDomain.SetData( "UpdatesExists", false );
                    }
                // Удаляем все файлы обновления, которые были загружены ранее, но по каким-либо причинам небыли применены
                Update.DeleteAllPreviousUpdate();
                // Загружаем обновления
                Update.Download();
                // Проставляем в домен приложения флаг о наличии обновлений
                if ( solutionDomain != null )
                    {
                    solutionDomain.SetData( "UpdatesExists", true );
                    solutionDomain.SetData( "AvailableUpdateNumber", Update.UpdateNumber );
                    }
                }
            }

        private static bool TryResultConvert( object resultObj, out RunResult result )
            {
            result = RunResult.Error;
            if ( !( resultObj is Int32 ) )
                {
                return false;
                }

            int resultInt = ( int )resultObj;
            if ( resultInt < 0 || Enum.GetValues( typeof( RunResult ) ).Length <= resultInt )
                {
                return false;
                }

            result = ( RunResult )resultInt;
            return true;
            }

        }
    }
