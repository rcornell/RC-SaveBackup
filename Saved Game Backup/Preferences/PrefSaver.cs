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

            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Save Backup Tool\\";
            if (Directory.Exists(path)) {
                try {
                    var fullPath = path + @"UserPrefs.dat";
                    var prefs = JsonConvert.DeserializeObject<UserPrefs>(File.ReadAllText(fullPath));
                    return prefs;
                }
                catch (SerializationException ex) {
                    SBTErrorLogger.Log(ex.Message);
                    MessageBox.Show("If you see this, something went wrong \r\nloading user preferences.\r\nTry deleting the UserPrefs.dat file\r\ninDocuments\\Save Backup Tool\\");
                }
                return UserPrefs.GetDefaultPrefs();
            }

            MessageBox.Show("If you see this, something went wrong \r\nloading user preferences.\r\nTry deleting the UserPrefs.dat file\r\ninDocuments\\Save Backup Tool\\");
            return UserPrefs.GetDefaultPrefs();
        }

        public void SavePrefs(UserPrefs prefs) {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Save Backup Tool\\";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var fullPath = path + @"UserPrefs.dat";
            var text = JsonConvert.SerializeObject(prefs);
            File.WriteAllText(fullPath, text);

        }

        public bool CheckForPrefs() {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Save Backup Tool\\UserPrefs.dat";
            return File.Exists(path);
        }

        public bool SaveDropboxToken(UserLogin userLogin) {
            if (!CheckForPrefs()) //true if no prefs exist
                return CreateDropboxOnlyPrefs(userLogin);
            var saver = new PrefSaver();
            var prefs = saver.LoadPrefs();
            prefs.UserSecret = userLogin.Secret;
            prefs.UserToken = userLogin.Token;
            saver.SavePrefs(prefs);
            return true;
        }

        private bool CreateDropboxOnlyPrefs(UserLogin userLogin) {
            if (userLogin != null) {
                var prefs = new UserPrefs {UserSecret = userLogin.Secret, UserToken = userLogin.Token};
                SavePrefs(prefs);
                return true;
            }
            return false;
        }

        public static void DeleteDropboxToken() {
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
