using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Saved_Game_Backup.BackupClasses;

namespace Saved_Game_Backup.Preferences {   
    [Serializable]
    public class UserPrefs {

        private BackupSyncOptions _backupSyncOptions;
        public BackupSyncOptions BackupSyncOptions {
            get { return _backupSyncOptions; }
            set {
                _backupSyncOptions = value;
            }
        }

        private ObservableCollection<Game> _selectedGames;
        public ObservableCollection<Game> SelectedGames {
            get { return _selectedGames; }
            set { _selectedGames = value; }
        } 

        private int _maxBackups;
        public int MaxBackups {
            get { return _maxBackups; } 
            set { _maxBackups = value; }
        }

        public string UserSecret { get; set; }
        public string UserToken { get; set; }
        public string LastBackupTime;
        public string ThemeName;

        public static UserPrefs GetDefaultPrefs() {
            var prefs = new UserPrefs() {
                SelectedGames = new ObservableCollection<Game>(),
                MaxBackups = 5,
                BackupSyncOptions = new BackupSyncOptions(),
                ThemeName = @"DarkStyle.xaml"
            };
            return prefs;
        }

    }
}
