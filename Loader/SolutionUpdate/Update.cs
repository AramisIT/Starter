using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Microsoft.Win32;
using Aramis.Loader.Properties;
using System.IO;
using System.Data;

namespace Aramis.Loader.SolutionUpdate
    {
    /// <summary>
    /// Делегат загрузки файла
    /// </summary>
    /// <param name="info">Информация о загруженном файле</param>
    /// <param name="tiks">Время, которое заняла загрузка</param>
    public delegate void OnFileDownloadingDelegate(UpdatingInfo info);

    /// <summary>
    /// Делегат начала загрузки файла
    /// </summary>
    /// <param name="info">Информация о загруженном файле</param>
    public delegate void OnFileDownloadingStartDelegate(UpdatingInfo info);

    /// <summary>
    /// Класс, который отвечает за обновление системы.
    /// Получение списка обновлений.
    /// Загрузка файлов.
    /// Применение обновлений
    /// </summary>
    public static class Update
        {
        public static readonly string UpdateDirectory = Program.SolutionPath + "Update\\";

        /// <summary>
        /// Имя файла с информацией об апдейте(включает путь к файлу)
        /// </summary>
        public static string UpdateInfoPath
            {
            get { return UpdateDirectory + "UpdateInfo.inf"; }
            }

        private static List<UpdatingInfo> filesList;
        public static int UpdateNumber
            {
            get;
            private set;
            }
        public static bool Error
            {
            get;
            private set;
            }

        /// <summary>
        /// Срабатывает при окончании загрузки файла
        /// </summary>
        public static event OnFileDownloadingDelegate OnFileDownloading;

        /// <summary>
        /// Срабатывает при начале загрузки файла
        /// </summary>
        public static event OnFileDownloadingStartDelegate OnFileDownloadingStart;

        /// <summary>
        /// Количество файлов доступных к загрузке
        /// </summary>
        public static int FilesCount
            {
            get;
            private set;
            }

        /// <summary>
        /// Полный размер обновления
        /// </summary>
        public static long TotalUpdateSize
            {
            get;
            private set;
            }

        /// <summary>
        /// Получает из базы данных список доступных файлов обновления
        /// </summary>
        public static void GetUpdateFilesList()
            {
            Error = false;
            filesList = new List<UpdatingInfo>();
            TotalUpdateSize = 0;
            FilesCount = 0;
            SqlDataReader result;
            using ( SqlConnection DBConnection = new SqlConnection(Settings.Default.ConnectionString) )
                {
                try
                    {
                    DBConnection.Open();
                    using ( SqlCommand Command = DBConnection.CreateCommand() )
                        {
                        Command.CommandText = "select ApplicationFile from isUpdate where action = 0 and ApplicationName = @ApplicationName";
                        Command.Parameters.AddWithValue("@ApplicationName", Program.SolutionName);
                        object exeFilename = Command.ExecuteScalar();
                        if ( exeFilename == null )
                            {
                            Error = true;
                            return;
                            }
                        Program.SolutionEXEFileName = exeFilename.ToString();
                        Command.CommandText = String.Format("select top 1 UpdateId from {0}Update where Actual = 1", Program.SolutionName);
                        SqlDataReader updateData = Command.ExecuteReader();
                        if ( UpdateNumber == 0 )
                            {
                            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(String.Format("software\\Aramis .NET\\{0}", Program.SolutionName), RegistryKeyPermissionCheck.ReadWriteSubTree, System.Security.AccessControl.RegistryRights.FullControl);
                            if ( regKey != null )
                                {
                                UpdateNumber = Convert.ToInt32(regKey.GetValue("UpdateNumber"));
                                }
                            else
                                {
                                Registry.CurrentUser.CreateSubKey(String.Format("software\\Aramis .NET\\{0}", Program.SolutionName), RegistryKeyPermissionCheck.ReadWriteSubTree);
                                regKey = Registry.CurrentUser.OpenSubKey(String.Format("software\\Aramis .NET\\{0}", Program.SolutionName), RegistryKeyPermissionCheck.ReadWriteSubTree, System.Security.AccessControl.RegistryRights.FullControl);
                                regKey.SetValue("UpdateNumber", "0", RegistryValueKind.String);
                                }
                            }
                        if ( updateData != null && updateData.Read() )
                            {
                            if ( UpdateNumber == Convert.ToInt32(updateData["UpdateId"]) )
                                {
                                return;
                                }
                            else
                                {
                                UpdateNumber = Convert.ToInt32(updateData["UpdateId"]);
                                }
                            }
                        updateData.Close();                        
                        Command.CommandText = String.Format("select FilePath Path, HashCode, RowId, DXDLL, Size from {0}Update where Actual = 1", Program.SolutionName);
                        result = Command.ExecuteReader();
                        while ( result.Read() )
                            {
                            string Path = result["Path"].ToString().Trim();
                            string HashCode = result["HashCode"].ToString().Trim();
                            string Guid = result["RowId"].ToString().Trim();
                            bool isDXDLL = ( bool ) result["DXDLL"];
                            long size = ( long ) result["Size"];
                            if ( !File.Exists(Program.SolutionPath + Path) || ( !isDXDLL && HashCode != HashGenerator.GetFileHash(Program.SolutionPath + Path, HashGenerator.HashType.SHA512) ) )
                                {
                                filesList.Add(new UpdatingInfo()
                                {
                                    FileHash = HashCode,
                                    FileName = Program.SolutionPath + Path,
                                    FileSize = size,
                                    UpdateFileName = UpdateDirectory + Path + "." + Guid
                                });
                                TotalUpdateSize += size;
                                }
                            }
                        }

                    DBConnection.Close();
                    }
                catch ( Exception exp )
                    {
                    Error = true;
                    //String.Format("Ошибка при загрузке обновлений\r\n{0}\r\n", exp.Message).Error();
                    }
                }
            FilesCount = filesList.Count;
            }

        /// <summary>
        /// Загружает обновления по списку обновлений 
        /// </summary>
        public static void Download()
            {
            if ( Error )
                {
                return;
                }

            if ( !Directory.Exists(Program.SolutionPath) )
                {
                Directory.CreateDirectory(Program.SolutionPath);
                }
            foreach ( UpdatingInfo file in filesList )
                {
                if ( OnFileDownloadingStart != null )
                    {
                    OnFileDownloadingStart(file);
                    }
                if ( !Directory.Exists(Path.GetDirectoryName(file.UpdateFileName)) )
                    {
                    Directory.CreateDirectory(Path.GetDirectoryName(file.UpdateFileName));
                    }
                if ( !Directory.Exists(Path.GetDirectoryName(file.FileName)) )
                    {
                    Directory.CreateDirectory(Path.GetDirectoryName(file.FileName));
                    }
                Exception result = DownloadUpdate(file.UpdateFileName, file.FileHash);
                if ( result != null )
                    {
                    Error = true;
                    return;
                    }
                File.AppendAllLines(UpdateInfoPath, new string[] { String.Format("{0}|{1}", file.UpdateFileName, file.FileName) });
                if ( OnFileDownloading != null )
                    {
                    OnFileDownloading(file);
                    }
                }
            }

        /// <summary>
        /// Загружает файл из БД
        /// </summary>
        /// <param name="localFullPath">Имя файла назначения(включая информацию о пути)</param>
        /// <param name="hash">Хэш загружаемого файла</param>
        /// <returns></returns>
        private static Exception DownloadUpdate(string localFullPath, string hash)
            {
            string SELECT = String.Format("SELECT TOP 1 UpdateFile " +
                        "FROM {0}Update WHERE (HashCode = @HashCode and Actual = 1)", Program.SolutionName);
            using ( SqlConnection DBConnection = new SqlConnection(Settings.Default.ConnectionString) )
                {
                try
                    {
                    DBConnection.Open();
                    using ( SqlTransaction Transaction = DBConnection.BeginTransaction() )
                        {
                        byte[] buffer;
                        using ( SqlCommand Command = DBConnection.CreateCommand() )
                            {
                            Command.Transaction = Transaction;
                            Command.CommandText = SELECT;
                            Command.Parameters.Add("@HashCode", SqlDbType.NChar).Value = hash;
                            using ( SqlDataReader Reader = Command.ExecuteReader() )
                                {
                                Reader.Read();
                                buffer = Reader.GetSqlBinary(0).Value;
                                Reader.Close();
                                }
                            }

                        FileStream dest = File.Create(localFullPath);
                        dest.Write(buffer, 0, buffer.Length);
                        dest.Close();
                        Transaction.Commit();
                        }
                    DBConnection.Close();
                    }
                catch ( Exception exp )
                    {
                    return exp;
                    }
                }
            return null;
            }

        /// <summary>
        /// Удаляет все файлы обновлений, которые были загружены ранее
        /// </summary>
        public static void DeleteAllPreviousUpdate()
            {
            if ( File.Exists(UpdateInfoPath) )
                {
                string[] result = File.ReadAllLines(UpdateInfoPath);
                for ( int i = 0; i < result.Length; i++ )
                    {
                    string filepath = result[i].Split('|')[0];
                    File.Delete(filepath);
                    }
                File.Delete(UpdateInfoPath);
                }
            }

        /// <summary>
        /// Применяет загруженные обновления(удаляет старые и помещает на их место новые файлы в соответствии с UpdateInfo.inf)
        /// </summary>
        public static void ApplyUpdate()
            {
            AppDomain copierDomain = AppDomain.CreateDomain("CopierDomain");
            copierDomain.SetData("Path", UpdateInfoPath);
            copierDomain.ExecuteAssembly(@"w:\Work\Temp Dreamering\C# projects\Starter\Copier\bin\Release\Copier.exe");
            AppDomain.Unload(copierDomain);
            //if ( File.Exists(UpdateInfoPath) )
            //    {
            //    string[] result = File.ReadAllLines(UpdateInfoPath);
            //    for ( int i = 0; i < result.Length; i++ )
            //        {
            //        try
            //            {
            //            string[] filepaths = result[i].Split('|');                        
            //            if ( File.Exists(filepaths[0]) )
            //                {
            //                File.Copy(filepaths[0], filepaths[1], true);
            //                File.Delete(filepaths[0]);
            //                }
            //            }
            //        catch ( Exception exp )
            //            {
            //            exp.Message.Error();
            //            Error = true;
            //            }
            //        }
            //    File.Delete(UpdateInfoPath);
            //    }
            //SetUpdateNumber();

            }

        /// <summary>
        /// Назначает номер обновления
        /// </summary>
        private static void SetUpdateNumber()
            {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(String.Format("software\\Aramis .NET\\{0}", Program.SolutionName), RegistryKeyPermissionCheck.ReadWriteSubTree, System.Security.AccessControl.RegistryRights.FullControl);
            if ( regKey == null )
                {
                Registry.CurrentUser.CreateSubKey(String.Format("software\\Aramis .NET\\{0}", Program.SolutionName), RegistryKeyPermissionCheck.ReadWriteSubTree);
                regKey = Registry.CurrentUser.OpenSubKey(String.Format("software\\Aramis .NET\\{0}", Program.SolutionName), RegistryKeyPermissionCheck.ReadWriteSubTree, System.Security.AccessControl.RegistryRights.FullControl);
                }
            regKey.SetValue("UpdateNumber", UpdateNumber, RegistryValueKind.String);
            regKey.Close();
            }
        }
    }