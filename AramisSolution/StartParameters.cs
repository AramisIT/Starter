using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AramisStarter
    {
    internal enum ExecuteTypes
        {
        JustRun,
        Terminate,
        Restart
        }

    internal class StartParameters
        {
        public ExecuteTypes ExecuteType { get; private set; }

        public int TerminatingProcessId { get; private set; }

        public Guid OneOffTicket { get; private set; }

        public string DatabaseName { get; private set; }

        public string ServerName { get; private set; }

        public StartParameters(string startupParameter)
            {
            //exsample - 8040;4dee93e9-f991-47f8-9493-931aa945897a;AramisPlatform;10.9.1.3\Ms^sql^server^2008

            ExecuteType = ExecuteTypes.JustRun;

            if (string.IsNullOrEmpty(startupParameter)) return;

            var parameters = startupParameter.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            const int MIN_PARAMETERS_COUNT = 1;
            if (parameters.Length < MIN_PARAMETERS_COUNT) return;

            int processId;
            if (!Int32.TryParse(parameters.First(), out processId)) return;
            TerminatingProcessId = processId;

            ExecuteType = ExecuteTypes.Terminate;

            const int MAX_PARAMETERS_COUNT = 4;
            if (parameters.Length < MAX_PARAMETERS_COUNT) return;

            const int TICKET_INDEX = 1;
            Guid ticket;
            if (!Guid.TryParse(parameters[TICKET_INDEX], out ticket)) return;
            OneOffTicket = ticket;

            const int DATABASE_INDEX = 2;
            DatabaseName = parameters[DATABASE_INDEX];

            const int SERVER_NAME_INDEX = 3;
            ServerName = parameters[SERVER_NAME_INDEX].Replace('^', ' ');

            ExecuteType = ExecuteTypes.Restart;
            }


        }
    }
