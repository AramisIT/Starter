using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Aramis.Loader.Properties;

namespace Aramis.Loader.SolutionUpdate
    {
    public static class Waiting
        {
        /// <summary>
        /// Задержка между проверками 
        /// </summary>
        private static readonly int delay = 2000;

        public delegate void OnWaitingEndDelegate();
        
        /// <summary>
        /// Вызывается после окончания ожидания. 
        /// ВАЖНО!!! Отписываться от собития НЕ НУЖНО. Все подписи сбрасываются после вызова.
        /// </summary>
        public static event OnWaitingEndDelegate OnWaitingEnd;

        /// <summary>
        /// Ожидает окончания процесса с определенным кодом.
        /// </summary>
        public static bool Wait(WaitingCodes code)
            {
            using ( SqlConnection DBConnection = new SqlConnection(Settings.Default.ConnectionString) )
                {
                try
                    {
                    DBConnection.Open();
                    using ( SqlCommand Command = DBConnection.CreateCommand() )
                        {
                        Command.CommandText = "select * from isUpdate where action = @Code and [ApplicationName] = @ApplicationName";
                        Command.Parameters.AddWithValue("@Code", ( int ) code);
                        Command.Parameters.AddWithValue("@ApplicationName", Program.SolutionName);
                        while (Command.ExecuteScalar() != null)
                            {
                            System.Threading.Thread.Sleep(delay);
                            } 
                        }
                    }
                catch 
                    {
                    "Ошибка соединения с базой данных.\r\nПроверьте подключение к сети.".Error();
                    return false;
                    }
                }
            if ( OnWaitingEnd != null )
                {
                OnWaitingEnd();
                OnWaitingEnd = null;
                }            
            return true;
            }
        }
    }
