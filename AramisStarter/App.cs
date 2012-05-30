using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using Aramis.NET;
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

        private List<SolutionInfo> solutions;

        private void SaveSolutionsList()
            {
            XElement doc = new XElement( ROOT_NODE_NAME );
            doc.SetAttributeValue( DEFAULT_SOLUTION, "" );

            solutions.ForEach( solutionInfo =>
                {
                    XElement solutionXElement = new XElement( SOLUTION_NODE );
                    solutionXElement.SetAttributeValue( NAME_ATTRIBUTE, solutionInfo.SolutionName );
                    solutionXElement.Add( new XElement( SERVER_FRIENDLY_XML_NAME, solutionInfo.SolutionFriendlyName ) );
                    solutionXElement.Add( new XElement( SQL_BASE_NAME_XML_NAME, solutionInfo.SqlBaseName ) );
                    solutionXElement.Add( new XElement( SERVER_XML_NAME, solutionInfo.SqlServerName ) );
                    doc.Add( solutionXElement );
                } );

            try
                {
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
                solutions = new List<SolutionInfo>();
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

            solutions = newSolutionsList;
            solutions.Sort( ( solInfo1, solInfo2 ) =>
                {
                    return solInfo1.SolutionFriendlyName.CompareTo( solInfo2.SolutionFriendlyName );
                } );
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
        private string solutionsPath;

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
            this.solutionsPath = solutionsPath;
            }

        public void Start( string[] args )
            {
            if ( Thread.CurrentThread.Name == null )
                {
                Thread.CurrentThread.Name = "Main starter thread";
                }

            ReadSolutions();

            // Нужно что это окно было создано первым, т.к. оно будет главным
            LoginWindow mainWindow = LoginWindow.Window;

            if ( solutions.Count == 0 )
                {
                AddNewSystemWindow addNewSystemWindow = new AddNewSystemWindow();
                addNewSystemWindow.ShowDialog();
                if ( addNewSystemWindow.NewSolution != null )
                    {
                    solutions.Add( addNewSystemWindow.NewSolution );
                    SaveSolutionsList();
                    }
                }

            ReadSolutions();

            SelectedSolution = null;

            if ( solutions.Count == 1 )
                {
                SelectedSolution = solutions[ 0 ];
                }
            else if ( solutions.Count > 1 )
                {
                //SelectSolutionWindow seleectingWindow = new SelectSolutionWindow( Solutions );
                //selectedSolution.ShowDialog();
                //selectedSolution = selectedSolution.SelectedSolution;
                }

            if ( SelectedSolution != null )
                {
                InitWithSelectedSolution();
                Run( mainWindow );
                }
            }

        internal static void Stop()
            {
            SolutionUpdater.Stop();
            LoginWindow.Window.Dispatcher.Invoke( () => LoginWindow.Window.Close() );
            }

        #endregion


        }
    }
