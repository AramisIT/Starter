using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aramis.Loader.SolutionsInfoLoader
    {
    public class SolutionInfo
        {
        public SolutionInfo(string description, string applicationDirectory)
            {
            this.Description = description;
            this.ApplicationDirectory = applicationDirectory;
            }
        public string Description
            {
            get;
            private set;
            }

        public string ApplicationDirectory
            {
            get;
            private set;
            }

        public override string ToString()
            {
            return Description;
            }
        }
    }
