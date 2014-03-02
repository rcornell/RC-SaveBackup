using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GalaSoft.MvvmLight.Messaging;
using Saved_Game_Backup.Helper;

namespace Saved_Game_Backup { 

    public class BackupToZip {

        private static readonly BackupResultHelper ErrorResultHelper = new BackupResultHelper() { Success = false, AutobackupEnabled = false };
        private static ProgressHelper _progress;

        public static async Task<BackupResultHelper> BackupAndZip(List<Game> gamesList, FileInfo targetFi) {
            //Initialize progress helper
            _progress = new ProgressHelper(){FilesComplete = 0, TotalFiles = 0};
            
            //Delete existing file if it exists
            if (targetFi.Exists)
                targetFi.Delete();

            //Establish total file count for all games. If no files found for a game, return an error.
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
            _progress.TotalFiles = totalFiles;


            try {
                //Create dictionary of source FileInfo's and target truncated paths
                var zipFileDictionary = new Dictionary<FileInfo, string>();
                foreach (var game in gamesList) {
                    var gameFiles = new DirectoryInfo(game.Path).GetFiles("*", SearchOption.AllDirectories);
                    foreach (var file in gameFiles) {
                        var index = file.FullName.IndexOf(game.RootFolder, StringComparison.Ordinal);
                        var substring = file.FullName.Substring(index);
                        zipFileDictionary.Add(file, substring);
                    }
                }

                //Zip files
                using (var destination = ZipFile.Open(targetFi.FullName, ZipArchiveMode.Create)) {
                    foreach (var pair in zipFileDictionary) {
                        await
                            Task.Run(
                                () =>
                                    destination.CreateEntryFromFile(pair.Key.FullName, pair.Value,
                                        CompressionLevel.Optimal));
                        _progress.FilesComplete++;
                        Messenger.Default.Send(_progress);
                    }
                }
            }
            catch (ArgumentException ex) {
                SBTErrorLogger.Log(ex.Message);
            } catch (FileNotFoundException ex) {
                SBTErrorLogger.Log(ex.Message);
            } catch (UnauthorizedAccessException ex) {
                SBTErrorLogger.Log(ex.Message);
            } catch (IOException ex) {
                SBTErrorLogger.Log(ex.Message);
            } catch (Exception ex) {
                SBTErrorLogger.Log(ex.Message);
            }
            var time = DateTime.Now.ToLongTimeString();
            return new BackupResultHelper(){Success = true, Message = "Zip backup complete!", BackupDateTime = time};
        }
    }
}
