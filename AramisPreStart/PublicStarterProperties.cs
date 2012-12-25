using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AramisPreStart
    {
    public static class PublicStarterProperties
        {
        private static string defaultServerName;

        public static string DefaultServerName
            {
            get
                {
                if ( defaultServerName == null )
                    {
                    #region read property value
                   
                    string serverName = null;
                    try
                        {
                        serverName = System.Configuration.ConfigurationManager.AppSettings[ "ServerName" ];
                        }
                    catch
                        {
                        }

                    if ( serverName != null && serverName is string )
                        {
                        defaultServerName = serverName.Trim();
                        } 

                    #endregion
                    }

                return defaultServerName;
                }
            }
        }
    }
