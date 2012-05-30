using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;


namespace AramisStarter.Utils
    {
    class SyncHelper
        {

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

        }
    }
