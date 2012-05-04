using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Aramis.NET;

namespace AramisStarter
    {
    public class App : Application, IStarter
        {
        [STAThread()]
        static void Main( string[] args )
            {
            ( new App() ).Start( args );
            }

        public void Start( string[] args )
            {
            Run( new LoginWindow() );
            }
        }
    }
