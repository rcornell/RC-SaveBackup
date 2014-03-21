
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Timers;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
using Saved_Game_Backup.BackupClasses;
using Saved_Game_Backup.Helper;
using Saved_Game_Backup.OnlineStorage;
using Saved_Game_Backup.Preferences;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using Timer = System.Timers.Timer;

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
    
        public string About { get { return "I made this program in an attempt to help people keep track of their saved games in case of catastrophe. It should work, but Autobackup can be touchy sometimes. If you have any issues please email me at rob.cornell@gmail.com."; }}

        private TimeSpan _span;
        public TimeSpan Span {
            get { return _span; }
            set {
                _span = value;
                RaisePropertyChanged(() => Span);
                
            }
        }

        private Visibility _autoBackupVisibility;
        public Visibility AutoBackupVisibility {
            get { return _autoBackupVisibility; }
            set {
                _autoBackupVisibility = value; 
                RaisePropertyChanged(() => AutoBackupVisibility);
            }
        }

        private Visibility _backupEnabledVisibility;
        public Visibility BackupEnabledVisibility {
            get { return _backupEnabledVisibility; }
            set {
                _backupEnabledVisibility = value;
                RaisePropertyChanged(() => BackupEnabledVisibility);
            }
            
        }

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
        private ObservableCollection<object> _brushes;
        public ObservableCollection<object> Brushes {
            get { return _brushes; }
            set {
                _brushes = value;
                RaisePropertyChanged(() => Brushes);
            }
            
        }
        private ObservableCollection<BackupType> _backupTypes;
        public ObservableCollection<BackupType> BackupTypes {
            get { return _backupTypes; }
            set {
                _backupTypes = value;
                RaisePropertyChanged(() => BackupTypes);
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

        public Theme Theme { get; set; }

        //private Brush _background;
        //public Brush Background {
        //    get { return _background; }
        //    set {
        //        _background = value; 
        //        RaisePropertyChanged(() => Background);
        //    }
        //}
        //private Brush _listBoxBackground;
        //public Brush ListBoxBackground {
        //    get { return _listBoxBackground; }
        //    set {
        //        _listBoxBackground = value;
        //        RaisePropertyChanged(() => ListBoxBackground);
        //    }
        //}

        private BackupType _backupType;
        public BackupType BackupType {
            get { return _backupType; }
            set {
                _backupType = value;
                AutoBackupVisibility = _backupType == BackupType.Autobackup ? Visibility.Visible : Visibility.Hidden;
                if (_backupType == BackupType.Autobackup && BackupEnabled) BackupButtonText = "Disable auto-backup.";
                else if (_backupType == BackupType.Autobackup && !BackupEnabled) BackupButtonText = "Enable auto-backup.";
                else if (_backupType == BackupType.ToFolder) BackupButtonText = "Backup to folder";
                else BackupButtonText = "Backup to .zip";
                RaisePropertyChanged(() => AutoBackupVisibility);
                RaisePropertyChanged(() => BackupButtonText);
                RaisePropertyChanged(() => BackupType);
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
        private string _displaySpecifiedFolder;
        public string DisplaySpecifiedFolder {
            get { return _displaySpecifiedFolder; }
            set {
                if (value == null) return;
                _displaySpecifiedFolder = DirectoryFinder.FormatDisplayPath(value);
                RaisePropertyChanged(() => DisplaySpecifiedFolder);
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
                BackupEnabledVisibility = _backupEnabled ? Visibility.Visible : Visibility.Hidden;
                RaisePropertyChanged(() => BackupEnabled);
                RaisePropertyChanged(() => BackupEnabledVisibility);
            } 
        }    

        private BackupSyncOptions _backupSyncOptions;
        public BackupSyncOptions BackupSyncOptions {
            get { return _backupSyncOptions; }
            set {
                _backupSyncOptions = value;
                RaisePropertyChanged(() => BackupSyncOptions);
            }
        }

        private double _percentComplete;
        public double PercentComplete {
            get { return _percentComplete; }
            set {
                _percentComplete = value;
                RaisePropertyChanged(() => PercentComplete);
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
                Span = new TimeSpan(0, 0, _interval, 0);
                if (BackupEnabled)
                    BackupAuto.ChangeInterval(Interval);
                RaisePropertyChanged(() => Interval);
                RaisePropertyChanged(() => Span);
            }
        }      

        public RelayCommand ShowAbout {
            get { return new RelayCommand(ExecuteShowAbout);}
        }
        public RelayCommand MoveToBackupList
        {
            get
            {
                return new RelayCommand(AddGameToBackupList);
            }
        }
        public RelayCommand<Game> MoveToGamesList {
            get {
                return new RelayCommand<Game>(RemoveGameFromBackupList);
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

        public GamesDBAPI GamesDbApi;

        //TEST COMMAND & METHOD
        public RelayCommand DropBoxTest {
            get { return new RelayCommand(ExecuteDropBoxTest);}
        }
        public async void ExecuteDropBoxTest() {
            var drop = SingletonHelper.DropBoxAPI;
            await drop.Initialize();
            Debug.WriteLine(@"Creating and uploading zip file");     
            if (File.Exists(@"C:\Users\Rob\Desktop\Saves.zip")) File.Delete(@"C:\Users\Rob\Desktop\Saves.zip");
            ZipFile.CreateFromDirectory(@"C:\Users\Rob\Desktop\SBTTest", @"C:\Users\Rob\Desktop\Saves.zip");
            var file = new FileInfo(@"C:\Users\Rob\Desktop\Saves.zip");
            //await drop.Upload("/", file);
            //drop.CheckForSaveFile();
            //await drop.DeleteFile(@"/SaveMonkey/Saves.zip"); 
            Debug.WriteLine(@"Zip uploaded");
        }

        public MainViewModel() {
            GamesDbApi = SingletonHelper.GamesDBAPI;
            BackupSyncOptions = new BackupSyncOptions();
            PercentComplete = 0;
            NumberOfBackups = 0;
            Interval = 5;
            Span = new TimeSpan(0,0,Interval,0); //Must always be initialized after Interval
            BackupEnabledVisibility = Visibility.Hidden;
            GamesList = DirectoryFinder.GetGamesList();
            GamesToBackup = new ObservableCollection<Game>();
            BackupTypes = new ObservableCollection<BackupType>() {
                BackupType.Autobackup,
                BackupType.ToFolder,
                BackupType.ToZip
            };
            BackupType = BackupType.ToFolder;
            DirectoryFinder.CreateSbtDirectories();
            SetUpInterface();
            RegisterAll();
            
        }

        private void RegisterAll() {
            //Updates autobackup folder in ui
            Messenger.Default.Register<FolderHelper>(this, h => DisplaySpecifiedFolder = h.FolderPath);

            //Updates progress bar
            Messenger.Default.Register<ProgressHelper>(this, p => {
                PercentComplete = p.PercentComplete;
                Debug.WriteLine(@"Percent complete is {0}%", PercentComplete * 100);
            });

            //Updates countdown clock
            Messenger.Default.Register<TimeSpan>(this, s => {
                Span = Span.Subtract(s);
            });

            //Updates number of autobackup events
            try {
                Messenger.Default.Register<int>(this, i => {
                    NumberOfBackups = i;
                });
            } catch (Exception ex) {
                SBTErrorLogger.Log(ex.Message);
            }

            //Updates last backup time
            Messenger.Default.Register<string>(this, t => {
                LastBackupTime = t;
            });
        }

        ///Currently crashing designer.
        //~MainViewModel() {
        //  CloseApplication();
        //}

        private void SetUpInterface() {
            var prefSaver = new PrefSaver();
            var prefs = prefSaver.CheckForPrefs() ? prefSaver.LoadPrefs() : UserPrefs.GetDefaultPrefs();
            MaxBackups = prefs.MaxBackups;
            ThemeInt = prefs.Theme;
            LastBackupTime = prefs.LastBackupTime;
            BackupSyncOptions = prefs.BackupSyncOptions ?? new BackupSyncOptions();
            GamesToBackup = prefs.SelectedGames ?? new ObservableCollection<Game>();
            var themeSelector = new ThemeSelector();
            Theme = themeSelector.ToggleTheme(ThemeInt);
            AutoBackupVisibility = Visibility.Hidden;
        }

        private void SaveUserPrefs() {         
            var prefSaver = new PrefSaver();
            if (prefSaver.CheckForPrefs()) {
                var prefs = prefSaver.LoadPrefs();
                prefs.MaxBackups = MaxBackups;
                prefs.Theme = ThemeInt;
                prefs.SelectedGames = GamesToBackup;
                prefs.LastBackupTime = LastBackupTime;
                prefs.BackupSyncOptions = BackupSyncOptions;
                prefSaver.SavePrefs(prefs);
                return;
            }
            var newPrefs = new UserPrefs() {
                BackupSyncOptions = BackupSyncOptions,
                LastBackupTime = LastBackupTime,
                MaxBackups = MaxBackups,
                SelectedGames = GamesToBackup,
                Theme = ThemeInt
            };
            prefSaver.SavePrefs(newPrefs);
        }

        private void ExecuteDetectGames() {
            GamesToBackup = DirectoryFinder.GetInstalledGames(GamesList);
        }

        private async void AddGameToBackupList() {
            if (_selectedGame == null)
                return;
            var game = SelectedGame;
            if (GamesToBackup.Contains(game)) return;
            GamesToBackup.Add(game);
            if (BackupEnabled) { //Adding game to autobackup while it is running
                var helper = Backup.AddToAutobackup(game);
                HandleBackupResult(helper);
            }

            RaisePropertyChanged(() => GamesToBackup);            

            if (!game.ThumbnailPath.Contains("Loading")) return;
            await GamesDbApi.GetThumb(game);
        }

        private void RemoveGameFromBackupList(Game game) {
            if (game == null || !GamesToBackup.Contains(game))
                return;
            var gameToRemove = game;
    
            if (BackupEnabled) {
                var backupResult = Backup.RemoveFromAutobackup(gameToRemove);
                GamesToBackup.Remove(gameToRemove);
                HandleBackupResult(backupResult);
                return;
            }
            GamesToBackup.Remove(gameToRemove);
        }

        private void UpdateGamesList() {
            //Called when a game is added to the games list by user.
            GamesList = DirectoryFinder.GetGamesList();
        }

        private async void ExecuteStartBackup() {
            var result = await Backup.StartBackup(GamesToBackup.ToList(), BackupType, BackupEnabled, BackupSyncOptions, Interval);
            HandleBackupResult(result);
        }

        private void HandleBackupResult(BackupResultHelper result) {
            if (!result.Success && result.RemoveFromAutobackup) { //If no source files found for a game added to autobackup
                GamesToBackup.Remove(result.Game);
                MessageBox.Show(result.Message, @"Operation failed", MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            } 
            if(!result.Success) {
                MessageBox.Show(result.Message, @"Operation failed", MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }

            BackupEnabled = result.AutobackupEnabled;
            if (!string.IsNullOrWhiteSpace(result.BackupButtonText))
                BackupButtonText = result.BackupButtonText;

            if (!result.AutobackupEnabled && BackupType != BackupType.Autobackup) LastBackupTime = result.BackupDateTime;
            if (BackupType != BackupType.Autobackup) MessageBox.Show(@"Backup complete");
            if (BackupType == BackupType.Autobackup && !BackupEnabled) BackupButtonText = "Enable auto-backup";
        }

        private void ExecuteReset() {
            Backup.Reset(GamesToBackup.ToList(), BackupType, BackupEnabled);
            GamesToBackup.Clear();
            DisplaySpecifiedFolder = "";
            SelectedGame = null;
            SelectedBackupGame = null;
        }

        private void ExecuteSetThemeLight() {
            _themeInt = 0;
            var themeSelector = new ThemeSelector();
            Theme = themeSelector.ToggleTheme(_themeInt);
        }
        
        private void ExecuteSetThemeDark() {
            _themeInt = 1;
            var themeSelector = new ThemeSelector();
            Theme = themeSelector.ToggleTheme(_themeInt);
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