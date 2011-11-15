using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.Data.SqlClient;
using System.IO;
using Aramis.Loader.Properties;
using System.Text;

namespace Aramis.Loader
    {
    /// <summary>
    /// Результаты работы лоадера
    /// </summary>
    public enum LoaderResult
        {
        /// <summary>
        /// Возвращает код выхода
        /// </summary>
        Exit,
        /// <summary>
        /// Возвращает код обновления
        /// </summary>
        Update,
        /// <summary>
        /// Возвращает код ошибки
        /// </summary>
        Error
        }

    /// <summary>
    /// Коды ожидания для БД
    /// </summary>
    public enum WaitingCodes
        {
        /// <summary>
        /// Код загрузки файлов в БД. 
        /// Пока для решения используется этот код - загружать файлы нет смысла, так как они, вероятно, не актуальны
        /// </summary>
        DOWNLOADING_FILES_DB_CODE = 888,
        /// <summary>
        /// Код обновления структуры БД. 
        /// Пока для решения используется этот код, запускать систему нельзя, так как структура БД еще не обновлена.
        /// </summary>
        UPDATING_DB_STRUCTURE_CODE = 999
        }

    static class Program
        {
        /// <summary>
        /// Человеческое название лоадера
        /// </summary>
        public const string LOADER_DESCRIPTION = "Автоматический загрузчик Aramis";

        #region Properties
        private static string solutionsFileName = "Solutions.xml";
        private static string applicationPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static string solutionName;

        /// <summary>
        /// Имя запускаемого по умолчанию решения
        /// </summary>
        public static string SolutionName
            {
            get { return solutionName; }
            set
                {
                solutionName = value;
                SolutionPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Aramis .NET\\" + value + "\\";
                }
            }

        /// <summary>
        /// Имя запускаемого файла решения
        /// </summary>
        public static string SolutionEXEFileName
            {
            get;
            set;
            }

        /// <summary>
        /// Путь к каталогу с решением
        /// </summary>
        public static string SolutionPath
            {
            get;
            private set;
            }

        /// <summary>
        /// Аргументы коммандной строки для запускаемого решения
        /// </summary>
        public static string[] Args
            {
            get;
            private set;
            }

        /// <summary>
        /// Флаг, который поднимается при автостарте(запуск решения в фоновом режиме)
        /// </summary>
        public static bool IsAutoStart
            {
            get;
            private set;
            }

        /// <summary>
        /// Путь, из которого было запущено приложение Loader.exe
        /// </summary>
        public static string ApplicationPath
            {
            get { return applicationPath; }
            }

        /// <summary>
        /// Путь у файлу с информацией о доступных к запуску решениях
        /// </summary>
        public static string SolutionsXMLFilePath
            {
            get { return String.Format("{0}\\{1}", applicationPath, solutionsFileName); }
            }
        #endregion

        [STAThread]
        static void Main(string[] args)
            {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if ( LoaderUpdatesExists() )
                {
                return;
                }

            ParceArgs(args);

            Application.Run(new SplashForm());
            }

        private static void ParceArgs(string[] args)
            {
            StringBuilder stringArgs = new StringBuilder();
            foreach ( string str in args )
                {
                stringArgs.AppendFormat("{0} ", str);
                }
            string[] splitedArgs = stringArgs.ToString().Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> resultArgs = new List<string>();
            foreach ( string arg in splitedArgs )
                {
                string trimedArg = arg.Trim();
                if ( trimedArg.IndexOf(' ') != -1 )
                    {
                    string[] argVithValue = ( from x in trimedArg.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) select x.Trim() ).ToArray<string>();
                    switch ( argVithValue[0] )
                        {
                        case "solution":
                            SolutionName = argVithValue[1];
                            break;
                        }
                    }
                else
                    {
                    resultArgs.Add(String.Format("-{0}", trimedArg));
                    switch ( trimedArg.ToLower() )
                        {
                        case "auto":
                            IsAutoStart = true;
                            break;
                        }
                    }
                }
            Args = resultArgs.ToArray();
            }

        private static bool LoaderUpdatesExists()
            {
            SortedDictionary<string, string> updateLoaderFilesInfo = CheckLoaderFiles();
            if ( updateLoaderFilesInfo.Count == 0 )
                {
                return false;
                }
            // ... загрузили

            // ... добавили что нужно сюда переименовать
            //updateLoaderFilesInfo.Add("file 1.dll", null); // удалить "file 1.dll" из папки загрузчика (пустая строка означает что такой файл нужно удалить)
            //updateLoaderFilesInfo.Add(@"bin\file 2.dll", null); // удалить "file 2.dll" из подкаталога папки "bin"
            //updateLoaderFilesInfo.Add(@"assembly\file 3.dll", "4323-42342-342-34234-324"); // переименовать ранее загруженный файл "4323-42342-342-34234-324" в файл "file 3.dll" в папке assembly
            AppDomain.CurrentDomain.SetData("UpdateLoaderFilesInfo", updateLoaderFilesInfo);
            AppDomain.CurrentDomain.SetData("Result", 2);
            return true;
            }

        private static SortedDictionary<string, string> CheckLoaderFiles()
            {
            SortedDictionary<string, string> resultDict = new SortedDictionary<string, string>();
            List<string> filesHash = new List<string>();
            using ( SqlConnection connection = new SqlConnection(Settings.Default.ConnectionString) )
                {
                try
                    {
                    connection.Open();
                    using ( SqlCommand command = connection.CreateCommand() )
                        {
                        command.CommandText = "select HashCode from Loader";
                        SqlDataReader result = command.ExecuteReader();
                        while ( result.Read() )
                            {
                            if ( ( string ) result[0] != HashGenerator.GetFileHash(Application.StartupPath + "\\Loader.dll", HashGenerator.HashType.SHA512) )
                                {
                                filesHash.Add(( string ) result[0]);
                                }
                            }
                        result.Close();
                        }
                    foreach ( string hash in filesHash )
                        {
                        using ( SqlCommand cmd = connection.CreateCommand() )
                            {
                            cmd.CommandText = String.Format("select top 1 UpdateFile, FileName from Loader where HashCode = '{0}'", ( string ) hash);
                            byte[] buffer;
                            using ( SqlDataReader Reader = cmd.ExecuteReader() )
                                {
                                Reader.Read();
                                buffer = Reader.GetSqlBinary(0).Value;
                                string fileName = String.Format("{0}\\{1}", Application.StartupPath, Reader.GetSqlString(1).Value);
                                string loadedFileName = String.Format("{0}\\{1}", Application.StartupPath, hash);
                                Reader.Close();
                                FileStream dest = File.Create(loadedFileName);
                                dest.Write(buffer, 0, buffer.Length);
                                dest.Close();
                                resultDict.Add(fileName, null);
                                resultDict.Add(loadedFileName, fileName);
                                }
                            }
                        }
                    }
                catch
                    {

                    }
                }
            return resultDict;
            }

        public static void ExitWithResult(LoaderResult result)
            {
            AppDomain.CurrentDomain.SetData("Result", ( int ) result);
            Application.Exit();
            }

        }
    }
