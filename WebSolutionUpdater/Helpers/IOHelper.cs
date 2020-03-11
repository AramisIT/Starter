using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace WebSolutionUpdater.Helpers
    {
    public class IOHelper
        {
        public static bool TryRemoveFile(string fileName, out string errorDescription)
            {
            errorDescription = "";
            if (!File.Exists(fileName)) return true;

            try
                {
                File.Delete(fileName);
                }
            catch
                {
                Thread.Sleep(300);
                try
                    {
                    File.Delete(fileName);
                    }
                catch (Exception exp)
                    {
                    errorDescription = string.Format(@"Can't remove file ""{0}"": {1}", fileName, exp.Message);
                    return false;
                    }
                }

            return !File.Exists(fileName);
            }

        public static bool TryRemoveFile(string fileName)
            {
            string errorDescription;
            return TryRemoveFile(fileName, out errorDescription);
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
                RemoveEmptyDirectory(dir.FullName);
                //) return false;
                }

            return true;
            }

        public static bool RemoveEmptyDirectory(string directoryPath)
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

        public static bool TryCreateDirectory(string directoryPath, out string error)
            {
            if (!Directory.Exists(directoryPath))
                {
                try
                    {
                    Directory.CreateDirectory(directoryPath);
                    }
                catch (Exception exp)
                    {
                    error = exp.Message;
                    return false;
                    }
                }

            error = null;
            return true;
            }
        }
    }
