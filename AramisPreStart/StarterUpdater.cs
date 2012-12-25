using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace AramisPreStart
    {
    internal class StarterUpdater
        {
        private const string UPDATE_FOLDER = "Update";
        private const string TEMP_FOLDER = "TEMP STORING ";

        private string tempStoringPath;
        private string starterPath;
        private string updatePath;

        private StarterUpdater( string starterPath )
            {
            this.starterPath = starterPath;
            this.updatePath = starterPath + @"\" + UPDATE_FOLDER;
            this.tempStoringPath = starterPath + @"\" + TEMP_FOLDER + Guid.NewGuid().ToString();
            }

        private void TryToUpdate()
            {
            RemoveTempFolders();

            List<FileInfo> updateFiles = FindStarterUpdateFiles();
            if ( updateFiles.Count == 0 )
                {
                return;
                }

            if ( RemoveOldStarterFiles( updateFiles ) )
                {
                MoveNewStarterFiles( updateFiles );
                }
            }

        /// <summary>
        /// Удаляет устаревшие временные папки
        /// </summary>
        private void RemoveTempFolders()
            {
            ( new DirectoryInfo( starterPath ) ).GetDirectories( TEMP_FOLDER + "*" ).ToList<DirectoryInfo>().ForEach( dirInfo =>
                {
                    try
                        {
                        Directory.Delete( dirInfo.FullName, true );
                        }
                    catch ( Exception exp )
                        {
                        Trace.WriteLine( exp.Message );
                        }
                } );
            }

        private bool RemoveOldStarterFiles( List<FileInfo> updateFiles )
            {
            List<string> filesToRemove = new List<string>();

            updateFiles.ForEach( fileInfo =>
                {
                    string destinationFileName = starterPath + @"\" + fileInfo.Name;
                    if ( File.Exists( destinationFileName ) )
                        {
                        filesToRemove.Add( fileInfo.Name );
                        }
                } );

            if ( filesToRemove.Count == 0 )
                {
                return true;
                }

            if ( Directory.Exists( tempStoringPath ) )
                {
                Directory.Delete( tempStoringPath, true );
                }

            Directory.CreateDirectory( tempStoringPath );

            bool filesMoved = TryToMoveCurrentStarterFiles( filesToRemove );

            try
                {
                // May be here old files, that use another prosess
                Directory.Delete( tempStoringPath, true );
                }
            catch { }

            return filesMoved;
            }

        private bool TryToMoveCurrentStarterFiles( List<string> filesToMove )
            {
            List<string> movedFiles = new List<string>();

            foreach ( string fileName in filesToMove )
                {
                if ( MoveFile( fileName, tempStoringPath, starterPath ) )
                    {
                    movedFiles.Add( fileName );
                    }
                else
                    {
                    break;
                    }
                }

            if ( movedFiles.Count == filesToMove.Count )
                {
                return true;
                }
            else
                {
                // Moving files back
                foreach ( string fileName in movedFiles )
                    {
                    MoveFile( fileName, starterPath, tempStoringPath );
                    }
                return false;
                }
            }

        private bool MoveFile( string fileName, string destinitionPath, string sourcePath )
            {
            try
                {
                File.Move( sourcePath + "\\" + fileName, destinitionPath + "\\" + fileName );
                }
            catch 
                {
                return false;
                }

            return true;
            }

        private void MoveNewStarterFiles( List<FileInfo> updateFiles )
            {
            updateFiles.ForEach( fileInfo =>
                {
                    string destinationFileName = starterPath + @"\" + fileInfo.Name;
                    File.Move( fileInfo.FullName, destinationFileName );
                } );
            }

        private List<FileInfo> FindStarterUpdateFiles()
            {
            if ( Directory.Exists( updatePath ) )
                {
                DirectoryInfo updateDirInfo = new DirectoryInfo( updatePath );
                return updateDirInfo.GetFiles().ToList<FileInfo>();
                }
            else
                {
                return new List<FileInfo>();
                }
            }

        internal static void TryToUpdateStarterFiles( string starterPath )
            {
            StarterUpdater updater = new StarterUpdater( starterPath );
            updater.TryToUpdate();
            }
        }
    }
