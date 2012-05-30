﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
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
    public partial class LoginWindow : Window
        {
        private LoginWindow()
            {
            InitializeComponent();
            Icon = EmbededResourcesConverter.BitmapSourceFromBitmap( Properties.Resources.Transparent );            
            }

        private void Window_Loaded( object sender, RoutedEventArgs e )
            {
            Title = string.Format( Title, App.SelectedSolution.SolutionFriendlyName );
            if ( !VistaGlassHelper.ExtendGlass( this, -1, -1, -1, -1 ) )
                {
                UseAlternativeStyle();
                }
            FillNames();
            SelectLastLogin();
            }

        private void UseAlternativeStyle()
            {
            backGroungImage.Source = EmbededResourcesConverter.BitmapSourceFromBitmap( Properties.Resources.BBB );
            //ImageSource imgSource = new ImageSource();
            //this.Background = new ImageBrush(imgSource);
            backGroungImage.Visibility = System.Windows.Visibility.Visible;
            }

        private void SelectLastLogin()
            {
            string lastLogin = Authorization.GetLastLogin();

            if ( lastLogin == null )
                {
                return;
                }

            foreach ( ComboBoxItem item in NamesDescriptions.Items )
                {
                string id = item.Tag as string;
                if ( lastLogin == id )
                    {
                    NamesDescriptions.SelectedItem = item;
                    return;
                    }
                }
            }

        private void FillNames()
            {
            NamesDescriptions.Items.Clear();

            using ( SqlConnection conn = new SqlConnection( DatabaseHelper.GetConnectionString( App.SelectedSolution.SqlServerName, App.SelectedSolution.SqlBaseName, "GetUsersDescriptions" ) ) )
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
            savePasswordCheckBox.Visibility = System.Windows.Visibility.Collapsed;
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
            return DatabaseHelper.CheckPassword( userId, securePassword );
            }

        private void passwordTextBox_PasswordChanged( object sender, RoutedEventArgs e )
            {
            UpdateSavePasswordOption();
            HideErrorMessage();
            }

        private void UpdateSavePasswordOption()
            {
            savePasswordCheckBox.Visibility = passwordBox.Password.Length == 0 ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            if ( passwordBox.SecurePassword.Length == 0 )
                {
                SavePassword = false;
                savedPassword = null;
                }
            }

        private void ComboBox_SelectionChanged_1( object sender, SelectionChangedEventArgs e )
            {
            HideErrorMessage();
            bool passwordWasSaved = SavePassword;
            savedPassword = Authorization.TryToRestoreQuickStart( UserName );
            SavePassword = savedPassword != null;

            if ( SavePassword )
                {
                passwordBox.Password = "************";
                }
            else if ( passwordWasSaved )
                {
                passwordBox.Password = "";
                }

            if ( NamesDescriptions.SelectedItem != null )
                {
                object itemTag = ( NamesDescriptions.SelectedItem as ComboBoxItem ).Tag;
                UserName = itemTag as string;
                }
            }

        private void passwordBox_KeyDown( object sender, KeyEventArgs e )
            {
            if ( e.Key == Key.Enter )
                {
                TryToLogin();
                }
            }

        private static void ShowSplash()
            {
            Splash.SplashWindow.SetProgress( SolutionUpdater.DownloadingComplateProgress );
            Splash.SplashWindow.ShowSlowly();
            }

        private void TryToLogin()
            {
            if ( NamesDescriptions.SelectedItem != null )
                {
                bool passwordCorrect = CheckPassword( UserName, UserPassword );

                if ( passwordCorrect )
                    {
                    SaveAuthorization();
                    Hide();
                    ShowSplash();
                    authorized = true;
                    Starter.Init( App.SelectedSolution );
                    }
                else
                    {
                    savedPassword = null;
                    ShowError();
                    passwordBox.Focus();
                    return;
                    }
                }
            }

        private void SaveAuthorization()
            {
            Authorization.SaveLastLogin( UserName );
            if ( SavePassword )
                {
                Authorization.SaveQuickStart( UserName, UserPassword );
                }
            else
                {
                Authorization.EraseQuickStartData( UserName );
                }
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

        private static SecureString savedPassword;
        private volatile bool authorized;

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
            get;
            private set;
            }

        internal static SecureString UserPassword
            {
            get
                {
                return savedPassword ?? Window.passwordBox.SecurePassword;
                }
            }

        private static bool SavePassword
            {
            get
                {
                return Window.savePasswordCheckBox.IsChecked == true;
                }
            set
                {
                Window.savePasswordCheckBox.IsChecked = value;
                }
            }

        private void NamesDescriptions_KeyDown( object sender, KeyEventArgs e )
            {
            if ( e.Key == Key.Enter )
                {
                passwordBox.Focus();
                }
            }

        internal static bool Authorized
            {
            get
                {
                return Window.authorized;
                }
            }
        }
    }
