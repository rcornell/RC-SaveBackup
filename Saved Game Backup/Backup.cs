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
                message = @"Autobackup Enabled!";
                backupEnabled = true;
            } else if (backupType == BackupType.Autobackup && success && backupEnabled) {
                message = @"Autobackup Disabled!";
                backupEnabled = false;
            }
            else {
                message = @"Backup Complete!";
                //Messenger.Default.Send<DateTime>(DateTime.Now);
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
                    SBTErrorLogger.Log(ex.Message);
                }
                catch (NullReferenceException ex) {
                    SBTErrorLogger.Log(ex.Message); 
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
                _fileWatcherList[watcherNumber].Error += OnError;
                _fileWatcherList[watcherNumber].NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite
                                                               | NotifyFilters.FileName ;
                _fileWatcherList[watcherNumber].IncludeSubdirectories = true;
                _fileWatcherList[watcherNumber].Filter = "*";
                _fileWatcherList[watcherNumber].EnableRaisingEvents = true;
                _fileWatcherList[watcherNumber].InternalBufferSize = 65536;
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
            Debug.WriteLine("DelayTimer elapsed");
            _canBackupTimer.Enabled = true;
            _canBackupTimer.Start();
            _delayTimer.Enabled = false;
        }

        static void _canBackupTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            Debug.WriteLine("CanBackup timer elapsed");
            _lastAutoBackupTime = DateTime.Now;
            _canBackupTimer.Enabled = false;
        }

        private static void OnRenamed(object source, RenamedEventArgs e) {
            try {
                var startMsg = string.Format(@"START OnRenamed for {0}", e.FullPath);
                Debug.WriteLine(startMsg);
                while (true) {
                    if (!_delayTimer.Enabled && !_canBackupTimer.Enabled) {
                        _delayTimer.Enabled = true;
                        _delayTimer.Start();
                        continue;
                    }
                    if (!_canBackupTimer.Enabled && _delayTimer.Enabled) {
                        continue;
                    }
                    if (_canBackupTimer.Enabled) {
                        #region RenameSetupStuff

                        Game autoBackupGame = null;

                        try {
                            foreach (
                                var a in
                                    _gamesToAutoBackup.Where(
                                        a => e.FullPath.Contains(a.Name) || e.FullPath.Contains(a.RootFolder))) {
                                autoBackupGame = a;
                            }
                        }
                        catch (NullReferenceException ex) {
                            SBTErrorLogger.Log(ex.Message);
                        }

                        if (autoBackupGame.RootFolder == null) {
                            var dir = new DirectoryInfo(autoBackupGame.Path);
                            autoBackupGame.RootFolder = dir.Name;
                        }

                        var originBaseIndex = e.OldFullPath.IndexOf(autoBackupGame.RootFolder);
                        var originTruncBase = e.OldFullPath.Substring(originBaseIndex);
                        var renameOriginPath = new FileInfo(Path.Combine(_specifiedAutoBackupFolder, originTruncBase));
                            //Path of old fileName in backup folder
                        Debug.WriteLine(@"START OnRenamed origin path is {0}", renameOriginPath);

                        var destBaseIndex = e.FullPath.IndexOf(autoBackupGame.RootFolder);
                        var destTruncBase = e.FullPath.Substring(destBaseIndex);
                        var renameDestPath = new FileInfo(Path.Combine(_specifiedAutoBackupFolder, destTruncBase));
                            //Path of new fileName in backup folder
                        Debug.WriteLine(@"START OnRenamed destination path is {0}", renameDestPath);

                        #endregion

                        if (Directory.Exists(e.FullPath)) {}
                        else {
                            try {
                                //If autobackup target directory contains the old file name, use File.Copy()
                                //If autobackup target directory does not contain the old file name, copy the new file from gamesave directory.
                                if (!Directory.Exists(renameDestPath.DirectoryName))
                                    Directory.CreateDirectory(renameDestPath.DirectoryName);
                                if (!File.Exists(renameOriginPath.FullName)) {
                                    using (
                                        var inStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read,
                                            FileShare.ReadWrite)) {
                                        using (
                                            var outStream = new FileStream(renameDestPath.FullName, FileMode.Create,
                                                FileAccess.ReadWrite, FileShare.Read)) {
                                            inStream.CopyTo(outStream);
                                            Debug.WriteLine(
                                                @"SUCCESSFUL RENAME Old filename was {0}. New filename is {1}.",
                                                e.OldName, e.Name);
                                        }
                                    }
                                }
                                else {
                                    Debug.WriteLine(
                                        @"START OnRenamed Cant find source file in game directory. Looking in autobackup folder.");
                                    using (
                                        var inStream = new FileStream(renameOriginPath.FullName, FileMode.Open,
                                            FileAccess.Read, FileShare.ReadWrite)) {
                                        using (
                                            var outStream = new FileStream(renameDestPath.FullName, FileMode.Create,
                                                FileAccess.ReadWrite, FileShare.Read)) {
                                            inStream.CopyTo(outStream);
                                            Debug.WriteLine(
                                                @"SUCCESSFUL RENAME Old filename was {0}. New filename is {1}.",
                                                e.OldName, e.Name);
                                        }
                                    }
                                    File.Delete(renameOriginPath.FullName);
                                    Debug.WriteLine(
                                        @"SUCCESSFUL RENAME Old filename was {0}. New filename is {1}.", e.OldName,
                                        e.Name);
                                }
                            }
                            catch (FileNotFoundException ex) {
                                SBTErrorLogger.Log(ex.Message);
                            }
                            catch (IOException ex) {
                                var newMessage = string.Format(@"{0} {1} {2} {3}", ex.Message, e.ChangeType, e.OldName,
                                    e.Name);
                                SBTErrorLogger.Log(newMessage);
                                if (ex.Message.Contains(@"it is being used")) {
                                    Debug.WriteLine(@"Recursively calling OnRenamed()");
                                    OnRenamed(source, e);
                                }
                            }
                            catch (ArgumentException ex) {
                                SBTErrorLogger.Log(ex.Message);
                            }
                        }
                    }
                    break;
                }
                var exitMsg = string.Format(@"EXIT OnRenamed for {0}", e.FullPath);
                Debug.WriteLine(exitMsg);
            }
            catch (ArgumentException ex) {
                SBTErrorLogger.Log(ex.Message);
            }
        }

        private static void OnChanged(object source, FileSystemEventArgs e) {
            try {
            while (true)
            {
                if (!_delayTimer.Enabled && !_canBackupTimer.Enabled)
                {
                    _delayTimer.Enabled = true;
                    _delayTimer.Start();
                    continue;
                }
                if (!_canBackupTimer.Enabled && _delayTimer.Enabled)
                {
                    continue;
                }
                if (_canBackupTimer.Enabled)
                {
                    switch (e.ChangeType.ToString())
                    {
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
                }
                break;
            }
            }
            catch (ArgumentException ex)
            {
                SBTErrorLogger.Log(ex.Message);
            }
        }

        private static void SaveChanged(object source, FileSystemEventArgs e) {
            var startMsg = string.Format(@"Start SaveChanged for file {0}", e.FullPath);
            Debug.WriteLine(startMsg);
            try {
            Game autoBackupGame = null;

            try {
                foreach (
                    var a in
                        _gamesToAutoBackup.Where(a => e.FullPath.Contains(a.Name) || e.FullPath.Contains(a.RootFolder))) {
                    autoBackupGame = a;
                }
            }
            catch (NullReferenceException ex) {
                SBTErrorLogger.Log(ex.Message);
            }

            if (autoBackupGame.RootFolder == null) {
                var dir = new DirectoryInfo(autoBackupGame.Path);
                autoBackupGame.RootFolder = dir.Name;
            }

            var destBaseIndex = e.FullPath.IndexOf(autoBackupGame.RootFolder);
            var destTruncBase = e.FullPath.Substring(destBaseIndex);
            var changeDestPath = new FileInfo(Path.Combine(_specifiedAutoBackupFolder, destTruncBase));

            if (Directory.Exists(e.FullPath) && autoBackupGame != null) {} 
            else {
                try {
                    if (!Directory.Exists(changeDestPath.DirectoryName))
                        Directory.CreateDirectory(changeDestPath.DirectoryName);
                    if (File.Exists(e.FullPath)) {
                        using (
                            var inStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read,
                                FileShare.ReadWrite)) {
                            Debug.WriteLine(@"START SaveChanged inStream created");
                            using (var outStream = new FileStream(changeDestPath.ToString(), FileMode.Create,
                                FileAccess.ReadWrite, FileShare.Read)) {
                                    Debug.WriteLine(@"START SaveChanged outStream created");
                                inStream.CopyTo(outStream);
                            }
                        }
                    }
                }
                catch (FileNotFoundException ex) {
                    SBTErrorLogger.Log(ex.Message);
                    Debug.WriteLine(@"ABORT SaveChanged Exception encountered");
                }
                catch (IOException ex) {
                    //activeWatcher.EnableRaisingEvents = true;
                    var newMessage = string.Format(@"{0} {1} {2}", ex.Message, e.ChangeType, e.Name);
                    SBTErrorLogger.Log(newMessage);
                    Debug.WriteLine(@"ABORT SaveChanged IOException encountered");
                    if (ex.Message.Contains(@"it is being used")) {
                        Debug.WriteLine(@"Recursively calling SaveChanged()");
                        SaveChanged(source, e);
                    }
                }
                catch (ArgumentException ex) {
                    SBTErrorLogger.Log(ex.Message);    
                }
            }
            }
            catch (ArgumentException ex)
            {
                SBTErrorLogger.Log(ex.Message);
            }
            var exitMsg = string.Format(@"EXIT SaveChanged for file {0}", e.FullPath);
            Debug.WriteLine(exitMsg);
        }

        private static void SaveDeleted(object source, FileSystemEventArgs e) {
            Game autoBackupGame = null;

            try {
                foreach (
                    var a in
                        _gamesToAutoBackup.Where(
                            a => e.FullPath.Contains(a.Name) || e.FullPath.Contains(a.RootFolder))) {
                    autoBackupGame = a;
                }
            }
            catch (NullReferenceException ex) {
                SBTErrorLogger.Log(ex.Message);
            }

            if (autoBackupGame.RootFolder == null) {
                var dir = new DirectoryInfo(autoBackupGame.Path);
                autoBackupGame.RootFolder = dir.Name;
            }

            var targetBaseIndex = e.FullPath.IndexOf(autoBackupGame.RootFolder);
            var targetTruncBase = e.FullPath.Substring(targetBaseIndex);
            var deleteTargetPath = new FileInfo(Path.Combine(_specifiedAutoBackupFolder, targetTruncBase)); 

            if (Directory.Exists(e.FullPath)) {} 
            else {
                try {
                    if (!File.Exists(deleteTargetPath.FullName)) return; //If autobackup directory does not contain file to be deleted.
                        File.Delete(deleteTargetPath.FullName);                     
                }
                catch (FileNotFoundException ex) {
                    SBTErrorLogger.Log(ex.Message);
                }
                catch (IOException ex) {
                    SBTErrorLogger.Log(ex.Message);
                }
            }
        }
    
        private static void SaveCreated(object source, FileSystemEventArgs e) {
            try {
            var startMsg = string.Format(@"START SaveCreated for {0}", e.FullPath);
            Debug.WriteLine(startMsg);

            Game autoBackupGame = null;
            try {
                foreach (
                    var a in _gamesToAutoBackup.Where(a => e.FullPath.Contains(a.Name) || e.FullPath.Contains(a.RootFolder))) {
                    autoBackupGame = a;
                }
            }
            catch (NullReferenceException ex) {
                SBTErrorLogger.Log(ex.Message);
            }

            if (autoBackupGame.RootFolder == null) {
                var dir = new DirectoryInfo(autoBackupGame.Path);
                autoBackupGame.RootFolder = dir.Name;
            }

            var destBaseIndex = e.FullPath.IndexOf(autoBackupGame.RootFolder);
            var destTruncBase = e.FullPath.Substring(destBaseIndex);
            var createdDestPath = new FileInfo(Path.Combine(_specifiedAutoBackupFolder, destTruncBase));
            Debug.WriteLine(@"START SaveCreated destination path is {0}", createdDestPath);

            if (Directory.Exists(e.FullPath)) {} 
            else {
               if (!Directory.Exists(createdDestPath.DirectoryName))
                        Directory.CreateDirectory(createdDestPath.DirectoryName);
                if (!File.Exists(e.FullPath)) {
                    var fileName = new FileInfo(e.FullPath);
                    Debug.WriteLine(@"ABORT SaveCreated source file not found. File was {0}", fileName); //Concerning
                    return;
                }
                try {
                    using (
                        var inStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read,
                            FileShare.ReadWrite)) {
                        using (var outStream = new FileStream(createdDestPath.FullName, FileMode.Create,
                            FileAccess.ReadWrite, FileShare.Read)) {
                            inStream.CopyTo(outStream);
                            Debug.WriteLine(@"SUCCESSFUL CREATE for {0}", createdDestPath);
                        }
                    }
                }
                catch (FileNotFoundException ex) {
                    var newMessage = string.Format(@"{0} {1} {2}", ex.Message, e.ChangeType, e.Name);
                    SBTErrorLogger.Log(newMessage);
                }
                catch (IOException ex) {
                    var newMessage = string.Format(@"{0} {1} {2}", ex.Message, e.ChangeType, e.Name);
                    SBTErrorLogger.Log(newMessage);
                    if (ex.Message.Contains(@"it is being used")) { 
                        Debug.WriteLine(@"Recursively calling SaveCreated()");
                        SaveCreated(source, e);
                    }
                }
                catch (ArgumentException ex) {
                    SBTErrorLogger.Log(ex.Message);
                }
                catch (Exception ex) {
                    SBTErrorLogger.Log(ex.Message);
                }
            }

            var exitMsg = string.Format(@"EXIT SaveCreated for {0}", e.FullPath);
            Debug.WriteLine(exitMsg);
            }
            catch (ArgumentException ex)
            {
                SBTErrorLogger.Log(ex.Message);
            }
        }

        private static void OnError(object source, ErrorEventArgs e) {
            var ex = e.GetException();
            var message = ex.Message;
            SBTErrorLogger.Log(message);
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
                SBTErrorLogger.Log(ex.Message);
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
