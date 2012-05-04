using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using ConnectionGetterInterface;

namespace ConnectionGetter
    {
    public class Connection45 : IConnectionGetter
        {
        public SqlConnection ConnectionToSQL
            {
            get
                {
                SecureString password = new SecureString();
                password.AppendChar( '1' );
                password.AppendChar( '2' );
                password.AppendChar( '3' );

                password.MakeReadOnly();

                SqlCredential credential = new SqlCredential( "sa", password );
                SqlConnection conn = new SqlConnection( "Server=ATOS;Initial Catalog=AramisUTK;", credential );
                MessageBox.Show( "with SqlCredential" );
                //conn.Open();
                return conn;
                }
            }

        public void CheckAccessibility()
            {
            MessageBox.Show( "CheckAccessibility" );
            SecureString passwd = new SecureString();
            passwd.MakeReadOnly();
            SqlCredential credential = new SqlCredential( "sa", passwd );
            MessageBox.Show( "Accessibility checked" );
            }
        }
    }
