using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Timer = System.Timers.Timer;


namespace Saved_Game_Backup
{
    public class Backup {

        private static ObservableCollection<Game> _gamesToAutoBackup = new ObservableCollection<Game>();
        private static List<FileSystemWatcher> _fileWatcherList;
        private static string _specifiedAutoBackupFolder;
        private static string _hardDrive;
        private static bool _autoBackupAllowed;
        private static Timer _timer;
        
        public Backup() {
            
        }

        

        //Doesn't know if you cancel out of a dialog.
        //Needs threading when it is processing lots of files. Progress bar? Progress animation?

        

        public static bool BackupSaves(ObservableCollection<Game> gamesList, string harddrive, bool zipping, string specifiedfolder = null) {
            var destination = harddrive + "SaveBackups";

            if (!zipping) {
                var fd = new FolderBrowserDialog() { RootFolder = Environment.SpecialFolder.MyComputer, 
                        Description = "Select the root folder where this utility will create the SaveBackups folder.",
                        ShowNewFolderButton = true };

                if (fd.ShowDialog() == DialogResult.OK)
                    destination = fd.SelectedPath + "\\SaveBackups";
                else {
                    return false;
                }
            }

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
                BackupGame(g.Path, destination + "\\" + g.Name);
            }

            return true;
        }

        private static void BackupGame(string sourceDirName, string destDirName) {

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
                BackupGame(subdir.FullName, temppath);
            }
        }

        private static void DeleteDirectory(string deleteDirName) {

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

        public static bool BackupAndZip(ObservableCollection<Game> gamesList, string harddrive, bool zipping,string specifiedfolder = null) {

            var fd = new SaveFileDialog()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                FileName = "SaveBackups.zip",
                Filter = "Zip files (*.zip) | *.zip",
                Title = "Select the root folder where this utility will create the SaveBackups folder.",
                CheckFileExists = false,
                OverwritePrompt = true
            };

            string zipResult;
            if (fd.ShowDialog() == DialogResult.OK) {

                zipResult = fd.FileName;
            }
            else {
                return false;
            }

            //Run the main BackupSaves method.
            BackupSaves(gamesList, harddrive, zipping, specifiedfolder);
            
            var zipSource = harddrive + "SaveBackups";

            if(File.Exists(zipResult))
                File.Delete(zipResult);
            
            ZipFile.CreateFromDirectory(zipSource, zipResult);

            DeleteDirectory(zipSource);
            Directory.Delete(zipSource);

            return true;
        }

        public static void ActivateAutoBackup(ObservableCollection<Game> gamesToBackup, string harddrive, string specifiedFolder = null) {
            _timer = new Timer { Interval = 10000 };
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
            
            _fileWatcherList = new List<FileSystemWatcher>();
            _gamesToAutoBackup = gamesToBackup;

            _specifiedAutoBackupFolder = specifiedFolder;

            if (specifiedFolder == null) {
                specifiedFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Save Backups\\";
                _specifiedAutoBackupFolder = specifiedFolder;
            }

            _hardDrive = harddrive;
            

            int watcherNumber = 0;
            foreach (Game game in gamesToBackup) {
                _fileWatcherList.Add(new FileSystemWatcher(game.Path));
                _fileWatcherList[watcherNumber].Changed += OnChanged;
                _fileWatcherList[watcherNumber].EnableRaisingEvents = true;
                watcherNumber++;
            }

            
        }

        public static void DeactivateAutoBackup() {
            foreach (FileSystemWatcher f in _fileWatcherList) {
                f.EnableRaisingEvents = false;
            }
            _fileWatcherList.Clear();
        }

        private static void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            _autoBackupAllowed = true;
        }

        private static void OnChanged(object source, FileSystemEventArgs e) {
            
            Console.WriteLine(e.FullPath);
            Console.WriteLine(e.Name);
            Console.WriteLine(e.ChangeType);
            var file = new FileInfo(e.FullPath);
            Console.WriteLine(file.Name);

            if(_autoBackupAllowed)
                foreach (Game game in _gamesToAutoBackup) {
                    if (e.FullPath.Contains(game.Path))
                        BackupGame(game.Path, _specifiedAutoBackupFolder + "\\" + game.Name + "\\");
                }
            _autoBackupAllowed = false;

            //BackupFile(e);
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
                System.Windows.MessageBox.Show("Cannot find the MyDocuments folder on your computer. \r\n" + ex);
                return null;
            }

            return path;
        }

        private static void BackupFile(FileSystemEventArgs e) {
            
        }

        
    }
}
