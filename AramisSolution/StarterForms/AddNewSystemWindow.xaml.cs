using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AramisStarter.Utils;
using Windows;

namespace AramisStarter
    {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class AddNewSystemWindow : Window
        {

        public AddNewSystemWindow()
            {
            InitializeComponent();
            Icon = EmbededResourcesConverter.BitmapSourceFromBitmap( Properties.Resources.Transparent );
            }

        internal static readonly string STARTER_PATH = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ) + @"\Aramis .NET\Starter";
        private static readonly string STARTER_UPDATE_DATABASE_PATH_INFO = STARTER_PATH + @"\StarterUpdateSource.ini";

        private string GetDefaultServerName()
            {
            if ( File.Exists( STARTER_UPDATE_DATABASE_PATH_INFO ) )
                {
                string databasePath = File.ReadAllText( STARTER_UPDATE_DATABASE_PATH_INFO ).Trim();
                int separatorIndex = databasePath.IndexOf( ';' );
                if ( separatorIndex >= 1 )
                    {
                    return databasePath.Substring( 0, separatorIndex ).Trim();
                    }
                }

            return "";
            }

        private string GetDefaultDatabaseName()
            {
            if ( File.Exists( STARTER_UPDATE_DATABASE_PATH_INFO ) )
                {
                string databasePath = File.ReadAllText( STARTER_UPDATE_DATABASE_PATH_INFO ).Trim();
                int separatorIndex = databasePath.IndexOf( ';' );
                if ( separatorIndex >= 1 )
                    {
                    return databasePath.Substring( separatorIndex + 1 ).Trim();
                    }
                }

            return null;
            }

        private void Window_Loaded_1( object sender, RoutedEventArgs e )
            {
            VistaGlassHelper.ExtendGlass( this, -1, -1, -1, -1 );
            string serverName = GetDefaultServerName();
            if ( serverName != null )
                {
                serverNameTextBox.Text = serverName;
                serverNameTextBox.SelectionStart = serverName.Length;
                serverNameTextBox.SelectionLength = 0;
                serverNameTextBox.Focus();
                }
            }

        private void Button_Click_1( object sender, RoutedEventArgs e )
            {
            AddCurrentSolution();
            }

        private void AddCurrentSolution()
            {
            if ( databaseNameComboBox.SelectedItem == null )
                {
                return;
                }

            ComboBoxItem currentItem = databaseNameComboBox.SelectedItem as ComboBoxItem;
            SolutionInfo resultSolution = currentItem.Tag as SolutionInfo;
            newSolution = resultSolution;
            if ( DatabaseHelper.ReadSolutionInfo( resultSolution.SqlServerName, resultSolution.SqlBaseName, out resultSolution ) )
                {
                isSolutionRecognized = true;
                Close();
                }
            else
                {
                ShowError( "Не удалось получить описание системы для этой базы данных." );
                //  "Не удалось получить описание системы для этой базы данных.".ShowError();
                }
            }

        private void serverNameTextBox_LostFocus( object sender, RoutedEventArgs e )
            {
            FillDatabasesList();
            }

        private void FillDatabasesList()
            {
            databaseNameComboBox.Items.Clear();

            string errorMessage;
            List<SolutionInfo> databasesInfo = DatabaseHelper.GetDatabasesList( serverNameTextBox.Text.Trim(), out errorMessage );

            if ( errorMessage == null )
                {
                string defaultDatabaseName = GetDefaultDatabaseName().ToLower();
                ComboBoxItem defaultItem = null;

                databasesInfo.ForEach( databaseInfo =>
                    {
                        ComboBoxItem item = new ComboBoxItem();
                        item.Content = databaseInfo.SqlBaseName;
                        item.Tag = databaseInfo;
                        databaseNameComboBox.Items.Add( item );

                        if ( defaultItem == null && defaultDatabaseName == databaseInfo.SqlBaseName.ToLower() )
                            {
                            defaultItem = item;
                            }
                    } );

                if ( databaseNameComboBox.Items.Count > 0 )
                    {
                    databaseNameComboBox.SelectedItem = defaultItem != null ? defaultItem : databaseNameComboBox.Items[ 0 ];
                    }
                }
            else
                {
                ShowError( errorMessage );
                return;
                }
            }

        private void ShowError( string errorMessage )
            {
            goButton.Visibility = System.Windows.Visibility.Hidden;
            myMessage.Text = errorMessage;
            myMessage.Visibility = Visibility.Visible;
            }

        private void serverNameTextBox_KeyDown( object sender, KeyEventArgs e )
            {
            if ( e.Key == Key.Enter )
                {
                databaseNameComboBox.Focus();
                }
            }

        private SolutionInfo newSolution
            {
            get;
            set;
            }

        private bool isSolutionRecognized;

        private void databaseNameComboBox_KeyDown( object sender, KeyEventArgs e )
            {
            if ( e.Key == Key.Enter )
                {
                AddCurrentSolution();
                }
            }

        private void serverNameTextBox_TextChanged( object sender, TextChangedEventArgs e )
            {
            HideError();
            }

        private void HideError()
            {
            goButton.Visibility = System.Windows.Visibility.Visible;
            myMessage.Visibility = Visibility.Hidden;
            myMessage.Text = "";
            }

        private SolutionInfo getCurrentSolutionInfo()
            {
            if ( databaseNameComboBox.SelectedItem != null )
                {
                return ( ( ComboBoxItem )databaseNameComboBox.SelectedItem ).Tag as SolutionInfo;
                }
            else
                {
                return null;
                }
            }

        private void databaseNameComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
            {
            SolutionInfo currentSolutionInfo = getCurrentSolutionInfo();
            if ( currentSolutionInfo != null )
                {
                if ( currentSolutionInfo.SolutionName == null )
                    {
                    SolutionInfo newSolutionInfo;
                    if ( DatabaseHelper.ReadSolutionInfo( currentSolutionInfo.SqlServerName, currentSolutionInfo.SqlBaseName, out newSolutionInfo ) )
                        {
                        currentSolutionInfo.SolutionFriendlyName = newSolutionInfo.SolutionFriendlyName;
                        currentSolutionInfo.SolutionName = newSolutionInfo.SolutionName;
                        solutionNameTextBox.IsReadOnly = false;
                        }
                    else
                        {
                        currentSolutionInfo.SolutionFriendlyName = "<система не обнаружена>";
                        solutionNameTextBox.IsReadOnly = true;
                        }
                    }
                solutionNameTextBox.Text = currentSolutionInfo.SolutionFriendlyName;
                HideError();
                }
            }

        internal static SolutionInfo AddNewSolution()
            {
            AddNewSystemWindow addNewSystemWindow = new AddNewSystemWindow();
            addNewSystemWindow.ShowDialog();
            return addNewSystemWindow.isSolutionRecognized ? addNewSystemWindow.newSolution : null;
            }

        private void solutionNameTextBox_TextChanged( object sender, TextChangedEventArgs e )
            {
            SolutionInfo currentSolutionInfo = getCurrentSolutionInfo();
            if ( currentSolutionInfo != null && !solutionNameTextBox.IsReadOnly )
                {
                currentSolutionInfo.SolutionFriendlyName = solutionNameTextBox.Text;
                }
            }
        }
    }
