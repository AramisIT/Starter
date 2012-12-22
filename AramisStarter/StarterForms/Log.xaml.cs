using System;
using System.Collections.Generic;
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

namespace AramisStarter
    {
    /// <summary>
    /// Interaction logic for Log.xaml
    /// </summary>
    public partial class Log : Window
        {
        internal static volatile bool Testing = false;

        private static volatile bool windowCreated = false;
        private static Log logWindow;

        public Log()
            {
            InitializeComponent();
            logWindow = this;
            windowCreated = true;
            }

        private void Button_Click_1( object sender, RoutedEventArgs e )
            {
            logTextBox.Text = "";
            }

        private static object appendLocker = new object();

        public static void Append( string message )
            {
            lock ( appendLocker )
                {
                if ( Log.Testing )
                    {
                    if ( !windowCreated )
                        {
                        return;
                        }

                    logWindow.Dispatcher.Invoke( new Action<string>( AppendMessage ), new object[] { message } );
                    }
                }
            }

        private static void AppendMessage( string message )
            {
            logWindow.logTextBox.Text = message + "\r\n" + logWindow.logTextBox.Text;
            }

        internal static void CloseWindow()
            {
            if ( windowCreated )
                {
                logWindow.Close();
                }
            }
        }
    }
