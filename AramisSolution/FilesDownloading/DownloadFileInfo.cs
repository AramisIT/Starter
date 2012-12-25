using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AramisStarter.FilesDownloading
    {
    class DownloadFileInfo
        {
        internal string Path
            {
            get;
            private set;
            }

        internal long FileSize
            {
            get;
            private set;
            }

        public DownloadFileInfo( string path, long fileSize )
            {           
            this.Path = path;
            this.FileSize = fileSize;
            }
        }
    }
