using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Saved_Game_Backup {   
    [Serializable]
    public class UserPrefs {

        private string _hardDrive;
        public string HardDrive {
            get { return _hardDrive; }
            set { _hardDrive = value; }
        }

        private ObservableCollection<Game> _selectedGames;
        public ObservableCollection<Game> SelectedGames {
            get { return _selectedGames; }
            set { _selectedGames = value; }
        } 

        private int _theme;
        public int Theme {
            get { return _theme; } 
            set { _theme = value; }
        }

        private int _maxBackups;
        public int MaxBackups {
            get { return _maxBackups; } 
            set { _maxBackups = value; }
        }

        public UserPrefs(){}

        public UserPrefs(int theme, int maxBackups, string hardDrive, ObservableCollection<Game> games) {
            _theme = theme;
            _maxBackups = maxBackups;
            _selectedGames = games;
            _hardDrive = hardDrive;
        }
    }
}
