using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Text;
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
using Saved_Game_Backup.BackupClasses;
using Saved_Game_Backup.Helper;
using Xceed.Wpf.DataGrid;
using MessageBox = System.Windows.MessageBox;
using Timer = System.Timers.Timer;


namespace Saved_Game_Backup {
    public class Backup {

        private static DirectoryInfo _specifiedFolder;
        private static FileInfo _specifiedFile;  

        private static readonly string HardDrive = Path.GetPathRoot(Environment.SystemDirectory);
        private static readonly string UserPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private static readonly CultureInfo Culture = CultureInfo.CurrentCulture;
        private static readonly BackupResultHelper ErrorResultHelper = new BackupResultHelper(){Success = false ,AutobackupEnabled = false, Message=@"Error during operation"};
        private static BackupResultHelper _resultHelper;
        public static FolderBrowserDialog FolderBrowser = new FolderBrowserDialog() {
            ShowNewFolderButton = true,
            Description = @"Select a target folder for auto-backup to copy to."
        };

        public static SaveFileDialog SaveFileDialog = new SaveFileDialog() {
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
            Filter = @"Zip files | *.zip",
            DefaultExt = "zip",
        };

        public Backup() {}

        public static async Task<BackupResultHelper> StartBackup(List<Game> games, BackupType backupType, bool backupEnabled, BackupSyncOptions backupSyncOptions, int interval = 0) {
            //Check for problems with parameters
            if (!games.Any() && backupType == BackupType.Autobackup && backupEnabled) {
                _resultHelper = new BackupResultHelper {Message = @"Auto-backup disabled", AutobackupEnabled = false};
                return _resultHelper;
            }
            if (!games.Any()) {
                ErrorResultHelper.Message = @"No games selected";
                return ErrorResultHelper;
            }

            var gamesToBackup= new List<Game>();
            if (!backupEnabled) {
                if (!GetDirectoryOrFile(backupType)) return ErrorResultHelper;
                gamesToBackup = ModifyGamePaths(games);
            }

            switch (backupType) {                  
                case BackupType.ToZip:
                   return await BackupToZip.BackupAndZip(gamesToBackup, _specifiedFile);
                case BackupType.ToFolder:
                    return BackupToFolder.BackupSaves(gamesToBackup, _specifiedFolder);
                case BackupType.Autobackup:
                    return BackupAuto.ToggleAutoBackup(gamesToBackup, backupEnabled, backupSyncOptions, interval, _specifiedFolder);
            }

            return ErrorResultHelper;
        }

        private static bool GetDirectoryOrFile(BackupType backupType) {
                switch (backupType)
                {
                    case BackupType.ToFolder:
                    case BackupType.Autobackup:
                        if (FolderBrowser.ShowDialog() == DialogResult.OK) {
                            _specifiedFolder = new DirectoryInfo(FolderBrowser.SelectedPath);
                            var path = DirectoryFinder.FormatDisplayPath(_specifiedFolder.FullName);
                            Messenger.Default.Send(new FolderHelper(path));
                            return true;
                        }
                        break;
                    case BackupType.ToZip:
                        if (SaveFileDialog.ShowDialog() == DialogResult.OK) {
                            _specifiedFile = new FileInfo(SaveFileDialog.FileName);
                            return true;
                        }
                        break;
                }
            return false;
        }

        /// <summary>
        /// Edits the truncated paths in the Games.json file and inserts the 
        /// user's path before the \\Documents\\ or \\AppData\\ folder path.
        /// If the game has its own user path, indicated by HasCustomPath
        /// being true, the game's path is not modified before being added to the
        /// new list.
        /// </summary>
        /// <param name="gamesToBackup"></param>
        public static List<Game> ModifyGamePaths(IEnumerable<Game> gamesToBackup) {
            var editedList = new List<Game>();
            try {
                foreach (var game in gamesToBackup) {
                    if (!game.HasCustomPath && game.Path.Contains("Documents"))
                        editedList.Add(new Game(game.Name, UserPath + game.Path, game.ID, game.ThumbnailPath,
                            game.HasCustomPath, game.HasThumb, game.RootFolder));
                    else if (!game.HasCustomPath && game.Path.Contains("Program Files"))
                        editedList.Add(new Game(game.Name, HardDrive + game.Path, game.ID, game.ThumbnailPath,
                            game.HasCustomPath, game.HasThumb, game.RootFolder));
                    else if (!game.HasCustomPath && game.Path.Contains("AppData"))
                        editedList.Add(new Game(game.Name, UserPath + game.Path, game.ID, game.ThumbnailPath,
                            game.HasCustomPath, game.HasThumb, game.RootFolder));
                    else if (!game.HasCustomPath && game.Path.Contains("Desktop"))
                        editedList.Add(new Game(game.Name, UserPath + game.Path, game.ID, game.ThumbnailPath,
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

        public static Game ModifyGamePaths(Game game) {
            var editedGame = new Game();
            try
            {
                
                    if (!game.HasCustomPath && game.Path.Contains("Documents"))
                        editedGame = new Game(game.Name, UserPath + game.Path, game.ID, game.ThumbnailPath,
                            game.HasCustomPath, game.HasThumb, game.RootFolder);
                    else if (!game.HasCustomPath && game.Path.Contains("Program Files"))
                        editedGame = new Game(game.Name, HardDrive + game.Path, game.ID, game.ThumbnailPath,
                            game.HasCustomPath, game.HasThumb, game.RootFolder);
                    else if (!game.HasCustomPath && game.Path.Contains("AppData"))
                        editedGame = new Game(game.Name, UserPath + game.Path, game.ID, game.ThumbnailPath,
                            game.HasCustomPath, game.HasThumb, game.RootFolder);
                    else if (!game.HasCustomPath && game.Path.Contains("Desktop"))
                        editedGame = new Game(game.Name, UserPath + game.Path, game.ID, game.ThumbnailPath,
                            game.HasCustomPath, game.HasThumb, game.RootFolder);
                    else
                        editedGame = game;
                
            }
            catch (Exception ex)
            {
                SBTErrorLogger.Log(ex.Message);
            }
            return editedGame;
        }

        public static bool CanBackup(List<Game> gamesToBackup) { 
            if (HardDrive == null) {
                MessageBox.Show(
                    "Cannot find OS drive. \r\nPlease add each game using \r\nthe 'Add Game to List' button.");
                return false;
            }

            if (gamesToBackup.Any()) return true;
            MessageBox.Show("No games selected. \n\rPlease select at least one game.");
            return false;
        }

        public static void Reset(List<Game> games, BackupType backupType, bool backupEnabled) {
            if (backupEnabled) {
                foreach (var game in games) BackupAuto.RemoveFromAutobackup(game);
            }
            games.Clear();
        }

        private static BackupResultHelper HandleBackupResult(bool success, bool backupEnabled, string messageToShow,
            BackupType backupType, string date) {
            var backupButtonText = "";
            if (!success) return new BackupResultHelper(success, backupEnabled, messageToShow, date, backupButtonText);

            var message = messageToShow;
            switch (backupType) {
                case BackupType.ToFolder:
                    backupButtonText = "Backup to folder";
                    break;
                case BackupType.ToZip:
                    backupButtonText = "Backup to zip";
                    break;
                default:
                    if (backupEnabled && backupType == BackupType.Autobackup) backupButtonText = "Disable auto-backup";
                    else backupButtonText = "Enable auto-backup";
                    break;
            }


            return new BackupResultHelper(success, backupEnabled, message, date, backupButtonText);
        }

        public static BackupResultHelper RemoveFromAutobackup(Game game) {
            if (game == null) return ErrorResultHelper;
            var result = BackupAuto.RemoveFromAutobackup(game);
            return result;
        }

        public static BackupResultHelper AddToAutobackup(Game game) {
            if (game == null) return ErrorResultHelper;
            var editedGame = ModifyGamePaths(game);
            var result = BackupAuto.AddToAutobackup(editedGame);
            return result;
        }
    }
}