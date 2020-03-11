using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AramisStarter.Utils;


namespace AramisStarter
    {
    public class SolutionInfo
        {

        private static readonly string STARTER_PATH = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ) + @"\Aramis .NET\Starter";
        private static readonly string STARTER_UPDATE_DATABASE_PATH_INFO = STARTER_PATH + @"\StarterUpdateSource.ini";

        public string SqlServerName
            {
            get;
            set;
            }

        public string SqlBaseName
            {
            get;
            set;
            }

        public string SolutionName
            {
            get;
            set;
            }

        public string SolutionFriendlyName
            {
            get;
            set;
            }

        internal string BuildFullSolutionName()
            {
            return string.Format( @"{0}.{1}.{2}", SolutionName, SqlBaseName, GetSafeServerName() );
            }

        private string GetSafeServerName()
            {
            int serverNameLength = SqlServerName.Length;
            StringBuilder safeServerName = new StringBuilder();
            char defaultSeparator = '_';
            List<char> forbiddenSeparators = new List<char>() { '\\', '/' };
            bool isFirstChar = true;

            for ( int charIndex = 0; charIndex < serverNameLength; charIndex++ )
                {
                char currentChar = SqlServerName[ charIndex ];
                if ( Char.IsLetterOrDigit( currentChar ) || currentChar == '.' )
                    {
                    if ( isFirstChar )
                        {
                        isFirstChar = false;
                        currentChar = Char.ToUpper( currentChar );
                        }

                    safeServerName.Append( currentChar );
                    }
                else if ( forbiddenSeparators.Contains( currentChar ) )
                    {
                    safeServerName.Append( defaultSeparator );
                    }
                }

            return safeServerName.ToString();
            }

        internal static SolutionInfo GetDefaultSolution()
            {
            SolutionInfo newSolutionInfo;
            if ( DatabaseHelper.ReadSolutionInfo( GetDefaultServerName(), GetDefaultDatabaseName(), out newSolutionInfo ) )
                {
                return newSolutionInfo;
                }
            else
                {
                return null;
                }
            }

        internal static string GetDefaultServerName()
            {
            if ( File.Exists( STARTER_UPDATE_DATABASE_PATH_INFO ) )
                {
                string databasePath = File.ReadAllText( STARTER_UPDATE_DATABASE_PATH_INFO ).Trim();
                int separatorIndex = databasePath.IndexOf( ';' );
                if ( separatorIndex >= 1 )
                    {
                    return databasePath.Substring( 0, separatorIndex ).Trim();
                    }
                }

            return "";
            }

        private static string GetDefaultDatabaseName()
            {
            if ( File.Exists( STARTER_UPDATE_DATABASE_PATH_INFO ) )
                {
                string databasePath = File.ReadAllText( STARTER_UPDATE_DATABASE_PATH_INFO ).Trim();
                int separatorIndex = databasePath.IndexOf( ';' );
                if ( separatorIndex >= 1 )
                    {
                    return databasePath.Substring( separatorIndex + 1 ).Trim();
                    }
                }

            return null;
            }


        }
    }
