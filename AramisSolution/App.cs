using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml.Linq;
using AramisStarter.FilesDownloading;
using AramisStarter.Utils;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace AramisStarter
    {
    public class App : Application
        {
        #region private

        private const string SOLUTION_NODE = "Solution";

        private const string SERVER_XML_NAME = "ServerName";

        private const string SQL_BASE_NAME_XML_NAME = "DatabaseName";

        private const string ROOT_NODE_NAME = "root";

        private const string NAME_ATTRIBUTE = "Name";

        private const string SERVER_FRIENDLY_XML_NAME = "SolutionFriendlyName";

        private const string DEFAULT_SOLUTION = "DefaultSolution";

        private ObservableCollection<SolutionInfo> solutions;

        internal static void SaveSolutionsList(ObservableCollection<SolutionInfo> solutions)
            {
            if (solutionsPath == null)
                {
                return;
                }

            XElement doc = new XElement(ROOT_NODE_NAME);
            doc.SetAttributeValue(DEFAULT_SOLUTION, "");

            foreach (SolutionInfo solutionInfo in solutions)
                {
                XElement solutionXElement = new XElement(SOLUTION_NODE);
                solutionXElement.SetAttributeValue(NAME_ATTRIBUTE, solutionInfo.SolutionName);
                solutionXElement.Add(new XElement(SERVER_FRIENDLY_XML_NAME, solutionInfo.SolutionFriendlyName));
                solutionXElement.Add(new XElement(SQL_BASE_NAME_XML_NAME, solutionInfo.SqlBaseName));
                solutionXElement.Add(new XElement(SERVER_XML_NAME, solutionInfo.SqlServerName));
                doc.Add(solutionXElement);
                }

            string directoryName = Path.GetDirectoryName(solutionsPath);
            try
                {
                if (!Directory.Exists(directoryName))
                    {
                    Directory.CreateDirectory(directoryName);
                    }
                doc.Save(solutionsPath);
                }
            catch
                {
                }
            }

        private void ReadSolutions()
            {
            string xmlData = ReadSolutionsFromDisk();
            if (xmlData == "")
                {
                solutions = new ObservableCollection<SolutionInfo>();
                return;
                }

            List<SolutionInfo> newSolutionsList = new List<SolutionInfo>();
            try
                {
                XElement doc = XDocument.Parse(xmlData).Element(ROOT_NODE_NAME);
                doc.Elements(SOLUTION_NODE).ToList<XElement>().ForEach(solutionXElement =>
                    {
                        SolutionInfo solutionInfo = new SolutionInfo();

                        solutionInfo.SolutionName = solutionXElement.Attribute(NAME_ATTRIBUTE).Value;
                        solutionInfo.SqlServerName = solutionXElement.Element(SERVER_XML_NAME).Value;
                        solutionInfo.SolutionFriendlyName = solutionXElement.Element(SERVER_FRIENDLY_XML_NAME).Value;
                        solutionInfo.SqlBaseName = solutionXElement.Element(SQL_BASE_NAME_XML_NAME).Value;

                        newSolutionsList.Add(solutionInfo);
                    });
                }
            catch
                {
                newSolutionsList = new List<SolutionInfo>();
                }

            solutions = new ObservableCollection<SolutionInfo>(newSolutionsList);
            }

        private string ReadSolutionsFromDisk()
            {
            if (File.Exists(solutionsPath))
                {
                try
                    {
                    return File.ReadAllText(solutionsPath).Trim();
                    }
                catch (Exception exp)
                    {
                    return "";
                    }
                }
            else
                {
                return "";
                }
            }

        /// <summary>
        /// Пусть к xml-файлу со списком доступных решений
        /// </summary>
        private static string solutionsPath;

        private static bool releaseMode = IsReleaseMode();
        private static bool IsReleaseMode()
            {
            bool isReleaseMode = true;
#if Debug
            isReleaseMode = false;
#endif
            return isReleaseMode;
            }

        private static void InitWithSelectedSolution()
            {
            string fullSolutionName = SelectedSolution.BuildFullSolutionName();

            if (releaseMode)
                {
                SolutionDirPath = string.Format(@"{0}\Aramis .NET\{1}\", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), fullSolutionName);
                }
            else
                {
                SolutionDirPath = @"..\..\..\..\GreenHouse\GreenHouse\bin\Release\";
                }

            RegistryHelper.Init(fullSolutionName);

            SolutionUpdater.Init();
            }

        #endregion

        #region public

        internal static StartParameters StartParameters { get; set; }

        /// <summary>
        /// Загружаемое решение
        /// </summary>
        internal static SolutionInfo SelectedSolution
            {
            get;
            private set;
            }

        /// <summary>
        /// Путь к каталогу решения. Заканчивается на "\".
        /// </summary>
        internal static string SolutionDirPath
            {
            get;
            private set;
            }

        public App(string solutionsPath)
            : base()
            {
            App.solutionsPath = solutionsPath;
            }

        [STAThread()]
        static void Main(string[] args)
            {
            //MessageBox.Show(args.FirstOrDefault());

            string STARTER_PATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Aramis .NET\Starter";
            string solutionPath = STARTER_PATH + @"\Solutions.xml";

            var firstParameter = args.FirstOrDefault();

            if (!string.IsNullOrEmpty(firstParameter))
                {
                App.RunDefaultCredential = firstParameter.EndsWith("RunDefaultCredential",
                    StringComparison.OrdinalIgnoreCase);
                }

            App.StartParameters = new StartParameters(firstParameter);
            if (App.StartParameters.TerminatingProcessId > 0)
                {
                ProcessHelper.TerminateOldProcess(App.StartParameters.TerminatingProcessId);
                if (App.StartParameters.ExecuteType == ExecuteTypes.Terminate) return;
                }

            App app = new App(solutionPath);
            app.Start(args);
            }

        private void Start(string[] args)
            {
            if (ProcessHelper.GetOtherSameProcessesList(true).Count > 0)
                {
                if (MessageBox.Show(string.Format("Для продолжения запуска требуется закрыть другие запущенные копии системы.\r\n\r\nНажмите Да и система продолжит запуск\r\n\r\nНажмите Нет для отмены{0}",
                    App.StartParameters.TerminatingProcessId > 0 ? "\r\nПроцесс: " + App.StartParameters.TerminatingProcessId.ToString() : ""), "Aramis system", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
                    {
                    ProcessHelper.GetOtherSameProcessesList(true).ForEach(process => process.SafetyKill());
                    }
                else
                    {
                    return;
                    }
                }


            Log.Testing = (new List<string>() { "---utk-server1", "-atosit", "donotenter" }).Contains(System.Environment.MachineName.ToLower().Trim());

            if (Thread.CurrentThread.Name == null)
                {
                Thread.CurrentThread.Name = "Main starter thread";
                }

            ReadSolutions();
            //  ( new WpfApplication1.SolutionSelectingWindowXXX() ).ShowDialog();
            // Нужно что это окно было создано первым, т.к. оно будет главным
            LoginWindow mainWindow = LoginWindow.Window;

            bool emptySolutionListAtStart = solutions.Count == 0;
            if (emptySolutionListAtStart)
                {
                TryToAddDefaultSolution();

                if (solutions.Count == 0)
                    {
                    SolutionInfo newSolutionInfo = AddNewSystemWindow.AddNewSolution();
                    if (newSolutionInfo != null)
                        {
                        solutions.Add(newSolutionInfo);
                        SaveSolutionsList(solutions);
                        }
                    }
                }

            ReadSolutions();

            SelectedSolution = null;

            if (solutions.Count == 0)
                {
                return;
                }
            else if (solutions.Count == 1 && !emptySolutionListAtStart && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                {
                SelectedSolution = solutions[0];
                }
            else if (App.StartParameters.ExecuteType == ExecuteTypes.Restart)
                {
                foreach (var solution in solutions)
                    {
                    if (solution.SqlBaseName.Equals(App.StartParameters.DatabaseName, StringComparison.OrdinalIgnoreCase)
                        && solution.SqlServerName.Equals(App.StartParameters.ServerName, StringComparison.OrdinalIgnoreCase))
                        {
                        SelectedSolution = solution;
                        break;
                        }
                    }
                }

            if (SelectedSolution == null)
                {
                SolutionSelectingWindow seleectingWindow = new SolutionSelectingWindow(solutions);
                seleectingWindow.ShowDialog();
                SelectedSolution = seleectingWindow.SelectedSolution;
                }

            if (SelectedSolution != null)
                {
                InitWithSelectedSolution();

                //if (Log.Testing || Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.LeftAlt))
                //    {
                //    (new Log()).Show();
                //    }

                Run(mainWindow);
                }
            }

        private void TryToAddDefaultSolution()
            {
            SolutionInfo defaultSolution = SolutionInfo.GetDefaultSolution();

            if (defaultSolution != null)
                {
                solutions.Add(defaultSolution);
                SaveSolutionsList(solutions);
                }
            }

        internal static void Stop()
            {
            SolutionUpdater.Stop();
            LoginWindow.Window.Dispatcher.Invoke(new Action(() =>
                {
                    Log.CloseWindow();
                    LoginWindow.Window.Close();
                }));
            }

        #endregion

        public static bool RunDefaultCredential { get; set; }
        }
    }
