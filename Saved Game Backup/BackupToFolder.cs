using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Saved_Game_Backup
{
    public class BackupToFolder
    {
        private static readonly string _hardDrive = Path.GetPathRoot(Environment.SystemDirectory);
        private static string _myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static readonly string _userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private static string _userName = Environment.UserName;

        public static bool BackupSaves(List<Game> gamesList) {
            DirectoryInfo targetDi;

            var fd = new FolderBrowserDialog() {
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Description = @"Select the folder where this utility will create the SaveBackups folder.",
                ShowNewFolderButton = true
            };

            if (fd.ShowDialog() == DialogResult.OK)
                targetDi = new DirectoryInfo(fd.SelectedPath);
            else {
                return false;
            }

            if (!Directory.Exists(targetDi.FullName) && !string.IsNullOrWhiteSpace(targetDi.FullName))
                Directory.CreateDirectory(targetDi.FullName);

            //This backs up each game using BackupGame()
            foreach (var game in gamesList) {
                BackupGame(game, targetDi.FullName);
            }
            return true;
        }

        private static void BackupGame(Game game, string destDirName) {
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
                }
                catch (IOException ex) {
                    SBTErrorLogger.Log(ex.Message);
                }
                catch (NullReferenceException ex) {
                    SBTErrorLogger.Log(ex.Message);
                }
            }
        }

    }
}
