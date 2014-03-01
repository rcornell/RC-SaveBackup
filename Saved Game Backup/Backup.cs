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
        
        private static readonly string HardDrive = Path.GetPathRoot(Environment.SystemDirectory);
        private static readonly string UserPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private static readonly CultureInfo Culture = CultureInfo.CurrentCulture;
        private static readonly BackupResultHelper ErrorResultHelper = new BackupResultHelper(){Success = false ,AutobackupEnabled = false, Message="Error during operation"};

        public Backup() {}

        public static BackupResultHelper StartBackup(List<Game> games, BackupType backupType, bool backupEnabled, int interval = 0, DirectoryInfo targetDi = null, FileInfo targetFile = null) {
            
            var gamesToBackup = ModifyGamePaths(games);
            
            //Check for problems with parameters
            if (!games.Any() && backupType == BackupType.Autobackup && backupEnabled)
                return HandleBackupResult(true, false, "Autobackup Disabled", backupType,
                    DateTime.Now.ToString(Culture));
            if (!games.Any())
                return HandleBackupResult(false, false, "No games selected.", backupType,
                    DateTime.Now.ToString(Culture));


            switch (backupType) {                  
                case BackupType.ToZip:
                    return BackupToZip.BackupAndZip(gamesToBackup, targetFile);
                case BackupType.ToFolder:
                    return BackupToFolder.BackupSaves(gamesToBackup, targetDi);
                case BackupType.Autobackup:
                    return BackupAuto.ToggleAutoBackup(gamesToBackup, backupEnabled, interval, targetDi);
            }

            return ErrorResultHelper;
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

        public static BackupResultHelper Reset(List<Game> games, BackupType backupType, bool backupEnabled) {
            var message = "";
            if (backupEnabled) {
                foreach (var game in games) BackupAuto.RemoveFromAutobackup(game);
                message = "Autobackup disabled";
            }
            games.Clear();
            return HandleBackupResult(true, false, message, backupType, DateTime.Now.ToLongTimeString());
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

       
    }
}