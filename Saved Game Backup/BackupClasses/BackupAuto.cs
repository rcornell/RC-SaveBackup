using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Windows.Navigation;
using GalaSoft.MvvmLight.Messaging;
using Saved_Game_Backup.BackupClasses;
using Saved_Game_Backup.Helper;
using Saved_Game_Backup.OnlineStorage;
using Timer = System.Timers.Timer;

namespace Saved_Game_Backup
{
    public class BackupAuto {
        private static readonly string MyDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static DirectoryInfo _autoBackupDirectoryInfo;
        private static Timer _delayTimer;
        private static Timer _canBackupTimer;
        private static Timer _pollAutobackupTimer;
        private static DateTime _lastAutoBackupTime; //Resharper says not used bc it is sent via Messenger to MainViewModel
        private static int _numberOfBackups = 0;
        private static bool _watcherCopiedFile;
        private static List<FileSystemWatcher> _fileWatcherList;
        public static Stopwatch Watch;
        public static List<FileInfo> SourceFiles;
        public static List<FileInfo> TargetFiles;
        public static List<Game> GamesToBackup;
        public static Dictionary<FileInfo, string> HashDictionary;
        public static Dictionary<Game, List<FileInfo>> GameFileDictionary;
        public static Dictionary<Game, List<FileInfo>> GameTargetDictionary;
        private static bool _firstPoll;
        private static BackupSyncOptions _backupSyncOptions;
        private static bool _backupEnabled;
        private static readonly BackupResultHelper ErrorResultHelper = new BackupResultHelper() { Success = false, AutobackupEnabled = false, Message = "Error during operation" };
        private static readonly BackupResultHelper FilesNotFoundHelper = new BackupResultHelper() { Success = false };
        private static ProgressHelper _progress;
        private static int _interval;
        private static int _elapsed;

        public static BackupResultHelper ToggleAutoBackup(List<Game> gamesToBackup, bool backupEnabled, BackupSyncOptions backupSyncOptions, int interval, DirectoryInfo autobackupDi) {
            if (backupEnabled) return ShutdownAutobackup();
            GamesToBackup = gamesToBackup;
            _backupEnabled = false;
            _backupSyncOptions = backupSyncOptions;
            _autoBackupDirectoryInfo = autobackupDi;
            _progress = new ProgressHelper(){ FilesComplete = 0, TotalFiles = 0};
            return InitializeAutobackup(interval);
        }

        private static BackupResultHelper InitializeAutobackup(int interval) {
            if (!GamesToBackup.Any()) return ErrorResultHelper;
            InitializeWatchers();
            var result = InitializePollAutobackup(interval);
            _backupEnabled = result.Success;
            return result;
        }

        private static BackupResultHelper ShutdownAutobackup() {
            ShutdownWatchers();
            ShutdownPollAutobackup();
            _backupEnabled = false;
            return new BackupResultHelper(){Success = true, AutobackupEnabled = false, ShuttingDownAutobackup = true, Message = @"Auto-backup disabled"};
        }

        public static BackupResultHelper RemoveFromAutobackup(Game game) {
            if (!_fileWatcherList.Any() && !GamesToBackup.Any())
                return new BackupResultHelper(){ Success = true, AutobackupEnabled = false, BackupDateTime = DateTime.Now.ToLongTimeString(),Message = "No games to remove."};

            for (var i = 0; i < _fileWatcherList.Count; i++) {
                if (_fileWatcherList[i].Path.Contains(game.Path)) { //Remove watcher from watcher list
                    _fileWatcherList.RemoveAt(i);
                }
            }
            for (var i = 0; i < GamesToBackup.Count; i++) {
                if (GamesToBackup[i].Name == game.Name) //Remove game from GamesToBackup
                    GamesToBackup.RemoveAt(i);
            }

            //If there is a difference between lists, shut down all operations.
            if ((!_fileWatcherList.Any() && GamesToBackup.Any()) || (_fileWatcherList.Any() && !GamesToBackup.Any())) {
                return ShutdownAutobackup(); //If you hit this, there is a problem.
            }


            var message = "";
            var time = DateTime.Now.ToLongTimeString();
            if (_fileWatcherList.Any()) //Games remaining
                message = string.Format("{0} removed from auto-backup", game.Name);
            else {
                return ShutdownAutobackup();
            }

            return new BackupResultHelper() { //Only reachable if games remain in GamesToBackup
                Success = true,
                AutobackupEnabled = true,
                BackupDateTime = time,
                Message = message
            };

        }        

        public static BackupResultHelper AddToAutobackup(Game game) {
            if (!GetSourceFiles(game, false)) {
                FilesNotFoundHelper.Message = string.Format("No files found for " + game.Name + ". Game not added.");
                FilesNotFoundHelper.RemoveFromAutobackup = true;
                FilesNotFoundHelper.AutobackupEnabled = _backupEnabled;
                return FilesNotFoundHelper;
            }
            GetTargetFiles(game, false);
            InitializeWatchers(game);
            GamesToBackup.Add(game);                      
            return new BackupResultHelper() { AutobackupEnabled = true, Message = string.Format(game.Name + @" added"), Success = true };
        }

        public static void InitializeWatchers(Game gameToAdd = null) {
            if (!_backupEnabled) {
                _delayTimer = new Timer {Interval = 5000, AutoReset = true};
                _delayTimer.Elapsed += _delayTimer_Elapsed;

                _canBackupTimer = new Timer {Interval = 5000, AutoReset = true};
                _canBackupTimer.Elapsed += _canBackupTimer_Elapsed;

                _lastAutoBackupTime = DateTime.Now;

                _fileWatcherList = new List<FileSystemWatcher>();
            }

            var watcherNumber = 0;

            if (gameToAdd != null) {
                CreateWatcher(gameToAdd, watcherNumber);
                watcherNumber++;
            }
            else {
                foreach (var game in GamesToBackup.Where(game => Directory.Exists(game.Path))) {
                    Debug.WriteLine(@"Initializing Watchers");
                    CreateWatcher(game, watcherNumber);
                    watcherNumber++;
                }
            }
            Debug.WriteLine(@"Finished adding {0} Watchers to list", watcherNumber);
        }

        public static void CreateWatcher(Game game, int watcherNumber = 0) {
            var filePath = new FileInfo(game.Path + "\\");
            _fileWatcherList.Add(new FileSystemWatcher(filePath.FullName));
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
            Debug.WriteLine(@"Watcher added to list for " + game.Name);
        }

        /// <summary>
        /// Returns false if no game files are found
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        private static bool GetSourceFiles(Game game, bool updatingDictionary) {           
            Debug.WriteLine(@"Getting files in source directory for " + game.Name);
            var sourceDirectory = new DirectoryInfo(game.Path);
            if (!Directory.Exists(sourceDirectory.FullName))
                return false;
            var sources = sourceDirectory.GetFiles("*", SearchOption.AllDirectories).ToList();

            if (!updatingDictionary) { //First adding to dictionary
                GameFileDictionary.Add(game, sources);
                return true;    
            }
            
            if (GameFileDictionary.ContainsKey(game)) //Updating dictionary. Remove entry for game, then re-add with updated source.
                GameFileDictionary.Remove(game);
            GameFileDictionary.Add(game, sources);
            return true;
        }

        private static void GetTargetFiles(Game game, bool updatingDictionary) {
            Debug.WriteLine(@"Getting files in target directory for " + game.Name);
            var targetDirectory = new DirectoryInfo(_autoBackupDirectoryInfo.FullName + "\\" + game.Name);
            if (!Directory.Exists(targetDirectory.FullName)) Directory.CreateDirectory(targetDirectory.FullName);
            var targets = targetDirectory.GetFiles("*", SearchOption.AllDirectories).ToList();

            if (!updatingDictionary) { //First adding to dictionary
                GameTargetDictionary.Add(game, targets);
                return;
            }
            
            if (GameTargetDictionary.ContainsKey(game)) //Updating dictionary. Remove entry for game, then re-add with updated source.
                GameTargetDictionary.Remove(game);
            GameTargetDictionary.Add(game, targets);
        }

        public static void ShutdownWatchers() {
            Debug.WriteLine(@"Shutting down Watchers and timers");
            _fileWatcherList.Clear();
            _delayTimer.Stop();
            _delayTimer.Dispose();
            _canBackupTimer.Stop();
            _canBackupTimer.Dispose();
            Debug.WriteLine(@"All Watchers and timers shut down and cleared");
        }

        private static void OnRenamed(object sender, RenamedEventArgs e) {
            try {
                var startMsg = String.Format(@"START OnRenamed for {0}", e.FullPath);
                Debug.WriteLine(startMsg);
                while (true) {
                    if (!_delayTimer.Enabled && !_canBackupTimer.Enabled) {
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
                                    GamesToBackup.Where(
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
                                            _watcherCopiedFile = true;
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
                                var newMessage = String.Format(@"{0} {1} {2} {3}", ex.Message, e.ChangeType, e.OldName,
                                    e.Name);
                                SBTErrorLogger.Log(newMessage);
                            }
                            catch (ArgumentException ex) {
                                SBTErrorLogger.Log(ex.Message);
                            }
                        }
                    }
                    break;
                }
                var exitMsg = String.Format(@"EXIT OnRenamed for {0}", e.FullPath);
                Debug.WriteLine(exitMsg);
            }
            catch (ArgumentException ex) {
                SBTErrorLogger.Log(ex.Message);
            }

            if (!_watcherCopiedFile) return;
            Messenger.Default.Send(_numberOfBackups++);
            Messenger.Default.Send(DateTime.Now.ToLongTimeString());
            _watcherCopiedFile = false;
        }

        private static void OnChanged(object sender, FileSystemEventArgs e) {
            try {
                while (true) {
                    if (!_delayTimer.Enabled && !_canBackupTimer.Enabled) {
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

            if (!_watcherCopiedFile) return;
            Messenger.Default.Send(_numberOfBackups++);
            _watcherCopiedFile = false;
        }

        private static void SaveChanged(object sender, FileSystemEventArgs e) {
            var startMsg = String.Format(@"Start SaveChanged for file {0}", e.FullPath);
            Debug.WriteLine(startMsg);
            try {
                Game autoBackupGame = null;

                try {
                    foreach (
                        var a in
                            GamesToBackup.Where(
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
                                    _watcherCopiedFile = true;
                                }
                            }
                        }
                    }
                    catch (FileNotFoundException ex) {
                        SBTErrorLogger.Log(ex.Message);
                        Debug.WriteLine(@"ABORT SaveChanged Exception encountered");
                    }
                    catch (IOException ex) {
                        var newMessage = String.Format(@"{0} {1} {2}", ex.Message, e.ChangeType, e.Name);
                        SBTErrorLogger.Log(newMessage);
                        Debug.WriteLine(@"ABORT SaveChanged IOException encountered");
                    }
                    catch (ArgumentException ex) {
                        SBTErrorLogger.Log(ex.Message);
                        Debug.WriteLine(@"ABORT SaveChanged Exception encountered");
                    }
                }
            }
            catch (ArgumentException ex) {
                SBTErrorLogger.Log(ex.Message);
            }
            var exitMsg = String.Format(@"EXIT SaveChanged for file {0}", e.FullPath);
            Debug.WriteLine(exitMsg);
        }

        private static void SaveDeleted(object sender, FileSystemEventArgs e) {
            try {
                Game autoBackupGame = null; //make argumentexception try catch

                try {
                    foreach (
                        var a in
                            GamesToBackup.Where(
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
                var startMsg = String.Format(@"START SaveCreated for {0}", e.FullPath);
                Debug.WriteLine(startMsg);

                Game autoBackupGame = null;
                try {
                    foreach (
                        var a in
                            GamesToBackup.Where(
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
                                _watcherCopiedFile = true;
                            }
                        }
                    }
                    catch (FileNotFoundException ex) {
                        var newMessage = String.Format(@"{0} {1} {2}", ex.Message, e.ChangeType, e.Name);
                        SBTErrorLogger.Log(newMessage);
                    }
                    catch (IOException ex) {
                        var newMessage = String.Format(@"{0} {1} {2}", ex.Message, e.ChangeType, e.Name);
                        SBTErrorLogger.Log(newMessage);
                    }
                    catch (ArgumentException ex) {
                        SBTErrorLogger.Log(ex.Message);
                    }
                    catch (Exception ex) {
                        SBTErrorLogger.Log(ex.Message);
                    }
                }

                var exitMsg = String.Format(@"EXIT SaveCreated for {0}", e.FullPath);
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

        private static void DisableWatchers() {
            Debug.WriteLine(@"Disabling Watchers during PollAutobackup");
            foreach (var watcher in _fileWatcherList)
                watcher.EnableRaisingEvents = false;
        }

        private static void EnableWatchers() {
            foreach (var watcher in _fileWatcherList)
                watcher.EnableRaisingEvents = true;
            Debug.WriteLine(@"Enabled Watchers after PollAutobackup");
        }

        private static BackupResultHelper InitializePollAutobackup(int interval) {
            _elapsed = 0;
            _interval = interval;
            _firstPoll = true;
            Debug.WriteLine(@"Starting setup for PollAutobackup");

            GameTargetDictionary = new Dictionary<Game, List<FileInfo>>();
            GameFileDictionary = new Dictionary<Game, List<FileInfo>>();

            foreach (var game in GamesToBackup) {
                if (!GetSourceFiles(game, false)) //Returns false and stops the startup process if no source files are found.
                    return new BackupResultHelper {
                        Success = false, 
                        AutobackupEnabled= _backupEnabled, 
                        Message = @"Game directory not found.", 
                        BackupDateTime = DateTime.Now.ToLongTimeString(), 
                        BackupButtonText = @"Enable auto-backup"
                    };
                GetTargetFiles(game, false);
            }
            Debug.WriteLine(@"Finished setup for PollAutobackup.");
            Debug.WriteLine(@"Initializing Poll Autobackup Timer.");
            Watch = new Stopwatch();
            _pollAutobackupTimer = new Timer { Interval = _backupSyncOptions.BackupOnInterval ? 1000 : 60000 }; //Timer interval is one second if interval, 45sec if time.
            _pollAutobackupTimer.Elapsed += _pollAutobackupTimer_Elapsed;
            _pollAutobackupTimer.Start();
            
            Debug.WriteLine(@"Finished initializing Poll Autobackup Timer.");
            return new BackupResultHelper(true, true, "Autobackup enabled", DateTime.Now.ToLongTimeString(), "Disable autobackup");
        }

        private static void ShutdownPollAutobackup() {
            _pollAutobackupTimer.Stop();
            GameFileDictionary.Clear();
            GameTargetDictionary.Clear();
            GamesToBackup.Clear();
        }

        private static async void PollAutobackup() {
            if(Watch.IsRunning) Watch.Stop();
            Watch.Start();
            var startTime = Watch.Elapsed;
            Debug.WriteLine(@"Starting PollAutobackup at {0}", startTime);

            if (!_firstPoll)
                foreach (var game in GamesToBackup)
                    GetSourceFiles(game, true); //Check for new source files since last poll.

            //Main backup loop
            foreach (var game in GamesToBackup) {
                var fileCompareFilesCopied = false;
                var scannerFilesCopied = false;
                Debug.WriteLine(@"Starting Main Backup Loop for " + game.Name + " at {0}", Watch.Elapsed);
                List<FileInfo> sourceFiles;
                List<FileInfo> targetFiles;
                GameFileDictionary.TryGetValue(game, out sourceFiles);
                GameTargetDictionary.TryGetValue(game, out targetFiles);

                var filesToCopy = CheckForMissingTargetFiles(sourceFiles, targetFiles); //Look for source files NOT in target directory & copy them.
                fileCompareFilesCopied = await CopySaves(game, filesToCopy);
                if (fileCompareFilesCopied) GetTargetFiles(game, true); //Update target files if copy occurred.

                filesToCopy = await FileScanner(sourceFiles, targetFiles); //Only called when files exist in the target directory to compare.
                scannerFilesCopied = await CopySaves(game, filesToCopy);
                if (scannerFilesCopied) GetTargetFiles(game, true); //Update target files if copy occurred.

                Debug.WriteLine(@"Finishing Main Backup Loop for " + game.Name + " at {0}", Watch.Elapsed);                
            }

            

            var endTime = Watch.Elapsed;
            Debug.WriteLine(@"PollAutobackup ended at {0}", endTime);
            Debug.WriteLine(@"PollAutobackup completed in {0}", (endTime - startTime));

            _firstPoll = false;
            if (_backupSyncOptions.SyncToDropbox) await SyncToDropbox();
            EnableWatchers();
        }

        private async static Task SyncToDropbox() {
            var startTime = Watch.Elapsed;
            Debug.WriteLine(@"Starting SyncToDropbox at {0}", startTime);
            var drop = new DropBoxAPI();
            await drop.Initialize();
            if (_backupSyncOptions.SyncToZip) { //Find a way to check for existing file
                Debug.WriteLine(@"Creating and uploading zip file");
                var zipDestPath = MyDocuments + @"\Save Backup Tool\Saves.zip";
                if (File.Exists(zipDestPath)) File.Delete(zipDestPath);
                await Task.Run(() => ZipFile.CreateFromDirectory(_autoBackupDirectoryInfo.FullName, zipDestPath)); 
                var file = new FileInfo(@"C:\Users\Rob\Desktop\Saves.zip");
                await drop.Upload("/", file);
                Debug.WriteLine(@"Zip uploaded");
            }
            else {//Commented bc dropbox permissions don't allow these files.
                
                //Debug.WriteLine(@"Creating and uploading directories and files");
                //var parentFileNameDictionary = new Dictionary<FileInfo, string>();
                //var allFiles = Directory.GetFiles(_autoBackupDirectoryInfo.FullName, "*", SearchOption.AllDirectories);
                //var truncatedFiles = new List<string>();
                //foreach (var game in GamesToBackup) {
                //    for (var i = 0; i < allFiles.Count(); i++) {
                //        if (allFiles[i].Contains(game.RootFolder)){
                //            var parent = Directory.GetParent(allFiles[i]).ToString();
                //            var parentIndex = parent.IndexOf(game.RootFolder);
                //            var parentSub = parent.Substring(parentIndex);
                //            parentSub = @"/" + parentSub.Replace(@"\", @"/");

                //            var fileName = new FileInfo(allFiles[i].ToString());
                //            parentFileNameDictionary.Add(fileName,parentSub);
                //        }
                //    }
                //}
                //foreach (var pair in parentFileNameDictionary) {
                //    await drop.Upload(pair.Value, pair.Key); //Value is parent directory, Key is FileInfo
                //    Debug.WriteLine(@"File uploaded");
                //}
            }
            var endtime = Watch.Elapsed;
            Debug.WriteLine(@"Finishing SyncToDropbox at {0}", endtime);
            Debug.WriteLine(@"SyncToDropbox finished in {0}", (endtime - startTime));
        }
        
        /// <summary>
        /// Finds files that don't exist in target directory and calls CopySaves to copy them.
        /// Does not catch files that exist in target directory with the same name as source.
        /// That gets handled later.
        /// </summary>
        /// <param name="sourceFiles"></param>
        /// <param name="targetFiles"></param>
        /// <returns></returns>
        private static List<FileInfo> CheckForMissingTargetFiles(List<FileInfo> sourceFiles, List<FileInfo> targetFiles) {
            Debug.WriteLine(@"Checking for source files NOT present in target directory");
            var sourceFilesToCopy = new List<FileInfo>();
            foreach (var source in sourceFiles) {
                if (targetFiles.Exists(a => a.ToString().Contains(source.Name))) continue;
                sourceFilesToCopy.Add(source);
            }
            Debug.WriteLine(@"Finished checking for source files NOT present in target directory. Found {0} files.", sourceFilesToCopy.Count);
            return sourceFilesToCopy;
        }

        /// <summary>
        /// Scans files in-depth to check for matching files
        /// </summary>
        /// <param name="sourceFiles"></param>
        /// <param name="targetFiles"></param>
        /// <returns></returns>
        private async static Task<List<FileInfo>> FileScanner(IEnumerable<FileInfo> sourceFiles, List<FileInfo> targetFiles) {
            var startTime = Watch.Elapsed;
            Debug.WriteLine(@"Scanner started at {0}", startTime);
            var filesToCopy = new List<FileInfo>();
            foreach (var source in sourceFiles) {
                var source1 = source; //suggested by resharper
                foreach (var target in targetFiles) {
                    var fileAdded = false;
                    var matchedFileFound = false;
                    if (source1.Length != target.Length && source1.Name == target.Name) { //Same name, different Length. Copy.
                        filesToCopy.Add(source1);
                        fileAdded = true;
                    } else if (source.Length == target.Length && source1.Name == target.Name) { //Same name, same length. Compare bytes.
                        if (await Task.Run(() => !FileCompare(source.FullName, target.FullName))) {
                            filesToCopy.Add(source1); //Bytes are different. Copy.
                            fileAdded = true;
                        }
                    } 
                    if (source.Name == target.Name) { //If length are the same, FileCompare is the same, but names match. Skip the rest of targetFiles loop.
                        matchedFileFound = true;
                    }
                    if (fileAdded || matchedFileFound) break;
                }
            }
            var endTime = Watch.Elapsed;
            Debug.WriteLine(@"Scanner complete after {0}", endTime);
            Debug.WriteLine(@"Scanner has {0} files to copy", filesToCopy.Count);
            Debug.WriteLine(@"Scanner completed in {0}", (endTime - startTime));
            
            return filesToCopy;
        }

        private static async Task<bool> CopySaves(Game game, IEnumerable<FileInfo> filesToCopy) {
            var startTime = Watch.Elapsed;
            var fileCopied = false;
            _progress.TotalFiles = filesToCopy.Count();
            Debug.WriteLine(@"CopySaves starting at {0}", startTime);
            try {
                foreach (var sourceFile in filesToCopy) {
                    var index = sourceFile.FullName.IndexOf(game.RootFolder, StringComparison.Ordinal);
                    var substring = sourceFile.FullName.Substring(index);
                    var destPath = _autoBackupDirectoryInfo.FullName + "\\" + substring;
                    var dir = new FileInfo(destPath);
                    if (!Directory.Exists(dir.DirectoryName))
                        Directory.CreateDirectory(dir.DirectoryName);
                    using (
                        var inStream = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read,
                            FileShare.ReadWrite)) {
                        using (var outStream = new FileStream(destPath, FileMode.Create)) {
                            await inStream.CopyToAsync(outStream);
                            Debug.WriteLine(@"SUCCESSFUL COPY: {0} copied to {1}", sourceFile.Name, destPath);
                            fileCopied = true;
                            _numberOfBackups++;
                            _progress.FilesComplete++;
                            Messenger.Default.Send(_progress);
                            Messenger.Default.Send(DateTime.Now.ToLongTimeString());
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
            _progress.FilesComplete = 0;
            Messenger.Default.Send(_progress);
            return fileCopied;
        }

         // This method accepts two strings the represent two files to 
        // compare. A return value of 0 indicates that the contents of the files
        // are the same. A return value of any other value indicates that the 
        // files are not the same.
        private static bool FileCompare(string file1, string file2) {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest; //Useful or no?

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
    
        #region Timers
        private static void _pollAutobackupTimer_Elapsed(object sender, ElapsedEventArgs e) {
            if (_backupSyncOptions.BackupOnInterval) {
                _elapsed++;
                Debug.WriteLine(@"Seconds elapsed {0}", _elapsed);
                Messenger.Default.Send(new TimeSpan(0, 0, 0, 1)); //Send to UI to update timer
                if (_elapsed/60 < _interval) return; //If false, poll autobackup
                _pollAutobackupTimer.Stop();
                Debug.WriteLine(@"Poll Autobackup timer elapsed.");
                DisableWatchers();
                PollAutobackup();
                _elapsed = 0;
                Messenger.Default.Send(new TimeSpan(0, 0, -_interval, 0));
                _pollAutobackupTimer.Start();
                return;
            }
            if (DateTime.Now.Hour == _backupSyncOptions.BackupTime.Hour &&
                DateTime.Now.Minute == _backupSyncOptions.BackupTime.Minute) {
                Debug.WriteLine(@"Current time is Backup Time");
                DisableWatchers();
                PollAutobackup();
            }
        }

        private static void _delayTimer_Elapsed(object sender, ElapsedEventArgs e) {
            Debug.WriteLine("DelayTimer elapsed");
            _canBackupTimer.Start();
            _delayTimer.Stop();
        }

        private static void _canBackupTimer_Elapsed(object sender, ElapsedEventArgs e) {
            Debug.WriteLine("CanBackup timer elapsed");
            _lastAutoBackupTime = DateTime.Now;
            _canBackupTimer.Stop();
        }
        #endregion

        public static void ChangeInterval(int interval) {
            _pollAutobackupTimer.Stop();
            _elapsed = 0;
            _interval = interval;
            _pollAutobackupTimer.Start();
        }
    }
}
