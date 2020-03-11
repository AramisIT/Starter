using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace System
    {
    internal static class ErrorsNotifier
        {
        internal static void ShowError(this string errorMessage )
            {
            MessageBox.Show( errorMessage, "Aramis .NET starter", MessageBoxButton.OK, MessageBoxImage.Error );
            }
        }
    }
