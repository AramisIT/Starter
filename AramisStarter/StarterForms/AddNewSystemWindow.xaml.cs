using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
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

        private void Window_Loaded_1( object sender, RoutedEventArgs e )
            {
            VistaGlassHelper.ExtendGlass( this, -1, -1, -1, -1 );
            string serverName = Aramis.NET.PublicStarterProperties.DefaultServerName;
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
            NewSolution = currentItem.Tag as SolutionInfo;           
            Close();
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
                databasesInfo.ForEach( databaseInfo =>
                    {
                        ComboBoxItem item = new ComboBoxItem();
                        item.Content = databaseInfo.SqlBaseName;
                        item.Tag = databaseInfo;
                        databaseNameComboBox.Items.Add( item );
                    } );
                if ( databaseNameComboBox.Items.Count > 0 )
                    {
                    databaseNameComboBox.SelectedItem = databaseNameComboBox.Items[ 0 ];
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
        
        internal SolutionInfo NewSolution
            {
            get;
            private set;
            }

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

        private void databaseNameComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
            {
            if ( databaseNameComboBox.SelectedItem != null)
                {
                SolutionInfo currentSolutionInfo = ( ( ComboBoxItem )databaseNameComboBox.SelectedItem ).Tag as SolutionInfo;
                solutionNameTextBox.Text = currentSolutionInfo.SolutionName;
                }
            }
        }
    }
