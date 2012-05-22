using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
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
using Windows;

namespace AramisStarter
    {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
        {
        private SolutionInfo solution;
        private Splash splash;

        private LoginWindow()
            {
            InitializeComponent();
            Icon = EmbededResourcesConverter.BitmapSourceFromBitmap( Properties.Resources.Transparent);
            }

        private void Window_Loaded_1( object sender, RoutedEventArgs e )
            {
            Title = string.Format( Title, solution.SolutionFriendlyName );
            VistaGlassHelper.ExtendGlass( this, -1, -1, -1, -1 );
            FillNames();
            }

        private void FillNames()
            {
            NamesDescriptions.Items.Clear();

            using ( SqlConnection conn = new SqlConnection( DBChecker.GetConnectionString( solution.SqlServerName, solution.SolutionName, "GetUsersDescriptions" ) ) )
                {
                try
                    {
                    conn.Open();
                    }
                catch ( Exception exp )
                    {
                    ShowError( "Нет подключения" );
                    return;
                    }

                try
                    {
                    using ( SqlCommand cmd = new SqlCommand( "GetUsers", conn ) )
                        {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        using ( SqlDataReader reader = cmd.ExecuteReader() )
                            {
                            while ( reader.Read() )
                                {
                                ComboBoxItem item = new ComboBoxItem();
                                item.Content = reader[ "Description" ].ToString();
                                item.Tag = reader[ "Id" ].ToString();
                                NamesDescriptions.Items.Add( item );
                                }
                            }
                        }
                    }
                catch ( Exception exp )
                    {
                    Trace.WriteLine( exp.Message );
                    ShowError( exp.Message );
                    }
                }
            }

        private void ShowError( string errorMessage = "Неверный пароль" )
            {
            savePasswordButton.Visibility = System.Windows.Visibility.Collapsed;
            goButton.Visibility = System.Windows.Visibility.Collapsed;

            myMessage.Text = errorMessage;
            myMessage.Visibility = Visibility.Visible;
            }

        private void Button_Click_1( object sender, RoutedEventArgs e )
            {
            TryToLogin();
            }

        private bool CheckPassword( string userId, SecureString securePassword )
            {
            return DBChecker.CheckPassword( solution.SqlServerName, solution.SolutionName, userId, securePassword );
            }

        private void passwordTextBox_PasswordChanged( object sender, RoutedEventArgs e )
            {
            UpdateSavePasswordOption();
            HideErrorMessage();
            }

        private void UpdateSavePasswordOption()
            {
            savePasswordButton.Visibility = passwordBox.Password.Length == 0 ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            }

        private void ComboBox_SelectionChanged_1( object sender, SelectionChangedEventArgs e )
            {            
            HideErrorMessage();
            }

        internal void SetSolution( SolutionInfo selectedSolution )
            {
            this.solution = selectedSolution;
            }

        private void passwordBox_KeyDown( object sender, KeyEventArgs e )
            {
            if ( e.Key == Key.Enter )
                {
                TryToLogin();
                }
            }

        private void TryToLogin()
            {
            if ( NamesDescriptions.SelectedItem != null )
                {
                bool passwordCorrect = CheckPassword( UserName, UserPassword );

                if ( passwordCorrect )
                    {
                    ShowSplash();
                    }
                else
                    {
                    ShowError();
                    passwordBox.Focus();
                    return;
                    }
                }
            }

        private void ShowSplash()
            {
            Hide();
            Starter.RunApplication();            
            }

        private void HideErrorMessage()
            {
            if ( goButton.Visibility != System.Windows.Visibility.Visible )
                {
                goButton.Visibility = System.Windows.Visibility.Visible;
                myMessage.Visibility = Visibility.Collapsed;
                UpdateSavePasswordOption();
                }
            }

        private static LoginWindow window;

        internal static LoginWindow Window
            {
            get
                {
                if ( window == null )
                    {
                    window = new LoginWindow();
                    }
                return window;
                }
            }

        internal static string UserName
            {
            get
                {
                if ( Window.NamesDescriptions.SelectedItem != null )
                    {
                    object itemTag = ( Window.NamesDescriptions.SelectedItem as ComboBoxItem ).Tag;
                    return itemTag as string;
                    }

                return "";
                }
            }

        internal static SecureString UserPassword
            {
            get
                {
                return Window.passwordBox.SecurePassword;
                }
            }

        private void NamesDescriptions_KeyDown( object sender, KeyEventArgs e )
            {
            if ( e.Key == Key.Enter )
                {
                passwordBox.Focus();
                }
            }
        }
    }
