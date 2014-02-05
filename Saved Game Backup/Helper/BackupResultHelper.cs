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
        public DateTime BackupDateTime { get; set; }

        public BackupResultHelper(bool success, bool autobackupEnabled, string message, DateTime dateTime) {
            Success = success;
            AutobackupEnabled = autobackupEnabled;
            Message = message;
            BackupDateTime = dateTime;
        }

        public BackupResultHelper(bool success, bool autobackupEnabled, string message)
        {
            Success = success;
            AutobackupEnabled = autobackupEnabled;
            Message = message;
        }
    }
}
