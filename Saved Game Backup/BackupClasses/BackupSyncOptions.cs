using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using Saved_Game_Backup.Annotations;

namespace Saved_Game_Backup.BackupClasses {

    [Serializable]
    public class BackupSyncOptions : ObservableObject {


        private bool _syncToDropbox;
        public bool SyncToDropbox {
            get { return _syncToDropbox; }
            set {
                _syncToDropbox = value;
                RaisePropertyChanged(() => SyncToDropbox);
                SyncEnabled = value;
            }
        }
        private bool _syncEnabled;
        public bool SyncEnabled {
            get { return _syncEnabled; }
            set {
                _syncEnabled = value;
                RaisePropertyChanged(() => SyncEnabled);
            }
        }

        private bool _syncToZip;
        public bool SyncToZip {
            get { return _syncToZip; }
            set {
                _syncToZip = value;
                if (value)
                    _syncToFolder = false;
                RaisePropertyChanged(() => SyncToZip);
                RaisePropertyChanged(() => SyncToFolder);
            }
        }
        private bool _syncToFolder;
        public bool SyncToFolder {
            get { return _syncToFolder; }
            set {
                _syncToFolder = value;
                if (value)
                    _syncToZip = false;
                RaisePropertyChanged(() => SyncToZip);
                RaisePropertyChanged(() => SyncToFolder);
            }
        }

        public BackupSyncOptions() {
            SyncToDropbox = false;
        }

    }
}