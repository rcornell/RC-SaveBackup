
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Saved_Game_Backup.Helper;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

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

        private const string _about =
            "I made this program in an attempt to help people keep track of their saved games in case of catastrophe. It should work, but Autobackup can be touchy sometimes. If you have any issues please email me at rob.cornell@gmail.com.";
        public string About { get { return _about; }}

        private Visibility _autoBackupVisibility;
        public Visibility AutoBackupVisibility {
            get { return _autoBackupVisibility; }
            set {
                _autoBackupVisibility = value; 
                RaisePropertyChanged(() => AutoBackupVisibility);
            }
        }
        public Visibility BackupLimitVisibility { get; set; }

        private ObservableCollection<Game> _gamesList;
        public ObservableCollection<Game> GamesList {
            get {
                return _gamesList;
            }
            set {
                _gamesList = value;
                RaisePropertyChanged(() => GamesList);
            }
        }

        private ObservableCollection<Game> _gamesToBackup;
        public ObservableCollection<Game> GamesToBackup {
            get {
                return _gamesToBackup;
            }
            set {
                _gamesToBackup = value;
                RaisePropertyChanged(() => GamesToBackup);
            }
        }

        private ObservableCollection<string> _gameNames;
        public ObservableCollection<string> GameNames {
            get {
                return _gameNames;
            }
            set {
                _gameNames = value;
                RaisePropertyChanged(() => GameNames); 
            }
        }

        private Brush _background;
        public Brush Background {
            get { return _background; }
            set {
                _background = value; 
                RaisePropertyChanged(() => Background);
            }
        }

        private Brush _listBoxBackground;
        public Brush ListBoxBackground {
            get { return _listBoxBackground; }
            set {
                _listBoxBackground = value;
                RaisePropertyChanged(() => ListBoxBackground);
            }
        }

        private ObservableCollection<object> _brushes;
        public ObservableCollection<object> Brushes {
            get { return _brushes; }
            set {
                _brushes = value;
                RaisePropertyChanged(() => Brushes);
            }
            
        }

        private ObservableCollection<BackupType> _backupTypes;
        public ObservableCollection<BackupType> BackupTypes
        {
            get { return _backupTypes; }
            set {
                _backupTypes = value;
                RaisePropertyChanged(() => BackupTypes);
            }
        }

        private BackupType _backupType;
        public BackupType BackupType {
            get { return _backupType; }
            set {
                _backupType = value;
                BackupLimitVisibility = _backupType == BackupType.Autobackup ? Visibility.Visible : Visibility.Hidden;
                if (_backupType == BackupType.Autobackup && BackupEnabled) BackupButtonText = "Disable auto-backup.";
                if (_backupType == BackupType.Autobackup && !BackupEnabled) BackupButtonText = "Enable auto-backup.";
                if (_backupType != BackupType.Autobackup) BackupButtonText = "Backup Saves";
                RaisePropertyChanged(() => BackupLimitVisibility);
                RaisePropertyChanged(() => BackupButtonText);
                RaisePropertyChanged(() => BackupType);
            }
        }

        private string _lastBackupTime;
        public string LastBackupTime {
            get {
                return _lastBackupTime;
            }
            set {
                _lastBackupTime = value;
                RaisePropertyChanged(() => LastBackupTime);
            }
        }

        private Game _selectedGame;
        public Game SelectedGame {
            get { return _selectedGame; }
            set {
                _selectedGame = value; 
                RaisePropertyChanged(() => SelectedGame);
            }
        }

        private Game _selectedBackupGame;
        public Game SelectedBackupGame
        {
            get { return _selectedBackupGame; }
            set {
                _selectedBackupGame = value; 
                RaisePropertyChanged(() => SelectedBackupGame);
            }
        }

        private string _specifiedFolder;
        public string SpecifiedFolder {
            get { return _specifiedFolder; }
            set {
                _specifiedFolder = value; 
                RaisePropertyChanged(() => SpecifiedFolder);
            }
        }

        private string _backupButtonText;
        public string BackupButtonText {
            get {
                return _backupButtonText;
            }
            set {
                _backupButtonText = value;
                RaisePropertyChanged(() => BackupButtonText);
            }
        }

        private bool _backupEnabled;
        public bool BackupEnabled {
            get { return _backupEnabled; }
            set {
                _backupEnabled = value;
                AutoBackupVisibility = _backupEnabled ? Visibility.Visible : Visibility.Hidden;
                RaisePropertyChanged(() => BackupEnabled);
                RaisePropertyChanged(() => AutoBackupVisibility);
            } 
        }

        private int _maxBackups;
        public int MaxBackups {
            get { return _maxBackups; }
            set {
                _maxBackups = value; 
                RaisePropertyChanged(() => MaxBackups);
            }
        }

        private int _interval;
        public int Interval {
            get { return _interval; }
            set {
                _interval = value;
                RaisePropertyChanged(() => Interval);
            }
        }

        private int _themeInt;
        public int ThemeInt {
            get { return _themeInt; }
            set {
                _themeInt = value; 
                RaisePropertyChanged(() => ThemeInt);
            }
        }

        private int _numberOfBackups;
        public int NumberOfBackups {
            get { return _numberOfBackups; }
            set {
                _numberOfBackups = value; 
                RaisePropertyChanged(() => NumberOfBackups);
            }
        }

        public RelayCommand ShowAbout {
            get { return new RelayCommand(ExecuteShowAbout);}
        }
        public RelayCommand MoveToBackupList
        {
            get
            {
                return new RelayCommand(ToBackupList);
            }
        }
        public RelayCommand<Game> MoveToGamesList {
            get {
                return new RelayCommand<Game>(ToGamesList);
            }
        }    
        public RelayCommand StartBackup {
            get { return new RelayCommand(ExecuteStartBackup);}
        }
        public RelayCommand ResetList
        {
            get { return new RelayCommand(ExecuteReset); }
        }
        public RelayCommand DetectGames {
            get { return new RelayCommand(ExecuteDetectGames); }
        }
        public RelayCommand SetThemeLight {
            get { return new RelayCommand(ExecuteSetThemeLight); }
        }
        public RelayCommand SetThemeDark
        {
            get { return new RelayCommand(ExecuteSetThemeDark); }
        }
        public RelayCommand OpenAddGameWindow
        {
            get { return new RelayCommand(ExecuteOpenAddGameWindow); }
        }
        public RelayCommand Close {
            get { return new RelayCommand(CloseApplication); }
        }

        public RelayCommand TestPollAutoBackup {
            get { return new RelayCommand(PollAutobackup);}
        }

        public MainViewModel() {
            NumberOfBackups = 0;
            GamesList = DirectoryFinder.ReturnGamesList();
            GamesToBackup = new ObservableCollection<Game>();
            BackupTypes = new ObservableCollection<BackupType>() {
                BackupType.Autobackup,
                BackupType.ToFolder,
                BackupType.ToZip
            };
            BackupType = BackupType.ToFolder;
            
            BackupLimitVisibility = Visibility.Hidden;
            DirectoryFinder.CheckDirectories();
            SetUpInterface();

            try {
                Messenger.Default.Register<int>(this, i => {
                    NumberOfBackups = i;
                });
            } catch (Exception ex) {
                SBTErrorLogger.Log(ex.Message);
            }
        }

        public async void PollAutobackup() {
            var testList = Backup.ModifyGamePaths(GamesToBackup); //REMOVE AFTER TESTING
           await Backup.SetupPollAutobackup(false, 10000, testList);
        }

        ///Currently crashing designer.
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
                LastBackupTime = prefs.LastBackupTime;
                if (prefs.SelectedGames != null) GamesToBackup = prefs.SelectedGames;
            }
            Brushes = Theme.ToggleTheme(_themeInt);
            AutoBackupVisibility = Visibility.Hidden;
        }

        private void SaveUserPrefs() {
            var p = new PrefSaver();
            p.SavePrefs(new UserPrefs(_themeInt, _maxBackups, GamesToBackup, LastBackupTime));
        }

        private void ExecuteDetectGames() {
            GamesToBackup = DirectoryFinder.PollDirectories(GamesList);
        }

        private async void ToBackupList() {
            if (_selectedGame == null)
                return;
            var game = SelectedGame;
            if (GamesToBackup.Contains(game)) return;
            GamesToBackup.Add(game);

            RaisePropertyChanged(() => GamesToBackup);            

            if (!game.ThumbnailPath.Contains("Loading")) return;
            await GamesDBAPI.GetThumb(game);
        }

        private void ToGamesList(Game game) {
            if (game == null || !GamesToBackup.Contains(game))
                return;
            var gameToMove = game;
    
            if (BackupEnabled) {
                var result = Backup.RemoveFromAutobackup(gameToMove);
                GamesToBackup.Remove(gameToMove);
                RaisePropertyChanged(() => GamesToBackup);
                HandleBackupResult(result);
            }
            if (!GamesToBackup.Contains(gameToMove)) return;
            GamesToBackup.Remove(gameToMove);
            RaisePropertyChanged(() => GamesToBackup);
        }

        private void UpdateGamesList() {
            GamesList = DirectoryFinder.ReturnGamesList();
            foreach (var game in GamesToBackup) {
                SelectedGame = game;
                ToGamesList(SelectedGame);
            }
            RaisePropertyChanged(() => GamesList);
        }

        private void ExecuteStartBackup() {
            var result = Backup.StartBackup(GamesToBackup.ToList(), BackupType, BackupEnabled).Result;
            HandleBackupResult(result);
        }

        private void HandleBackupResult(BackupResultHelper result) {
            if (!result.Success) { 
                MessageBox.Show(result.Message, @"Operation failed", MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }
            BackupEnabled = result.AutobackupEnabled;
            BackupButtonText = result.BackupButtonText;

            if (!result.AutobackupEnabled && BackupType != BackupType.Autobackup) LastBackupTime = result.BackupDateTime;

            if (string.IsNullOrWhiteSpace(result.Message)) return;
            MessageBox.Show(result.Message, @"Operation successful", MessageBoxButton.OK);
        }

        private void ExecuteReset() {
            GamesToBackup.Clear();
            SpecifiedFolder = null;
            SelectedGame = null;
            SelectedBackupGame = null;

            var result = Backup.Reset(GamesToBackup.ToList(), BackupType, BackupEnabled);
            HandleBackupResult(result);
        }

        private void ExecuteSetThemeLight() {
            _themeInt = 0;
            Brushes = Theme.ToggleTheme(_themeInt);
        }
        
        private void ExecuteSetThemeDark() {
            _themeInt = 1;
            Brushes = Theme.ToggleTheme(_themeInt);
        }

        private void CloseApplication() {
            SaveUserPrefs();
            if (Application.Current != null)
                Application.Current.Shutdown();
        }      

        private async void ExecuteOpenAddGameWindow() {
            Game newGameForJson = null;
            Messenger.Default.Register<AddGameMessage>(this, g => {
                newGameForJson = g.Game;
            });
            var addGameWindow = new AddGameWindow();
            addGameWindow.ShowDialog();

            if (newGameForJson == null) return;
            await GiantBombAPI.AddToJson(newGameForJson);
            var msg = string.Format("{0} added to list.", newGameForJson.Name);
            MessageBox.Show(msg);
            UpdateGamesList();
        }

        private void ExecuteShowAbout() {
            MessageBox.Show(About, "About SaveMonkey", MessageBoxButton.OK);
        }
    }
}