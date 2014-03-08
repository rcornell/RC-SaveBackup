using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saved_Game_Backup.BackupClasses
{
    public class BackupSyncOptions {
        public bool SyncToDropbox { get; set; }
        public bool SyncToGoogleDrive { get; set; }
        public bool SyncToSkydrive { get; set; }
        public bool ToZip { get; set; }
        public bool ToFolder { get; set; }

        public BackupSyncOptions() {
            SyncToDropbox = false;
            SyncToGoogleDrive = false;
            SyncToSkydrive = false;
            ToZip = false;
            ToFolder = false;
        }
    }
}
