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
using System.Timers;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using GalaSoft.MvvmLight.Messaging;
using Saved_Game_Backup.Helper;
using Xceed.Wpf.DataGrid;
using MessageBox = System.Windows.MessageBox;
using Timer = System.Timers.Timer;


namespace Saved_Game_Backup {
    public class Backup {
        private static ObservableCollection<Game> _gamesToAutoBackup = new ObservableCollection<Game>();
        private static List<FileSystemWatcher> _fileWatcherList;
        private static readonly string _hardDrive = Path.GetPathRoot(Environment.SystemDirectory);
        private static string _myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static readonly string _userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private static string _userName = Environment.UserName;
        private static DirectoryInfo _autoBackupDirectoryInfo;
        private static Timer _delayTimer;
        private static Timer _canBackupTimer;
        private static Timer _pollAutobackupTimer;
        private static DateTime _lastAutoBackupTime;
        private static int _numberOfBackups = 0;
        private static CultureInfo _culture = CultureInfo.CurrentCulture;


        //Properties for new methods.
        public static Stopwatch Watch;
        public static List<FileInfo> SourceFiles;
        public static List<FileInfo> TargetFiles;
        public static List<Game> GamesToBackup;
        public static Dictionary<FileInfo, string> HashDictionary;
        public static Dictionary<Game, List<FileInfo>> GameFileDictionary;
        public static Dictionary<Game, List<FileInfo>> GameTargetDictionary;
        public static Dictionary<Game, List<FileInfo>> FilesToCopyDictionary;
        private static bool _firstPoll;

        public Backup() {
            Watch = new Stopwatch();
            HashDictionary = new Dictionary<FileInfo, string>();
            GameFileDictionary = new Dictionary<Game, List<FileInfo>>();
            GameTargetDictionary = new Dictionary<Game, List<FileInfo>>();
            FilesToCopyDictionary = new Dictionary<Game, List<FileInfo>>();
            _firstPoll = true;
        }

        public static async Task<BackupResultHelper> StartBackup(List<Game> games, BackupType backupType,
            bool backupEnabled, int interval = 0) {
            GamesToBackup = ModifyGamePaths(games);
            var success = false;
            var helper = new BackupResultHelper();
            var message = "";
            if (!games.Any() && backupType == BackupType.Autobackup && backupEnabled)
                return HandleBackupResult(true, false, "Autobackup Disabled", backupType,
                    DateTime.Now.ToString(_culture));
            if (!games.Any())
                return HandleBackupResult(success, false, "No games selected.", backupType,
                    DateTime.Now.ToString(_culture));

            var gamesToBackup = ModifyGamePaths(games);
            switch (backupType) {                   //CHANGE SWITCH TO RETURN BACKUPRESULTHELPER
                case BackupType.ToZip:
                    success = BackupAndZip(gamesToBackup);
                    break;
                case BackupType.ToFolder:
                    success = BackupSaves(gamesToBackup);
                    break;
                case BackupType.Autobackup:
                    return await ToggleAutoBackup(backupEnabled, interval);
                default:
                    success = false;
                    break;
            }


            if (backupType == BackupType.Autobackup && success && !backupEnabled) {
                message = @"Autobackup Enabled!";
                backupEnabled = true;
            }
            else if (backupType == BackupType.Autobackup && success && backupEnabled) {
                message = @"Autobackup Disabled!";
                backupEnabled = false;
            }
            else {
                message = @"Backup Complete!";
                //Messenger.Default.Send<DateTime>(DateTime.Now);
            }

            return HandleBackupResult(success, backupEnabled, message, backupType,
                DateTime.Now.ToString(CultureInfo.CurrentCulture));
        }

        public static bool BackupSaves(List<Game> gamesList, string specifiedfolder = null) {
            string destination;

            var fd = new FolderBrowserDialog() {
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Description = @"Select the folder where this utility will create the SaveBackups folder.",
                ShowNewFolderButton = true
            };

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

        public static bool BackupAndZip(List<Game> gamesList, string specifiedfolder = null) {
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
            if (zipDestination.Exists)
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
        public static async Task<BackupResultHelper> ToggleAutoBackup(bool backupEnabled, int interval) {
            if (backupEnabled) {
                return SetupPollAutobackup(backupEnabled, interval).Result;
            }

            return SetupPollAutobackup(backupEnabled, interval).Result; 
        }

        public static void ActivateAutoBackup(List<Game> gamesToBackup, string specifiedFolder = null)
        {
            _delayTimer = new Timer {Interval = 5000, AutoReset = true};
            _delayTimer.Elapsed += _delayTimer_Elapsed;

            _canBackupTimer = new Timer {Interval = 5000, AutoReset = true};
            _canBackupTimer.Elapsed += _canBackupTimer_Elapsed;

            _lastAutoBackupTime = DateTime.Now;

            _fileWatcherList = new List<FileSystemWatcher>();
            if (_autoBackupDirectoryInfo == null) {
                var fb = new FolderBrowserDialog() {SelectedPath = _hardDrive, ShowNewFolderButton = true};
                if (fb.ShowDialog() == DialogResult.OK)
                    _autoBackupDirectoryInfo = new DirectoryInfo(fb.SelectedPath);
            }

            var watcherNumber = 0;
            foreach (var game in gamesToBackup.Where(game => Directory.Exists(game.Path))) {
                var filePath = new FileInfo(game.Path + "\\");
                _fileWatcherList.Add(new FileSystemWatcher(filePath.ToString()));
                _fileWatcherList[watcherNumber].Changed += OnChanged;
                _fileWatcherList[watcherNumber].Created += OnChanged;
                _fileWatcherList[watcherNumber].Deleted += OnChanged;
                _fileWatcherList[watcherNumber].Renamed += OnRenamed;
                _fileWatcherList[watcherNumber].Error += OnError;
                _fileWatcherList[watcherNumber].NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite
                                                               | NotifyFilters.FileName;
                _fileWatcherList[watcherNumber].IncludeSubdirectories = true;
                _fileWatcherList[watcherNumber].Filter = "*";
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
                ? HandleBackupResult(true, true, "Game removed from autobackup", BackupType.Autobackup,
                    DateTime.Now.ToString(_culture))
                : HandleBackupResult(true, false, "Last game removed from Autobackup.\r\nAutobackup disabled.",
                    BackupType.Autobackup, DateTime.Now.ToString(_culture));
        }

        public static void DeactivateAutoBackup() {
            foreach (var f in _fileWatcherList) {
                f.EnableRaisingEvents = false;
            }
            _fileWatcherList.Clear();
        }

        public static bool CanBackup(List<Game> gamesToBackup)
        {
            if (_hardDrive == null) {
                MessageBox.Show(
                    "Cannot find OS drive. \r\nPlease add each game using \r\nthe 'Add Game to List' button.");
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

        private static void _canBackupTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            Debug.WriteLine("CanBackup timer elapsed");
            _lastAutoBackupTime = DateTime.Now;
            _canBackupTimer.Enabled = false;
        }

        private static void OnRenamed(object sender, RenamedEventArgs e) {
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
                        var originTruncBase = e.OldFullPath.Substring(originBaseIndex - 1);
                        var renameOriginPath = _autoBackupDirectoryInfo.FullName + originTruncBase;
                        //Path of old fileName in backup folder
                        Debug.WriteLine(@"START OnRenamed origin path is " + renameOriginPath);

                        var destBaseIndex = e.FullPath.IndexOf(autoBackupGame.RootFolder);
                        var destTruncBase = e.FullPath.Substring(destBaseIndex - 1);
                        var renameDestPath = _autoBackupDirectoryInfo.FullName + destTruncBase;
                        var renameDestDir = new DirectoryInfo(renameDestPath);
                        //Path of new fileName in backup folder
                        Debug.WriteLine(@"START OnRenamed destination path is " + renameDestPath);

                        #endregion

                        if (Directory.Exists(e.FullPath)) {}
                        else {
                            try {
                                //If autobackup target directory contains the old file name, use File.Copy()
                                //If autobackup target directory does not contain the old file name, copy the new file from gamesave directory.
                                if (!Directory.Exists(renameDestDir.Parent.FullName))
                                    Directory.CreateDirectory(renameDestDir.Parent.FullName);
                                if (!File.Exists(renameOriginPath)) {
                                    using (
                                        var inStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read,
                                            FileShare.ReadWrite)) {
                                        using (
                                            var outStream = new FileStream(renameDestPath, FileMode.Create,
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
                                        var inStream = new FileStream(renameOriginPath, FileMode.Open,
                                            FileAccess.Read, FileShare.ReadWrite)) {
                                        using (
                                            var outStream = new FileStream(renameDestPath, FileMode.Create,
                                                FileAccess.ReadWrite, FileShare.Read)) {
                                            inStream.CopyTo(outStream);
                                            Debug.WriteLine(
                                                @"SUCCESSFUL RENAME Old filename was {0}. New filename is {1}.",
                                                e.OldName, e.Name);
                                        }
                                    }
                                    File.Delete(renameOriginPath);
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
                                    OnRenamed(sender, e);
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

        private static void OnChanged(object sender, FileSystemEventArgs e) {
            try {
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
                        switch (e.ChangeType.ToString()) {
                            case "Changed":
                                SaveChanged(sender, e);
                                break;
                            case "Deleted":
                                SaveDeleted(sender, e);
                                break;
                            default:
                                SaveCreated(sender, e);
                                break;
                        }
                    }
                    break;
                }
            }
            catch (ArgumentException ex) {
                SBTErrorLogger.Log(ex.Message);
            }
        }

        private static void SaveChanged(object sender, FileSystemEventArgs e) {
            var startMsg = string.Format(@"Start SaveChanged for file {0}", e.FullPath);
            Debug.WriteLine(startMsg);
            try {
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

                var destBaseIndex = e.FullPath.IndexOf(autoBackupGame.RootFolder);
                var destTruncBase = e.FullPath.Substring(destBaseIndex - 1);
                var changeDestPath = _autoBackupDirectoryInfo.FullName + destTruncBase;
                var changeDestDir = new DirectoryInfo(changeDestPath);

                if (Directory.Exists(e.FullPath)) {}
                else {
                    try {
                        if (!Directory.Exists(changeDestDir.Parent.FullName))
                            Directory.CreateDirectory(changeDestDir.Parent.FullName);
                        if (File.Exists(e.FullPath)) {
                            using (
                                var inStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read,
                                    FileShare.ReadWrite)) {
                                Debug.WriteLine(@"START SaveChanged inStream created");
                                using (
                                    var outStream = new FileStream(changeDestPath, FileMode.Create, FileAccess.ReadWrite,
                                        FileShare.Read)) {
                                    Debug.WriteLine(@"START SaveChanged outStream created");
                                    inStream.CopyTo(outStream);
                                    Debug.WriteLine(@"SUCCESSFUL SAVECHANGED");
                                }
                            }
                        }
                    }
                    catch (FileNotFoundException ex) {
                        SBTErrorLogger.Log(ex.Message);
                        Debug.WriteLine(@"ABORT SaveChanged Exception encountered");
                    }
                    catch (IOException ex) {
                        var newMessage = string.Format(@"{0} {1} {2}", ex.Message, e.ChangeType, e.Name);
                        SBTErrorLogger.Log(newMessage);
                        Debug.WriteLine(@"ABORT SaveChanged IOException encountered");
                        if (ex.Message.Contains(@"it is being used")) {
                            Debug.WriteLine(@"Recursively calling SaveChanged()");
                            SaveChanged(sender, e);
                        }
                    }
                    catch (ArgumentException ex) {
                        SBTErrorLogger.Log(ex.Message);
                    }
                }
            }
            catch (ArgumentException ex) {
                SBTErrorLogger.Log(ex.Message);
            }
            var exitMsg = string.Format(@"EXIT SaveChanged for file {0}", e.FullPath);
            Debug.WriteLine(exitMsg);
        }

        private static void SaveDeleted(object sender, FileSystemEventArgs e) {
            try {
                Game autoBackupGame = null; //make argumentexception try catch

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
                var targetTruncBase = new FileInfo(e.FullPath.Substring(targetBaseIndex));
                var deleteTargetPath =
                    new FileInfo(Path.Combine(_autoBackupDirectoryInfo.FullName, targetTruncBase.FullName));

                if (Directory.Exists(e.FullPath)) {}
                else {
                    try {
                        if (!File.Exists(deleteTargetPath.FullName))
                            return; //If autobackup directory does not contain file to be deleted.
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
            catch (ArgumentException ex) {
                SBTErrorLogger.Log(ex.Message);
            }
        }

        private static void SaveCreated(object sender, FileSystemEventArgs e) {
            try {
                var startMsg = string.Format(@"START SaveCreated for {0}", e.FullPath);
                Debug.WriteLine(startMsg);

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

                var destBaseIndex = e.FullPath.IndexOf(autoBackupGame.RootFolder);
                var destTruncBase = e.FullPath.Substring(destBaseIndex - 1);
                var createdDestPath = _autoBackupDirectoryInfo.FullName + destTruncBase;
                var createdDestDir = new DirectoryInfo(createdDestPath);
                Debug.WriteLine(@"START SaveCreated destination path is {0}", createdDestPath);

                if (Directory.Exists(e.FullPath)) {}
                else {
                    if (!Directory.Exists(createdDestDir.Parent.FullName))
                        Directory.CreateDirectory(createdDestDir.Parent.FullName);
                    if (!File.Exists(e.FullPath)) {
                        var fileName = new FileInfo(e.FullPath);
                        Debug.WriteLine(@"ABORT SaveCreated source file not found. File was {0}", fileName);
                            //Concerning
                        return;
                    }
                    try {
                        using (
                            var inStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read,
                                FileShare.ReadWrite)) {
                            Debug.WriteLine(@"START SaveCreated inStream created");
                            using (var outStream = new FileStream(createdDestPath, FileMode.Create,
                                FileAccess.ReadWrite, FileShare.Read)) {
                                Debug.WriteLine(@"START SaveCreated outStream created");
                                inStream.CopyTo(outStream);
                                Debug.WriteLine(@"SUCCESSFUL CREATE for " + createdDestPath);
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
                            SaveCreated(sender, e);
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
            catch (ArgumentException ex) {
                SBTErrorLogger.Log(ex.Message);
            }
        }

        private static void OnError(object sender, ErrorEventArgs e) {
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
        internal static List<Game> ModifyGamePaths(IEnumerable<Game> gamesToBackup) {
            var editedList = new List<Game>();
            try {
                foreach (var game in gamesToBackup) {
                    if (!game.HasCustomPath && game.Path.Contains("Documents"))
                        editedList.Add(new Game(game.Name, _userPath + game.Path, game.ID, game.ThumbnailPath,
                            game.HasCustomPath, game.HasThumb, game.RootFolder));
                    else if (!game.HasCustomPath && game.Path.Contains("Program Files"))
                        editedList.Add(new Game(game.Name, _hardDrive + game.Path, game.ID, game.ThumbnailPath,
                            game.HasCustomPath, game.HasThumb, game.RootFolder));
                    else if (!game.HasCustomPath && game.Path.Contains("AppData"))
                        editedList.Add(new Game(game.Name, _userPath + game.Path, game.ID, game.ThumbnailPath,
                            game.HasCustomPath, game.HasThumb, game.RootFolder));
                    else if (!game.HasCustomPath && game.Path.Contains("Desktop"))
                        editedList.Add(new Game(game.Name, _userPath + game.Path, game.ID, game.ThumbnailPath,
                            game.HasCustomPath, game.HasThumb, game.RootFolder));
                    else
                        editedList.Add(game);
                }
            }
            catch (Exception ex) {
                SBTErrorLogger.Log(ex.Message);
            }
            return editedList;
        }

        public static BackupResultHelper Reset(List<Game> games, BackupType backupType,
            bool backupEnabled) {
            var message = "";
            if (backupEnabled) {
                DeactivateAutoBackup();
                message = "Autobackup Disabled";
            }
            games.Clear();
            return HandleBackupResult(true, false, message, backupType, DateTime.Now.ToString(_culture));
        }

        private static BackupResultHelper HandleBackupResult(bool success, bool backupEnabled, string messageToShow,
            BackupType backupType, string date) {
            var backupButtonText = "Backup Saves";
            if (!success) return new BackupResultHelper(success, backupEnabled, messageToShow, date, backupButtonText);

            var message = messageToShow;

            if (backupEnabled && backupType == BackupType.Autobackup) backupButtonText = "Disable Autobackup";
            if (!backupEnabled && backupType == BackupType.Autobackup) backupButtonText = "Enable Autobackup";


            return new BackupResultHelper(success, backupEnabled, message, date, backupButtonText);
        }

        


        // This method accepts two strings the represent two files to 
        // compare. A return value of 0 indicates that the contents of the files
        // are the same. A return value of any other value indicates that the 
        // files are not the same.
        private static bool FileCompare(string file1, string file2) {
            int file1Byte;
            int file2Byte;

            // Determine if the same file was referenced two times.
            if (file1 == file2) {
                // Return true to indicate that the files are the same.
                return true;
            }

            // Open the two files.
            var fs1 = new FileStream(file1, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var fs2 = new FileStream(file2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            // Check the file sizes. If they are not the same, the files 
            // are not the same.
            if (fs1.Length != fs2.Length) {
                // Close the file
                fs1.Close();
                fs2.Close();

                //Return false to indicate files are different
                return false;
            }

            // Read and compare a byte from each file until either a
            // non-matching set of bytes is found or until the end of
            // file1 is reached.
            do {
                // Read one byte from each file.
                file1Byte = fs1.ReadByte();
                file2Byte = fs2.ReadByte();
            } while ((file1Byte == file2Byte) && (file1Byte != -1));

            // Close the files.
            fs1.Close();
            fs2.Close();

            // Return the success of the comparison. "file1byte" is 
            // equal to "file2byte" at this point only if the files are 
            // the same.
            return ((file1Byte - file2Byte) == 0);
        }


        public static async Task<BackupResultHelper> SetupPollAutobackup(bool backupEnabled, int interval, List<Game> TESTGAMES = null) {
            //FOR TESTING ONLY
            if (TESTGAMES != null) GamesToBackup = TESTGAMES;


            if (backupEnabled) {
                _pollAutobackupTimer.Stop();
                _fileWatcherList.Clear();
                return new BackupResultHelper(true, !backupEnabled, "Autobackup disabled.",
                    DateTime.Now.ToLongTimeString(), "Enable autobackup");
            }
            if (_autoBackupDirectoryInfo == null) {
                var fb = new FolderBrowserDialog() {ShowNewFolderButton = true};
                if (fb.ShowDialog() == DialogResult.OK)
                    _autoBackupDirectoryInfo = new DirectoryInfo(fb.SelectedPath);
                else return new BackupResultHelper(false, backupEnabled, "", DateTime.Now.ToLongTimeString(), "Enable autobackup");
            }
            Debug.WriteLine(@"Setting up Poll Autobackup");

            GameTargetDictionary = new Dictionary<Game, List<FileInfo>>();
            GameFileDictionary = new Dictionary<Game, List<FileInfo>>();
            foreach (var game in GamesToBackup) {
                var targetDirectory = new DirectoryInfo(_autoBackupDirectoryInfo.FullName + "\\" + game.Name);
                if (!Directory.Exists(targetDirectory.FullName)) Directory.CreateDirectory(targetDirectory.FullName);
                var targets = targetDirectory.GetFiles("*", SearchOption.AllDirectories).ToList();
                GameTargetDictionary.Add(game, targets);

                var sourceDirectory = new DirectoryInfo(game.Path);
                if (!Directory.Exists(sourceDirectory.FullName))
                    return new BackupResultHelper(false, backupEnabled, "Game directory not found.", DateTime.Now.ToLongTimeString(),
                        "Enable Autobackup");
                var sources = sourceDirectory.GetFiles("*", SearchOption.AllDirectories).ToList();
                GameFileDictionary.Add(game, sources);

                var success = await ComputeSourceHashes();
                if (!success)
                    return new BackupResultHelper(false, backupEnabled,
                        "Error during autobackup hash creation.\r\nPlease email the developer if you encounter this.",
                        DateTime.Now.ToLongTimeString(), "Enable autobackup");
            }
            Debug.WriteLine(@"Setup of Poll Autobackup complete.");
            Debug.WriteLine(@"Initializing Poll Autobackup Timer.");
            _pollAutobackupTimer = new Timer { AutoReset = true, Enabled = true, Interval = interval }; //Only running once, remove autoreset when done testing
            _pollAutobackupTimer.Elapsed += _pollAutobackupTimer_Elapsed;
            _pollAutobackupTimer.Start();
            Debug.WriteLine(@"Finished initializing Poll Autobackup Timer.");
            return new BackupResultHelper(true, true, "Autobackup enabled", DateTime.Now.ToLongTimeString(), "Disable autobackup");
        }

        private static void _pollAutobackupTimer_Elapsed(object sender, ElapsedEventArgs e) {
            Debug.WriteLine(@"Poll Autobackup timer elapsed.");
            _pollAutobackupTimer.Enabled = false; //REMOVE AFTER TESTING
            PollAutobackup();
        }

        private async static Task<bool> ComputeSourceHashes() {
            HashDictionary = new Dictionary<FileInfo, string>();
            foreach (var game in GamesToBackup) {
                List<FileInfo> sourceFiles;
                GameFileDictionary.TryGetValue(game, out sourceFiles);
                if (sourceFiles == null) continue;
                foreach (var file in sourceFiles) {
                    var hash = MD5.Create().ComputeHash(File.ReadAllBytes(file.FullName));
                    //How can I get this to run asynchronously?
                    var hashString = BitConverter.ToString(hash).Replace("-", "");
                    HashDictionary.Add(file, hashString);
                }
            }
            return true;
        }

        //Should FileSystemWatcher add files to be copied and that's it?


        private static async void PollAutobackup() {
            Watch = new Stopwatch();
            Watch.Start();
            var startTime = Watch.Elapsed;
            Debug.WriteLine(@"Starting PollAutobackup at {0}", startTime);

            if (!_firstPoll)
                AppendSourceFiles();
            foreach (var game in GamesToBackup) {
                List<FileInfo> sourceFiles;
                List<FileInfo> targetFiles;
                GameFileDictionary.TryGetValue(game, out sourceFiles);
                GameTargetDictionary.TryGetValue(game, out targetFiles);
                var filesToCopy = await CompareFiles(sourceFiles, targetFiles);
                    //Look for source files NOT in target directory & copy them.
                await CopySaves(filesToCopy);
                filesToCopy.Clear();
                if (targetFiles != null && targetFiles.Any())
                    await Scanner(sourceFiles, targetFiles); //Only called when files exist in the target directory to compare.

                await CopyUnknownHashesFiles();
            }

            var endTime = Watch.Elapsed;
            Debug.WriteLine(@"PollAutobackup ended at {0}", endTime);
            Debug.WriteLine(@"PollAutobackup completed in {0}", (endTime - startTime));

            _firstPoll = false;
        }

        private static void AppendSourceFiles() {
            foreach (var game in GamesToBackup) {
                var directory = new DirectoryInfo(game.Path);
                var currentSourceFiles = new List<FileInfo>();
                GameFileDictionary.TryGetValue(game, out currentSourceFiles);
                if (currentSourceFiles == null) continue;
                foreach (var file in directory.GetFiles("*", SearchOption.AllDirectories).ToList()) {
                    if (currentSourceFiles.Exists(f => f.FullName == file.FullName && f.Length == file.Length)) continue;
                    currentSourceFiles.Add(file);
                }
            }
        }


        //Finds files that don't exist in target directory and calls CopySaves to copy them.
        //Does not catch files that exist in target directory with the same name as source.
        //That gets handled later.
        private static async Task<List<FileInfo>> CompareFiles(List<FileInfo> sourceFiles, List<FileInfo> targetFiles) {
            var sourceFilesToCopy = new List<FileInfo>();
            foreach (var source in sourceFiles) {
                if (targetFiles.Exists(a => a.ToString().Contains(source.Name))) continue;
                sourceFilesToCopy.Add(source);
            }
            return sourceFilesToCopy;
        }

        //Copies files in list for specified Game
        private static async Task CopySaves(List<FileInfo> filesToCopy) {
            //INSERT TRY/CATCH
            var startTime = Watch.Elapsed;
            Debug.WriteLine(@"CopySaves starting at {0}", startTime);
            try {
                foreach (var game in GamesToBackup) {
                    List<FileInfo> files;
                    GameFileDictionary.TryGetValue(game, out files);
                    if (files == null) continue;
                    foreach (var sourceFile in files) {
                        var index = sourceFile.FullName.IndexOf(game.RootFolder);
                        var substring = sourceFile.FullName.Substring(index);
                        var destPath = _autoBackupDirectoryInfo.FullName + substring;
                        var dir = new FileInfo(destPath);
                        if (!Directory.Exists(dir.DirectoryName))
                            Directory.CreateDirectory(dir.DirectoryName);
                        using (
                            var inStream = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read,
                                FileShare.ReadWrite)) {
                            using (var outStream = new FileStream(destPath, FileMode.Create)) {
                                await inStream.CopyToAsync(outStream);
                                Debug.WriteLine(@"SUCCESSFUL COPY: {0} copied to {1}", sourceFile.Name, destPath);
                                _numberOfBackups++;
                            }
                        }
                    }
                }
            }
            catch (ArgumentException ex) {
                Debug.WriteLine(@"ERROR during CopySaves");
                SBTErrorLogger.Log(ex.Message);
            }
            catch (IOException ex) {
                Debug.WriteLine(@"ERROR during CopySaves");
                SBTErrorLogger.Log(ex.Message);
            }
            catch (Exception ex) {
                Debug.WriteLine(@"ERROR during CopySaves");
                SBTErrorLogger.Log(ex.Message);
            }

            var endtime = Watch.Elapsed;
            Debug.WriteLine(@"CopySaves finished at {0}", endtime);
            Debug.WriteLine(@"CopySaves finished in {0}.", (endtime - startTime));
            Messenger.Default.Send(_numberOfBackups);
        }

        //Scans files in-depth to check for matching files
        private async static Task Scanner(List<FileInfo> sourceFiles, List<FileInfo> targetFiles) {
            var _startTime = Watch.Elapsed;
            Debug.WriteLine(@"Scanner started at {0}", _startTime);

            foreach (var game in GamesToBackup) {
                var filesToCopy = new List<FileInfo>();
                foreach (var source in sourceFiles) {
                    var source1 = source; //suggested by resharper
                    foreach (var target in targetFiles.Where(t => source1 != null && t.FullName == source1.FullName)) {
                        if (source.Length == target.Length) continue;
                        var hash = MD5.Create().ComputeHash(File.ReadAllBytes(target.FullName));
                        var hashString = BitConverter.ToString(hash).Replace("-", "");
                        if (!HashDictionary.ContainsValue(hashString)) //Compare using hashString
                            filesToCopy.Add(source);
                    }
                }
                if (filesToCopy.Any())
                    FilesToCopyDictionary.Add(game, filesToCopy);
            }
            var EndTime = Watch.Elapsed;
            Debug.WriteLine(@"Scanner complete after {0}", EndTime);
            Debug.WriteLine(@"Scanner completed in {0}", (EndTime - _startTime));
        }

        private static async Task CopyUnknownHashesFiles() {
            //have this copy everything in FilesToCopyDictionary

            foreach (var game in GamesToBackup) {
                List<FileInfo> filesToCopy;
                FilesToCopyDictionary.TryGetValue(game, out filesToCopy);
                if (filesToCopy == null) continue;
                foreach (var sourceFile in filesToCopy) {
                    var index = sourceFile.FullName.IndexOf(game.RootFolder);
                    var substring = sourceFile.FullName.Substring(index);
                    var destPath = _autoBackupDirectoryInfo.FullName + substring;
                    var dir = new FileInfo(destPath);
                    if (!Directory.Exists(dir.DirectoryName))
                        Directory.CreateDirectory(dir.DirectoryName);
                    using (
                        var inStream = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read,
                            FileShare.ReadWrite)) {
                        using (var outStream = new FileStream(destPath, FileMode.Create)) {
                            await inStream.CopyToAsync(outStream);
                            Debug.WriteLine(@"SUCCESSFUL COPY: {0} copied to {1}", sourceFile.Name, destPath);
                            _numberOfBackups++;
                        }
                    }
                }
            }
            Messenger.Default.Send(_numberOfBackups);
        }
    }
}