using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Saved_Game_Backup.ViewModel;

namespace Saved_Game_Backup.Helper
{
    public class OptionMessage {
        public string HardDrive;
        public BackupType BackupType;
        public string SpecifiedFolder;

        public OptionMessage(string hardDrive, BackupType backup, string folder = null) {
            HardDrive = hardDrive;
            BackupType = backup;
            SpecifiedFolder = folder;

        }

        public OptionMessage(OptionsViewModel vm)
        {
            HardDrive = vm.SelectedHardDrive;
            BackupType = vm.BackupType;
            SpecifiedFolder = vm.SpecifiedFolder;

        }
    }
}
