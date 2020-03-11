using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using AramisStarter.Utils;

namespace AramisStarter
    {
    public class App : Application
        {
        private App()
            : base()
            {

            }

        #region public

        internal static void AddStarterUpdateDatabasePath( string starterUpdateDatabasePathInfo, string errorMessage )
            {
            App app = new App();
            app.Run( new AddNewSystemWindow( starterUpdateDatabasePathInfo, errorMessage ) );
            }

        #endregion
        }
    }
