using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;

namespace AramisPreStart
    {
    class Startup
        {
        #region Константы
        private static bool releaseMode = IsReleaseMode();
        private static bool IsReleaseMode()
            {
            bool isReleaseMode = true;
#if DEBUG
            isReleaseMode = false;
#endif
            return isReleaseMode;
            }

        private const string STARTER_NAME = "AramisSolution.exe";
        internal static readonly string STARTER_PATH = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ) + @"\Aramis .NET\Starter";
        private static readonly string SOLUTIONS_PATH = STARTER_PATH + @"\Solutions.xml";
        private static readonly string FULL_STARTER_NAME = GetFullStarterPath();

        private static string GetFullStarterPath()
            {
            if ( releaseMode )
                {
                return STARTER_PATH + "\\" + STARTER_NAME;
                }
            else
                {
                string ProgramPath = System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location );
                return ProgramPath + "\\..\\..\\..\\AramisSolution\\bin\\Debug\\" + STARTER_NAME;
                }
            }

        // private static readonly string FULL_STARTER_NAME = @"X:\SOFTWARE\Starter\Starter\bin\Release\Starter.dll"; 

        #endregion

        private static readonly Mutex STARTER_RUNNING_MUTEX = new Mutex( false, "AramisPreStart_StarterRuningUpdating" );
        private const int BUFFER_SIZE = 256 * 128;

        [STAThread()]
        static void Main( string[] args )
            {
            if ( !EnterMutex( STARTER_RUNNING_MUTEX, 1000 ) )
                {
                return;
                }

            if ( releaseMode && !CheckStarterFiles() )
                {
                return;
                }

            if ( File.Exists( FULL_STARTER_NAME ) )
                {
                try
                    {
                    string parameter = args.Length == 0 ? "" : args[ 0 ];
                    Process.Start( FULL_STARTER_NAME, parameter );
                    }
                catch ( Exception exp )
                    {
                    ShowError( string.Format( "Can't start Aramis starter", exp.Message ) );
                    }
                }

            ExitMutex( STARTER_RUNNING_MUTEX );
            }

        private static bool CheckStarterFiles()
            {
            bool starterHasBeenInstalled = false;
            if ( !File.Exists( FULL_STARTER_NAME ) )
                {
                starterHasBeenInstalled = InstallStarter();

                if ( !starterHasBeenInstalled || !File.Exists( FULL_STARTER_NAME ) )
                    {
                    ShowError( string.Format( "Не удалось загрузить из базы файл: {0}", STARTER_NAME ) );
                    return false;
                    }
                }

            if ( !starterHasBeenInstalled )
                {
                StarterUpdater.TryToUpdateStarterFiles( STARTER_PATH );
                }

            return true;
            }

        private static bool InstallStarter()
            {
            if ( !Directory.Exists( STARTER_PATH ) )
                {
                Directory.CreateDirectory( STARTER_PATH );
                }

            bool starterInstalled = TryToDownLoadStarter();

            if ( !starterInstalled && !AramisStarter.AddNewSystemWindow.Showed )
                {
                RefreshStarterPathParameters();
                starterInstalled = TryToDownLoadStarter();
                }

            return starterInstalled;
            }

        private static bool TryToDownLoadStarter()
            {
            SqlConnection conn = DatabaseHelper.GetOpenedUpdateConnection();

            if ( conn == null )
                {
                if ( !AramisStarter.AddNewSystemWindow.Showed )
                    {
                    RefreshStarterPathParameters();

                    // Вторая попытка
                    conn = DatabaseHelper.GetOpenedUpdateConnection();
                    }
                }

            if ( conn == null )
                {
                return false;
                }

            return DownLoadStarter( conn );
            }

        private static void RefreshStarterPathParameters()
            {
            // Возможно нужно обновить параметры подключения
            StarterUpdateDatabasePath.RefreshStarterUpdateDatabasePath( "Не удалось подключиться к базе загрузчика." );
            }

        private static bool DownLoadStarter( SqlConnection conn )
            {
            try
                {
                using ( SqlCommand command = conn.CreateCommand() )
                    {
                    command.CommandText = "select [Data], LEN([Data]) FileSize, RTRIM([FileName]) FileName from [Starter]";
                    try
                        {
                        using ( SqlDataReader dataReader = command.ExecuteReader() )
                            {
                            while ( dataReader.Read() )
                                {
                                string filePath = String.Format( @"{0}\{1}", STARTER_PATH, dataReader[ "FileName" ] as string );
                                long fileSize = Convert.ToInt64( dataReader[ "FileSize" ] );
                                if ( !SaveTemporaryFile( filePath, fileSize, dataReader ) )
                                    {
                                    throw new Exception( string.Format( "Не удалось сохранить загруженный файл ({0})", filePath ) );
                                    }
                                }
                            }
                        }
                    catch ( SqlException sqlException )
                        {
                        throw new Exception( string.Format( "ошибка получения данных - {0}", sqlException.Message ) );
                        }
                    }
                }
            catch ( Exception exp )
                {
                string errorMessage = string.Format( "Ошибка при инсталяции стартера: {0}\r\n\r\nПриложение будет закрыто.", exp.Message );
                Trace.WriteLine( errorMessage );
                ShowError( errorMessage );
                return false;
                }
            finally
                {
                ( ( IDisposable )conn ).Dispose();
                }

            return true;
            }

        internal static void ShowError( string errorMessage )
            {
            MessageBox.Show( errorMessage, "Aramis .NET starter", MessageBoxButton.OK, MessageBoxImage.Error );
            }

        private static bool SaveTemporaryFile( string filePath, long fileSize, SqlDataReader dataReader )
            {
            FileStream file;
            byte[] readBuffer = new byte[ BUFFER_SIZE ];
            int readerPosition = 0;
            int bytesReaded;
            try
                {
                file = new FileStream( filePath, FileMode.Create );

                try
                    {
                    while ( readerPosition < fileSize )
                        {
                        bytesReaded = ( int )dataReader.GetBytes( 0, readerPosition, readBuffer, 0, BUFFER_SIZE );
                        file.Write( readBuffer, 0, bytesReaded );

                        readerPosition += bytesReaded;
                        }
                    }
                catch
                    {
                    file.Close();

                    DeleteFile( filePath );

                    return false;
                    }

                file.Close();
                }
            catch
                {
                return false;
                }

            return true;
            }

        private static bool DeleteFile( string filePath )
            {
            try
                {
                File.Delete( filePath );
                }
            catch
                {
                return false;
                }

            return true;
            }

        private static bool EnterMutex( Mutex mutex, int millisecondsTimeout = -1 )
            {
            try
                {
                if ( millisecondsTimeout == -1 )
                    {
                    return mutex.WaitOne();
                    }
                else
                    {
                    return mutex.WaitOne( millisecondsTimeout );
                    }
                }
            catch ( AbandonedMutexException )
                {
                return true;
                }
            }

        private static void ExitMutex( Mutex mutex )
            {
            try
                {
                mutex.ReleaseMutex();
                }
            catch { }
            }
        }
    }
