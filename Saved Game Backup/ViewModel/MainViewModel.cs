using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.RightsManagement;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using Saved_Game_Backup.Helper;

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


        //TEST!!!
        public ObservableCollection<WrapPanelGame> WrapPanelGamesList { get; set; }

        public RelayCommand AddPanelGame {
            get { return new RelayCommand(() => ExecuteAddPanelGame());}
        }

        public void ExecuteAddPanelGame() {
            WrapPanelGamesList = new ObservableCollection<WrapPanelGame> { new WrapPanelGame() { Game = GamesList[0]} };
            RaisePropertyChanged(() => WrapPanelGamesList);
        }


        private Visibility _autoBackupVisibility;
        public Visibility AutoBackupVisibility {
            get { return _autoBackupVisibility; }
            set { _autoBackupVisibility = value; }
        }
        public Visibility BackupLimitVisibility { get; set; }
       
        public ObservableCollection<string> HardDrives { get; set; } 
        public ObservableCollection<Game> GamesList { get; set; } 
        public ObservableCollection<Game> GamesToBackup { get; set; }
        public ObservableCollection<string> GameNames { get; set; }

        private Brush _background;
        public Brush Background {
            get { return _background; }
            set { _background = value; }
        }
        private Brush _listBoxBackground;
        public Brush ListBoxBackground {
            get { return _listBoxBackground; }
            set { _listBoxBackground = value; }
        }
        public ObservableCollection<object> Brushes { get; set; }

        private ObservableCollection<BackupType> _backupTypes;
        public ObservableCollection<BackupType> BackupTypes
        {
            get { return _backupTypes; }
            set
            {
                _backupTypes = value;
            }
        }
        private BackupType _backupType;
        public BackupType BackupType {
            get { return _backupType; }
            set {
                _backupType = value;
                BackupLimitVisibility = _backupType == BackupType.Autobackup ? Visibility.Visible : Visibility.Hidden;
                RaisePropertyChanged(() => BackupLimitVisibility);
            }
        }
        public DateTime LastBackupTime { get; set; }

        //private string _selectedHardDrive;
        //public string SelectedHardDrive {
        //    get { return _selectedHardDrive; }
        //    set { _selectedHardDrive = value; }
        //}
        private Game _selectedGame;
        public Game SelectedGame
        {
            get { return _selectedGame; }
            set { _selectedGame = value; }
        }
        private Game _selectedBackupGame;
        public Game SelectedBackupGame
        {
            get { return _selectedBackupGame; }
            set { _selectedBackupGame = value; }
        }
        private string _specifiedFolder;
        public string SpecifiedFolder {
            get { return _specifiedFolder; }
            set { _specifiedFolder = value; }
        }
        private bool _backupEnabled;
        private int _maxBackups;
        public int MaxBackups {
            get { return _maxBackups; }
            set { _maxBackups = value; }
        }
        private int _themeInt;
        public int ThemeInt {
            get { return _themeInt; }
            set { _themeInt = value; }
        }
        

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
        public RelayCommand StartBackup {
            get { return new RelayCommand(() => ExecuteStartBackup());}
        }
        public RelayCommand ResetList
        {
            get { return new RelayCommand(() => ExecuteReset()); }
        }
        public RelayCommand DetectGames {
            get { return new RelayCommand(() => ExecuteDetectGames()); }
        }
        public RelayCommand SetThemeLight
        {
            get { return new RelayCommand(() => ExecuteSetThemeLight()); }
        }
        public RelayCommand SetThemeDark
        {
            get { return new RelayCommand(() => ExecuteSetThemeDark()); }
        }
        //public RelayCommand OpenOptionsWindow {
        //    get { return new RelayCommand(() => ExecuteOpenOptionsWindow());}
        //}
        public RelayCommand OpenAddGameWindow
        {
            get { return new RelayCommand(() => ExecuteOpenAddGameWindow()); }
        }
        public RelayCommand Close {
            get { return new RelayCommand(() => CloseApplication()); }
        }

        public MainViewModel() {
            HardDrives = DirectoryFinder.CreateHardDriveCollection();
            GamesList = DirectoryFinder.ReturnGamesList();
            GamesToBackup = new ObservableCollection<Game>();
            BackupTypes = new ObservableCollection<BackupType>() {
                BackupType.Autobackup,
                BackupType.ToFolder,
                BackupType.ToZip
            };
            BackupLimitVisibility = Visibility.Hidden;
            DirectoryFinder.CheckDirectories();
            SetUpInterface();

            //Messenger.Default.Register<OptionMessage>(this, s => {
            //        BackupType = s.BackupType;
            //    if (s.HardDrive != null)
            //        SelectedHardDrive = s.HardDrive;
            //    if (s.SpecifiedFolder != null)
            //        SpecifiedFolder = s.SpecifiedFolder;
            //});

          
        }

        //~MainViewModel() {
        //  CloseApplication();
        //}

        private void SetUpInterface() {
            if (!PrefSaver.CheckForPrefs()) {
                _maxBackups = 5;
                _themeInt = 0;
            }
            else {
                var p = new PrefSaver();
                var prefs = p.LoadPrefs();
                _maxBackups = prefs.MaxBackups;
                _themeInt = prefs.Theme;
                GamesToBackup = prefs.SelectedGames;
                LastBackupTime = prefs.LastBackupTime;
               

                var listToRemove = new ObservableCollection<Game>();
                foreach (Game game in prefs.SelectedGames) {
                    foreach (Game g in GamesList) {
                        if (game.Name == g.Name)
                            listToRemove.Add(g);
                    }
                    foreach (Game gameBeingRemoved in listToRemove)
                        GamesList.Remove(gameBeingRemoved);
                }
                RaisePropertyChanged(() => GamesList);
            }
            Brushes = Theme.ToggleTheme(_themeInt);
            RaisePropertyChanged(() => Brushes);
            AutoBackupVisibility = Visibility.Hidden;
            RaisePropertyChanged(() => AutoBackupVisibility);
            RaisePropertyChanged(() => LastBackupTime);
        }

        private void SaveUserPrefs() {
            var p = new PrefSaver();
            p.SavePrefs(new UserPrefs(_themeInt, _maxBackups, GamesToBackup, LastBackupTime));
        }

        private void ExecuteDetectGames() {         
            GamesToBackup = DirectoryFinder.PollDirectories(GamesList);
            foreach (Game game in GamesToBackup)
                GamesList.Remove(game);

            RaisePropertyChanged(() => GamesToBackup);
            RaisePropertyChanged(() => GamesList);
        
        }

        private async void ToBackupList() {
            if (_selectedGame == null)
                return;
            var game = SelectedGame;
            GameListHandler.RemoveFromGamesList(GamesList, game);
            RaisePropertyChanged(() => GamesList);
            await GameListHandler.AddToBackupList(GamesToBackup, GamesList, game);
            RaisePropertyChanged(() => GamesToBackup);
            await GetThumb(game);
            RaisePropertyChanged(() => GamesToBackup);
        }

        private void ToGamesList() {
            if (_selectedBackupGame == null)
                return;

            var game = SelectedBackupGame;
            GameListHandler.RemoveFromBackupList(GamesToBackup, game);
            GameListHandler.AddToGamesList(GamesToBackup, GamesList, game);
            GamesList = new ObservableCollection<Game>(GamesList.OrderBy(s=> s.Name));
            RaisePropertyChanged(() => GamesList);
            RaisePropertyChanged(() => GamesToBackup);
        }

        private void ExecuteReset() {
            GamesList = DirectoryFinder.ReturnGamesList();
            GamesToBackup.Clear();
            _specifiedFolder = null;
            _selectedGame = null;
            _selectedBackupGame = null;

            RaisePropertyChanged(() => GamesList);
            RaisePropertyChanged(() => GamesToBackup);
            RaisePropertyChanged(() => SelectedBackupGame);
            RaisePropertyChanged(() => SelectedGame);
        }

        private void ExecuteSetThemeLight() {
            _themeInt = 0;
            Brushes = Theme.ToggleTheme(_themeInt);
            RaisePropertyChanged(() => Brushes);
        }
        
        private void ExecuteSetThemeDark() {
            _themeInt = 1;
            Brushes = Theme.ToggleTheme(_themeInt);
            RaisePropertyChanged(() => Brushes);
        }

        private void CloseApplication() {
            SaveUserPrefs();
            if (Application.Current != null)
                Application.Current.Shutdown();
        }

        private async Task GetThumb(Game game) {
            var gb = new GiantBombAPI(game);
            await gb.GetThumb(game);
            game.ThumbnailPath = gb.ThumbnailPath;
        }

        private void ExecuteStartBackup() {
            var success = Backup.StartBackup(GamesToBackup, BackupType, _backupEnabled);
            if (!success.Success) return;
            _backupEnabled = success.AutobackupEnabled;
            AutoBackupVisibility = _backupEnabled ? Visibility.Visible : Visibility.Hidden;
            LastBackupTime = success.BackupDateTime;
            RaisePropertyChanged(() => AutoBackupVisibility);
            RaisePropertyChanged(() => LastBackupTime);
            MessageBox.Show(success.Message);
        }

        private async void ExecuteOpenAddGameWindow() {
            Game newGameForJson = null;
            Messenger.Default.Register<AddGameMessage>(this, g => {
                newGameForJson = g.Game;
            });
            var addGameWindow = new AddGameWindow();
            addGameWindow.ShowDialog();

            if (newGameForJson == null) return;
            var gb = new GiantBombAPI();
            await gb.AddToJSON(newGameForJson);
            MessageBox.Show(newGameForJson.Name + " added to list.");
            UpdateGamesList();
        }

        private void UpdateGamesList() {
            GamesList = DirectoryFinder.ReturnGamesList();
            foreach (var game in GamesToBackup) {
                SelectedGame = game;
                ToGamesList();
            }
            RaisePropertyChanged(() => GamesList);
        }

        //Options window not used anymore.
        //private void ExecuteOpenOptionsWindow() {
        //    var optionsWindow = new OptionsWindow();
        //    optionsWindow.Show();
        //}
    }
}