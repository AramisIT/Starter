using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Windows;

namespace AramisStarter
    {
    /// <summary>
    /// Interaction logic for Splash.xaml
    /// </summary>
    public partial class Splash : Window
        {

        #region private

        private static object getFormLocker = new object();

        private BackgroundWorker checkLoadingStatusWorker;

        private Splash()
            {
            Owner = LoginWindow.Window;
            InitializeComponent();
            logoImage.Source = EmbededResourcesConverter.BitmapSourceFromBitmap( Properties.Resources.LogoImage );
            }

        private void Grid_Loaded_1( object sender, RoutedEventArgs e )
            {
            // VistaGlassHelper.ExtendGlass( this, -1, -1, -1, -1 );
            }

        private static Splash splashWindow;
        private const int MAX_PROGRESS_VALUE = 1000;
        private const int MIN_SPLASH_SHOWING_TIME_MILLISEC = 4000;

        void checkLoadingStatusWorker_ProgressChanged( object sender, ProgressChangedEventArgs e )
            {
            if ( e.ProgressPercentage > MAX_PROGRESS_VALUE )
                {
                Hide();
                }
            else if ( e.ProgressPercentage == 0 && Starter.ErrorStart )
                {
                Close();
                LoginWindow.Window.Close();
                }
            else
                {
                loadingProgress.Value = e.ProgressPercentage;
                }
            }

        void checkLoadingStatusWorker_DoWork( object sender, DoWorkEventArgs e )
            {
            while ( true )
                {
                if ( Starter.ErrorStart )
                    {
                    checkLoadingStatusWorker.ReportProgress( 0 );
                    break;
                    }

                if ( Starter.SolutionLoaded )
                    {
                    ReportProgress( MAX_PROGRESS_VALUE );
                    return;
                    }
                else
                    {
                    ReportProgress( SolutionUpdater.DownloadingComplateProgress );
                    if ( SolutionUpdater.DownloadingComplateProgress == MAX_PROGRESS_VALUE )
                        {
                        return;
                        }
                    }

                System.Threading.Thread.Sleep( 250 );
                }
            }

        private void ReportProgress( int progressValue )
            {
            checkLoadingStatusWorker.ReportProgress( progressValue );

            if ( progressValue == MAX_PROGRESS_VALUE )
                {
                Thread.Sleep( MIN_SPLASH_SHOWING_TIME_MILLISEC );
                checkLoadingStatusWorker.ReportProgress( progressValue + 1 );
                }

            }

        private void logoImage_Loaded( object sender, RoutedEventArgs e )
            {
            InitWorker();
            }

        private void InitWorker()
            {
            checkLoadingStatusWorker = new BackgroundWorker();
            checkLoadingStatusWorker.WorkerReportsProgress = true;
            checkLoadingStatusWorker.DoWork += checkLoadingStatusWorker_DoWork;
            checkLoadingStatusWorker.ProgressChanged += checkLoadingStatusWorker_ProgressChanged;
            checkLoadingStatusWorker.RunWorkerAsync();
            }

        private void Button_Click_1( object sender, RoutedEventArgs e )
            {
            SolutionUpdater.ResetVersion();
            }

        private void Button_Click_2( object sender, RoutedEventArgs e )
            {

            }

        private void Button_Click_3( object sender, RoutedEventArgs e )
            {
            LoginWindow.Window.Close();
            }

        #endregion

        #region public

        internal void HideWindow()
            {
            SetNewVersionDownloadingStatus( false );
            this.Dispatcher.Invoke( new Action( () => this.Hide() ) );
            }

        internal static Splash SplashWindow
            {
            get
                {
                lock ( getFormLocker )
                    {
                    if ( splashWindow == null )
                        {
                        splashWindow = new Splash();
                        }
                    }
                return splashWindow;
                }
            }

        internal void SetProgress( int progressValue )
            {
            int currentValue = ( int )SplashWindow.loadingProgress.Value;
            if ( currentValue != progressValue )
                {
                SplashWindow.loadingProgress.Value = progressValue;
                }
            }

        internal void ShowSlowly()
            {
            SplashWindow.logoImage.Opacity = 0;
            SplashWindow.loadingProgress.Opacity = 0;
            SplashWindow.progressRect.Opacity = 0;

            splashWindow.Show();

            DoubleAnimation animation = new DoubleAnimation { To = 1, Duration = TimeSpan.FromMilliseconds( 1800 ) };
            animation.Completed += new EventHandler( ( senderA, eA ) =>
            {
                SplashWindow.logoImage.Opacity = 1.0;
                SplashWindow.loadingProgress.Opacity = 1.0;
                SplashWindow.progressRect.Opacity = 1.0;
                SplashWindow.logoImage.BeginAnimation( OpacityProperty, null );
            } );

            SplashWindow.logoImage.BeginAnimation( OpacityProperty, animation );
            }

        internal static void SetNewVersionDownloadingStatus( bool isUpdateDownloadingNow )
            {  
            System.Windows.Application.Current.Dispatcher.Invoke( new Action( () => SplashWindow.newVersionDownloadingNotifying.Visibility = isUpdateDownloadingNow ? Visibility.Visible : Visibility.Hidden ) );
            }

        #endregion
        }
    }
