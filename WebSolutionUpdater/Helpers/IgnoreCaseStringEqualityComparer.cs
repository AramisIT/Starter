using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSolutionUpdater.Helpers
    {
    public class IgnoreCaseStringEqualityComparer : IEqualityComparer<string>
        {
        public bool Equals(string x, string y)
            {
            return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
            }

        public int GetHashCode(string obj)
            {
            return obj == null ? 0 : obj.ToLower().GetHashCode();
            }
        }
    }
