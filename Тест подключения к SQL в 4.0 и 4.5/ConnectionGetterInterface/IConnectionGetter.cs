using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace ConnectionGetterInterface
    {
    public interface IConnectionGetter
        {
        SqlConnection ConnectionToSQL
            {
            get;
            }

        void CheckAccessibility();
        }
    }
