using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using ConnectionGetterInterface;

namespace WpfApplication1
    {
    class Connection40: IConnectionGetter
        {
        public System.Data.SqlClient.SqlConnection ConnectionToSQL
            {
            get
                {
                MessageBox.Show( "Опасный подход" );
                return new System.Data.SqlClient.SqlConnection( "Data Source=ATOS;Initial Catalog=AramisUTK;user id=sa;password=123" );
                }
            }


        public void CheckAccessibility()
            {
            
            }
        }
    }
