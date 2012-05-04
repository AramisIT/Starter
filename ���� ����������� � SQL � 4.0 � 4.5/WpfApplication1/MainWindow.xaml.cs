using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Security;
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

namespace WpfApplication1
    {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
        {
        public MainWindow()
            {
            InitializeComponent();
            }

        private void Window_Loaded_1( object sender, RoutedEventArgs e )
            {

            }

        private void Button_Click_1( object sender, RoutedEventArgs e )
            {

            using ( SqlConnection cn = ConnectionCreator.ConnectionToSQL )
            // new SqlConnection( "Server=(local);Initial Catalog=AramisUTK;" ) )
                {
                // Use the connection
                using ( SqlCommand cmd = new SqlCommand( "Select GETDATE() d", cn ) )
                    {

                    cn.Open();
                    
                    Trace.WriteLine(cn.ConnectionString);
                    object data = cmd.ExecuteScalar();
                    MessageBox.Show( data.ToString() );
                    }
               
                }
            }
        }
    }
