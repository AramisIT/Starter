using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace AramisStarter
    {
    public class SolutionInfo
        {
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
        }
    }
