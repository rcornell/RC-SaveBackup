using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Saved_Game_Backup { 

    public class BackupToZip {

        private static readonly string _hardDrive = Path.GetPathRoot(Environment.SystemDirectory);
        private static string _myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static readonly string _userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private static string _userName = Environment.UserName;

        public static bool BackupAndZip(List<Game> gamesList, string specifiedfolder = null) {
            DirectoryInfo zipSourceDi;
            FileInfo zipDestination;

            var fd = new SaveFileDialog() {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                FileName = "SaveBackups.zip",
                Filter = @"Zip files (*.zip) | *.zip",
                Title = @"Select the location and name of the Zip file.",
                CheckFileExists = false,
                OverwritePrompt = true
            };

            if (fd.ShowDialog() == DialogResult.OK) {
                zipDestination = new FileInfo(fd.FileName);
                zipSourceDi = new DirectoryInfo(zipDestination.DirectoryName + "\\Temp");
            }
            else {
                return false;
            }

            if (!Directory.Exists(zipSourceDi.Parent.FullName))
                Directory.CreateDirectory(zipSourceDi.Parent.FullName);

            //Creates temporary directory at ZipSource + the game's name
            //To act as the source folder for the ZipFile class.
            foreach (var game in gamesList) {
                var dir = new DirectoryInfo(zipSourceDi.FullName + "\\" + game.Name);
                BackupGame(game, dir.FullName);
            }

            //Delete existing zip file if one exists.
            if (zipDestination.Exists)
                zipDestination.Delete();

            ZipFile.CreateFromDirectory(zipSourceDi.FullName, zipDestination.FullName, CompressionLevel.Optimal, false);

            //Delete temporary folder that held save files.
            Directory.Delete(zipSourceDi.FullName, true);

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
