﻿using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using Saved_Game_Backup.Helper;

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
            set {
                _hardDrives = value;
            }
        }

        private ObservableCollection<BackupType> _backupTypes;
        public ObservableCollection<BackupType> BackupTypes {
            get { return _backupTypes; }
            set {
                _backupTypes = value;
            }
        }

        private string _selectedHardDrive;
        public string SelectedHardDrive {
            get { return _selectedHardDrive; }
            set {
                _selectedHardDrive = value;
                Messenger.Default.Send<OptionMessage>(new OptionMessage(this));
            }
        }

        private string _specifiedFolder;
        public string SpecifiedFolder {
            get { return _specifiedFolder; }
            set {
                _specifiedFolder = value;
                Messenger.Default.Send<OptionMessage>(new OptionMessage(this));
            }
        }

        private BackupType _backupType;
        public BackupType BackupType {
            get { return _backupType; }
            set {
                _backupType = value;
                Messenger.Default.Send<OptionMessage>(new OptionMessage(this));
            }
        }

        private MainViewModel _mainView;
        public MainViewModel MainView {
            get { return _mainView; }
            set { _mainView = value; }
        }

        public RelayCommand ChooseFolder {
            get { return new RelayCommand(() => ExecuteChooseFolder());}
        }

        public RelayCommand<Window> Close {
            get{ return new RelayCommand<Window>(CloseWindow);}
        }

        [PreferredConstructor]
        public OptionsViewModel(MainViewModel main) {
            BackupTypes = new ObservableCollection<BackupType>() {
                BackupType.Autobackup,
                BackupType.ToFolder,
                BackupType.ToZip
            };
        }

        private void ExecuteChooseFolder() {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
                _specifiedFolder = dialog.SelectedPath;

        }

        private void CloseWindow(Window window) {
            if (window != null)
                window.Close();
        }

    }
}
