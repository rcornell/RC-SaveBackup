using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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

    public class AddGameViewModel : ViewModelBase {

        public Visibility PathCheckVisibility { get; set; }
        public Visibility NameCheckVisibility { get; set; }
        private const string defaultThumbnailPath = "pack://application:,,,/Assets/Loading.jpg";

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
                PathCheckVisibility = !string.IsNullOrWhiteSpace(_path) ? Visibility.Visible : Visibility.Hidden;
                RaisePropertyChanged(() => PathCheckVisibility);
            }
        }

        private string _name;
        public string Name {
            get { return _name; }
            set {
                _name = value;
                NameCheckVisibility = !string.IsNullOrWhiteSpace(_name) ? Visibility.Visible : Visibility.Hidden;
                RaisePropertyChanged(() => NameCheckVisibility);
            }
        }

        public RelayCommand<Window> CloseWindowCommand { get; private set; }

        public RelayCommand ChoosePath {
            get { return new RelayCommand(() => ExecuteChoosePath());}
        }

        public RelayCommand<Window> Add {
            get { return new RelayCommand<Window>(ExecuteAdd); }
        }

      
        public AddGameViewModel(MainViewModel mainview) {
            Background = mainview.Theme.BackgroundImage;
            NameCheckVisibility = Visibility.Hidden;
            PathCheckVisibility = Visibility.Hidden;
        }

        private void ExecuteChoosePath() {
            var fb = new FolderBrowserDialog();
            if (fb.ShowDialog() == DialogResult.OK)
                Path = fb.SelectedPath;
        }

        private void ExecuteAdd(Window window) {
            if (!string.IsNullOrWhiteSpace(_name) && !string.IsNullOrWhiteSpace(_path)) {
                var fi = new DirectoryInfo(_path);
                var newGame = new Game(_name, _path, 999999, defaultThumbnailPath, true, false, fi.Name);
                Messenger.Default.Send<AddGameMessage>(new AddGameMessage(newGame));
                //var gb = new GiantBombAPI();
                //await gb.AddToJSON(_name, _path);
                
            }
            if(window!=null)
                window.Close(); 
        }
    }
}