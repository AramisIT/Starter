using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using AramisIDE.SolutionUpdating;
using AramisIDE.Utils;
using Microsoft.SqlServer.Server;
using UpdateTask;
using WebSolutionUpdater.Helpers;

namespace WebSolutionUpdater
    {
    class FilesUpdater
        {
        private string publishDirectory;

        private string updateTempFolder;

        private string checkUrl;

        private WebAppUpdaterTasks webAppUpdaterTasks;

        private string errorDescription;

        private bool updateWebApplication(out string _errorDescription)
            {
            if (!createDirectories())
                {
                _errorDescription = errorDescription;
                return false;
                }

            if (!createFiles())
                {
                _errorDescription = errorDescription;
                return false;
                }

            if (!updateFiles())
                {
                _errorDescription = errorDescription;
                return false;
                }

            if (!removeFiles())
                {
                _errorDescription = errorDescription;
                return false;
                }

            if (!removeDirectories())
                {
                _errorDescription = errorDescription;
                return false;
                }

            _errorDescription = string.Empty;
            return true;
            }

        private bool createDirectories()
            {
            foreach (var directory in webAppUpdaterTasks.DirectoriesToAdd)
                {
                string error;
                if (!IOHelper.TryCreateDirectory(directory, out error))
                    {
                    Thread.Sleep(700);

                    if (!Directory.Exists(directory))
                        {
                        errorDescription = string.Format("Can't create the directory: {0}. {1}", directory, error);
                        return false;
                        }
                    }
                }

            return true;
            }

        private bool createFiles()
            {
            foreach (var fileInfo in webAppUpdaterTasks.FilesToAdd)
                {
                if (!moveFile(fileInfo))
                    {
                    errorDescription = string.Format("Can't move (or copy) file: {0}", fileInfo.FullPath);
                    return false;
                    }
                }

            return true;
            }

        private bool updateFiles()
            {
            var success = true;
            string _errorDescription;
            foreach (var fileTaskDetails in webAppUpdaterTasks.FilesToRewrite)
                {
                success = IOHelper.TryRemoveFile(fileTaskDetails.FullPath, out _errorDescription)
                          && moveFile(fileTaskDetails);

                if (!success && string.IsNullOrEmpty(errorDescription)
                    && !string.IsNullOrEmpty(_errorDescription))
                    {
                    this.errorDescription = _errorDescription;
                    }
                }

            return success;
            }

        private bool removeFiles()
            {
            foreach (var file in webAppUpdaterTasks.FilesToRemove)
                {
                if (!IOHelper.TryRemoveFile(file))
                    {
                    errorDescription = string.Format("Can't remove file: {0}", file);
                    return false;
                    }
                }

            return true;
            }

        private bool removeDirectories()
            {
            var success = true;
            foreach (var directoryPath in webAppUpdaterTasks.DirectoriesToRemove)
                {
                success = IOHelper.TryEmptyDirectory(directoryPath)
                    && IOHelper.RemoveEmptyDirectory(directoryPath)
                    && success;
                }

            return success;
            }

        private bool checkWebApplication(out string errorDescription)
            {
            errorDescription = string.Empty;

            var reply = new WebClientHelper(checkUrl).PerformPostRequest();
            bool isValid = reply.StartsWith("OK", StringComparison.InvariantCultureIgnoreCase);

            if (!isValid)
                {
                errorDescription = string.Format("Web app wasn't loaded: {0}", reply);
                }

            return isValid;
            }

        private bool moveFile(UpdateFileTaskDetails updateFileTaskDetails)
            {
            var sourceFileName = string.Format(@"{0}\{1}", updateTempFolder, updateFileTaskDetails.Id);
            var destinationFileName = updateFileTaskDetails.FullPath;
            try
                {
                try
                    {
                    File.Move(sourceFileName, destinationFileName);
                    }
                catch
                    {
                    File.Copy(sourceFileName, destinationFileName);
                    }
                }
            catch (Exception exp)
                {
                errorDescription = string.Format(@"Can't copy file ""{0}"": {1}", sourceFileName, exp.Message);
                return false;
                }

            return true;
            }

        private bool fillUpdateTask()
            {
            webAppUpdaterTasks = readFilesList();
            return webAppUpdaterTasks != null;
            }

        private WebAppUpdaterTasks readFilesList()
            {
            var taskFilePath = string.Format(@"{0}\tasks.xml", updateTempFolder);
            if (!File.Exists(taskFilePath)) return null;

            try
                {
                var taskXml = File.ReadAllText(taskFilePath);
                return XmlConvertor.ToObjectFromXmlString<WebAppUpdaterTasks>(taskXml);
                }
            catch
                {
                return null;
                }
            }

        public FilesUpdater(string publishDirectory, string updateTempFolder, string checkUrl)
            {
            this.publishDirectory = publishDirectory;
            this.updateTempFolder = updateTempFolder;
            this.checkUrl = checkUrl;
            }

        public bool PerformUpdate(out string _errorDescription)
            {
            if (string.IsNullOrEmpty(publishDirectory) || string.IsNullOrEmpty(updateTempFolder))
                {
                _errorDescription = "publishDirectory or updateTempFolder were not defined!";
                return false;
                }

            if (!fillUpdateTask())
                {
                _errorDescription = "Can't fill update task!";
                return false;
                }

            if (!updateWebApplication(out _errorDescription)) return false;

            var webApplicationIsLoaded = checkWebApplication(out _errorDescription);
            if (!webApplicationIsLoaded)
                {
                _errorDescription = "Web application isn't loaded!";
                }

            return true;
            }
        }
    }
