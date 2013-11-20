using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Ookii.Dialogs.Wpf;

namespace Saved_Game_Backup
{
    public class Backup {

        public Backup() {
            
        }

        //Need to allow overwriting of previous saves? Maybe archive old ones?

        public static void BackupSaves(ObservableCollection<Game> gamesList, string harddrive, string specifiedfolder = null) {

            string destination = harddrive + "SaveBackups";
            Directory.CreateDirectory(destination);

            //If user chooses a specific place where they store their saves, this 
            //changes each game's path to that folder followed by the game name.
            if (specifiedfolder != null) {
                for (int i = 0; i <= gamesList.Count; i++) {
                    gamesList[i].Path = specifiedfolder + "\\" + gamesList[i].Name;
                }
            }

            //This backs up each game using BackupGame()
            foreach (Game g in gamesList) {
                BackupGame(g.Path, destination + "\\" + g.Name, false);
            }

            

        }

        private static void BackupGame(string sourceDirName, string destDirName, bool copySubDirs) {

            // Get the subdirectories for the specified directory.
            var dir = new DirectoryInfo(sourceDirName);
            var dirs = dir.GetDirectories();

            if (!dir.Exists) {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it. 
            if (Directory.Exists(destDirName)) {
                DeleteDirectory(destDirName);
            } 
                
            Directory.CreateDirectory(destDirName);
  

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files) {
                var temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            } 
            
            foreach (DirectoryInfo subdir in dirs) {
                var temppath = Path.Combine(destDirName, subdir.Name);
                BackupGame(subdir.FullName, temppath, copySubDirs);
            }
            
        }

        private static void DeleteDirectory(string deleteDirName) {
            //var dir = new DirectoryInfo(destDirName);
            //var dirs = dir.GetDirectories();

            //foreach (DirectoryInfo subdir in dirs) {

            //    var filesToDelete = subdir.GetFiles();
            //    foreach (FileInfo f in filesToDelete) {
            //        File.Delete(f.Name);
            //    }
            //}

            //foreach (DirectoryInfo subdir in dirs)
            //{
            //    var temppath = Path.Combine(destDirName, subdir.Name);
            //    DeleteDirectory(temppath);
            //}

            var dir = new DirectoryInfo(deleteDirName);
            var dirs = dir.GetDirectories();

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                File.Delete(file.FullName);
            }

            foreach (DirectoryInfo subdir in dirs)
            {
                var temppath = Path.Combine(deleteDirName, subdir.Name);
                DeleteDirectory(temppath);
                Directory.Delete(subdir.FullName);
            }








        }

        public static void BackupAndZip(ObservableCollection<Game> gamesList, string harddrive, string specifiedfolder = null) {
            BackupSaves(gamesList, harddrive, specifiedfolder);
            
            var zipSource = harddrive + "SaveBackups";

            #region 

            //Old code from when the save location was predetermined.
            //
            //var myDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            //if (File.Exists(zipResult)) {

            //    int i = 1;
            //    while (File.Exists(zipResult)) {
            //       zipResult = myDocs + "\\SaveBackups (" + i + ").zip";
            //        i++;
            //    }

            #endregion

            var fd = new VistaSaveFileDialog {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                FileName = "SaveBackups.zip",
                Filter = "Zip files (*.zip) | *.zip"
            };
            fd.ShowDialog();
            var zipResult = fd.FileName;

            if(fd.OverwritePrompt)
                File.Delete(zipResult);
            
            ZipFile.CreateFromDirectory(zipSource, zipResult);
        }
        

        private static string CreateFolderPath() {
            string path;
            
            try
            {
                //username = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show("Cannot find the MyDocuments folder on your computer. \r\n" + ex);
                return null;
            }

            return path;
        }
    }
}
