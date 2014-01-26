using System;
using System.Collections.ObjectModel;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace Saved_Game_Backup.ViewModel {
    
    public class OptionsViewModel : ViewModelBase {



        private Brush _background;
        public Brush Background { 
            get { return _background; } 
            set { _background = value; }
        }

        private ObservableCollection<string> _hardDrives;
        public ObservableCollection<string> HardDrives {
            get { return _hardDrives; }
            set { _hardDrives = value; }
        }

        private ObservableCollection<BackupType> _backupTypes;
        public ObservableCollection<BackupType> BackupTypes {
            get { return _backupTypes; }
            set { _backupTypes = value; }
        }

        private string _selectedHardDrive;
        public string SelectedHardDrive {
            get { return _selectedHardDrive; }
            set { _selectedHardDrive = value; }
        }

        private string _specifiedFolder;
        public string SpecifiedFolder {
            get { return _specifiedFolder; }
            set { _specifiedFolder = value; }
        }

        private BackupType _backupType;
        public BackupType BackupType {
            get { return _backupType; }
            set { _backupType = value; }
        }

        public RelayCommand ChooseFolder {
            get { return new RelayCommand(() => ExecuteChooseFolder());}
        }

        private MainViewModel _mainView;

        public OptionsViewModel(){}

        public OptionsViewModel(MainViewModel main) {
            _background = main.Background;
        }

        private void ExecuteChooseFolder() {
            var path= Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
            var dialog = new FolderBrowserDialog();
            dialog.RootFolder = path;
        }
    }
}
