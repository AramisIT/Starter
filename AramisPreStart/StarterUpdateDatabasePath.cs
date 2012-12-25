using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AramisPreStart
    {
    internal class StarterUpdateDatabasePath
        {
        /// Пусть к файлу с именем сервера БД и именем самой БД в которой содержится стартер.
        /// Формат файла: <Имя сервера>;<Имя базы>
        /// Например: Atos\BEST_SQL_SERVER;AramisMainUpdate
        private static readonly string STARTER_UPDATE_DATABASE_PATH_INFO = Startup.STARTER_PATH + @"\StarterUpdateSource.ini";

        /// <summary>
        /// Максимальный размер файла с настройками. 
        /// Если пользователь случайно выбрал не тот файл, чтобы ему не пришлось ждать под дня, пока загрузится файл в память
        /// </summary>
        private const long FILE_MAX_SIZE = 1000;

        internal string DatabaseName
            {
            get;
            private set;
            }

        internal string ServerName
            {
            get;
            private set;
            }

        internal bool IsValidPath
            {
            get
                {
                return !string.IsNullOrEmpty( ServerName ) && !string.IsNullOrEmpty( DatabaseName );
                }
            }

        internal static bool GetStarterDatabasePath( out StarterUpdateDatabasePath starterUpdateDatabasePath )
            {
            if ( ReadStarterUpdateDatabasePath( STARTER_UPDATE_DATABASE_PATH_INFO, out starterUpdateDatabasePath ) )
                {
                return true;
                }

            RefreshStarterUpdateDatabasePath();

            if ( ReadStarterUpdateDatabasePath( STARTER_UPDATE_DATABASE_PATH_INFO, out starterUpdateDatabasePath ) )
                {
                return true;
                }
            else
                {
                return false;
                }
            }

        internal static void RefreshStarterUpdateDatabasePath( string errorMessage = null )
            {
            AramisStarter.App.AddStarterUpdateDatabasePath( STARTER_UPDATE_DATABASE_PATH_INFO, errorMessage );
            }

        internal static bool ReadStarterUpdateDatabasePath( string starterUpdateDatabasePathInfo, out StarterUpdateDatabasePath starterUpdateDatabasePath )
            {
            starterUpdateDatabasePath = new StarterUpdateDatabasePath();

            if ( File.Exists( starterUpdateDatabasePathInfo ) && ( new FileInfo( starterUpdateDatabasePathInfo ) ).Length <= FILE_MAX_SIZE )
                {
                string path = File.ReadAllText( starterUpdateDatabasePathInfo ).Trim();

                int separatorIndex = path.IndexOf( ';' );
                if ( separatorIndex < 1 || separatorIndex == path.Length )
                    {
                    return false;
                    }

                starterUpdateDatabasePath.ServerName = path.Substring( 0, separatorIndex ).Trim();
                starterUpdateDatabasePath.DatabaseName = path.Substring( separatorIndex + 1 ).Trim();

                return starterUpdateDatabasePath.IsValidPath;
                }

            return false;
            }
        }
    }
