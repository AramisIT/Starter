using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AramisStarter
    {
    class Starter
        {
        private const string LOADED_STR = "AramisSystemLoaded";
        private static AppDomain solutionDomain;
        private static volatile bool waitForStarting = true;

        public static bool WaitForStarting
            {
            get { return Starter.waitForStarting; }
            }

        internal static Guid CurrentSessionId
            {
            get;
            set;
            }

        internal static void RunApplication()
            {
            solutionDomain = AppDomain.CreateDomain( "Solution domain" );
            solutionDomain.SetData( LOADED_STR, false );
            waitForStarting = false;
            ShowSplash();
            }

        private static void ShowSplash()
            {
            Splash.SplashWindow.SetProgress( 0 );
            Splash.SplashWindow.ShowSlowly();
            }

        internal static void BeginSolutionLoading( SolutionInfo selectedSolution )
            {
            // System.Windows.MessageBox.Show( Process.GetCurrentProcess().ProcessName );
            }

        internal static bool EnterMutex( Mutex mutex, int millisecondsTimeout = -1 )
            {
            try
                {
                if ( millisecondsTimeout == -1 )
                    {
                    return mutex.WaitOne();
                    }
                else
                    {
                    return mutex.WaitOne( millisecondsTimeout );
                    }
                }
            catch ( AbandonedMutexException )
                {
                return true;
                }
            }

        internal static void ExitMutex( Mutex mutex )
            {
            try
                {
                mutex.ReleaseMutex();
                }
            catch { }
            }

        internal static bool SolutionLoaded
            {
            get
                {
                bool solutionLoaded = false;
                try
                    {
                    solutionLoaded = ( bool )solutionDomain.GetData( LOADED_STR );
                    }
                catch { }

                return true; //solutionLoaded
                }
            }

        }
    }
