using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Saved_Game_Backup
{
    public class Backup {

        public Backup() {
            
        }

        public static void BackupSaves(ObservableCollection<Game> gamesList, string harddrive, string specifiedfolder = null) {
            string folderPath;
            if (specifiedfolder == null) {
                folderPath = CreateFolderPath(harddrive);
            }
            else {
                folderPath = specifiedfolder;
            }

            var games = new List<Game>(gamesList);

            string destination = harddrive + "SaveBackups";
            Directory.CreateDirectory(destination);

            #region 

            ////Reads the hardDrivePath and puts the characters in to the hardDrivePathStream array
            //var hardDrivePathStream = new char[harddrive.Length];
            //using (var sr = new StringReader(harddrive)) {
            //    sr.Read(hardDrivePathStream,0,harddrive.Length);
            //}


            ////Reads the folderPath and puts the characters in to the folderPathStream array
            //var folderPathStream = new char[folderPath.Length];
            //using (var sr = new StringReader(folderPath)) {
            //    sr.Read(folderPathStream, 0, harddrive.Length);
            //}

            //folderPathStream[0] = hardDrivePathStream[0];

            //using (var sw = new StringWriter(new StringBuilder(folderPath))) {
            //    sw.Write(folderPathStream);
            //}

            //string[] filesToCopy;

            //foreach (Game g in gamesList) {
            //    filesToCopy.
            //}

            #endregion

            //The below code now copies files from one directory given in g.Path.
            //You need to make it recursive to also look through subdirectories
            //and create them and copy their files. test
            
            foreach (Game g in gamesList) {
                BackupGame(g.Path, destination + "\\" + g.Name, false);

            }

        }

        private static void BackupGame(string sourceDirName, string destDirName, bool copySubDirs) {
            #region 

            //if (!copysub) sourceDirectory = g.Path;
            //DirectoryInfo dir = new DirectoryInfo(sourceDirectory);
            //DirectoryInfo[] dirs = dir.GetDirectories();

            //FileInfo[] fileInfo = dir.GetFiles();

            //string newDir = destinationDirectory + "\\" + g.Name;

            //if (!Directory.Exists(newDir))
            //{
            //    Directory.CreateDirectory(newDir);
            //}

            //foreach (FileInfo f in fileInfo)
            //{
            //    string newFile = Path.Combine(newDir, f.Name);
            //    f.CopyTo(newFile, false);
            //}

            //foreach (DirectoryInfo subdir in dirs)
            //{
            //    string temppath = Path.Combine(newDir, subdir.Name);
            //    BackupGame(g, temppath);
            //}

            #endregion 
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists) {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(destDirName)) {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files) {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            } 
            
            foreach (DirectoryInfo subdir in dirs) {
                string temppath = Path.Combine(destDirName, subdir.Name);
                BackupGame(subdir.FullName, temppath, copySubDirs);
            }
            
        }

        private static void BackupAndZip(ObservableCollection<Game> gamesList, string harddrive, string specifiedfolder = null) {
                BackupSaves(gamesList, harddrive, specifiedfolder);

                string zipSource = harddrive + "SaveBackups";
                string zipResult = harddrive + "SaveBackups.zip";
                ZipFile.CreateFromDirectory(zipSource, zipResult);

        }
        

        private static string CreateFolderPath(string harddrive) {
            string username;
            string path;
            
            try
            {
                //username = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show("I cannot find your Windows username, e.g. \"RobPC\". " + ex);
                return null;
            }

            return path;
        }
    }
}
