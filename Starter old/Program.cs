using System;
using System.Collections.Generic;
using System.IO;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Starter
    {
    static class Program
        {
        private const string DATABASE_NAME = "AramisITApplication";
        private const string DATABASE_LOGIN = "GetStarter";
        private const string DATABASE_PASSWORD = "vjhrjdysqcjrjcnsdftn";
        private static readonly string CONNECTION_STRING = GetConnectionString();
        private const string STARTER_NAME = "starter.dll";
        private static readonly string STARTER_PATH = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ) + @"\Aramis .NET\Starter";

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

        const string LOADER_PATH = @"X:\My work\Projects\UTK\SOFTWARE\Starter\Loader\bin\Release\Loader.exe";

        private static string[] Args
            {
            get;
            set;
            }

        enum LoaderResult
            {
            Exit,
            Update,
            Error,
            Restart
            }

        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main( string[] args )
            {
            Program.Args = args;
            if ( !File.Exists( STARTER_PATH + "\\" + STARTER_NAME ) )
                {
                if ( !InstallStarter() )
                    {
                    return;
                    }
                }

            }

        private static bool InstallStarter()
            {
            if ( !File.Exists( STARTER_PATH ) )
                {
                System.IO.Directory.CreateDirectory( STARTER_PATH );
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
                                string filePath = String.Format( @"{0}\{1}", STARTER_PATH, dataReader[ "FileName" ] as string);
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
                Trace.WriteLine(exp.Message);
                //System.Windows.Forms.Form.ShowDialog();
                return false;
                }

            return true;          
            }

        private static LoaderResult RunLoader()
            {
            AppDomain loaderDomain = AppDomain.CreateDomain( "LoaderDomain", null, new AppDomainSetup { ApplicationBase = Path.GetDirectoryName( LOADER_PATH ) } );
            loaderDomain.ExecuteAssembly( LOADER_PATH, Args );

            object resultObj = loaderDomain.GetData( "Result" );
            LoaderResult result = LoaderResult.Error;

            if ( resultObj == null || !TryResultConvert( resultObj, out result ) )
                {
                LogError( "Wrong loader result" );
                AppDomain.Unload( loaderDomain );
                return result;
                }
            else
                {
                switch ( result )
                    {
                    case LoaderResult.Update:
                        object updateLoaderFilesInfoObj = loaderDomain.GetData( "UpdateLoaderFilesInfo" );
                        AppDomain.Unload( loaderDomain );
                        if ( updateLoaderFilesInfoObj == null || !( updateLoaderFilesInfoObj is Dictionary<string, string> ) )
                            {
                            LogError( "Wrong loader update info" );
                            return LoaderResult.Error;
                            }
                        Dictionary<string, string> updateLoaderFilesInfo = updateLoaderFilesInfoObj as Dictionary<string, string>;
                        UpdateFiles( updateLoaderFilesInfo );
                        break;

                    case LoaderResult.Error:
                        object errorMessage = loaderDomain.GetData( "ErrorDescription" );
                        if ( errorMessage != null && errorMessage is string )
                            {
                            LogError( errorMessage as string );
                            }
                        AppDomain.Unload( loaderDomain );
                        break;

                    default:
                        AppDomain.Unload( loaderDomain );
                        break;
                    }
                return result;
                }
            }

        private static void UpdateFiles( Dictionary<string, string> updateLoaderFilesInfo )
            {
            foreach ( KeyValuePair<string, string> pair in updateLoaderFilesInfo )
                {
                if ( pair.Value == null && File.Exists( pair.Key ) )
                    {
                    File.Delete( pair.Key );
                    }
                else if ( File.Exists( pair.Key ) )
                    {
                    File.Move( pair.Key, pair.Value );
                    }
                }
            }

        private static bool TryResultConvert( object resultObj, out LoaderResult result )
            {
            result = LoaderResult.Error;
            if ( !( resultObj is Int32 ) )
                {
                return false;
                }

            int resultInt = ( int )resultObj;
            if ( resultInt < 0 || Enum.GetValues( typeof( LoaderResult ) ).Length <= resultInt )
                {
                return false;
                }

            result = ( LoaderResult )resultInt;
            return true;
            }

        private static void LogError( string errorMessage )
            {
            throw new NotImplementedException();
            }
        }
    }
