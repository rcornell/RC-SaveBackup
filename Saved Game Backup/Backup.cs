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
        
        private static readonly string _hardDrive = Path.GetPathRoot(Environment.SystemDirectory);
        private static string _myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static readonly string _userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private static string _userName = Environment.UserName;
        private static CultureInfo _culture = CultureInfo.CurrentCulture;
        //Properties for PollAutobackup


        public Backup() {}

        public static BackupResultHelper StartBackup(List<Game> games, BackupType backupType,
            bool backupEnabled, int interval = 0) {
            BackupAuto.GamesToBackup = ModifyGamePaths(games);
            var success = false;
            var message = "";
            
            //Check for problems with parameters
            if (!games.Any() && backupType == BackupType.Autobackup && backupEnabled)
                return HandleBackupResult(true, false, "Autobackup Disabled", backupType,
                    DateTime.Now.ToString(_culture));
            if (!games.Any())
                return HandleBackupResult(success, false, "No games selected.", backupType,
                    DateTime.Now.ToString(_culture));


            switch (backupType) {                  
                case BackupType.ToZip:
                    success = BackupToZip.BackupAndZip(BackupAuto.GamesToBackup);
                    break;
                case BackupType.ToFolder:
                    success = BackupToFolder.BackupSaves(BackupAuto.GamesToBackup);
                    break;
                case BackupType.Autobackup:
                    return BackupAuto.ToggleAutoBackup(backupEnabled, interval);
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
            }
            return HandleBackupResult(success, backupEnabled, message, backupType,
                DateTime.Now.ToString(CultureInfo.CurrentCulture));
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

        #region User Interface Methods
        public static bool CanBackup(List<Game> gamesToBackup) { 
            if (_hardDrive == null) {
                MessageBox.Show(
                    "Cannot find OS drive. \r\nPlease add each game using \r\nthe 'Add Game to List' button.");
                return false;
            }

            if (gamesToBackup.Any()) return true;
            MessageBox.Show("No games selected. \n\rPlease select at least one game.");
            return false;
        }

        public static BackupResultHelper Reset(List<Game> games, BackupType backupType,
            bool backupEnabled) {
            var message = "";
            if (backupEnabled) {
                //ShutdownWatchers();
                message = "Autobackup Disabled";
            }
            games.Clear();
            return HandleBackupResult(true, false, message, backupType, DateTime.Now.ToString(_culture));
        }
        #endregion

        private static BackupResultHelper HandleBackupResult(bool success, bool backupEnabled, string messageToShow,
            BackupType backupType, string date) {
            var backupButtonText = "Backup Saves";
            if (!success) return new BackupResultHelper(success, backupEnabled, messageToShow, date, backupButtonText);

            var message = messageToShow;

            if (backupEnabled && backupType == BackupType.Autobackup) backupButtonText = "Disable Autobackup";
            if (!backupEnabled && backupType == BackupType.Autobackup) backupButtonText = "Enable Autobackup";


            return new BackupResultHelper(success, backupEnabled, message, date, backupButtonText);
        }

        public static void RemoveFromAutobackup(Game game) {
            BackupAuto.RemoveFromAutobackup(game);
        }

       
    }
}