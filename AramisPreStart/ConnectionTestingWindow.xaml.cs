using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AramisPreStart
    {
    /// <summary>
    /// Interaction logic for ConnectionTestingWindow.xaml
    /// </summary>
    public partial class ConnectionTestingWindow : Window
        {
        private string databaseName;
        private string sqlServer;
        public ConnectionTestingWindow(string sqlServer, string databaseName)
            {
            this.sqlServer = sqlServer;
            this.databaseName = databaseName;
            InitializeComponent();
            }



        private void Button_Click(object sender, RoutedEventArgs e)
            {
            logTextBox.Text = string.Empty;

            testConnection(string.Empty);
            testConnection("Files");
            testConnection("Update");

            logTextBox.Text = logTextBox.Text.Trim();
            }

        private void testConnection(string databaseNameSuffix)
            {
            string currentDatabaseName = this.databaseName + databaseNameSuffix;
            string connectionString = string.Format("data source={0};initial catalog={1};user id={2}; password={3};",
                sqlServer,
                currentDatabaseName,
                loginTextBox.Text.Trim(),
                passwordTextBox.Text.Trim());

            using (var connection = new SqlConnection(connectionString))
                {
                string resultStr = currentDatabaseName + ": ";
                try
                    {
                    connection.Open();
                    resultStr += "OK";
                    }
                catch (Exception exp)
                    {
                    resultStr += exp.Message;
                    }

                logTextBox.Text += string.Format("{0}\r\n\r\n\r\n", resultStr);
                }
            }
        }
    }
