using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

    public class AddGameViewModel {

        private Brush _background;
        public Brush Background
        {
            get { return _background; }
            set { _background = value; }
        }

        private string _path;
        public string Path {
            get { return _path; }
            set {
                _path = value; 
            }
        }

        private string _name;
        public string Name {
            get { return _name; }
            set { _name = value; }
        }

        public RelayCommand<Window> CloseWindowCommand { get; private set; }

        public RelayCommand ChoosePath {
            get { return new RelayCommand(() => ExecuteChoosePath());}
        }

        public RelayCommand<Window> Add {
            get { return new RelayCommand<Window>(ExecuteAdd); }
        }

      
        public AddGameViewModel(MainViewModel mainview) {
            Background = mainview.Background;
        }

        private void ExecuteChoosePath() {
            var fb = new FolderBrowserDialog();
            if (fb.ShowDialog() == DialogResult.OK)
                Path = fb.SelectedPath;
        }

        private void ExecuteAdd(Window window) {
            if (!string.IsNullOrWhiteSpace(_name) && !string.IsNullOrWhiteSpace(_path)) {
                var gb = new GiantBombAPI();
                gb.AddToJSON(_name, _path);
            }
            if(window!=null)
                window.Close(); 
        }
    }
}