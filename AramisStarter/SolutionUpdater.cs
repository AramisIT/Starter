using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AramisStarter
    {
    class SolutionUpdater
        {
        private const string DATABASE_LOGIN = "AramisUpdateFilesGetter";
        private const string DATABASE_PASSWORD = "vjhrjdysqcjrjcnsdftn";
        internal volatile bool UpdateForStartChecked
            {
            get;
            private set;
            }

        private static object locker = new object();

        private BackgroundWorker updaterThread;

        private SolutionUpdater()
            {
            updaterThread = new BackgroundWorker();
            updaterThread.WorkerSupportsCancellation = true;
            updaterThread.DoWork += updaterThread_DoWork;
            }

        private void updaterThread_DoWork( object sender, DoWorkEventArgs e )
            {
            while ( !StopWork )
                {
                TryToLoadUpdate();

                TryToUpdateFiles();

                Sleep();
                }
            }

        private void TryToUpdateFiles()
            {

            }

        private void TryToLoadUpdate()
            {
            if ( Starter.EnterMutex( TryToUpdateAramisSolution ) )
                {
                try
                    {
                    if ( SolutionUpdateExists() )
                        {
                        DownLoadSolutionUpdate();
                        }
                    }
                catch { }
                }
            }

        private void DownLoadSolutionUpdate()
            {
            FillNewFilesTable();
            DownloadUpdateFiles();
            }

        private Exception DownloadUpdate( string localFullPath, string hash )
            {
            //CommandBehavior.SequentialAccess
            //  SqlDataReader GetBytes()
            //string SELECT = String.Format( "SELECT TOP 1 UpdateFile " +
            //            "FROM {0}Update WHERE (HashCode = @HashCode and Actual = 1)", Loader.ApplicationDirectory );
            //using ( SqlConnection DBConnection = new SqlConnection( Loader.ConnectionStr ) )
            //    {
            //    try
            //        {
            //        DBConnection.Open();
            //        using ( SqlTransaction Transaction = DBConnection.BeginTransaction() )
            //            {
            //            byte[] buffer;
            //            using ( SqlCommand Command = DBConnection.CreateCommand() )
            //                {
            //                Command.Transaction = Transaction;
            //                Command.CommandText = SELECT;
            //                Command.Parameters.Add( "@HashCode", SqlDbType.NChar ).Value = hash;
            //                using ( SqlDataReader Reader = Command.ExecuteReader() )
            //                    {
            //                    Reader.Read();
            //                    buffer = Reader.GetSqlBinary( 0 ).Value;
            //                    Reader.Close();
            //                    }
            //                }

            //            FileStream dest = File.Create( localFullPath );
            //            dest.Write( buffer, 0, buffer.Length );
            //            dest.Close();
            //            //LoadFile(localPath, filePath, txnToken);
            //            Transaction.Commit();
            //            }
            //        DBConnection.Close();
            //        }
            //    catch ( Exception exp )
            //        {
            //        String.Format( "UpdateInfo.DownloadUpdate.\r\n\t{0}", exp.Message ).Log();
            //        return exp;
            //        }
            //    }

            //UP();
            return null;
            }
        private void DownloadUpdateFiles()
            {
            return;

            if ( !Directory.Exists( updateTemporaryFolderPath ) )
                {
                Directory.CreateDirectory( updateTemporaryFolderPath );
                }
            else
                {
                ClearUpdateFolder();
                }

            foreach ( DataRow row in newFilesTable.Rows )
                {
                
                string hash = row[ "HashCode" ].ToString();
                string localShortPath = row[ "Guid" ].ToString();

                // Имя относительно Aramis.net\[Solution folder]
                string realFileName = row[ "Path" ].ToString();
                
                string tempFileName = updateTemporaryFolderPath + localShortPath;
                

                //Exception result = DownloadUpdate( tempFileName, hash );
                //while ( result != null && i < 3 )
                //    {
                //    result = DownloadUpdate( tempFileName, hash );
                //    i++;
                //    }
                //if ( i == 3 )
                //    {
                //    string.Format( "Проблема при загрузке обновлений\r\nОписание: {0}", result.Message ).Log();
                //    MessageBox.Show( string.Format( "Проблема при загрузке обновлений\r\nОписание: {0}", result.Message ), "Aramis.exe", MessageBoxButtons.OK, MessageBoxIcon.Error );
                //    //if ( File.Exists(Form1.APPLICATION_PATH + "backup\\" + localShortPath) )
                //    //    File.Copy(Form1.APPLICATION_PATH + "backup\\" + localShortPath, Form1.APPLICATION_PATH + localShortPath, true);
                //    CloseApp();
                //    return;
                //    }
                //String.Format( "Downloaded -> {0}", realFileName ).Log();
                //if ( !Loader.NeedShowForms )
                //    {
                //    File.AppendAllLines( Form1.UPDATE_INFO_PATH, new string[] { String.Format( "{0}|{1}{2}|{2}|{3}", tempFileName, Form1.APPLICATION_PATH, realFileName, localShortPath ) } );
                //    }
                }
            }

        private void ClearUpdateFolder()
            {
            DirectoryInfo dir = new DirectoryInfo( updateTemporaryFolderPath );
            List<FileInfo> files = dir.GetFiles().ToList<FileInfo>();
            files.ForEach( fileInfo =>
                {
                File.Delete( fileInfo.FullName );
                } );
            }

        private void FillNewFilesTable()
            {
            newFilesTable.Rows.Clear();

            string queryText = string.Format( "select FilePath Path, HashCode, RowId, DXDLL from {0}Update where Actual = 1", App.SelectedSolution.SolutionName );

            using ( SqlConnection conn = GetUpdateConnection() )
                {
                conn.Open();
                using ( SqlCommand cmd = new SqlCommand( queryText, conn ) )
                    {
                    using ( SqlDataReader reader = cmd.ExecuteReader() )
                        {
                        while ( reader.Read() )
                            {
                            string path = reader[ "Path" ].ToString().Trim();
                            string hashCode = reader[ "HashCode" ].ToString().Trim();
                            string guid = reader[ "RowId" ].ToString().Trim();
                            bool isDevXpress = ( bool )reader[ "DXDLL" ];
                            if ( FileHaveToBeDownloaded( path, hashCode, isDevXpress ) )
                                {
                                DataRow row = newFilesTable.NewRow();
                                row[ "Path" ] = path;
                                row[ "HashCode" ] = hashCode;
                                row[ "GUID" ] = guid;
                                newFilesTable.Rows.Add( row );
                                }
                            }
                        }
                    }
                }
            }

        private static bool FileHaveToBeDownloaded( string path, string hashCode, bool isDevXpress )
            {
            string filePath = solutionPath + path;

            if ( !File.Exists( filePath ) )
                {
                return true;
                }
            else
                {
                if ( isDevXpress )
                    {
                    return false;
                    }
                else
                    {
                    return hashCode != GetFileHash( filePath );
                    }
                }
            }

        private static string GetFileHash( string filePath )
            {
            return HashGenerator.GetFileHash( filePath, HashGenerator.HashType.SHA512 );
            }

        private bool SolutionUpdateExists()
            {
            return updateNumber < GetAccessibleUpdateNumber();
            }

        private int GetAccessibleUpdateNumber()
            {
            string queryText = string.Format( "select ISNULL(MAX(UpdateId), 0) from {0}Update", App.SelectedSolution.SolutionName );
            using ( SqlConnection conn = GetUpdateConnection() )
                {
                conn.Open();
                using ( SqlCommand cmd = new SqlCommand( queryText, conn ) )
                    {
                    object versionObj = cmd.ExecuteScalar();
                    return versionObj == null ? 0 : Convert.ToInt32( versionObj );
                    }
                }
            }

        private SqlConnection GetUpdateConnection()
            {
            string updateConnectionString = string.Format( "Data Source={0}; Initial Catalog={1}; User ID={2}; Password={3}",
               App.SelectedSolution.SqlServerName,
               "AramisUpdate",
               DATABASE_LOGIN,
               DATABASE_PASSWORD );

            return new SqlConnection( updateConnectionString );
            }

        private static void SetRegistryKey()
            {
            string keyName = string.Format( "Software\\Aramis .NET\\{0}\\TraceData", App.SelectedSolution.SolutionName );

            registryKey = Registry.CurrentUser.OpenSubKey( keyName, RegistryKeyPermissionCheck.ReadWriteSubTree, System.Security.AccessControl.RegistryRights.FullControl );

            if ( registryKey == null )
                {
                Registry.CurrentUser.CreateSubKey( keyName, RegistryKeyPermissionCheck.ReadWriteSubTree );
                registryKey = Registry.CurrentUser.OpenSubKey( keyName, RegistryKeyPermissionCheck.ReadWriteSubTree, System.Security.AccessControl.RegistryRights.FullControl );
                }
            }

        private static void Sleep()
            {
            Thread.Sleep( UPDATE_CHECK_INTERVAL_SEC );
            }

        private static SolutionUpdater updater;

        internal static SolutionUpdater Updater
            {
            get
                {
                if ( updater == null )
                    {
                    lock ( locker )
                        {
                        if ( updater == null )
                            {
                            updater = new SolutionUpdater();
                            }
                        }
                    }

                return updater;
                }
            }

        internal volatile bool StopWork = false;
        private const int UPDATE_CHECK_INTERVAL_SEC = 5;

        private static Mutex TryToUpdateAramisSolution;

        private const string TRY_TO_UPDATE_ARAMIS_SOLUTION_STR = "TryToUpdateAramisSolution";
        private static RegistryKey registryKey;
        private const string UPDATE_NUMBER = "UpdateNumber";
        private static int updateNumber;
        private static DataTable newFilesTable;
        private static string solutionPath;
        private static string updateTemporaryFolderPath;

        internal static void Init()
            {
            // temp line
            Updater.UpdateForStartChecked = true;

            InitSolutionPathes();

            InitNewFilesTable();

            SetRegistryKey();

            ReadUpdateNumber();

            TryToUpdateAramisSolution = new Mutex( false, TRY_TO_UPDATE_ARAMIS_SOLUTION_STR + App.SelectedSolution.SolutionName );
            }

        private static void InitSolutionPathes()
            {
            solutionPath = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ) + "\\Aramis .NET\\" + App.SelectedSolution.SolutionName + "\\";
            updateTemporaryFolderPath = solutionPath + @"\Update\";
            }

        private static void InitNewFilesTable()
            {
            newFilesTable = new DataTable();
            newFilesTable.Columns.Add( "Path", typeof( string ) );
            newFilesTable.Columns.Add( "HashCode", typeof( string ) );
            newFilesTable.Columns.Add( "GUID", typeof( string ) );
            }

        private static void ReadUpdateNumber()
            {
            object updateNumberObj = registryKey.GetValue( UPDATE_NUMBER );
            updateNumber = updateNumberObj == null ? 0 : Convert.ToInt32( updateNumberObj.ToString() );
            }
        }
    }
