using System.Collections.ObjectModel;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace Saved_Game_Backup.ViewModel {
    
    public class OptionsViewModel : ViewModelBase {

        private ObservableCollection<string> _hardDrives;
        public ObservableCollection<string> HardDrives {
            get { return _hardDrives; }
            set { _hardDrives = value; }
        }

        private string _selectedHardDrive;
        public string SelectedHardDrive {
            get { return _selectedHardDrive; }
            set { _selectedHardDrive = value; }
        }
        
        public OptionsViewModel(){}



        public OptionsViewModel(MainViewModel main) { }
         
    }
}
