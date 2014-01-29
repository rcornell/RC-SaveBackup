using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Saved_Game_Backup
{
    public enum BackupType {
        ToFolder,
        ToZip,
        Autobackup
    }

    public class BackupTypeToStringConverter : IValueConverter {

        public BackupTypeToStringConverter(){}

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var returnList = new ObservableCollection<string>();
           
            foreach (var s in (ObservableCollection<BackupType>)value){
                switch (s) {
                        case BackupType.Autobackup:
                        returnList.Add("Autobackup");
                        break;
                        case BackupType.ToFolder:
                        returnList.Add("Backup to Folder");
                        break;
                        case BackupType.ToZip:
                        returnList.Add("Backup to .Zip");
                        break;
                }
            }
            return returnList;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            //BackupType backupReturn;
            //switch ((string)value) {
            //    case "Autobackup":
            //        backupReturn = BackupType.Autobackup;
            //        break;
            //    case "Backup to Folder":
            //        backupReturn = BackupType.ToFolder;
            //        break;
            //    case "Backup to .Zip":
            //        backupReturn = BackupType.ToZip;
            //        break;
            //    default:
            //        backupReturn = BackupType.ToFolder;
            //        break;
            //}
            //return backupReturn;
            return null;
        }
    }

    public class StringToBackupTypeConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            BackupType backupReturn;
            switch ((BackupType)value)
            {

                case BackupType.Autobackup:
                    backupReturn = BackupType.Autobackup;
                    break;
                case BackupType.ToFolder:
                    backupReturn = BackupType.ToFolder;
                    break;
                case BackupType.ToZip:
                    backupReturn = BackupType.ToZip;
                    break;
                default:
                    backupReturn = BackupType.ToFolder;
                    break;
                    
            }
            return backupReturn;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            BackupType backupReturn;
            switch ((string)value) {
                case "Autobackup":
                    backupReturn = BackupType.Autobackup;
                    break;
                case "Backup to Folder":
                    backupReturn = BackupType.ToFolder;
                    break;
                case "Backup to .Zip":
                    backupReturn = BackupType.ToZip;
                    break;
                default:
                    backupReturn = BackupType.ToFolder;
                    break;
            }
            return backupReturn;
        }
    }
}
