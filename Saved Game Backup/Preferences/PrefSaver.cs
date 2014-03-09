using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DropNet.Models;
using Newtonsoft.Json;
using Saved_Game_Backup.Preferences;
using Saved_Game_Backup.ViewModel;
using ThicknessConverter = Xceed.Wpf.DataGrid.Converters.ThicknessConverter;

namespace Saved_Game_Backup
{
    public class PrefSaver {

        public PrefSaver(){}

        public UserPrefs LoadPrefs() {
            UserPrefs prefs;
            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Save Backup Tool\\";
            if (Directory.Exists(path))
            {
                try {
                    var fullPath = path + @"UserPrefs.dat";
                    prefs = JsonConvert.DeserializeObject<UserPrefs>(File.ReadAllText(fullPath));
                    return prefs;
                }
                catch (SerializationException ex) {
                    SBTErrorLogger.Log(ex.Message);
                }
                return new UserPrefs(0,5,null, "");
            }

            MessageBox.Show("If you see this, something went wrong \r\nloading user preferences.");
            return new UserPrefs(0,5, new ObservableCollection<Game>(), "");
        }

        public void SavePrefs(UserPrefs prefs) {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Save Backup Tool\\";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var fullPath = path + @"UserPrefs.dat";
            var text = JsonConvert.SerializeObject(prefs);
            File.WriteAllText(fullPath, text);

        }

        public static bool CheckForPrefs() {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Save Backup Tool\\UserPrefs.dat";
            return File.Exists(path);
        }

        public bool SaveDropBoxToken(UserLogin userLogin) {
            if (!CheckForPrefs()) 
                return CreatePrefs(userLogin);
            var saver = new PrefSaver();
            var prefs = saver.LoadPrefs();
            prefs.UserSecret = userLogin.Secret;
            prefs.UserToken = userLogin.Token;
            saver.SavePrefs(prefs);
            return true;
        }

        private bool CreatePrefs(UserLogin userLogin = null) {
            if (userLogin != null) {
                var prefs = new UserPrefs {UserSecret = userLogin.Secret, UserToken = userLogin.Token};
                SavePrefs(prefs);
                return true;
            }
            return false;
        }

        public static void DeleteDropboxLogin() {
            Debug.WriteLine(@"Deleting dropbox key/token");
            var saver = new PrefSaver();
            var prefs = saver.LoadPrefs();
            prefs.UserSecret = null;
            prefs.UserToken = null;
            saver.SavePrefs(prefs);
            Debug.WriteLine(@"Dropbox key/token deleted");
        }

    }
}
