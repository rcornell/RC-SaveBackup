using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.RightsManagement;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Documents;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace Saved_Game_Backup.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase {
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>

        public ObservableCollection<string> HardDrives { get; set; } 
        public ObservableCollection<Game> GamesList { get; set; } 
        public ObservableCollection<Game> GamesToBackup { get; set; }
        public ObservableCollection<string> GameNames { get; set; } 

        private string _selectedHardDrive;
        public string SelectedHardDrive {
            get { return _selectedHardDrive; }
            set { _selectedHardDrive = value; }
        }
        private string _selectedGame;
        public string SelectedGame
        {
            get { return _selectedGame; }
            set { _selectedGame = value; }
        }
        private string _selectedBackupGame;
        public string SelectedBackupGame
        {
            get { return _selectedBackupGame; }
            set { _selectedBackupGame = value; }
        }
        private string _specifiedFolder;

        public RelayCommand MoveToBackupList
        {
            get
            {
                return new RelayCommand(() => ToBackupList());
            }
        }
        public RelayCommand MoveToGamesList
        {
            get
            {
                return new RelayCommand(() => ToGamesList());
            }
        }
        public RelayCommand BackupSaves {
            get { return new RelayCommand(() => ExecuteBackup());}
        }
        public RelayCommand BackupAndZip {
            get { return new RelayCommand(() => ExecuteBackupAndZip()); }
        }
        public RelayCommand ResetList
        {
            get { return new RelayCommand(() => ExecuteReset()); }
        }
        public RelayCommand SpecifyFolder {
            get {return new RelayCommand(() => ExecuteSpecifyFolder());}
        }

        private void ExecuteSpecifyFolder() {
           _specifiedFolder = DirectoryFinder.SpecifyFolder();
        }

        public MainViewModel() {

            HardDrives = new ObservableCollection<string>();
            GamesList = DirectoryFinder.ReturnGamesList();
            GamesToBackup = new ObservableCollection<Game>();
                
            CreateHardDriveCollection();
        }

        private void CreateHardDriveCollection() {
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in drives) {
                HardDrives.Add(drive.RootDirectory.ToString());
            }
        }

        private void ToBackupList() {
            Game game = null;
            for (int i = 0; i < GamesList.Count(); i++) {
                if (_selectedGame == GamesList[i].Name) {
                    game = GamesList[i];
                    break;
                }
            }

            if (game != null) {
                GamesList.Remove(game);
                RaisePropertyChanged(() => GamesList);
                GamesToBackup.Add(game);
                RaisePropertyChanged(() => GamesToBackup);
            }
        }

        private void ToGamesList()
        {
            Game game = null;
            for (int i = 0; i < GamesToBackup.Count(); i++) {
                if (_selectedBackupGame == GamesToBackup[i].Name) {
                    game = GamesToBackup[i];
                    break;
                }
            }

            if (game != null) {
                GamesToBackup.Remove(game);
                RaisePropertyChanged(() => GamesToBackup);
                GamesList.Add(game);
                RaisePropertyChanged(() => GamesList);
            }
        }

        private void ExecuteBackup() {
            Backup.BackupSaves(GamesToBackup, SelectedHardDrive, _specifiedFolder);
            MessageBox.Show("Backup folders created in " + _selectedHardDrive + "SaveBackups.");
        }
        
        private void ExecuteBackupAndZip() {
            Backup.BackupAndZip(GamesToBackup, SelectedHardDrive, _specifiedFolder);
            MessageBox.Show("Saved games successfully backed up. \r\n");
        }

        private void ExecuteReset() {
            GamesList = DirectoryFinder.ReturnGamesList();
            GamesToBackup.Clear();
            _specifiedFolder = null;
            _selectedHardDrive = null;
            _selectedGame = null;
            _selectedBackupGame = null;

            RaisePropertyChanged(() => GamesList);
            RaisePropertyChanged(() => GamesToBackup);
            RaisePropertyChanged(() => SelectedHardDrive);
            RaisePropertyChanged(() => SelectedBackupGame);
            RaisePropertyChanged(() => SelectedGame);
        }

    }
}