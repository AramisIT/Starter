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
using System.Threading.Tasks;
using System.Windows;

namespace Aramis.NET
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

        private const string DATABASE_NAME = "AramisITApplication";
        private const string DATABASE_LOGIN = "GetStarter";
        private const string DATABASE_PASSWORD = "vjhrjdysqcjrjcnsdftn";
        private static readonly string CONNECTION_STRING = GetConnectionString();
        private const string STARTER_NAME = "AramisStarter.dll";
        private static readonly string STARTER_PATH = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ) + @"\Aramis .NET\Starter";
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
                return ProgramPath + "\\..\\..\\..\\AramisStarter\\bin\\Debug\\" + STARTER_NAME;
                }
            }

        // private static readonly string FULL_STARTER_NAME = @"X:\SOFTWARE\Starter\Starter\bin\Release\Starter.dll";

        private static string GetConnectionString()
            {
            return string.Format( "Data Source={0};Initial Catalog={1};User ID={2};Password={3}",
                GetDataSource(),
                DATABASE_NAME,
                DATABASE_LOGIN,
                DATABASE_PASSWORD );
            }

        private static string GetDataSource()
            {
            return ConfigurationManager.AppSettings[ "DataBaseComputer" ] ?? "";
            }

        #endregion



        private static Mutex starterRuningMutex = new Mutex( false, "Aramis.Net_StarterRuningUpdating" );

        [STAThread()]
        static void Main( string[] args )
            {
            if ( !EnterMutex( starterRuningMutex, 1000 ) )
                {
                return;
                }

            if ( releaseMode && !CheckStarterFiles() )
                {
                return;
                }

            IStarter starter = GetStarter();

            ExitMutex( starterRuningMutex );

            if ( starter != null )
                {
                starter.Start( args );
                }
            }

        private static bool CheckStarterFiles()
            {
            bool starterHasBeenInstalled = false;
            if ( !File.Exists( FULL_STARTER_NAME ) )
                {
                starterHasBeenInstalled = InstallStarter();
                if ( !starterHasBeenInstalled )
                    {
                    return false;
                    }
                if ( !File.Exists( FULL_STARTER_NAME ) )
                    {
                    MessageBox.Show( string.Format( "Не удалось загрузить из базы файл: {0}", STARTER_NAME ) );
                    return false;
                    }
                }

            if ( !starterHasBeenInstalled )
                {
                StarterUpdater.TryToUpdateStarterFiles( STARTER_PATH );
                }

            return true;
            }

        private static IStarter GetStarter()
            {
            Assembly starterAssembly = Assembly.LoadFrom( FULL_STARTER_NAME );
            List<Type> matchedTypes = ( from x in starterAssembly.GetTypes() where typeof( IStarter ).IsAssignableFrom( x ) select x ).ToList<Type>();

            if ( matchedTypes.Count == 0 )
                {
                MessageBox.Show( "Не удалось найти класс, реализующий IStarter в загрузчике.", "Aramis.NET", MessageBoxButton.OK, MessageBoxImage.Error );
                return null;
                }
            else
                {
                Type starterType = matchedTypes[ 0 ];
                object starterObj = Activator.CreateInstance( starterType );
                return starterObj as IStarter;
                }
            }

        private static bool InstallStarter()
            {
            if ( !Directory.Exists( STARTER_PATH ) )
                {
                Directory.CreateDirectory( STARTER_PATH );
                }

            try
                {
                using ( SqlConnection connection = new SqlConnection( CONNECTION_STRING ) )
                    {
                    connection.Open();
                    using ( SqlCommand command = connection.CreateCommand() )
                        {
                        command.CommandText = "select [FileData], [FileName] from dbo.StarterFiles";
                        using ( SqlDataReader dataReader = command.ExecuteReader() )
                            {
                            byte[] buffer;
                            while ( dataReader.Read() )
                                {
                                string filePath = String.Format( @"{0}\{1}", STARTER_PATH, dataReader[ "FileName" ] as string );
                                buffer = ( byte[] )dataReader[ "FileData" ];

                                FileStream fileStream = File.Create( filePath );
                                fileStream.Write( buffer, 0, buffer.Length );
                                fileStream.Close();
                                }
                            dataReader.Close();
                            }
                        }
                    }
                }
            catch ( Exception exp )
                {
                string errorMessage = string.Format( "Ошибка при инсталяции стартера: {0}\r\n\r\nПриложение будет закрыто.", exp.Message );
                Trace.WriteLine( errorMessage );
                MessageBox.Show( errorMessage );
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
