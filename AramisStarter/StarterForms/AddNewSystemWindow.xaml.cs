using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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

            ComboBoxItem comboBoxItem = databaseNameComboBox.SelectedItem as ComboBoxItem;
            string databaseName = comboBoxItem.Content as string;
            NewSolution = new SolutionInfo()
                {
                    SqlServerName = serverNameTextBox.Text.Trim(),
                    SqlBaseName = databaseName
                };
            SetSystemName( NewSolution );
            Close();
            }

        private void SetSystemName( SolutionInfo solution )
            {
            string systemNameInfo = GetSystemNameInfo( solution.SqlBaseName );

            int separatorIndex = systemNameInfo.IndexOf( ';' );

            if ( separatorIndex <= 0 )
                {
                solution.SolutionName = solution.SqlBaseName;
                solution.SolutionFriendlyName = solution.SqlBaseName;
                }
            else
                {
                solution.SolutionName = systemNameInfo.Substring( 0, separatorIndex );
                solution.SolutionFriendlyName = systemNameInfo.Substring( separatorIndex + 1, systemNameInfo.Length - ( separatorIndex + 1 ) );
                }
            }

        private string GetSystemNameInfo( string databaseName )
            {
            try
                {
                using ( SqlConnection conn = new SqlConnection( DatabaseHelper.GetConnectionString( serverNameTextBox.Text.Trim(), databaseName, "GetUsersDescriptions" ) ) )
                    {
                    conn.Open();
                    using ( SqlCommand cmd = new SqlCommand( "select dbo.GetAramisSystemName()", conn ) )
                        {
                        string result = cmd.ExecuteScalar() as string;
                        return result;
                        }
                    }
                }
            catch
                {
                return databaseName;
                }
            }

        private void serverNameTextBox_LostFocus( object sender, RoutedEventArgs e )
            {
            FillDatabasesList();
            }

        private void FillDatabasesList()
            {
            databaseNameComboBox.Items.Clear();

            using ( SqlConnection conn = new SqlConnection( DatabaseHelper.GetConnectionString( serverNameTextBox.Text.Trim() ) ) )
                {
                try
                    {
                    conn.Open();
                    }
                catch ( Exception exp )
                    {
                    ShowError( "Не обнаружен сервер" );
                    return;
                    }

                try
                    {
                    using ( SqlCommand cmd = new SqlCommand( "GetAramisDatabasesList", conn ) )
                        {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        using ( SqlDataReader reader = cmd.ExecuteReader() )
                            {
                            while ( reader.Read() )
                                {
                                ComboBoxItem item = new ComboBoxItem();
                                item.Content = reader[ 0 ].ToString();

                                databaseNameComboBox.Items.Add( item );
                                }
                            }
                        }
                    }
                catch ( Exception exp )
                    {
                    Trace.WriteLine( exp.Message );
                    ShowError( "Не удалось получить список информационных баз" );
                    }
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
        }
    }
