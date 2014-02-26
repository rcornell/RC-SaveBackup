using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saved_Game_Backup.Helper
{
    public class BackupResultHelper {

        public bool Success { get; set; }
        public bool AutobackupEnabled { get; set; }
        public string Message { get; set; }
        public string BackupDateTime { get; set; }
        public string BackupButtonText { get; set; }
        
        public BackupResultHelper(){}

        public BackupResultHelper(bool success, bool autobackupEnabled, string message, string dateTime, string backupButtonText) {
            Success = success;
            AutobackupEnabled = autobackupEnabled;
            Message = message;
            BackupDateTime = dateTime;
            BackupButtonText = backupButtonText;
        }
    }
}
