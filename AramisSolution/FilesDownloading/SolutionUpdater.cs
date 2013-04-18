using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using System.Windows.Forms;
using System.Xml.Linq;
using AramisStarter.Utils;
using Microsoft.Win32;

namespace AramisStarter.FilesDownloading
    {
    /// <summary>
    /// Обеспечивает механизм загрузки новых файлов из БД и применения этих файлов: удаления старых, перемещения новых
    /// </summary>
    internal class SolutionUpdater
        {
        #region private

        private static volatile Int32 downloadingComplateProgress = 0;

        private volatile bool readyToRun = false;
        private volatile bool waitForOtherProcessesComplated;
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
        private DateTime lastCheckingForStarterUpdateTime = DateTime.Now.AddDays(-1);
        public static readonly string STARTER_PATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Aramis .NET\Starter\" + Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static readonly string STARTER_HASH = GetFileHash(STARTER_PATH);
        private static readonly string UPDATE_STARTER_PATH = string.Format(@"{0}\Update\{1}", Path.GetDirectoryName(STARTER_PATH), Path.GetFileName(STARTER_PATH));
        private static string GetUpdateStarterHash()
            {
            if (File.Exists(UPDATE_STARTER_PATH))
                {
                return GetFileHash(UPDATE_STARTER_PATH);
                }
            else
                {
                return "";
                }
            }
        private static Mutex starterRuningMutex = new Mutex(false, "Aramis.Net_StarterRuningUpdating");

        private bool timeToCheckForStarterUpdate
            {
            get
                {
                return ((TimeSpan)(DateTime.Now - lastCheckingForStarterUpdateTime)).TotalMinutes > CHECK_FOR_STARTER_UPDATE_INTERVAL_MINUTES;
                }
            }

        private static object locker = new object();

        private Thread updaterThread;

        private SolutionUpdater()
            {
            updaterThread = new Thread(UpdaterThread_DoWork);
            updaterThread.IsBackground = true;
            }

        private bool TryToUpdateFiles()
            {
            Log.Append("TryToUpdateFiles() - enter");
            if (!File.Exists(downloadsSchemaPath))
                {
                Starter.SetUpdateExistingStatus(false);
                Log.Append("if ( !File.Exists( downloadsSchemaPath ) )");
                return true;
                }

            bool possibilityToChangeFilesToNewVersion = !Starter.SolutionExecuting && SyncHelper.EnterMutex(Starter.RunSolutionMutex, TRY_TO_UPDATE_FILES_TIME_OUT);
            Log.Append("possibilityToChangeFilesToNewVersion = " + possibilityToChangeFilesToNewVersion.ToString());

            if (possibilityToChangeFilesToNewVersion)
                {
                bool filesUpdated = UpdateFiles();

                SyncHelper.ExitMutex(Starter.RunSolutionMutex);

                return filesUpdated;
                }
            else
                {
                Starter.SetUpdateExistingStatus(true, GetDownloadedUpdateNumber());
                return false;
                }
            }

        private bool UpdateFiles()
            {
            Log.Append("UpdateFiles() - enter");

            if (Starter.SolutionExecuting)
                {
                Log.Append("if ( Starter.SolutionExecuting ) return false;");
                return false;
                }

            List<Process> anotherProcesses = GetAnotherProcesses();
            Log.Append("anotherProcesses = GetAnotherProcesses();");
            if (anotherProcesses.Count > 0)
                {
                Log.Append("anotherProcesses.Count > 0");
                if (waitForOtherProcessesComplated && !MaxTimeForSolutionExitExeeded())
                    {
                    Log.Append("waitForOtherProcessesComplated && !MaxTimeForSolutionExitExeeded()");
                    return false;
                    }

                anotherProcesses.ForEach(anotherProcess =>
                    {
                        anotherProcess.Kill();
                    });
                }

            Log.Append("before bool downloadedFilesUsed = UseDownloadedFiles();");
            bool downloadedFilesUsed = UseDownloadedFiles();

            if (!readyToRun)
                {
                Log.Append("UpdateFiles() - exit; downloadedFilesUsed = " + downloadedFilesUsed.ToString());
                }

            Log.Append("UpdateFiles() exit downloadedFilesUsed = " + downloadedFilesUsed.ToString());
            return downloadedFilesUsed;
            }

        private bool MaxTimeForSolutionExitExeeded()
            {
            if (startForsedUpdateTime.Date.Equals(EMPTY_TIME))
                {
                startForsedUpdateTime = DateTime.Now;
                return false;
                }

            totalSec = (int)((TimeSpan)(DateTime.Now - startForsedUpdateTime)).TotalSeconds;

            bool maxTimeExeeded = totalSec > MAX_TIME_FOR_SOLUTION_EXIT_SEC;
            if (maxTimeExeeded)
                {
                startForsedUpdateTime = EMPTY_TIME;
                waitForOtherProcessesComplated = false;
                }

            return maxTimeExeeded;
            }

        /// <summary>
        /// Проверяет существование других процессов этого решения и этой базы, запущенных под текущим пользователем Windows
        /// </summary>        
        private List<Process> GetAnotherProcesses()
            {
            SortedDictionary<int, Process> currentUserRealProcesses = new SortedDictionary<int, Process>();
            ProcessHelper.GetOtherSameProcessesList(true).ForEach(process => currentUserRealProcesses.Add(process.Id, process));

            SortedDictionary<int, Process> allRealProcesses = new SortedDictionary<int, Process>();
            ProcessHelper.GetOtherSameProcessesList().ForEach(process => allRealProcesses.Add(process.Id, process));

            List<Process> currentSQLBaseExistinsProcess = GetCurrentSQLBaseExistinsProcess(currentUserRealProcesses, allRealProcesses);
            return currentSQLBaseExistinsProcess;
            }

        private List<Process> GetCurrentSQLBaseExistinsProcess(SortedDictionary<int, Process> currentUserRealProcesses,
                SortedDictionary<int, Process> allRealProcesses)
            {
            List<Process> result = new List<Process>();

            string[] processPIDs = RegistryHelper.ProcessesIDsRegistryKey.GetValueNames();

            int pid;
            string startDatetimeStr;
            List<string> pIDsToDelete = new List<string>();
            string currentProcessId = Process.GetCurrentProcess().Id.ToString();

            foreach (string pIDStr in processPIDs)
                {
                if (pIDStr == currentProcessId)
                    {
                    continue;
                    }

                if (Int32.TryParse(pIDStr, out pid))
                    {
                    #region getting startDatetime

                    try
                        {
                        startDatetimeStr = RegistryHelper.ProcessesIDsRegistryKey.GetValue(pIDStr) as string;
                        }
                    catch
                        {
                        continue;
                        }

                    #endregion

                    Process process;
                    if (currentUserRealProcesses.TryGetValue(pid, out process))
                        {
                        // процес еще не устарел
                        if (process.StartTime.ToString(Starter.DATE_TIME_FORMAT) == startDatetimeStr)
                            {
                            result.Add(process);
                            }
                        else
                            {
                            pIDsToDelete.Add(pIDStr);
                            }
                        }
                    else if (allRealProcesses.TryGetValue(pid, out process))
                        {
                        // процес устарел: был создал до перезагрузки, Id случайно совпал, и не был ранее удален при выходе из приложения
                        if (!(process.StartTime.ToString(Starter.DATE_TIME_FORMAT) == startDatetimeStr))
                            {
                            pIDsToDelete.Add(pIDStr);
                            }
                        }
                    else
                        {
                        pIDsToDelete.Add(pIDStr);
                        }
                    }
                }

            #region deleting deprecated notes

            pIDsToDelete.ForEach(pidToDelete =>
                {
                    try
                        {
                        RegistryHelper.ProcessesIDsRegistryKey.DeleteValue(pidToDelete);
                        }
                    catch { }
                });

            #endregion

            return result;
            }

        /// <summary>
        /// Заменяет текущие файлы решения загруженными обновлениями
        /// </summary>
        private bool UseDownloadedFiles()
            {
            Log.Append("UseDownloadedFiles() - enter");

            int version;
            Dictionary<string, string> filesInfo = ReadUpdateFilesInfo(out version);
            Log.Append("Dictionary<string, string> filesInfo = ReadUpdateFilesInfo( out version );");
            if (version <= currentVersion)
                {
                Log.Append(" if ( version <= currentVersion ) return true;");
                return true;
                }

            if (filesInfo == null)
                {
                Log.Append(" if ( filesInfo == null ) return false;");
                return false;
                }

            foreach (KeyValuePair<string, string> idPathPair in filesInfo)
                {
                if (!AcceptDownLoadedFile(GetTemporaryFilePath(idPathPair.Key), App.SolutionDirPath + idPathPair.Value))
                    {
                    Log.Append(string.Format("Can't accept downloadedFile: [{0}]", idPathPair.Value));
                    return false;
                    }
                }

            ClearUpdateFolder();

            bool result = WriteCurrentUpdateVersion(version);
            Log.Append(string.Format("UseDownloadedFiles() - exit; WriteCurrentUpdateVersion( version ) = {0}; version = {1}", result, version));

            return result;
            }

        private static bool WriteCurrentUpdateVersion(int version)
            {
            try
                {
                registryKey.SetValue(UPDATE_NUMBER, version);
                ReadUpdateNumber();
                }
            catch
                {
                return false;
                }

            return currentVersion == version;
            }

        private bool AcceptDownLoadedFile(string tempFilePath, string requiredFilePath)
            {
            if (!File.Exists(tempFilePath))
                {
                DeleteFile(downloadsSchemaPath);
                ResetVersion();
                return false;
                }

            if (File.Exists(requiredFilePath))
                {
                if (!DeleteFile(requiredFilePath))
                    {
                    return false;
                    }
                }

            string folderName = Path.GetDirectoryName(requiredFilePath);
            if (!Directory.Exists(folderName))
                {
                try
                    {
                    Directory.CreateDirectory(folderName);
                    }
                catch
                    {
                    return false;
                    }
                }

            try
                {
                File.Move(tempFilePath, requiredFilePath);
                }
            catch
                {
                return false;
                }

            return true;
            }

        private Dictionary<string, string> ReadUpdateFilesInfo(out int version)
            {
            List<XElement> filesElems;
            if (!ReadFilesSchemaFromDisk(out filesElems, out version))
                {
                version = 0;
                return null;
                }

            Dictionary<string, string> result = new Dictionary<string, string>();

            foreach (XElement filesElem in filesElems)
                {
                string id = filesElem.Attribute(FILE_ID_ATTRIBUTE).Value;
                string filePath = filesElem.Element(FILE_PATH_NODE).Value;
                result.Add(id, filePath);
                }

            return result;
            }

        private bool ReadFilesSchemaFromDisk(out List<XElement> elems, out int version)
            {
            try
                {
                XElement doc = XDocument.Load(downloadsSchemaPath).Element(ROOT_NODE_NAME);
                version = Convert.ToInt32(doc.Attribute(VERSION_NUMBER_ATTRIBUTE).Value);
                elems = doc.Elements(FILE_NODE).ToList<XElement>();
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
                ReadUpdateNumber();
                if (SolutionUpdateExists())
                    {
                    if (!readyToRun)
                        {
                        Splash.SetNewVersionDownloadingStatus(true);
                        }

                    bool newUpdatesDownloaded = DownLoadSolutionUpdate();
                    if (newUpdatesDownloaded)
                        {
                        Starter.SetUpdateExistingStatus(true, accessibleUpdateNumber);
                        Log.Append("newUpdatesDownloaded");
                        }
                    return newUpdatesDownloaded;
                    }
                else
                    {
                    return true;
                    }
                }
            catch (Exception exp)
                {
                Trace.WriteLine(string.Format("{0}: TryToDownLoadUpdate(); {1}", Process.GetCurrentProcess().Id, exp.Message));
                return false;
                }
            }

        private bool DownLoadSolutionUpdate()
            {
            long totalBytes;

            if (!FillUpdateListsInfo(out totalBytes) || !CheckUpdateDir())
                {
                return false;
                }

            return DownloadUpdateFiles(totalBytes);
            }

        private static bool CheckUpdateDir()
            {
            try
                {
                if (!Directory.Exists(updateTemporaryFolderPath))
                    {
                    Directory.CreateDirectory(updateTemporaryFolderPath);
                    }
                return true;
                }
            catch
                {
                return false;
                }
            }

        private bool DownloadUpdateFiles(long totalBytes)
            {
            RemoveFilesSchema();

            string queryText = string.Format("select f.RowId Id, f.UpdateFile FileData from {0}Update f join @FilesId ids on ids.FileId = f.RowId order by f.RowId",
                App.SelectedSolution.SolutionName);

            int tryNumber = 1;
            int taskSize = filesToDownLoad.Count;
            long totalDownloadedBytes = 0;
            while (filesToDownLoad.Count > 0 && tryNumber <= MAX_DOWNLOADING_ATTEMPTS_COUNT)
                {
                DataTable filesToDownLoadTable = GetFilesToDownLoadTable();

                SqlConnection conn = DatabaseHelper.GetUpdateConnection();
                try
                    {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(queryText, conn))
                        {
                        SqlParameter tableParameter = cmd.Parameters.AddWithValue("@FilesId", filesToDownLoadTable);
                        tableParameter.SqlDbType = SqlDbType.Structured;
                        tableParameter.TypeName = "tvp_FilesToDownload";

                        using (SqlDataReader dataReader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                            {
                            while (dataReader.Read())
                                {
                                Guid fileId = (Guid)dataReader["Id"];

                                DownloadFileInfo fileInfo = filesToDownLoad[fileId];
                                string temporaryFilePath = GetTemporaryFilePath(fileId);

                                if (SaveTemporaryFile(temporaryFilePath, fileInfo.FileSize, dataReader))
                                    {
                                    filesToDownLoad.Remove(fileId);
                                    totalDownloadedBytes += fileInfo.FileSize;
                                    downloadingComplateProgress = (int)(1000 * totalDownloadedBytes / totalBytes);
                                    }
                                }
                            }
                        }
                    }
                catch (Exception exp)
                    {
                    Trace.WriteLine(String.Format("Ошибка получения файлов: {0}", exp.Message));
                    return false;
                    }
                finally
                    {
                    if (conn != null)
                        {
                        ((IDisposable)conn).Dispose();
                        }
                    }
                }

            bool allFilesDownloaded = filesToDownLoad.Count == 0;
            if (allFilesDownloaded)
                {
                return WriteFilesSchema();
                }

            return allFilesDownloaded;
            }

        private void RemoveFilesSchema()
            {
            DeleteFile(downloadsSchemaPath);
            }

        private bool WriteFilesSchema()
            {
            XElement doc = new XElement(ROOT_NODE_NAME);
            doc.SetAttributeValue(VERSION_NUMBER_ATTRIBUTE, accessibleUpdateNumber.ToString());

            foreach (KeyValuePair<Guid, DownloadFileInfo> kvP in filesToUpdate)
                {
                XElement fileXElement = new XElement(FILE_NODE);

                fileXElement.SetAttributeValue(FILE_ID_ATTRIBUTE, kvP.Key.ToString());
                fileXElement.Add(new XElement(FILE_PATH_NODE, kvP.Value.Path));

                doc.Add(fileXElement);
                }


            try
                {
                doc.Save(downloadsSchemaPath);
                }
            catch
                {
                return false;
                }

            return true;
            }

        /// <summary>
        /// В запросе сам файл должен выбираться в последнем столбце
        /// </summary>
        /// <param name="filePath">Full file name</param>
        /// <param name="fileSize">File size</param>
        /// <param name="dataReader">Созданный риадер с параметром CommandBehavior.SequentialAccess</param>
        /// <returns></returns>
        private static bool SaveTemporaryFile(string filePath, long fileSize, SqlDataReader dataReader)
            {
            FileStream file;
            byte[] readBuffer = new byte[BUFFER_SIZE];
            int readerPosition = 0;
            int bytesReaded;
            try
                {
                file = new FileStream(filePath, FileMode.Create);

                try
                    {
                    while (readerPosition < fileSize)
                        {
                        bytesReaded = (int)dataReader.GetBytes(1, readerPosition, readBuffer, 0, BUFFER_SIZE);
                        file.Write(readBuffer, 0, bytesReaded);

                        readerPosition += bytesReaded;
                        }
                    }
                catch (Exception exp)
                    {
                    Trace.WriteLine(String.Format("Ошибка записи файла: {0}", exp.Message));

                    file.Close();

                    DeleteFile(filePath);

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

        private static bool DeleteFile(string filePath)
            {
            try
                {
                File.Delete(filePath);
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
            filesToDownLoadTable.Columns.Add(FILE_ID_STR, typeof(Guid));

            foreach (Guid key in filesToDownLoad.Keys)
                {
                DataRow newRow = filesToDownLoadTable.NewRow();
                newRow[FILE_ID_STR] = key;

                filesToDownLoadTable.Rows.Add(newRow);
                }

            return filesToDownLoadTable;
            }

        private void ClearUpdateFolder()
            {
            DirectoryInfo dir = new DirectoryInfo(updateTemporaryFolderPath);
            List<FileInfo> files = dir.GetFiles().ToList<FileInfo>();
            files.ForEach(fileInfo =>
                {
                    DeleteFile(fileInfo.FullName);
                });
            }

        private bool FillUpdateListsInfo(out long totalBytes)
            {
            filesToUpdate.Clear();
            filesToDownLoad.Clear();
            totalBytes = 0;

            string queryText = string.Format("select Size FileSize, FilePath Path, HashCode, RowId, DXDLL from {0}Update where Actual = 1", App.SelectedSolution.SolutionName);

            SqlConnection conn = DatabaseHelper.GetUpdateConnection();
            try
                {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(queryText, conn))
                    {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                        totalBytes = AddFilesInfoToUpdateLists(reader);
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
                if (conn != null)
                    {
                    ((IDisposable)conn).Dispose();
                    }
                }
            }

        private long AddFilesInfoToUpdateLists(SqlDataReader reader)
            {
            long totalBytes = 0;
            Guid fileId;
            string filePath;
            string hashCode;
            bool isDevXpress;
            long fileSize;
            List<string> filesToDelete = new List<string>();

            while (reader.Read())
                {
                filePath = reader["Path"].ToString().Trim();
                hashCode = reader["HashCode"].ToString().Trim();
                isDevXpress = (bool)reader["DXDLL"];
                fileSize = (long)reader["FileSize"];

                if (FileHaveToBeUpdated(filePath, fileSize, hashCode, isDevXpress))
                    {
                    fileId = (Guid)reader["RowId"];
                    DownloadFileInfo fileInfo = new DownloadFileInfo(filePath, fileSize);

                    filesToUpdate.Add(fileId, fileInfo);

                    #region Delecting is temporary file exists, is it fully downloaded

                    string temporaryFileName = GetTemporaryFilePath(fileId);

                    if (File.Exists(temporaryFileName))
                        {
                        bool sizesEquals = fileSize == (new FileInfo(temporaryFileName).Length);
                        if (!sizesEquals)
                            {
                            filesToDownLoad.Add(fileId, fileInfo);
                            filesToDelete.Add(temporaryFileName);
                            }
                        }
                    else
                        {
                        totalBytes += fileInfo.FileSize;
                        filesToDownLoad.Add(fileId, fileInfo);
                        }

                    #endregion
                    }
                }

            // Delete not fully downloaded files
            filesToDelete.ForEach(fileToDeletePath => File.Delete(fileToDeletePath));
            return totalBytes;
            }

        private static bool FileHaveToBeUpdated(string path, long fileSize, string hashCode, bool isDevXpress)
            {
            string filePath = App.SolutionDirPath + path;

            if (!File.Exists(filePath))
                {
                return true;
                }
            else
                {
                if (isDevXpress)
                    {
                    return false;
                    }
                else
                    {
                    bool sizeEqual = fileSize == (new System.IO.FileInfo(filePath).Length);
                    if (sizeEqual)
                        {
                        return hashCode != GetFileHash(filePath);
                        }
                    else
                        {
                        return true;
                        }
                    }
                }
            }

        private static string GetFileHash(string filePath)
            {
            return HashGenerator.GetFileHash(filePath, HashGenerator.HashType.SHA512);
            }

        private bool SolutionUpdateExists()
            {
            accessibleUpdateNumber = GetAccessibleUpdateNumber();
            if (currentVersion >= accessibleUpdateNumber)
                {
                return false;
                }

            return GetDownloadedUpdateNumber() < accessibleUpdateNumber;
            }

        private int GetDownloadedUpdateNumber()
            {
            int version;
            List<XElement> filesElems;
            if (File.Exists(downloadsSchemaPath) && ReadFilesSchemaFromDisk(out filesElems, out version))
                {
                return version;
                }

            return 0;
            }

        private int GetAccessibleUpdateNumber()
            {
            string queryText = string.Format("select ISNULL(MAX(UpdateId), 0) from {0}Update", App.SelectedSolution.SolutionName);
            SqlConnection conn = DatabaseHelper.GetUpdateConnection();
            try
                {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(queryText, conn))
                    {
                    object versionObj = cmd.ExecuteScalar();
                    return versionObj == null ? 0 : Convert.ToInt32(versionObj);
                    }
                }
            catch (Exception exp)
                {
                Trace.WriteLine(string.Format("GetAccessibleUpdateNumber error: {0}", exp.Message));
                return 0;
                }
            finally
                {
                if (conn != null)
                    {
                    ((IDisposable)conn).Dispose();
                    }
                }
            }

        private static void SetRegistryKey()
            {
            registryKey = RegistryHelper.InitSubKey("TraceData");
            }

        private static void Sleep()
            {
            try
                {
                Thread.Sleep(1000 * UPDATE_CHECK_INTERVAL_SEC);
                }
            catch (ThreadInterruptedException)
                {
                }
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

        private volatile bool stopWork = false;
        private const int MAX_TIME_FOR_SOLUTION_EXIT_SEC = 25;
        private static readonly DateTime EMPTY_TIME = new DateTime(1, 1, 1);
        private DateTime startForsedUpdateTime = EMPTY_TIME;
        private int totalSec;

        private static string updateTemporaryFolderPathValue;

        private static string updateTemporaryFolderPath
            {
            get
                {
                return updateTemporaryFolderPathValue ??
                    (updateTemporaryFolderPathValue = App.SolutionDirPath + @"\Downloads\");
                }
            }

        private static string downloadsSchemaPath
            {
            get
                {
                return updateTemporaryFolderPath + "schema.xml";
                }
            }

        private static string GetTemporaryFilePath(Guid fileGuid)
            {
            return GetTemporaryFilePath(fileGuid.ToString());
            }

        private static string GetTemporaryFilePath(string fileGuidStr)
            {
            return updateTemporaryFolderPath + fileGuidStr;
            }

        private static void ReadUpdateNumber()
            {
            object updateNumberObj = registryKey.GetValue(UPDATE_NUMBER);
            currentVersion = updateNumberObj == null ? 0 : Convert.ToInt32(updateNumberObj.ToString());
            }

        private void StartUpdater()
            {
            updaterThread.Start();
            }

        private static void InitMutexes()
            {
            TryToUpdateAramisSolution = new Mutex(false, string.Format("{0}.{1}", TRY_TO_UPDATE_ARAMIS_SOLUTION_STR, App.SelectedSolution.BuildFullSolutionName()));
            }

        private void TryToUpdateStarter()
            {
            string queryText = "select top 1 LEN([Data]) fileSize, [Data] from [Starter] where [FileName] = @FileName and [HashCode] <> @Hash";

            SqlConnection conn = DatabaseHelper.GetUpdateConnection();
            try
                {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(queryText, conn))
                    {
                    cmd.Parameters.AddWithValue("@FileName", Path.GetFileName(STARTER_PATH));
                    string fileHash = GetUpdateStarterHash();
                    cmd.Parameters.AddWithValue("@Hash", fileHash == "" ? STARTER_HASH : fileHash);

                    using (SqlDataReader dataReader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                        {
                        if (dataReader.Read())
                            {
                            if (!SyncHelper.EnterMutex(starterRuningMutex, 1000))
                                {
                                return;
                                }

                            #region Checking for update directory existing

                            if (!Directory.Exists(Path.GetDirectoryName(UPDATE_STARTER_PATH)))
                                {
                                try
                                    {
                                    Directory.CreateDirectory(Path.GetDirectoryName(UPDATE_STARTER_PATH));
                                    }
                                catch (Exception exp)
                                    {
                                    Trace.WriteLine("Can't create starter update directory: " + exp.Message);
                                    }
                                }

                            #endregion

                            long fileSize = Convert.ToInt64(dataReader["fileSize"]);
                            bool updateSaved = SaveTemporaryFile(UPDATE_STARTER_PATH, fileSize, dataReader);

                            SyncHelper.ExitMutex(starterRuningMutex);

                            if (!updateSaved)
                                {
                                return;
                                }
                            }
                        }
                    }
                }
            catch (Exception exp)
                {
                Trace.WriteLine(exp.Message);
                return;
                }
            finally
                {
                if (conn != null)
                    {
                    ((IDisposable)conn).Dispose();
                    }
                }

            lastCheckingForStarterUpdateTime = DateTime.Now;
            }

        private void TryToPerformUpdating()
            {
            Log.Append("TryToPerformUpdating() enter");

            if (!TryToDownLoadUpdate())
                {
                Log.Append(" if ( !TryToDownLoadUpdate() ) return;");
                return;
                }

            bool filesUpdated = TryToUpdateFiles();
            if (filesUpdated)
                {
                Starter.SetUpdateExistingStatus(false);
                }

            if (filesUpdated && !readyToRun && currentVersion > 0)
                {
                downloadingComplateProgress = 1000;
                Log.Append("readyToRun = true;");
                readyToRun = true;
                }

            Log.Append("TryToPerformUpdating() exit");
            }

        private void UpdaterThread_DoWork()
            {
            if (Thread.CurrentThread.Name == null)
                {
                Thread.CurrentThread.Name = "Solution updater";
                }

            while (!stopWork)
                {

                Log.Append("while ( !stopWork )");

                if (SyncHelper.EnterMutex(TryToUpdateAramisSolution, 1000))
                    {
                    Log.Append("if SyncHelper.EnterMutex( TryToUpdateAramisSolution, 1000 )");

                    TryToPerformUpdating();
                    Log.Append("TryToPerformUpdating();");

                    SyncHelper.ExitMutex(TryToUpdateAramisSolution);
                    Log.Append("SyncHelper.ExitMutex( TryToUpdateAramisSolution );");
                    }

                if (timeToCheckForStarterUpdate)
                    {
                    Log.Append("if ( timeToCheckForStarterUpdate )");

                    TryToUpdateStarter();
                    Log.Append("after TryToUpdateStarter();");
                    }

                Log.Append("before UpdaterThread_DoWork Sleep();");
                Sleep();
                Log.Append("after UpdaterThread_DoWork Sleep();");
                }

            Log.Append("UpdaterThread_DoWork() - exit");
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
            lock (resetVersionLocker)
                {
                Random rand = new Random((int)(DateTime.Now.Ticks % Int32.MaxValue));
                int attempt = 1;

                while (!WriteCurrentUpdateVersion(0) && attempt < MAX_RESET_VERSION_ATTEMPT_COUNT)
                    {
                    attempt++;
                    Thread.Sleep(rand.Next(3000));
                    }
                }
            }

        internal static SolutionUpdater Updater
            {
            get
                {
                if (updater == null)
                    {
                    lock (resetVersionLocker)
                        {
                        if (updater == null)
                            {
                            updater = new SolutionUpdater();
                            }
                        }
                    }

                return updater;
                }
            }



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
        internal static void MakeToUpdate(bool forsedUpdate)
            {
            Log.Append("readyToRun = false;");
            updater.readyToRun = false;
            updater.waitForOtherProcessesComplated = forsedUpdate;
            }

        internal static void Stop()
            {
            updater.stopWork = true;
            Thread threadToStop = updater.updaterThread;

            try
                {
                if ((threadToStop.ThreadState & System.Threading.ThreadState.WaitSleepJoin) > 0)
                    {
                    threadToStop.Interrupt();
                    Thread.Sleep(300);
                    }
                }
            catch { }

            if (threadToStop.IsAlive)
                {
                Thread.Sleep(300);
                if (threadToStop.IsAlive)
                    {
                    threadToStop.Abort();
                    }
                }
            }

        #endregion

        }
    }
