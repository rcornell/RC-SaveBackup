using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using Microsoft.Win32;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Saved_Game_Backup
{
    public class DirectoryFinder {

        public static ObservableCollection<string> HardDrives { get; set; } 

        private static string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static string _sbtPath;

        static DirectoryFinder() {
             
        }

        public static void CreateSbtDirectories() {
            _sbtPath = documentsPath + "\\Save Backup Tool\\";
            if (!Directory.Exists(_sbtPath + "Thumbnails\\"))
                Directory.CreateDirectory(_sbtPath+"Thumbnails\\");
            if (!Directory.Exists(_sbtPath + "Error\\"))
                Directory.CreateDirectory(_sbtPath + "Error\\");
        }

        public static ObservableCollection<string> GetHardDriveCollection() {
            HardDrives = new ObservableCollection<string>();
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives) {
                HardDrives.Add(drive.RootDirectory.ToString());
            }
            return HardDrives;
        }

        public static ObservableCollection<Game> GetGamesList() {
            var jsonFi = new FileInfo(@"Assets\Games.json");
            return jsonFi.Exists
                ? 
                JsonConvert.DeserializeObject<ObservableCollection<Game>>(File.ReadAllText(jsonFi.FullName)) 
                : null;
        }

        public static ObservableCollection<Game> GetInstalledGames(ObservableCollection<Game> gamesList) {
            var listWithModifiedPaths = Backup.ModifyGamePaths(gamesList);
            var detectedGamesList = new ObservableCollection<Game>();
            foreach (var game in listWithModifiedPaths.Where(game => Directory.Exists(game.Path))) {
                detectedGamesList.Add(game);
            }
            return detectedGamesList;
        }

        public static string FormatDisplayPath(string directory) {
            if (directory.Length <= 30) return directory;
            var dir = new DirectoryInfo(directory);
            var parent = dir.Name;
            var root = dir.Root;
            return root + "...\\" + parent;
        }
    }
}
