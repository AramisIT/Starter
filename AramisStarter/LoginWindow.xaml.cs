using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
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
        public LoginWindow()
            {
            InitializeComponent();            
            }

        private void Window_Loaded_1( object sender, RoutedEventArgs e )
            {
            VistaGlassHelper.ExtendGlass( this, -1, -1, -1, -1 );
            }

        private void Button_Click_1( object sender, RoutedEventArgs e )
            {
            myMessage.Visibility = System.Windows.Visibility.Visible;
            ( sender as Control ).Visibility = System.Windows.Visibility.Hidden;
            savePasswordButton.Visibility = System.Windows.Visibility.Collapsed;
            passwordBox.Focus();
            }       

        private void passwordTextBox_PasswordChanged( object sender, RoutedEventArgs e )
            {
            savePasswordButton.Visibility = passwordBox.Password.Length == 0 ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            }

        private void ComboBox_SelectionChanged_1( object sender, SelectionChangedEventArgs e )
            {
            passwordBox.Focus();
            }
        }
    }
