using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GalaSoft.MvvmLight.Messaging;
using Saved_Game_Backup.Helper;

namespace Saved_Game_Backup {

    public class BackupToFolder {

        private static readonly BackupResultHelper ErrorResultHelper = new BackupResultHelper() { Success = false, AutobackupEnabled = false };
        private static ProgressHelper _progress;

        public static BackupResultHelper BackupSaves(List<Game> gamesList, DirectoryInfo targetDi) {
            _progress = new ProgressHelper(){FilesComplete=0, TotalFiles = 0};
            Debug.WriteLine(@"Starting BackupSaves");
            
            //Check for and create target directory
            if (!Directory.Exists(targetDi.FullName) && !string.IsNullOrWhiteSpace(targetDi.FullName))
                Directory.CreateDirectory(targetDi.FullName);

            //Get file count for progress bar
            Debug.WriteLine(@"Getting file count");
            var totalFiles = 0;
            foreach (var game in gamesList) {
                var files = Directory.GetFiles(game.Path, "*", SearchOption.AllDirectories);
                if (files.Any())
                    totalFiles += files.Count();
                else {
                    ErrorResultHelper.Message = @"No files found for " + game.Name;
                    return ErrorResultHelper;
                }
            }
            Debug.WriteLine(@"Found {0} files to copy", totalFiles);
            _progress.TotalFiles = totalFiles;

            //Copy files for each game to folder.
            foreach (var game in gamesList) {
                BackupGame(game, targetDi.FullName);
            }

            Debug.WriteLine(@"Backup saves complete");
            var time = DateTime.Now.ToLongTimeString();
            return new BackupResultHelper(){
                AutobackupEnabled = false, 
                Message = @"Backup complete", 
                Success = true, 
                BackupDateTime = time, 
                BackupButtonText = "Backup to folder"};
        }

        private static void BackupGame(Game game, string destDirName) {
            Debug.WriteLine(@"Starting file copy for " + game.Name);
            var allFiles = Directory.GetFiles(game.Path, "*.*", SearchOption.AllDirectories);
            foreach (var sourceFile in allFiles) {
                try {
                    var index = sourceFile.IndexOf(game.RootFolder, StringComparison.CurrentCulture);
                    var substring = sourceFile.Substring(index);
                    var destinationFi = new FileInfo(destDirName + "\\" + substring);
                    var destinationDir = destinationFi.DirectoryName;
                    if (!Directory.Exists(destinationDir)) Directory.CreateDirectory(destinationDir);
                    var file = new FileInfo(sourceFile);
                    file.CopyTo(destinationFi.FullName, true);
                    _progress.FilesComplete++;
                    Messenger.Default.Send(_progress);
                }
                catch (IOException ex) {
                    SBTErrorLogger.Log(ex.Message);
                }
                catch (NullReferenceException ex) {
                    SBTErrorLogger.Log(ex.Message);
                }
            }
            Debug.WriteLine(@"Finished file copy for " + game.Name);
        }

    }
}
