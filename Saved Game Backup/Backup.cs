using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
        private static int _numberOfBackups = 0;
        private static CultureInfo _culture = CultureInfo.CurrentCulture;
        
        public Backup() {
            
        }

        public static BackupResultHelper StartBackup(ObservableCollection<Game> games, BackupType backupType, bool backupEnabled) {
            var success = false;
            var message = "";
            if (!games.Any() && backupType == BackupType.Autobackup && backupEnabled)
                return HandleBackupResult(true, false, "Autobackup Disabled", backupType, DateTime.Now.ToString(_culture));
            if (!games.Any())
                return HandleBackupResult(success, false, "No games selected.", backupType, DateTime.Now.ToString(_culture));

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

           return HandleBackupResult(success, backupEnabled, message, backupType, DateTime.Now.ToString(CultureInfo.CurrentCulture));

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
            _gamesToAutoBackup = gamesToBackup; //Is this line needed?

            if (_specifiedAutoBackupFolder == null) {
                var fb = new FolderBrowserDialog() {SelectedPath = _hardDrive, ShowNewFolderButton = true};
                if (fb.ShowDialog() == DialogResult.OK)
                    _specifiedAutoBackupFolder = fb.SelectedPath;
            }

            var watcherNumber = 0;
            foreach (var game in gamesToBackup.Where(game => Directory.Exists(game.Path))) {
                _fileWatcherList.Add(new FileSystemWatcher(game.Path));
                _fileWatcherList[watcherNumber].Changed += OnChanged;
                _fileWatcherList[watcherNumber].Created += OnChanged;
                _fileWatcherList[watcherNumber].Deleted += OnChanged;
                _fileWatcherList[watcherNumber].Renamed += OnRenamed;
                _fileWatcherList[watcherNumber].NotifyFilter = NotifyFilters.CreationTime |  NotifyFilters.LastWrite
                                                               | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                _fileWatcherList[watcherNumber].IncludeSubdirectories = true;
                _fileWatcherList[watcherNumber].Filter = "";
                _fileWatcherList[watcherNumber].EnableRaisingEvents = true;
                watcherNumber++;
            }
        }

        

        public static BackupResultHelper RemoveFromAutobackup(Game game) {
            if (!_fileWatcherList.Any())
                HandleBackupResult(false, false, "No games on autobackup.", BackupType.Autobackup,
                    DateTime.Now.ToString(_culture));
            for (var i = 0; i <= _fileWatcherList.Count(); i++) {
                if (!_fileWatcherList[i].Path.Contains(game.Path)) continue;
                _fileWatcherList.RemoveAt(i);
                break;
            }
            
            
            return _fileWatcherList.Any()
                ? HandleBackupResult(true, true, "Game removed from autobackup", BackupType.Autobackup, DateTime.Now.ToString(_culture))
                : HandleBackupResult(true, false, "Last game removed from Autobackup.\r\nAutobackup disabled.", BackupType.Autobackup, DateTime.Now.ToString(_culture));

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

        private static void OnRenamed(object source, RenamedEventArgs e) {

            while (true) {
                if (!_delayTimer.Enabled && !_canBackupTimer.Enabled &&
                    (DateTime.Now - _lastAutoBackupTime).Seconds > 1) {
                    _delayTimer.Enabled = true;
                    _delayTimer.Start();
                    continue;
                }
                if (!_canBackupTimer.Enabled && _delayTimer.Enabled) {
                    continue;
                }
                if (_canBackupTimer.Enabled) {
                    Console.WriteLine(@"A SaveRename is about to occur");
                    Game autoBackupGame = null;

                    try {
                        foreach (
                            var a in
                                _gamesToAutoBackup.Where(
                                    a => e.FullPath.Contains(a.Name) || e.FullPath.Contains(a.RootFolder))) {
                            autoBackupGame = a;
                        }
                        Console.WriteLine(@"In OnRenamed(), autoBackupGame is {0}", autoBackupGame.Name);
                    }
                    catch (NullReferenceException ex) {
                        SBTErrorLogger.Log(ex);
                    }

                    if (autoBackupGame.RootFolder == null) {
                        var dir = new DirectoryInfo(autoBackupGame.Path);
                        autoBackupGame.RootFolder = dir.Name;
                    }

                    var originBaseIndex = e.OldFullPath.IndexOf(autoBackupGame.RootFolder);
                    var originTruncBase = e.OldFullPath.Substring(originBaseIndex);
                    var renameOriginPath = Path.Combine(_specifiedAutoBackupFolder, originTruncBase); //Path of old fileName in backup folder
                    
                    var destBaseIndex = e.FullPath.IndexOf(autoBackupGame.RootFolder);
                    var destTruncBase = e.FullPath.Substring(destBaseIndex);
                    var renameDestPath = new FileInfo(Path.Combine(_specifiedAutoBackupFolder, destTruncBase)); //Path of new fileName in backup folder
                    Console.WriteLine(@"In OnRenamed(), renameDestPath is {0}", renameDestPath);

                    if (Directory.Exists(e.FullPath) && autoBackupGame != null) {
                        //True if directory, else it's a file.
                        //Do stuff for backing up a directory here.
                    }
                    else {
                        var activeWatcher = new FileSystemWatcher();
                        try {
                            //If autobackup target directory contains the old file name, use File.Copy()
                            //If autobackup target directory does not contain the old file name, copy the new file from gamesave directory.
                            Console.WriteLine(@"In OnRenamed(), renameDestPath is {0}", renameDestPath);
                            if (!Directory.Exists(renameDestPath.DirectoryName))
                                Directory.CreateDirectory(renameDestPath.DirectoryName);
                            if (!File.Exists(renameOriginPath)) {
                                using (var inStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                                    using (var outStream = new FileStream(renameDestPath.FullName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read)) {
                                        
                                        foreach (var watcher in _fileWatcherList.Where(w => w.Path == autoBackupGame.Path)) {
                                            activeWatcher = watcher;
                                        }
                                        activeWatcher.EnableRaisingEvents = false;
                                        inStream.CopyTo(outStream);
                                        
                                        Debug.WriteLine(@"Rename occurred for {0} on {1}. Old filename: {2}. New filename: {3}.", autoBackupGame.Name, DateTime.Now, e.OldName, e.Name);
                                    }
                                }
                                activeWatcher.EnableRaisingEvents = true;
                            } else {
                                File.Copy(renameOriginPath, renameDestPath.FullName, true);
                                File.Delete(renameOriginPath);
                                Debug.WriteLine(@"Rename occurred for {0} on {1}. Old filename: {2}. New filename: {3}.", autoBackupGame.Name, DateTime.Now, e.OldName, e.Name);
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
                }
                break;
            }
            Messenger.Default.Send<DateTime>(DateTime.Now);
        }

        private static void OnChanged(object source, FileSystemEventArgs e) {

            while (true) {
                if (!_delayTimer.Enabled && !_canBackupTimer.Enabled && (DateTime.Now - _lastAutoBackupTime).Seconds > 1) {
                    _delayTimer.Enabled = true;
                    _delayTimer.Start();
                    continue;
                }
                if (!_canBackupTimer.Enabled && _delayTimer.Enabled) {
                    continue;
                }
                if (_canBackupTimer.Enabled) {
                    switch (e.ChangeType.ToString()) {
                        case "Changed":
                            SaveChanged(source, e);
                            break;
                        case "Deleted":
                            SaveDeleted(source, e);
                            break;
                        default:
                            SaveCreated(source, e);
                            break;
                    }
                    #region old code
                    //Game autoBackupGame = null;
                    //Console.WriteLine(@"autoBackupGame set");

                    //foreach (var a in _gamesToAutoBackup.Where(a => e.FullPath.Contains(a.Name) || e.FullPath.Contains(a.RootFolder))) {
                    //    autoBackupGame = a;
                    //}

                    //if (autoBackupGame.RootFolder == null) {
                    //    var dir = new DirectoryInfo(autoBackupGame.Path);
                    //    autoBackupGame.RootFolder = dir.Name;
                    //}

                    //var activeWatcher = new FileSystemWatcher();
                    //foreach (var watcher in _fileWatcherList.Where(w => w.Path == autoBackupGame.Path)) {
                    //    activeWatcher = watcher;
                    //}

                    //var indexOfGamePart = e.FullPath.IndexOf(autoBackupGame.RootFolder);
                    //var friendlyPath = e.FullPath.Substring(0, indexOfGamePart);
                    //var newPath = e.FullPath.Replace(friendlyPath, "\\");
                    //Console.WriteLine(@"newPath set");

                    //if (Directory.Exists(e.FullPath) && autoBackupGame != null) {
                    //            //True if directory, else it's a file.
                    //            //Do stuff for backing up a directory here.
                    //} else {
                    //    //Do stuff for backing up a file here.
                    //    try {
                    //        var copyDestinationPath = new FileInfo(_specifiedAutoBackupFolder + newPath);
                    //        Console.WriteLine(@"copyDestinationPath set");
                    //        if (!Directory.Exists(copyDestinationPath.DirectoryName))
                    //            Directory.CreateDirectory(copyDestinationPath.DirectoryName);
                    //            if (File.Exists(e.FullPath)){
                    //            using (var inStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    //                using (var outStream = new FileStream(copyDestinationPath.ToString(), FileMode.Create,
                    //                        FileAccess.ReadWrite, FileShare.Read)) {
                    //                    inStream.CopyTo(outStream);
                    //                    Console.WriteLine(@"Backup occurred");
                    //                    _numberOfBackups++;
                    //                    Messenger.Default.Send(_numberOfBackups);
                    //                }
                    //            }
                    //        }
                    //    }
                    //    catch (FileNotFoundException ex) {
                    //        SBTErrorLogger.Log(ex);
                    //            //Will occur if a file is temporarily written during the saving process, then deleted.
                    //    }
                    //    catch (IOException ex) {
                    //        SBTErrorLogger.Log(ex); //Occurs if a game has locked access to a file.
                    //    }
                    //}
                    //Messenger.Default.Send<DateTime>(DateTime.Now);
                    #endregion
                }
                break;
            }
        }

        private static void SaveChanged(object source, FileSystemEventArgs e) {
            Game autoBackupGame = null;

            try {
                foreach (
                    var a in
                        _gamesToAutoBackup.Where(a => e.FullPath.Contains(a.Name) || e.FullPath.Contains(a.RootFolder))) {
                    autoBackupGame = a;
                }
                Console.WriteLine(@"In SaveChanged(), autoBackupGame is {0}", autoBackupGame.Name);
            }
            catch (NullReferenceException ex) {
                SBTErrorLogger.Log(ex);
            }

            if (autoBackupGame.RootFolder == null) {
                var dir = new DirectoryInfo(autoBackupGame.Path);
                autoBackupGame.RootFolder = dir.Name;
            }

            var activeWatcher = new FileSystemWatcher();
            foreach (var watcher in _fileWatcherList.Where(w => w.Path == autoBackupGame.Path)) {
                activeWatcher = watcher;
            }

            var destBaseIndex = e.FullPath.IndexOf(autoBackupGame.RootFolder);
            var destTruncBase = e.FullPath.Substring(destBaseIndex);
            var renameDestPath = new FileInfo(Path.Combine(_specifiedAutoBackupFolder, destTruncBase)); //Path of new fileName in backup folder
            Console.WriteLine(@"In SaveChanged(), renameDestPath is {0}", renameDestPath);

            if (Directory.Exists(e.FullPath) && autoBackupGame != null) {
                //True if directory, else it's a file.
                //Do stuff for backing up a directory here.
            } else {
                try { 
                    if (!Directory.Exists(renameDestPath.DirectoryName))
                        Directory.CreateDirectory(renameDestPath.DirectoryName);
                    if (File.Exists(e.FullPath))
                    {
                        using (var inStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            using (var outStream = new FileStream(renameDestPath.ToString(), FileMode.Create,
                                    FileAccess.ReadWrite, FileShare.Read)) {
                                activeWatcher.EnableRaisingEvents = false;
                                inStream.CopyTo(outStream);
                                activeWatcher.EnableRaisingEvents = true;
                                Debug.WriteLine(@"SaveChanged occurred for Backup #{0}, file {1}. Game was {2} on {3}.", ++_numberOfBackups, e.Name, autoBackupGame.Name, DateTime.Now);
                                Messenger.Default.Send(_numberOfBackups);
                            }
                        }
                        activeWatcher.EnableRaisingEvents = true;
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

        private static void SaveDeleted(object source, FileSystemEventArgs e) {
            Console.WriteLine(@"A SaveDelete is about to occur");
            Game autoBackupGame = null;

            try {
                foreach (
                    var a in
                        _gamesToAutoBackup.Where(
                            a => e.FullPath.Contains(a.Name) || e.FullPath.Contains(a.RootFolder))) {
                    autoBackupGame = a;
                }
                Console.WriteLine(@"In SaveDeleted(), autoBackupGame is {0}", autoBackupGame.Name);
            }
            catch (NullReferenceException ex) {
                SBTErrorLogger.Log(ex);
            }

            if (autoBackupGame.RootFolder == null) {
                var dir = new DirectoryInfo(autoBackupGame.Path);
                autoBackupGame.RootFolder = dir.Name;
            }

            var targetBaseIndex = e.FullPath.IndexOf(autoBackupGame.RootFolder);
            var targetTruncBase = e.FullPath.Substring(targetBaseIndex);
            var deleteTargetPath = new FileInfo(Path.Combine(_specifiedAutoBackupFolder, targetTruncBase)); //Path of new fileName in backup folder
            Console.WriteLine(@"In SaveDeleted(), deleteTargetPath is {0}", deleteTargetPath);

            if (Directory.Exists(e.FullPath) && autoBackupGame != null) {
                //True if directory, else it's a file.
            } else {
                try {
                    if (!File.Exists(deleteTargetPath.FullName)) return; //If autobackup directory does not contain file to be deleted.
                    File.Delete(deleteTargetPath.FullName);
                    Debug.WriteLine(@"Delete occurred for {0} on {1}. Deleted file {2} in autobackup folder.", autoBackupGame.Name, DateTime.Now, e.Name);
                }
                catch (FileNotFoundException ex) {
                    SBTErrorLogger.Log(ex);
                }
                catch (IOException ex) {
                    SBTErrorLogger.Log(ex); //Occurs if a game has locked access to a file.
                }
            }
        }
        
        private static void SaveCreated(object source, FileSystemEventArgs e) {
            Game autoBackupGame = null;

            try {
                foreach (
                    var a in _gamesToAutoBackup.Where(a => e.FullPath.Contains(a.Name) || e.FullPath.Contains(a.RootFolder))) {
                    autoBackupGame = a;
                }
                Console.WriteLine(@"In SaveCreated(), autoBackupGame is {0}", autoBackupGame.Name);
            }
            catch (NullReferenceException ex) {
                SBTErrorLogger.Log(ex);
            }

            if (autoBackupGame.RootFolder == null) {
                var dir = new DirectoryInfo(autoBackupGame.Path);
                autoBackupGame.RootFolder = dir.Name;
            }

            var activeWatcher = new FileSystemWatcher();
            foreach (var watcher in _fileWatcherList.Where(w => w.Path == autoBackupGame.Path)) {
                activeWatcher = watcher;
            }

            var destBaseIndex = e.FullPath.IndexOf(autoBackupGame.RootFolder);
            var destTruncBase = e.FullPath.Substring(destBaseIndex);
            var renameDestPath = new FileInfo(Path.Combine(_specifiedAutoBackupFolder, destTruncBase)); //Path of new fileName in backup folder
            Console.WriteLine(@"In SaveCreated(), renameDestPath is {0}", renameDestPath);

            if (Directory.Exists(e.FullPath) && autoBackupGame != null) {
                //True if directory, else it's a file.
                //Do stuff for backing up a directory here.
            } else {
                try {
                    if (!Directory.Exists(renameDestPath.DirectoryName))
                        Directory.CreateDirectory(renameDestPath.DirectoryName);
                    if (File.Exists(e.FullPath)) {
                        using (var inStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                            using (var outStream = new FileStream(renameDestPath.ToString(), FileMode.Create,
                                    FileAccess.ReadWrite, FileShare.Read)) {
                                activeWatcher.EnableRaisingEvents = false;
                                inStream.CopyTo(outStream);
                                
                                Debug.WriteLine(@"SaveCreated occurred for Backup #{0}. Game was {1} on {2}.", ++_numberOfBackups, autoBackupGame.Name, DateTime.Now);
                                Messenger.Default.Send(_numberOfBackups);
                            }
                        }
                        activeWatcher.EnableRaisingEvents = true;
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

        public static BackupResultHelper Reset(ObservableCollection<Game> games, BackupType backupType, bool backupEnabled) {
            var message = "";
            if (backupEnabled) {
                DeactivateAutoBackup();
                message = "Autobackup Disabled";
            }
            games.Clear();
            return HandleBackupResult(true, false, message, backupType, DateTime.Now.ToString(_culture));
        }

        private static BackupResultHelper HandleBackupResult(bool success, bool backupEnabled, string messageToShow, BackupType backupType, string date) {
            var backupButtonText = "Backup Saves";
            if (!success) return new BackupResultHelper(success, backupEnabled, messageToShow, date, backupButtonText);

            var message = messageToShow;
            
            if (backupEnabled && backupType == BackupType.Autobackup) backupButtonText = "Disable Autobackup";
            if (!backupEnabled && backupType == BackupType.Autobackup) backupButtonText = "Enable Autobackup";


            return new BackupResultHelper(success, backupEnabled, message, date, backupButtonText);

        }
    }
}
