using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using Aramis.NET;
using AramisStarter.FilesDownloading;
using AramisStarter.Utils;

namespace AramisStarter
    {
    public class App : Application, IStarter
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

        internal static void SaveSolutionsList( ObservableCollection<SolutionInfo> solutions )
            {
            if ( solutionsPath == null )
                {
                return;
                }

            XElement doc = new XElement( ROOT_NODE_NAME );
            doc.SetAttributeValue( DEFAULT_SOLUTION, "" );

            foreach ( SolutionInfo solutionInfo in solutions )
                {
                XElement solutionXElement = new XElement( SOLUTION_NODE );
                solutionXElement.SetAttributeValue( NAME_ATTRIBUTE, solutionInfo.SolutionName );
                solutionXElement.Add( new XElement( SERVER_FRIENDLY_XML_NAME, solutionInfo.SolutionFriendlyName ) );
                solutionXElement.Add( new XElement( SQL_BASE_NAME_XML_NAME, solutionInfo.SqlBaseName ) );
                solutionXElement.Add( new XElement( SERVER_XML_NAME, solutionInfo.SqlServerName ) );
                doc.Add( solutionXElement );
                }

            string directoryName = Path.GetDirectoryName( solutionsPath );
            try
                {
                if ( !Directory.Exists( directoryName ) )
                    {
                    Directory.CreateDirectory( directoryName );
                    }
                doc.Save( solutionsPath );
                }
            catch
                {
                }
            }

        private void ReadSolutions()
            {
            string xmlData = ReadSolutionsFromDisk();
            if ( xmlData == "" )
                {
                solutions = new ObservableCollection<SolutionInfo>();
                return;
                }

            List<SolutionInfo> newSolutionsList = new List<SolutionInfo>();
            try
                {
                XElement doc = XDocument.Parse( xmlData ).Element( ROOT_NODE_NAME );
                doc.Elements( SOLUTION_NODE ).ToList<XElement>().ForEach( solutionXElement =>
                    {
                        SolutionInfo solutionInfo = new SolutionInfo();

                        solutionInfo.SolutionName = solutionXElement.Attribute( NAME_ATTRIBUTE ).Value;
                        solutionInfo.SqlServerName = solutionXElement.Element( SERVER_XML_NAME ).Value;
                        solutionInfo.SolutionFriendlyName = solutionXElement.Element( SERVER_FRIENDLY_XML_NAME ).Value;
                        solutionInfo.SqlBaseName = solutionXElement.Element( SQL_BASE_NAME_XML_NAME ).Value;

                        newSolutionsList.Add( solutionInfo );
                    } );
                }
            catch
                {
                newSolutionsList = new List<SolutionInfo>();
                }

            newSolutionsList.Sort( ( solInfo1, solInfo2 ) =>
                {
                    return solInfo1.SolutionFriendlyName.CompareTo( solInfo2.SolutionFriendlyName );
                } );

            solutions = new ObservableCollection<SolutionInfo>( newSolutionsList );
            }

        private string ReadSolutionsFromDisk()
            {
            if ( File.Exists( solutionsPath ) )
                {
                try
                    {
                    return File.ReadAllText( solutionsPath ).Trim();
                    }
                catch ( Exception exp )
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

        private static void InitWithSelectedSolution()
            {
            SolutionDirPath = string.Format( @"{0}\Aramis .NET\{1}.{2}\", Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ),
               SelectedSolution.SolutionName, SelectedSolution.SqlBaseName );

            RegistryHelper.Init( SelectedSolution.SolutionName, SelectedSolution.SqlBaseName );

            SolutionUpdater.Init();
            }

        #endregion

        #region public

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

        public App( string solutionsPath )
            : base()
            {
            App.solutionsPath = solutionsPath;
            }

        public void Start( string[] args )
            {

            if ( ProcessHelper.GetOtherSameProcessesList( true ).Count > 0 )
                {
                if ( MessageBox.Show( "Для продолжения запуска требуется закрыть другие запущенные копии системы.\r\n\r\nНажмите Да и система продолжит запуск\r\n\r\nНажмите Нет для отмены", "Aramis system", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No ) == MessageBoxResult.Yes )
                    {
                    ProcessHelper.GetOtherSameProcessesList( true ).ForEach( process => process.Kill() );
                    }
                else
                    {
                    return;
                    }
                }


            Log.testing = ( new List<string>() { "db", "atos", "donotenter" } ).Contains( System.Environment.MachineName.ToLower().Trim() );

            if ( Thread.CurrentThread.Name == null )
                {
                Thread.CurrentThread.Name = "Main starter thread";
                }

            ReadSolutions();
          //  ( new WpfApplication1.SolutionSelectingWindowXXX() ).ShowDialog();
            // Нужно что это окно было создано первым, т.к. оно будет главным
            LoginWindow mainWindow = LoginWindow.Window;

            if ( solutions.Count == 0 )
                {
                SolutionInfo newSolutionInfo = AddNewSystemWindow.AddNewSolution();
                if ( newSolutionInfo != null )
                    {
                    solutions.Add( newSolutionInfo );
                    SaveSolutionsList( solutions );
                    }
                }

            ReadSolutions();

            SelectedSolution = null;

            if ( solutions.Count == 1 && !Keyboard.IsKeyDown( Key.LeftShift ) && !Keyboard.IsKeyDown( Key.RightShift ) )
                {
                SelectedSolution = solutions[ 0 ];
                }
            else
                {
                SolutionSelectingWindow seleectingWindow = new SolutionSelectingWindow( solutions );
                seleectingWindow.ShowDialog();
                SelectedSolution = seleectingWindow.SelectedSolution;
                }

            if ( SelectedSolution != null )
                {
                InitWithSelectedSolution();

                if ( Log.testing )
                    {
                    ( new Log() ).Show();
                    }

                Run( mainWindow );
                }
            }

        internal static void Stop()
            {
            SolutionUpdater.Stop();
            LoginWindow.Window.Dispatcher.Invoke( new Action( () =>
                {
                    Log.CloseWindow();
                    LoginWindow.Window.Close();
                } ) );
            }

        #endregion


        }
    }
