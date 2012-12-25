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
using AramisPreStart;
using AramisStarter.Utils;
using Microsoft.Win32;

namespace AramisStarter
    {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class AddNewSystemWindow : Window
        {

        internal static bool Showed
            {
            get;
            private set;
            }

        public AddNewSystemWindow( string starterUpdateDatabasePathInfo, string errorMessage )
            {
            InitializeComponent();
            Icon = EmbededResourcesConverter.BitmapSourceFromBitmap( AramisPreStart.Properties.Resources.transparent );
            this.starterUpdateDatabasePathInfo = starterUpdateDatabasePathInfo;
            
            if ( errorMessage != null )
                {
                SetParametersFromFile( starterUpdateDatabasePathInfo );
                ShowError(errorMessage);
                }

            Showed = true;
            }

        private void Window_Loaded_1( object sender, RoutedEventArgs e )
            {
            //if ( serverName != null )
            //    {
            //    serverNameTextBox.Text = serverName;
            //    serverNameTextBox.SelectionStart = serverName.Length;
            //    serverNameTextBox.SelectionLength = 0;
            //    serverNameTextBox.Focus();
            //    }
            }

        private void Button_Click_1( object sender, RoutedEventArgs e )
            {
            string serverName = serverNameTextBox.Text.Trim();
            string databaseName = databaseNameTextBox.Text.Trim();
            if ( !string.IsNullOrEmpty( serverName ) && !string.IsNullOrEmpty( databaseName ) )
                {
                SavePath( string.Format( "{0} ; {1}", serverName, databaseName ) );
                }

            Close();
            }

        private void SavePath( string databasePath )
            {
            using ( StreamWriter streamWriter = File.CreateText( starterUpdateDatabasePathInfo ) )
                {
                streamWriter.Write( databasePath );
                }
            }

        private void serverNameTextBox_LostFocus( object sender, RoutedEventArgs e )
            {

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
                databaseNameTextBox.Focus();
                }
            }

        private bool isSolutionRecognized;
        private string starterUpdateDatabasePathInfo;

        private void databaseNameComboBox_KeyDown( object sender, KeyEventArgs e )
            {
            if ( e.Key == Key.Enter )
                {

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

        private void solutionNameTextBox_TextChanged( object sender, TextChangedEventArgs e )
            {
            //SolutionInfo currentSolutionInfo = getCurrentSolutionInfo();
            //if ( currentSolutionInfo != null && !solutionNameTextBox.IsReadOnly )
            //    {
            //    currentSolutionInfo.SolutionFriendlyName = solutionNameTextBox.Text;
            //    }
            }

        private void Button_Click_2( object sender, RoutedEventArgs e )
            {
            LoadPathFromFile();
            }

        private void LoadPathFromFile()
            {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.Filter = "ini files (*.ini)|*.ini|All files (*.*)|*.*";
            openFileDialog.RestoreDirectory = true;

            bool? openResult = openFileDialog.ShowDialog();

            if ( openResult == true && openFileDialog.FileName != null )
                {
                if ( !SetParametersFromFile( openFileDialog.FileName ) )
                    {
                    ShowError( string.Format( "Требуемый формат файла:\r\n<Имя сервера>;<Имя базы>", openFileDialog.FileName ) );
                    }
                }
            }

        private bool SetParametersFromFile( string fileName )
            {
            StarterUpdateDatabasePath starterUpdateDatabasePath;
            if ( StarterUpdateDatabasePath.ReadStarterUpdateDatabasePath( fileName, out starterUpdateDatabasePath ) )
                {
                serverNameTextBox.Text = starterUpdateDatabasePath.ServerName;
                databaseNameTextBox.Text = starterUpdateDatabasePath.DatabaseName;
                return true;
                }
            else
                {
                return false;
                }
            }

        private void databaseNameTextBox_TextChanged( object sender, TextChangedEventArgs e )
            {
            HideError();
            }
        }
    }
