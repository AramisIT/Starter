using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Aramis.Loader;

namespace System
    {
    public static class Extensions
        {
        public static void Error(this string message)
            {
            MessageBox.Show(message, Program.LOADER_DESCRIPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
