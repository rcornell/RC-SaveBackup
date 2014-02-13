using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Security.RightsManagement;
using System.Threading.Tasks;
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


        //TEST!!!
        //public ObservableCollection<WrapPanelGame> GamesToBackup { get; set; }

        //public RelayCommand AddPanelGame {
        //    get { return new RelayCommand(() => ExecuteAddPanelGame());}
        //}

        //public void ExecuteAddPanelGame() {
        //    if(GamesToBackup==null)
        //        GamesToBackup = new ObservableCollection<WrapPanelGame>();
        //    GamesToBackup.Add(new WrapPanelGame() { Game = GamesList[0] });
        //    RaisePropertyChanged(() => GamesToBackup);
        //}

        //public ObservableCollection<WrapPanelGame> WrapPanelGames { get; set; } 
        //public WrapPanelGame SelectedWrapPanelGame { get; set; }

        private const string _about =
            "I made this program in an attempt to help people keep track of their saved games in case of catastrophe. It should work, but Autobackup can be touchy sometimes. If you have any issues please email me at rob.cornell@gmail.com.";
        public string About { get { return _about; }}

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
                if (_backupType == BackupType.Autobackup && _backupEnabled) BackupButtonText = "Disable Autobackup";
                if (_backupType == BackupType.Autobackup && !_backupEnabled) BackupButtonText = "Enable Autobackup";
                if (_backupType != BackupType.Autobackup) BackupButtonText = "Backup Saves";
                RaisePropertyChanged(() => BackupLimitVisibility);
                RaisePropertyChanged(() => BackupButtonText);
                
            }
        }
        public string LastBackupTime { get; set; }

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
        public string BackupButtonText { get; set; }
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

        public RelayCommand ShowAbout {
            get { return new RelayCommand(() => ExecuteShowAbout());}
        }
        public RelayCommand MoveToBackupList
        {
            get
            {
                return new RelayCommand(() => ToBackupList());
            }
        }
        public RelayCommand<Game> MoveToGamesList {
            get {
                return new RelayCommand<Game>(ToGamesList);
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
            BackupType = BackupType.ToFolder;
            
            BackupLimitVisibility = Visibility.Hidden;
            DirectoryFinder.CheckDirectories();
            SetUpInterface();

            Messenger.Default.Register<DateTime>(this, s => {
                LastBackupTime = s.ToLongDateString() + s.ToLongTimeString();
                RaisePropertyChanged(() => LastBackupTime);
            });

          
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
                LastBackupTime = prefs.LastBackupTime;
                if (prefs.SelectedGames != null) GamesToBackup = prefs.SelectedGames;
                RaisePropertyChanged(() => GamesToBackup);
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
            foreach (var game in GamesToBackup)
                GamesList.Remove(game);

            RaisePropertyChanged(() => GamesToBackup);
            RaisePropertyChanged(() => GamesList);
        
        }

        private async void ToBackupList() {
            if (_selectedGame == null)
                return;
            var game = SelectedGame;
            if (GamesToBackup.Contains(game)) return;
            GamesToBackup.Add(game);
            GamesToBackup = new ObservableCollection<Game>(GamesToBackup.OrderBy(s => s.Name));

            if (!game.ThumbnailPath.Contains("Loading")) return;
            await GiantBombAPI.GetThumb(game);
        }

        private void ToGamesList(Game game) {
            if (game == null || !GamesToBackup.Contains(game))
                return;
            var gameToMove = game;
    
             //Make this work
            if (_backupEnabled) {
                var result = Backup.RemoveFromAutobackup(gameToMove);
                RaisePropertyChanged(() => GamesToBackup);
                HandleBackupResult(result);
            }
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
            var success = Backup.StartBackup(GamesToBackup, BackupType, _backupEnabled);
            HandleBackupResult(success);
        }

        private void HandleBackupResult(BackupResultHelper result) {
            if (!result.Success) { 
                MessageBox.Show(result.Message, "Operation Failed.", MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }
            _backupEnabled = result.AutobackupEnabled;
            if (_backupEnabled && BackupType == BackupType.Autobackup) BackupButtonText = "Disable Autobackup";
            if (!_backupEnabled && BackupType == BackupType.Autobackup) BackupButtonText = "Enable Autobackup";
            RaisePropertyChanged(() => BackupButtonText);         

            AutoBackupVisibility = _backupEnabled ? Visibility.Visible : Visibility.Hidden;
            RaisePropertyChanged(() => AutoBackupVisibility);

            if (!result.AutobackupEnabled) LastBackupTime = result.BackupDateTime;
            RaisePropertyChanged(() => LastBackupTime);
            
            MessageBox.Show(result.Message, "Operation Successful", MessageBoxButton.OK);
        }

        private void ExecuteReset() {
            GamesToBackup.Clear();
            _specifiedFolder = null;
            _selectedGame = null;
            _selectedBackupGame = null;

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

        private async void ExecuteOpenAddGameWindow() {
            Game newGameForJson = null;
            Messenger.Default.Register<AddGameMessage>(this, g => {
                newGameForJson = g.Game;
            });
            var addGameWindow = new AddGameWindow();
            addGameWindow.ShowDialog();

            if (newGameForJson == null) return;
            await GiantBombAPI.AddToJson(newGameForJson);
            MessageBox.Show(newGameForJson.Name + " added to list.");
            UpdateGamesList();
        }

        private void ExecuteShowAbout() {
            MessageBox.Show(About, "About SaveMonkey", MessageBoxButton.OK);
        }
    }
}