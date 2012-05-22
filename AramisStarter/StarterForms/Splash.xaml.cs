﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private BackgroundWorker checkLoadingStatusWorker;

        private Splash()
            {
            InitializeComponent();
            logoImage.Source = EmbededResourcesConverter.BitmapSourceFromBitmap( Properties.Resources.LogoImage );
            }

        private void Grid_Loaded_1( object sender, RoutedEventArgs e )
            {
            // VistaGlassHelper.ExtendGlass( this, -1, -1, -1, -1 );
            }

        private static Splash splashWindow;

        internal static Splash SplashWindow
            {
            get
                {
                if ( splashWindow == null )
                    {
                    splashWindow = new Splash();
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
       

        void checkLoadingStatusWorker_ProgressChanged( object sender, ProgressChangedEventArgs e )
            {
            if ( e.ProgressPercentage == 100 )
                {
                Hide();
                }
            }

        void checkLoadingStatusWorker_DoWork( object sender, DoWorkEventArgs e )
            {
            while ( true )
                {
                if ( Starter.SolutionLoaded )
                    {
                    checkLoadingStatusWorker.ReportProgress( 100 );
                    break;
                    }

                System.Threading.Thread.Sleep(250);
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
        }
    }
