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

        private Dictionary<string, string> _gameSaveDirectories;
        public Dictionary<string, string> GameSaveDirectories {
            get
            {
                return _gameSaveDirectories;
            }
            set
            {
                _gameSaveDirectories = value;
            }
        }

        private static string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static string _sbtPath;

        static DirectoryFinder() {
             
        }

        public static void CheckDirectories() {
            _sbtPath = documentsPath + "\\Save Backup Tool\\";
            if (!Directory.Exists(_sbtPath + "Thumbnails\\"))
                Directory.CreateDirectory(_sbtPath+"Thumbnails\\");
            if (!Directory.Exists(_sbtPath + "Error\\"))
                Directory.CreateDirectory(_sbtPath + "Error\\");
        }

        public static ObservableCollection<Game> ReturnGamesList() {
            var jsonFi = new FileInfo(@"Database\Games.json");
            return jsonFi.Exists 
                ? 
                JsonConvert.DeserializeObject<ObservableCollection<Game>>(File.ReadAllText(jsonFi.FullName)) 
                : null;
        }

        //private void GenerateDictionary()
        //{
        //    _gameSaveDirectories.Add("Terraria", @"C:\Users\Rob\Documents\My Games\Terraria");
        //}

        

        /// <summary>
        /// Reads the json file and returns the list of games.
        /// </summary>
        /// <returns></returns>
        //public static ObservableCollection<Game> ReturnGamesList() {
        //    var gamesList = JsonConvert.DeserializeObject<ObservableCollection<Game>>(File.ReadAllText(_sbtPath)+"Games.json");

        //    return gamesList;
        //}

        /// <summary>
        /// Opens a folder browser dialog and sets the specifiedFolder string to the chosen path.
        /// </summary>
        /// <returns></returns>
        public static string SpecifyFolder() {
            var dialog = new FolderBrowserDialog() { Description = "Select a folder to save your backups."};
            dialog.ShowDialog();
            return dialog.SelectedPath;
        }

        public static ObservableCollection<Game> PollDirectories(string hardDrive, ObservableCollection<Game> gamesList) {
            var detectedGamesList = new ObservableCollection<Game>();
            foreach (Game game in gamesList) {
                if(Directory.Exists(game.Path))
                    detectedGamesList.Add(game);
            }
            return detectedGamesList;
        }
    }
}
