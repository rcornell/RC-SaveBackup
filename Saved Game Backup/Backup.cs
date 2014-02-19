using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using GalaSoft.MvvmLight.Messaging;
using Saved_Game_Backup.Helper;
using Xceed.Wpf.DataGrid;
using MessageBox = System.Windows.MessageBox;
using Timer = System.Timers.Timer;


namespace Saved_Game_Backup
{
    public class Backup {

        private static ObservableCollection<Game> _gamesToAutoBackup = new ObservableCollection<Game>();
        private static List<FileSystemWatcher> _fileWatcherList;
        private static readonly string _hardDrive = Path.GetPathRoot(Environment.SystemDirectory);
        private static string _myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static readonly string _userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private static string _userName = Environment.UserName;
        private static string _specifiedAutoBackupFolder;
        private static Timer _delayTimer;
        private static Timer _canBackupTimer;
        private static DateTime _lastAutoBackupTime;
        
        public Backup() {
            
        }

        public static BackupResultHelper StartBackup(ObservableCollection<Game> games, BackupType backupType, bool backupEnabled) {
            bool success;
            var message = "";
            //if (!games.Any() && backupType == BackupType.Autobackup && backupEnabled)
            //    return new BackupResultHelper(true, false, "Autobackup Disabled", DateTime.Now.ToString());
            if(!games.Any())
                return new BackupResultHelper(false, false, "No games selected.", DateTime.Now.ToString());

            var gamesToBackup = ModifyGamePaths(games);
            switch (backupType) {
                case BackupType.ToZip:
                    success = BackupAndZip(gamesToBackup);
                    break;
                case BackupType.ToFolder:
                    success = BackupSaves(gamesToBackup);
                    break;
                case BackupType.Autobackup:
                    success = ToggleAutoBackup(gamesToBackup, backupEnabled);
                    break;
                default:
                    success = false;
                    break;
            }

            //Can this be simplified?
            if (backupType == BackupType.Autobackup && success && !backupEnabled){
                message = "Autobackup Enabled!";
                backupEnabled = true;
            } else if (backupType == BackupType.Autobackup && success && backupEnabled) {
                message = "Autobackup Disabled!";
                backupEnabled = false;
            }
            else {
                message = "Backup Complete!";
                Messenger.Default.Send<DateTime>(DateTime.Now);
            }

            return new BackupResultHelper(success, backupEnabled, message, DateTime.Now.ToString());

        }

        public static bool BackupSaves(ObservableCollection<Game> gamesList, string specifiedfolder = null) {
            string destination;

            var fd = new FolderBrowserDialog() { SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer), 
                    Description = @"Select the folder where this utility will create the SaveBackups folder.",
                    ShowNewFolderButton = true };

            if (fd.ShowDialog() == DialogResult.OK)
                destination = fd.SelectedPath;
            else {
                return false;
            }

            if (!Directory.Exists(destination) && !string.IsNullOrWhiteSpace(destination))
                Directory.CreateDirectory(destination);

            //This backs up each game using BackupGame()
            foreach (var game in gamesList) {
                BackupGame(game, game.Path, destination);
            }

            return true;
        }

        private static void BackupGame(Game game, string sourceDirName, string destDirName) {
            var allFiles = Directory.GetFiles(sourceDirName, "*.*", SearchOption.AllDirectories);

            foreach (var sourcePath in allFiles) {
                try {
                    var positionOfRootFolder = sourcePath.IndexOf(game.RootFolder, StringComparison.CurrentCulture);
                    var pathEnd = sourcePath.Substring(positionOfRootFolder);
                    var destinationPath = new FileInfo(destDirName + "\\" + pathEnd);
                    var destinationDir = destinationPath.DirectoryName;
                    if (!Directory.Exists(destinationDir)) Directory.CreateDirectory(destinationDir);
                    var file = new FileInfo(sourcePath);
                    file.CopyTo(destinationPath.ToString(), true);
                }
                catch (IOException ex) {
                    SBTErrorLogger.Log(ex);
                }
                catch (NullReferenceException ex) {
                    SBTErrorLogger.Log(ex); 
                }
            }
        }

        public static bool BackupAndZip(ObservableCollection<Game> gamesList, string specifiedfolder = null) {
            string zipSource;
            FileInfo zipDestination;

            var fd = new SaveFileDialog() {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                FileName = "SaveBackups.zip",
                Filter = @"Zip files (*.zip) | *.zip",
                Title = @"Select the root folder where this utility will create the SaveBackups folder.",
                CheckFileExists = false,
                OverwritePrompt = true
            };

            if (fd.ShowDialog() == DialogResult.OK) {
                zipDestination = new FileInfo(fd.FileName);
                zipSource = zipDestination.DirectoryName + "\\Temp";
            }
            else {
                return false;
            }

            if (!Directory.Exists(zipSource))
                Directory.CreateDirectory(zipSource);

            //Creates temporary directory at ZipSource + the game's name
            //To act as the source folder for the ZipFile class.
            foreach (var game in gamesList) {
                BackupGame(game, game.Path, zipSource + "\\" + game.Name);
            }

            //Delete existing zip file if one exists.
            if(zipDestination.Exists)
                zipDestination.Delete();
            
            ZipFile.CreateFromDirectory(zipSource, zipDestination.FullName);

            //Delete temporary folder that held save files.
            Directory.Delete(zipSource, true);

            return true;
        }

        /// <summary>
        /// Returns false if turning off autobackup. Returns true if activating.
        /// </summary>
        /// <param name="gamesToBackup"></param>
        /// <param name="harddrive"></param>
        /// <param name="backupEnabled"></param>
        /// <param name="specifiedFolder"></param>
        /// <returns></returns>
        public static bool ToggleAutoBackup(ObservableCollection<Game> gamesToBackup, bool backupEnabled, string specifiedFolder = null) {
            if (backupEnabled){
                DeactivateAutoBackup();
                return true;
            }
            
            ActivateAutoBackup(gamesToBackup, specifiedFolder);
            return true;
        }

        
        public static void ActivateAutoBackup(ObservableCollection<Game> gamesToBackup, string specifiedFolder = null) {
            _delayTimer = new Timer { Interval = 5000, AutoReset = true};
            _delayTimer.Elapsed += _delayTimer_Elapsed;
            
            _canBackupTimer = new Timer { Interval = 5000, AutoReset = true};
            _canBackupTimer.Elapsed += _canBackupTimer_Elapsed;          

            _lastAutoBackupTime = DateTime.Now;

            _fileWatcherList = new List<FileSystemWatcher>();
            _gamesToAutoBackup = gamesToBackup;

            if (_specifiedAutoBackupFolder == null) {
                var fb = new FolderBrowserDialog() {SelectedPath = _hardDrive, ShowNewFolderButton = true};
                if (fb.ShowDialog() == DialogResult.OK)
                    _specifiedAutoBackupFolder = fb.SelectedPath;
            }

            var watcherNumber = 0;
            foreach (var game in gamesToBackup.Where(game => Directory.Exists(game.Path))) {
                _fileWatcherList.Add(new FileSystemWatcher(game.Path));
                _fileWatcherList[watcherNumber].Changed += OnChanged;
                _fileWatcherList[watcherNumber].Created += new FileSystemEventHandler(OnChanged);
                _fileWatcherList[watcherNumber].Deleted += new FileSystemEventHandler(OnChanged);
                _fileWatcherList[watcherNumber].Renamed += new RenamedEventHandler(OnChanged);
                _fileWatcherList[watcherNumber].NotifyFilter = NotifyFilters.CreationTime |  NotifyFilters.LastWrite
                                                               | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                _fileWatcherList[watcherNumber].IncludeSubdirectories = true;
                _fileWatcherList[watcherNumber].Filter = "";
                _fileWatcherList[watcherNumber].EnableRaisingEvents = true;
                watcherNumber++;
            }
        }

        

        public static BackupResultHelper RemoveFromAutobackup(Game game) {
            if (!_fileWatcherList.Any()) return new BackupResultHelper(false, false, "No games on autobackup.", DateTime.Now.ToString());
            for (var i = 0; i <= _fileWatcherList.Count(); i++) {
                if (!_fileWatcherList[i].Path.Contains(game.Path)) continue;
                _fileWatcherList.RemoveAt(i);
                break;
            }
            return _fileWatcherList.Any()
                ? new BackupResultHelper(true, true, "Game removed from autobackup", DateTime.Now.ToString())
                : new BackupResultHelper(true, false, "Last game removed from Autobackup.\r\nAutobackup disabled.", DateTime.Now.ToString());

        }

        public static void DeactivateAutoBackup() {
            foreach (var f in _fileWatcherList) {
                f.EnableRaisingEvents = false;
            }
            _fileWatcherList.Clear();
        }

        public static bool CanBackup(ObservableCollection<Game> gamesToBackup) {
            if (_hardDrive == null) {
                MessageBox.Show("Cannot find OS drive. \r\nPlease add each game using \r\nthe 'Add Game to List' button.");
                return false;
            }

            if (gamesToBackup.Any()) return true;
            MessageBox.Show("No games selected. \n\rPlease select at least one game.");
            return false;
        }

        private static void _delayTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            _canBackupTimer.Enabled = true;
            _canBackupTimer.Start();
            _delayTimer.Enabled = false;
        }

        static void _canBackupTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            _lastAutoBackupTime = DateTime.Now;
            _canBackupTimer.Enabled = false;
        }


        private static void OnChanged(object source, FileSystemEventArgs e) {

            while (true) {
                if (!_delayTimer.Enabled && !_canBackupTimer.Enabled && (DateTime.Now - _lastAutoBackupTime).Seconds > 10) {
                    _delayTimer.Enabled = true;
                    _delayTimer.Start();
                    continue;
                }
                if (!_canBackupTimer.Enabled && _delayTimer.Enabled) {
                    continue;
                }
                if (_canBackupTimer.Enabled){
                    Game autoBackupGame = null;
                    Console.WriteLine(@"autoBackupGame set");

                    foreach (var a in _gamesToAutoBackup.Where(a => e.FullPath.Contains(a.Name) || e.FullPath.Contains(a.RootFolder))) {
                        autoBackupGame = a;
                    }

                    if (autoBackupGame.RootFolder == null) {
                        var dir = new DirectoryInfo(autoBackupGame.Path);
                        autoBackupGame.RootFolder = dir.Name;
                    }

                    var indexOfGamePart = e.FullPath.IndexOf(autoBackupGame.RootFolder);
                    var friendlyPath = e.FullPath.Substring(0, indexOfGamePart);
                    var newPath = e.FullPath.Replace(friendlyPath, "\\");
                    Console.WriteLine(@"newPath set");

                    if (Directory.Exists(e.FullPath) && autoBackupGame != null) {
                                //True if directory, else it's a file.
                                //Do stuff for backing up a directory here.
                    } else {
                        //Do stuff for backing up a file here.
                        try {
                            var copyDestinationPath = new FileInfo(_specifiedAutoBackupFolder + newPath);
                            Console.WriteLine(@"copyDestinationPath set");
                            if (!Directory.Exists(copyDestinationPath.DirectoryName))
                                Directory.CreateDirectory(copyDestinationPath.DirectoryName);
                                if (File.Exists(e.FullPath)){
                                using (var inStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                                    using (var outStream = new FileStream(copyDestinationPath.ToString(), FileMode.Create,
                                            FileAccess.ReadWrite, FileShare.Read)) {
                                        inStream.CopyTo(outStream);
                                        Console.WriteLine(@"Backup occurred");
                                    }
                                }
                            }
                        }
                        catch (FileNotFoundException ex) {
                            SBTErrorLogger.Log(ex);
                                //Will occur if a file is temporarily written during the saving process, then deleted.
                        }
                        catch (IOException ex) {
                            SBTErrorLogger.Log(ex); //Occurs if a game has locked access to a file.
                        }
                    }
                    Messenger.Default.Send<DateTime>(DateTime.Now);
                }
                break;
            }
        }

        private static void BackupFile(FileSystemEventArgs e) {
            
        }

        /// <summary>
        /// Edits the truncated paths in the Games.json file and inserts the 
        /// user's path before the \\Documents\\ or \\AppData\\ folder path.
        /// If the game has its own user path, indicated by HasCustomPath
        /// being true, the game's path is not modified before being added to the
        /// new list.
        /// </summary>
        /// <param name="gamesToBackup"></param>
        internal static ObservableCollection<Game> ModifyGamePaths(IEnumerable<Game> gamesToBackup) {
            var editedList = new ObservableCollection<Game>();
            try {
                foreach (var game in gamesToBackup) {
                    if (!game.HasCustomPath && game.Path.Contains("Documents"))
                        editedList.Add(new Game(game.Name, _userPath + game.Path, game.ID, game.ThumbnailPath, game.HasCustomPath, game.HasThumb, game.RootFolder));
                    else if (!game.HasCustomPath && game.Path.Contains("Program Files"))
                        editedList.Add(new Game(game.Name, _hardDrive + game.Path, game.ID, game.ThumbnailPath, game.HasCustomPath, game.HasThumb, game.RootFolder));
                    else if (!game.HasCustomPath && game.Path.Contains("AppData"))
                        editedList.Add(new Game(game.Name, _userPath + game.Path, game.ID, game.ThumbnailPath, game.HasCustomPath, game.HasThumb, game.RootFolder));
                    else if (!game.HasCustomPath && game.Path.Contains("Desktop"))
                        editedList.Add(new Game(game.Name, _userPath + game.Path, game.ID, game.ThumbnailPath, game.HasCustomPath, game.HasThumb, game.RootFolder));
                    else
                        editedList.Add(game);
                }
            }
            catch (Exception ex) {
                SBTErrorLogger.Log(ex);
            }
            return editedList;
        }

        
    }
}
