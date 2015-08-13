using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace WebSolutionUpdater.Helpers
    {
    public class IOHelper
        {
        public static bool TryRemoveFile(string fileName)
            {
            if (!File.Exists(fileName)) return true;

            try
                {
                File.Delete(fileName);
                }
            catch
                {

                }
            return !File.Exists(fileName);
            }

        public static bool TryEmptyDirectory(string directoryPath)
            {
            if (!Directory.Exists(directoryPath)) return true;

            var dirInfo = new DirectoryInfo(directoryPath);
            return removeContent(dirInfo);
            }

        private static bool removeContent(DirectoryInfo dirInfo)
            {
            foreach (var file in dirInfo.GetFiles())
                {
                if (!TryRemoveFile(file.FullName)) return false;
                }

            foreach (var dir in dirInfo.GetDirectories())
                {
                if (!removeContent(dir)) return false;

                // if (!
                removeEmptyDirectory(dir.FullName);
                //) return false;
                }

            return true;
            }

        private static bool removeEmptyDirectory(string directoryPath)
            {
            try
                {
                Directory.Delete(directoryPath);
                }
            catch (Exception exp)
                {
                Trace.WriteLine(exp.Message);
                return false;
                }
            return true;
            }

        public static bool TryCreateDirectory(string directoryPath)
            {
            if (!Directory.Exists(directoryPath))
                {
                try
                    {
                    Directory.CreateDirectory(directoryPath);
                    }
                catch (Exception exp)
                    {
                    return false;
                    }
                }

            return true;
            }
        }
    }
