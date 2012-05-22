using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using Aramis.NET;

namespace AramisStarter
    {
    public class App : Application, IStarter
        {
        internal static string SolutionsPath
            {
            get;
            private set;
            }

        private List<SolutionInfo> solutions;
        private const string SOLUTION_NODE = "Solution";
        private const string SERVER_NODE_NAME = "ServerName";
        private const string ROOT_NODE_NAME = "root";
        private const string NAME_ATTRIBUTE = "Name";
        private const string SERVER_FRIENDLY_NODE_NAME = "SolutionFriendlyName";

        private void SaveSolutionsList()
            {
            XElement doc = new XElement( ROOT_NODE_NAME );
            solutions.ForEach( solutionInfo =>
                {
                    XElement solutionXElement = new XElement( SOLUTION_NODE );
                    solutionXElement.SetAttributeValue( NAME_ATTRIBUTE, solutionInfo.SolutionName );
                    solutionXElement.Add( new XElement( SERVER_FRIENDLY_NODE_NAME, solutionInfo.SolutionFriendlyName ) );
                    solutionXElement.Add( new XElement( SERVER_NODE_NAME, solutionInfo.SqlServerName ) );
                    doc.Add( solutionXElement );
                } );

            try
                {
                doc.Save( App.SolutionsPath );
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
                        solutionInfo.SqlServerName = solutionXElement.Element( SERVER_NODE_NAME ).Value;
                        solutionInfo.SolutionFriendlyName = solutionXElement.Element( SERVER_FRIENDLY_NODE_NAME ).Value;

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
            if ( File.Exists( SolutionsPath ) )
                {
                try
                    {
                    return File.ReadAllText( SolutionsPath ).Trim();
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

        public void Start( string[] args )
            {
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
                mainWindow.SetSolution( SelectedSolution );
                Starter.BeginSolutionLoading( SelectedSolution );
                Run( mainWindow );
                }
            }

        public App( string solutionsPath )
            {
            App.SolutionsPath = solutionsPath;
            }

        public App()
            : base()
            {
            }

        internal static SolutionInfo SelectedSolution
            {
            get;
            private set;
            }
        }
    }
