using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Saved_Game_Backup.Annotations;

namespace Saved_Game_Backup.BackupClasses {
    [Serializable]
    public class BackupSyncOptions : INotifyPropertyChanged {
        private bool _syncToDropbox;
        public bool SyncToDropbox {
            get { return _syncToDropbox; }
            set {
                _syncToDropbox = value;
                OnPropertyChanged(@"SyncToDropbox");
                SyncEnabled = value;
            }
        }
        private bool _syncEnabled;
        public bool SyncEnabled {
            get { return _syncEnabled; }
            set {
                _syncEnabled = value;
                OnPropertyChanged(@"SyncEnabled");
            }
        }

        public bool SyncToGoogleDrive { get; set; }
        public bool SyncToSkydrive { get; set; }

        private bool _syncToZip;
        public bool SyncToZip {
            get { return _syncToZip; }
            set {
                _syncToZip = value;
                _syncToFolder = !value;
                OnPropertyChanged(@"SyncToZip");
                OnPropertyChanged(@"SyncToFolder");
            }
        }
        private bool _syncToFolder;
        public bool SyncToFolder {
            get { return _syncToFolder; }
            set {
                _syncToFolder = value;
                _syncToZip = !value;
                OnPropertyChanged(@"SyncToZip");
                OnPropertyChanged(@"SyncToFolder");
            }
        }

        public BackupSyncOptions() {
            SyncToDropbox = false;
            SyncToGoogleDrive = false;
            SyncToSkydrive = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}