using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        private DetailedUpdateTask task;
        private string checkUrl;

        public FilesUpdater(string publishDirectory, string updateTempFolder, string checkUrl)
            {
            this.publishDirectory = publishDirectory;
            this.updateTempFolder = updateTempFolder;
            this.checkUrl = checkUrl;
            task = new DetailedUpdateTask();
            }

        private Dictionary<FilesGroupTypes, string> getMainDirectoriesNames()
            {
            return new Dictionary<FilesGroupTypes, string>()
                {
                {FilesGroupTypes.WebContent,string.Format(@"{0}\Content", publishDirectory)},
                {FilesGroupTypes.WebScripts,string.Format(@"{0}\Scripts", publishDirectory)},
                {FilesGroupTypes.WebViews,string.Format(@"{0}\Views", publishDirectory)},
                {FilesGroupTypes.WebRoot, publishDirectory},
                {FilesGroupTypes.WebBin, string.Format(@"{0}\bin", publishDirectory)}
                };
            }

        public bool PerformUpdate(out string errorDescription)
            {
            errorDescription = string.Empty;

            if (string.IsNullOrEmpty(publishDirectory) || string.IsNullOrEmpty(updateTempFolder))
                {
                errorDescription = "publishDirectory or updateTempFolder were not defined!";
                return false;
                }

            if (!fillUpdateTask())
                {
                errorDescription = "Can't fill update task!";
                return false;
                }

            foreach (var file in task.FilesToRemove)
                {
                if (!IOHelper.TryRemoveFile(file))
                    {
                    errorDescription = string.Format("Can't remove file: {0}", file);
                    return false;
                    }
                }

            foreach (var directory in task.DirectoriesToEmpty)
                {
                if (!IOHelper.TryEmptyDirectory(directory))
                    {
                    errorDescription = string.Format("Can't empty the directory: {0}", directory);
                    return false;
                    }
                }

            foreach (var directory in task.DirectoriesToCreate)
                {
                if (!IOHelper.TryCreateDirectory(directory))
                    {
                    errorDescription = string.Format("Can't create the directory: {0}", directory);
                    return false;
                    }
                }

            foreach (var kvp in task.FilesToMove)
                {
                var fileName = kvp.Key;
                var fileInfo = kvp.Value;
                if (!moveFile(fileInfo, fileName))
                    {
                    errorDescription = string.Format("Can't move (or copy) file: {0}", fileName);
                    return false;
                    }
                }

            checkWebApplication(out errorDescription);

            return true;
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

        private bool moveFile(UploadingFile fileInfo, string destinationFileName)
            {
            var sourceFileName = string.Format(@"{0}\{1}", updateTempFolder, fileInfo.Id);

            try
                {
                if (fileInfo.IsDesktop)
                    {
                    File.Copy(sourceFileName, destinationFileName);
                    }
                else
                    {
                    File.Move(sourceFileName, destinationFileName);
                    }
                }
            catch (Exception exp)
                {
                return false;
                }

            return true;
            }

        private bool fillUpdateTask()
            {
            UpdatingFilesList filesList = readFilesList();
            if (filesList == null || filesList.Files.Count == 0) return false;

            var mainDirectories = getMainDirectoriesNames();

            task.DirectoriesToEmpty.AddAnyway(mainDirectories[FilesGroupTypes.WebScripts]);
            task.DirectoriesToEmpty.AddAnyway(mainDirectories[FilesGroupTypes.WebContent]);
            task.DirectoriesToEmpty.AddAnyway(mainDirectories[FilesGroupTypes.WebViews]);

            foreach (var fileInfo in filesList.Files)
                {
                if (!fileInfo.IsWebSystem) continue;

                var fullPathToNewFile = mainDirectories[fileInfo.Group] + "\\" + fileInfo.FilePath;
                task.FilesToMove.AddAnyway(fullPathToNewFile, fileInfo);

                var fileDirectory = Path.GetDirectoryName(fullPathToNewFile);
                task.DirectoriesToCreate.AddAnyway(fileDirectory);

                if (fileInfo.Group == FilesGroupTypes.WebBin || fileInfo.Group == FilesGroupTypes.WebRoot)
                    {
                    task.FilesToRemove.AddAnyway(fullPathToNewFile);
                    }
                }

            return true;
            }

        private UpdatingFilesList readFilesList()
            {
            var taskFilePath = string.Format(@"{0}\tasks.xml", updateTempFolder);
            if (!File.Exists(taskFilePath)) return null;

            var taskXml = string.Empty;

            try
                {
                taskXml = File.ReadAllText(taskFilePath);
                }
            catch
                {
                return null;
                }

            return XmlConvertor.ToListFromXmlString(taskXml);
            }
        }
    }
