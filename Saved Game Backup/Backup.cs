using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Saved_Game_Backup
{
    public class Backup {

        public Backup() {
            
        }

        public static void BackupSaves(ObservableCollection<Game> gamesList, string harddrive, string specifiedfolder = null) {
            string folderPath;
            if (specifiedfolder == null) {
                folderPath = CreateFolderPath(harddrive);
            }
            else {
                folderPath = specifiedfolder;
            }

            var games = new List<Game>(gamesList);

            string destination = harddrive + "SaveBackups";
            Directory.CreateDirectory(destination);

            #region 

            ////Reads the hardDrivePath and puts the characters in to the hardDrivePathStream array
            //var hardDrivePathStream = new char[harddrive.Length];
            //using (var sr = new StringReader(harddrive)) {
            //    sr.Read(hardDrivePathStream,0,harddrive.Length);
            //}


            ////Reads the folderPath and puts the characters in to the folderPathStream array
            //var folderPathStream = new char[folderPath.Length];
            //using (var sr = new StringReader(folderPath)) {
            //    sr.Read(folderPathStream, 0, harddrive.Length);
            //}

            //folderPathStream[0] = hardDrivePathStream[0];

            //using (var sw = new StringWriter(new StringBuilder(folderPath))) {
            //    sw.Write(folderPathStream);
            //}

            //string[] filesToCopy;

            //foreach (Game g in gamesList) {
            //    filesToCopy.
            //}

            #endregion

            //The below code now copies files from one directory given in g.Path.
            //You need to make it recursive to also look through subdirectories
            //and create them and copy their files. test
            
            foreach (Game g in gamesList){
            
                DirectoryInfo dir = new DirectoryInfo(g.Path);
                DirectoryInfo[] dirs = dir.GetDirectories();

                FileInfo[] fileInfo = dir.GetFiles();

                string newDir = destination + "\\" + g.Name;

                if (!Directory.Exists(newDir)) {
                    Directory.CreateDirectory(newDir);
                }

                foreach (FileInfo f in fileInfo) {
                    string newFile = Path.Combine(newDir, f.Name);
                    f.CopyTo(newFile, false);
                }        
            }

        }

        private static string CreateFolderPath(string harddrive) {
            string username;
            string path;
            
            try
            {
                //username = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show("I cannot find your Windows username, e.g. \"RobPC\". " + ex);
                return null;
            }

            return path;
        }
    }
}
