﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using AramisStarter.Utils;
using Microsoft.Win32;

namespace AramisStarter
    {
    class SolutionUpdater
        {
        #region private

        private static volatile Int32 downloadingComplateProgress = 0;

        private volatile bool readyToRun = false;
        private static object resetVersionLocker = new object();

        private const int MAX_DOWNLOADING_ATTEMPTS_COUNT = 10;
        private const string FILE_ID_STR = "FileId";
        private const int BUFFER_SIZE = 256 * 256;
        private const string ROOT_NODE_NAME = "root";
        private const string FILE_NODE = "File";
        private const string FILE_ID_ATTRIBUTE = "Id";
        private const string FILE_PATH_NODE = "Path";
        private const string VERSION_NUMBER_ATTRIBUTE = "Version";
        private const int TRY_TO_UPDATE_FILES_TIME_OUT = 2000;
        private int accessibleUpdateNumber;
        private const int MAX_RESET_VERSION_ATTEMPT_COUNT = 3;

        private const int CHECK_FOR_STARTER_UPDATE_INTERVAL_MINUTES = 30;
        private DateTime lastCheckingForStarterUpdateTime = DateTime.Now.AddDays( -1 );
        private static readonly string STARTER_PATH = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ) + @"\Aramis .NET\Starter\" + Path.GetFileName( System.Reflection.Assembly.GetExecutingAssembly().Location );
        private static readonly string STARTER_HASH = GetFileHash( STARTER_PATH );
        private static readonly string UPDATE_STARTER_PATH = string.Format( @"{0}\Update\{1}", Path.GetDirectoryName( STARTER_PATH ), Path.GetFileName( STARTER_PATH ) );
        private static string GetUpdateStarterHash()
            {
            if ( File.Exists( UPDATE_STARTER_PATH ) )
                {
                return GetFileHash( UPDATE_STARTER_PATH );
                }
            else
                {
                return "";
                }
            }
        private static Mutex starterRuningMutex = new Mutex( false, "Aramis.Net_StarterRuningUpdating" );

        private bool timeToCheckForStarterUpdate
            {
            get
                {
                return ( ( TimeSpan )( DateTime.Now - lastCheckingForStarterUpdateTime ) ).TotalMinutes > CHECK_FOR_STARTER_UPDATE_INTERVAL_MINUTES;
                }
            }

        private static object locker = new object();

        private BackgroundWorker updaterThread;

        private static List<Process> GetOtherSameProcessesList( bool forCurrentWinUserOnly = false )
            {
            Process currentProcess = Process.GetCurrentProcess();
            string currentProcessName = currentProcess.ProcessName;
            int currentProcessID = currentProcess.Id;
            string currentUserSID = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;
            bool returnAllSameProcess = !forCurrentWinUserOnly;

            return (
                from process in Process.GetProcessesByName( currentProcessName )
                where ( ( returnAllSameProcess || GetUserSIDByProcessID( process.Id ) == currentUserSID ) ) && ( process.Id != currentProcessID )
                select process ).ToList<Process>();
            }

        private static string GetUserSIDByProcessID( int processID )
            {
            System.Management.ObjectQuery sq = new System.Management.ObjectQuery( string.Format( "Select * from Win32_Process Where ProcessID = '{0}'", processID ) );
            System.Management.ManagementObjectSearcher searcher = new System.Management.ManagementObjectSearcher( sq );
            if ( searcher.Get().Count == 0 )
                {
                return string.Empty;
                }
            else
                {
                foreach ( System.Management.ManagementObject searchResult in searcher.Get() )
                    {
                    string[] sid = new String[ 1 ];
                    searchResult.InvokeMethod( "GetOwnerSid", ( object[] )sid );
                    return sid[ 0 ];
                    }
                return string.Empty;
                }
            }

        private SolutionUpdater()
            {
            updaterThread = new BackgroundWorker();
            updaterThread.WorkerSupportsCancellation = true;
            updaterThread.DoWork += UpdaterThread_DoWork;
            }

        private bool TryToUpdateFiles()
            {
            if ( !File.Exists( downloadsSchemaPath ) )
                {
                Starter.SetUpdateExistingStatus( false );
                return true;
                }

            if ( !Starter.SolutionExecuting && SyncHelper.EnterMutex( Starter.RunSolutionMutex, TRY_TO_UPDATE_FILES_TIME_OUT ) )
                {
                bool filesUpdated = UpdateFiles();

                SyncHelper.ExitMutex( Starter.RunSolutionMutex );

                return filesUpdated;
                }
            else
                {
                Starter.SetUpdateExistingStatus( true, GetDownloadedUpdateNumber() );
                return false;
                }
            }

        private bool UpdateFiles()
            {
            if ( Starter.SolutionExecuting )
                {
                return false;
                }

            List<Process> anotherProcesses = GetAnotherProcesses();
            if ( anotherProcesses.Count > 0 )
                {
                anotherProcesses.ForEach( anotherProcess =>
                    {
                        anotherProcess.Kill();
                    } );
                }

            return UseDownloadedFiles();
            }

        /// <summary>
        /// Проверяет существование других процессов этого решения и этой базы, запущенных под текущим пользователем Windows
        /// </summary>        
        private List<Process> GetAnotherProcesses()
            {
            SortedDictionary<int, Process> currentUserRealProcesses = new SortedDictionary<int, Process>();
            GetOtherSameProcessesList( true ).ForEach( process => currentUserRealProcesses.Add( process.Id, process ) );

            SortedDictionary<int, Process> allRealProcesses = new SortedDictionary<int, Process>();
            GetOtherSameProcessesList().ForEach( process => allRealProcesses.Add( process.Id, process ) );

            List<Process> currentSQLBaseExistinsProcess = GetCurrentSQLBaseExistinsProcess( currentUserRealProcesses, allRealProcesses );
            return currentSQLBaseExistinsProcess;
            }

        private List<Process> GetCurrentSQLBaseExistinsProcess( SortedDictionary<int, Process> currentUserRealProcesses,
                SortedDictionary<int, Process> allRealProcesses )
            {
            List<Process> result = new List<Process>();

            string[] processPIDs = RegistryHelper.ProcessesIDsRegistryKey.GetValueNames();

            int pid;
            string startDatetimeStr;
            List<string> pIDsToDelete = new List<string>();
            string currentProcessId = Process.GetCurrentProcess().Id.ToString();

            foreach ( string pIDStr in processPIDs )
                {
                if ( pIDStr == currentProcessId )
                    {
                    continue;
                    }

                if ( Int32.TryParse( pIDStr, out pid ) )
                    {
                    #region getting startDatetime

                    try
                        {
                        startDatetimeStr = RegistryHelper.ProcessesIDsRegistryKey.GetValue( pIDStr ) as string;
                        }
                    catch
                        {
                        continue;
                        }

                    #endregion

                    Process process;
                    if ( currentUserRealProcesses.TryGetValue( pid, out process ) )
                        {
                        // процес еще не устарел
                        if ( process.StartTime.ToString( Starter.DATE_TIME_FORMAT ) == startDatetimeStr )
                            {
                            result.Add( process );
                            }
                        else
                            {
                            pIDsToDelete.Add( pIDStr );
                            }
                        }
                    else if ( allRealProcesses.TryGetValue( pid, out process ) )
                        {
                        // процес устарел: был создал до перезагрузки, Id случайно совпал, и не был ранее удален при выходе из приложения
                        if ( !( process.StartTime.ToString( Starter.DATE_TIME_FORMAT ) == startDatetimeStr ) )
                            {
                            pIDsToDelete.Add( pIDStr );
                            }
                        }
                    else
                        {
                        pIDsToDelete.Add( pIDStr );
                        }
                    }
                }

            #region deleting deprecated notes

            pIDsToDelete.ForEach( pidToDelete =>
                {
                    try
                        {
                        RegistryHelper.ProcessesIDsRegistryKey.DeleteValue( pidToDelete );
                        }
                    catch { }
                } );

            #endregion

            return result;
            }

        /// <summary>
        /// Заменяет текущие файлы решения загруженными обновлениями
        /// </summary>
        private bool UseDownloadedFiles()
            {
            int version;
            Dictionary<string, string> filesInfo = ReadUpdateFilesInfo( out version );
            if ( version <= currentVersion )
                {
                return true;
                }

            if ( filesInfo == null )
                {
                return false;
                }

            foreach ( KeyValuePair<string, string> idPathPair in filesInfo )
                {
                if ( !AcceptDownLoadedFile( GetTemporaryFilePath( idPathPair.Key ), App.SolutionDirPath + idPathPair.Value ) )
                    {
                    return false;
                    }
                }

            ClearUpdateFolder();
            return WriteCurrentUpdateVersion( version );
            }

        private static bool WriteCurrentUpdateVersion( int version )
            {
            try
                {
                registryKey.SetValue( UPDATE_NUMBER, version );
                ReadUpdateNumber();
                }
            catch
                {
                return false;
                }

            return currentVersion == version;
            }

        private bool AcceptDownLoadedFile( string tempFilePath, string requiredFilePath )
            {
            if ( !File.Exists( tempFilePath ) )
                {
                DeleteFile( downloadsSchemaPath );
                ResetVersion();
                return false;
                }

            if ( File.Exists( requiredFilePath ) )
                {
                if ( !DeleteFile( requiredFilePath ) )
                    {
                    return false;
                    }
                }

            string folderName = Path.GetDirectoryName( requiredFilePath );
            if ( !Directory.Exists( folderName ) )
                {
                try
                    {
                    Directory.CreateDirectory( folderName );
                    }
                catch
                    {
                    return false;
                    }
                }

            try
                {
                File.Move( tempFilePath, requiredFilePath );
                }
            catch
                {
                return false;
                }

            return true;
            }

        private Dictionary<string, string> ReadUpdateFilesInfo( out int version )
            {
            List<XElement> filesElems;
            if ( !ReadFilesSchemaFromDisk( out filesElems, out version ) )
                {
                version = 0;
                return null;
                }

            Dictionary<string, string> result = new Dictionary<string, string>();

            foreach ( XElement filesElem in filesElems )
                {
                string id = filesElem.Attribute( FILE_ID_ATTRIBUTE ).Value;
                string filePath = filesElem.Element( FILE_PATH_NODE ).Value;
                result.Add( id, filePath );
                }

            return result;
            }

        private bool ReadFilesSchemaFromDisk( out List<XElement> elems, out int version )
            {
            try
                {
                XElement doc = XDocument.Load( downloadsSchemaPath ).Element( ROOT_NODE_NAME );
                version = Convert.ToInt32( doc.Attribute( VERSION_NUMBER_ATTRIBUTE ).Value );
                elems = doc.Elements( FILE_NODE ).ToList<XElement>();
                }
            catch
                {
                version = 0;
                elems = null;
                return false;
                }

            return true;
            }

        private bool TryToDownLoadUpdate()
            {
            try
                {
                if ( SolutionUpdateExists() )
                    {
                    bool newUpdatesDownloaded = DownLoadSolutionUpdate();
                    if ( newUpdatesDownloaded )
                        {
                        Starter.SetUpdateExistingStatus( true, accessibleUpdateNumber );
                        }
                    return newUpdatesDownloaded;
                    }
                else
                    {
                    return true;
                    }
                }
            catch
                {
                return false;
                }
            }

        private bool DownLoadSolutionUpdate()
            {
            long totalBytes;

            if ( !FillUpdateListsInfo( out totalBytes ) || !CheckUpdateDir() )
                {
                return false;
                }

            return DownloadUpdateFiles( totalBytes );
            }

        private static bool CheckUpdateDir()
            {
            try
                {
                if ( !Directory.Exists( updateTemporaryFolderPath ) )
                    {
                    Directory.CreateDirectory( updateTemporaryFolderPath );
                    }
                return true;
                }
            catch
                {
                return false;
                }
            }

        private bool DownloadUpdateFiles( long totalBytes )
            {
            string queryText = string.Format( "select f.UpdateFile FileData, f.RowId Id from {0}Update f join @FilesId ids on ids.FileId = f.RowId order by f.RowId",
                App.SelectedSolution.SolutionName );
            //App.SelectedSolution.SqlBaseName);

            int tryNumber = 1;
            int taskSize = filesToDownLoad.Count;
            long totalDownloadedBytes = 0;
            while ( filesToDownLoad.Count > 0 && tryNumber <= MAX_DOWNLOADING_ATTEMPTS_COUNT )
                {
                DataTable filesToDownLoadTable = GetFilesToDownLoadTable();

                SqlConnection conn = DatabaseHelper.GetUpdateConnection();
                try
                    {
                    conn.Open();
                    using ( SqlCommand cmd = new SqlCommand( queryText, conn ) )
                        {
                        SqlParameter tableParameter = cmd.Parameters.AddWithValue( "@FilesId", filesToDownLoadTable );
                        tableParameter.SqlDbType = SqlDbType.Structured;
                        tableParameter.TypeName = "tvp_FilesToDownload";

                        using ( SqlDataReader dataReader = cmd.ExecuteReader() )
                            {
                            while ( dataReader.Read() )
                                {
                                Guid fileId = ( Guid )dataReader[ "Id" ];

                                DownloadFileInfo fileInfo = filesToDownLoad[ fileId ];
                                string temporaryFilePath = GetTemporaryFilePath( fileId );

                                if ( SaveTemporaryFile( temporaryFilePath, fileInfo.FileSize, dataReader ) )
                                    {
                                    filesToDownLoad.Remove( fileId );
                                    totalDownloadedBytes += fileInfo.FileSize;
                                    downloadingComplateProgress = ( int )( 1000 * totalDownloadedBytes / totalBytes );
                                    }
                                }
                            }
                        }
                    }
                catch
                    {
                    return false;
                    }
                finally
                    {
                    if ( conn != null )
                        {
                        ( ( IDisposable )conn ).Dispose();
                        }
                    }
                }

            bool allFilesDownloaded = filesToDownLoad.Count == 0;
            if ( allFilesDownloaded )
                {
                return WriteFilesSchema();
                }

            return allFilesDownloaded;
            }

        private bool WriteFilesSchema()
            {
            XElement doc = new XElement( ROOT_NODE_NAME );
            doc.SetAttributeValue( VERSION_NUMBER_ATTRIBUTE, accessibleUpdateNumber.ToString() );

            foreach ( KeyValuePair<Guid, DownloadFileInfo> kvP in filesToUpdate )
                {
                XElement fileXElement = new XElement( FILE_NODE );

                fileXElement.SetAttributeValue( FILE_ID_ATTRIBUTE, kvP.Key.ToString() );
                fileXElement.Add( new XElement( FILE_PATH_NODE, kvP.Value.Path ) );

                doc.Add( fileXElement );
                }


            try
                {
                doc.Save( downloadsSchemaPath );
                }
            catch
                {
                return false;
                }

            return true;
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

        private DataTable GetFilesToDownLoadTable()
            {
            DataTable filesToDownLoadTable = new DataTable();
            filesToDownLoadTable.Columns.Add( FILE_ID_STR, typeof( Guid ) );

            foreach ( Guid key in filesToDownLoad.Keys )
                {
                DataRow newRow = filesToDownLoadTable.NewRow();
                newRow[ FILE_ID_STR ] = key;

                filesToDownLoadTable.Rows.Add( newRow );
                }

            return filesToDownLoadTable;
            }

        private void ClearUpdateFolder()
            {
            DirectoryInfo dir = new DirectoryInfo( updateTemporaryFolderPath );
            List<FileInfo> files = dir.GetFiles().ToList<FileInfo>();
            files.ForEach( fileInfo =>
                {
                    DeleteFile( fileInfo.FullName );
                } );
            }

        private bool FillUpdateListsInfo( out long totalBytes )
            {
            filesToUpdate.Clear();
            filesToDownLoad.Clear();
            totalBytes = 0;

            string queryText = string.Format( "select Size FileSize, FilePath Path, HashCode, RowId, DXDLL from {0}Update where Actual = 1", App.SelectedSolution.SolutionName );

            SqlConnection conn = DatabaseHelper.GetUpdateConnection();
            try
                {
                conn.Open();
                using ( SqlCommand cmd = new SqlCommand( queryText, conn ) )
                    {
                    using ( SqlDataReader reader = cmd.ExecuteReader() )
                        {
                        totalBytes = AddFilesInfoToUpdateLists( reader );
                        }
                    }
                return true;
                }
            catch
                {
                return false;
                }
            finally
                {
                if ( conn != null )
                    {
                    ( ( IDisposable )conn ).Dispose();
                    }
                }
            }

        private long AddFilesInfoToUpdateLists( SqlDataReader reader )
            {
            long totalBytes = 0;
            Guid fileId;
            string filePath;
            string hashCode;
            bool isDevXpress;
            long fileSize;
            List<string> filesToDelete = new List<string>();

            while ( reader.Read() )
                {
                filePath = reader[ "Path" ].ToString().Trim();
                hashCode = reader[ "HashCode" ].ToString().Trim();
                isDevXpress = ( bool )reader[ "DXDLL" ];
                fileSize = ( long )reader[ "FileSize" ];

                if ( FileHaveToBeUpdated( filePath, fileSize, hashCode, isDevXpress ) )
                    {
                    fileId = ( Guid )reader[ "RowId" ];
                    DownloadFileInfo fileInfo = new DownloadFileInfo( filePath, fileSize );

                    filesToUpdate.Add( fileId, fileInfo );

                    #region Delecting is temporary file exists, is it fully downloaded

                    string temporaryFileName = GetTemporaryFilePath( fileId );

                    if ( File.Exists( temporaryFileName ) )
                        {
                        bool sizesEquals = fileSize == ( new FileInfo( temporaryFileName ).Length );
                        if ( !sizesEquals )
                            {
                            filesToDownLoad.Add( fileId, fileInfo );
                            filesToDelete.Add( temporaryFileName );
                            }
                        }
                    else
                        {
                        totalBytes += fileInfo.FileSize;
                        filesToDownLoad.Add( fileId, fileInfo );
                        }

                    #endregion
                    }
                }

            // Delete not fully downloaded files
            filesToDelete.ForEach( fileToDeletePath => File.Delete( fileToDeletePath ) );
            return totalBytes;
            }

        private static bool FileHaveToBeUpdated( string path, long fileSize, string hashCode, bool isDevXpress )
            {
            string filePath = App.SolutionDirPath + path;

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
                    bool sizeEqual = fileSize == ( new System.IO.FileInfo( filePath ).Length );
                    if ( sizeEqual )
                        {
                        return hashCode != GetFileHash( filePath );
                        }
                    else
                        {
                        return true;
                        }
                    }
                }
            }

        private static string GetFileHash( string filePath )
            {
            return HashGenerator.GetFileHash( filePath, HashGenerator.HashType.SHA512 );
            }

        private bool SolutionUpdateExists()
            {
            accessibleUpdateNumber = GetAccessibleUpdateNumber();
            if ( currentVersion >= accessibleUpdateNumber )
                {
                return false;
                }

            return GetDownloadedUpdateNumber() < accessibleUpdateNumber;
            }

        private int GetDownloadedUpdateNumber()
            {
            int version;
            List<XElement> filesElems;
            if ( File.Exists( downloadsSchemaPath ) && ReadFilesSchemaFromDisk( out filesElems, out version ) )
                {
                return version;
                }

            return 0;
            }

        private int GetAccessibleUpdateNumber()
            {
            string queryText = string.Format( "select ISNULL(MAX(UpdateId), 0) from {0}Update", App.SelectedSolution.SolutionName );
            SqlConnection conn = DatabaseHelper.GetUpdateConnection();
            try
                {
                conn.Open();
                using ( SqlCommand cmd = new SqlCommand( queryText, conn ) )
                    {
                    object versionObj = cmd.ExecuteScalar();
                    return versionObj == null ? 0 : Convert.ToInt32( versionObj );
                    }
                }
            catch
                {
                return 0;
                }
            finally
                {
                if ( conn != null )
                    {
                    ( ( IDisposable )conn ).Dispose();
                    }
                }
            }

        private static void SetRegistryKey()
            {
            registryKey = RegistryHelper.InitSubKey( "TraceData" );
            }

        private static void Sleep()
            {
            Thread.Sleep( 1000 * UPDATE_CHECK_INTERVAL_SEC );
            }

        private static SolutionUpdater updater;

        private const int UPDATE_CHECK_INTERVAL_SEC = 5;

        private static Mutex TryToUpdateAramisSolution;

        private const string TRY_TO_UPDATE_ARAMIS_SOLUTION_STR = "TryToUpdateAramisSolution";
        private static RegistryKey registryKey;
        private const string UPDATE_NUMBER = "UpdateNumber";
        private static int currentVersion;
        private SortedDictionary<Guid, DownloadFileInfo> filesToUpdate = new SortedDictionary<Guid, DownloadFileInfo>();
        private SortedDictionary<Guid, DownloadFileInfo> filesToDownLoad = new SortedDictionary<Guid, DownloadFileInfo>();

        private static string updateTemporaryFolderPathValue;

        private static string updateTemporaryFolderPath
            {
            get
                {
                return updateTemporaryFolderPathValue ??
                    ( updateTemporaryFolderPathValue = App.SolutionDirPath + @"\Downloads\" );
                }
            }

        private static string downloadsSchemaPath
            {
            get
                {
                return updateTemporaryFolderPath + "schema.xml";
                }
            }

        private static string GetTemporaryFilePath( Guid fileGuid )
            {
            return GetTemporaryFilePath( fileGuid.ToString() );
            }

        private static string GetTemporaryFilePath( string fileGuidStr )
            {
            return updateTemporaryFolderPath + fileGuidStr;
            }

        private static void ReadUpdateNumber()
            {
            object updateNumberObj = registryKey.GetValue( UPDATE_NUMBER );
            currentVersion = updateNumberObj == null ? 0 : Convert.ToInt32( updateNumberObj.ToString() );
            }

        private void StartUpdater()
            {
            updaterThread.RunWorkerAsync();
            }

        private static void InitMutexes()
            {
            TryToUpdateAramisSolution = new Mutex( false, TRY_TO_UPDATE_ARAMIS_SOLUTION_STR
                + "." + App.SelectedSolution.SolutionName
                + "." + App.SelectedSolution.SqlBaseName );
            }

        private void TryToUpdateStarter()
            {
            string queryText = "select top 1 [UpdateFile], LEN([UpdateFile]) fileSize from [Loader] where [FileName] = @FileName and [HashCode] <> @Hash";

            SqlConnection conn = DatabaseHelper.GetUpdateConnection();
            try
                {
                conn.Open();
                using ( SqlCommand cmd = new SqlCommand( queryText, conn ) )
                    {
                    cmd.Parameters.AddWithValue( "@FileName", Path.GetFileName( STARTER_PATH ) );
                    string fileHash = GetUpdateStarterHash();
                    cmd.Parameters.AddWithValue( "@Hash", fileHash == "" ? STARTER_HASH : fileHash );

                    using ( SqlDataReader dataReader = cmd.ExecuteReader() )
                        {
                        if ( dataReader.Read() )
                            {
                            if ( !SyncHelper.EnterMutex( starterRuningMutex, 1000 ) )
                                {
                                return;
                                }

                            #region Checking for update directory existing

                            if ( !Directory.Exists( Path.GetDirectoryName( UPDATE_STARTER_PATH ) ) )
                                {
                                try
                                    {
                                    Directory.CreateDirectory( Path.GetDirectoryName( UPDATE_STARTER_PATH ) );
                                    }
                                catch
                                    {
                                    }
                                }

                            #endregion

                            long fileSize = Convert.ToInt64( dataReader[ "fileSize" ] );
                            bool updateSaved = SaveTemporaryFile( UPDATE_STARTER_PATH, fileSize, dataReader );

                            SyncHelper.ExitMutex( starterRuningMutex );

                            if ( !updateSaved )
                                {
                                return;
                                }
                            }
                        }
                    }
                }
            catch
                {
                return;
                }
            finally
                {
                if ( conn != null )
                    {
                    ( ( IDisposable )conn ).Dispose();
                    }
                }

            lastCheckingForStarterUpdateTime = DateTime.Now;
            }

        private void UpdaterThread_DoWork( object sender, DoWorkEventArgs e )
            {
            if ( Thread.CurrentThread.Name == null )
                {
                Thread.CurrentThread.Name = "Solution updater";
                }

            while ( !StopWork )
                {
                if ( SyncHelper.EnterMutex( TryToUpdateAramisSolution, 1000 ) )
                    {
                    TryToPerformUpdating();

                    SyncHelper.ExitMutex( TryToUpdateAramisSolution );
                    }

                if ( timeToCheckForStarterUpdate )
                    {
                    TryToUpdateStarter();
                    }

                Sleep();
                }
            }

        private void TryToPerformUpdating()
            {
            if ( !TryToDownLoadUpdate() )
                {
                return;
                }

            bool filesUpdated = TryToUpdateFiles();
            if ( filesUpdated )
                {
                Starter.SetUpdateExistingStatus( false );
                }

            if ( filesUpdated && !readyToRun )
                {
                downloadingComplateProgress = 1000;
                readyToRun = true;
                }
            }

        #endregion

        #region public

        internal static Int32 DownloadingComplateProgress
            {
            get
                {
                return SolutionUpdater.downloadingComplateProgress;
                }
            }

        internal static void ResetVersion()
            {
            lock ( resetVersionLocker )
                {
                Random rand = new Random( ( int )( DateTime.Now.Ticks % Int32.MaxValue ) );
                int attempt = 1;

                while ( !WriteCurrentUpdateVersion( 0 ) && attempt < MAX_RESET_VERSION_ATTEMPT_COUNT )
                    {
                    attempt++;
                    Thread.Sleep( rand.Next( 3000 ) );
                    }
                }
            }

        internal static SolutionUpdater Updater
            {
            get
                {
                if ( updater == null )
                    {
                    lock ( resetVersionLocker )
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

        internal static void Init()
            {
            SetRegistryKey();

            ReadUpdateNumber();

            InitMutexes();

            Updater.StartUpdater();
            }

        internal static bool ReadyToRun
            {
            get
                {
                return updater.readyToRun;
                }
            }

        #endregion


        internal static void MakeToUpdate()
            {
            updater.readyToRun = false;
            }

        internal static void Stop()
            {
            updater.updaterThread.CancelAsync();
            }
        }
    }
