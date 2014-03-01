using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GalaSoft.MvvmLight.Messaging;
using Saved_Game_Backup.Helper;

namespace Saved_Game_Backup { 

    public class BackupToZip {

        private static readonly string _hardDrive = Path.GetPathRoot(Environment.SystemDirectory);
        private static string _myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static readonly string _userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private static string _userName = Environment.UserName;
        private static readonly BackupResultHelper ErrorResultHelper = new BackupResultHelper() { Success = false, AutobackupEnabled = false, Message = "No source files found for game." };
        private static int TotalFiles;
        private static int FilesComplete;
        private static ProgressHelper Progress;

        public static async Task<BackupResultHelper> BackupAndZip(List<Game> gamesList, FileInfo targetFi) {
            TotalFiles = 0;
            FilesComplete = 0;
            Progress = new ProgressHelper();
            var zipSourceDi = new DirectoryInfo(targetFi.Directory + "\\Temp");
            
            //Delete existing file if it exists
            if (targetFi.Exists)
                targetFi.Delete();

            if (!Directory.Exists(zipSourceDi.FullName))
                Directory.CreateDirectory(zipSourceDi.FullName);
            foreach (var game in gamesList.Where(game => Directory.GetFiles(game.Path, "*", SearchOption.AllDirectories).Any())) {
                TotalFiles += Directory.GetFiles(game.Path).Count();
            }
            Progress.TotalFiles = TotalFiles;


            //Creates temporary directory at ZipSource + the game's name
            //To act as the source folder for the ZipFile class.
            foreach (var game in gamesList) {
                var dir = new DirectoryInfo(zipSourceDi.FullName + "\\" + game.Name);
                if (!BackupGame(game, dir.FullName)) return ErrorResultHelper;
            }

            //Zip files from temp folder
            await Task.Run(() => ZipFile.CreateFromDirectory(zipSourceDi.FullName, targetFi.FullName, CompressionLevel.Optimal, false));

            //Delete temporary folder that held save files.
            Directory.Delete(zipSourceDi.FullName, true);
            var time = DateTime.Now.ToLongTimeString();
            return new BackupResultHelper(){Success = true, Message = "Backup complete!", BackupDateTime = time};
        }

        private static bool BackupGame(Game game, string destDirName) {
            var allFiles = Directory.GetFiles(game.Path, "*.*", SearchOption.AllDirectories);
            if (!allFiles.Any()) return false;
            foreach (var sourceFile in allFiles) {
                try {
                    var index = sourceFile.IndexOf(game.RootFolder, StringComparison.CurrentCulture);
                    var substring = sourceFile.Substring(index);
                    var destinationFi = new FileInfo(destDirName + "\\" + substring);
                    var destinationDir = destinationFi.DirectoryName;
                    if (!Directory.Exists(destinationDir)) Directory.CreateDirectory(destinationDir);
                    var file = new FileInfo(sourceFile);
                    file.CopyTo(destinationFi.FullName, true);
                    FilesComplete++;
                    UpdateProgress();
                }
                catch (IOException ex) {
                    SBTErrorLogger.Log(ex.Message);
                }
                catch (NullReferenceException ex) {
                    SBTErrorLogger.Log(ex.Message);
                }
            }
            return true;
        }

        private static void UpdateProgress() {
            Progress.FilesComplete = FilesComplete;
            Messenger.Default.Send(Progress);
        }
    }
}
