using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DropNet;
using DropNet.Exceptions;
using DropNet.Models;
using Xceed.Wpf.DataGrid.FilterCriteria;

namespace Saved_Game_Backup.OnlineStorage
{
    public class DropBoxAPI {

        private const string ApiKey = @"zv4hgyhmkkr90xz";
        private const string ApiSecret = @"1h4y0lb43hiu8ci";
        private DropNetClient _client;

        public DropBoxAPI() {
            _client = new DropNetClient(ApiKey, ApiSecret);
        }

        private bool LoadUserLogin() {
            if (!PrefSaver.CheckForPrefs()) return false;
            var prefSaver = new PrefSaver();
            var prefs = prefSaver.LoadPrefs();
            if (!string.IsNullOrEmpty(prefs.UserSecret) && 
                !string.IsNullOrEmpty(prefs.UserToken))
                _client.UserLogin = new UserLogin() {Secret = prefs.UserSecret, Token=prefs.UserToken};
            return true;
        }

        private void SaveUserLogin(UserLogin userLogin) {
            PrefSaver.SaveDropBoxToken(userLogin);
        }

        //Use async methods
        public async Task Initialize() { 
            var login = _client.GetToken(); //Gets oauth token
            if (LoadUserLogin()) return; //True if user secret and token exist and were loaded
            var authUrl = _client.BuildAuthorizeUrl();
            Process.Start(authUrl);
            var result = MessageBox.Show(@"Please log in to DropBox and authorize SaveMonkey", "Authorize App",
                MessageBoxButton.OKCancel, MessageBoxImage.Information);
            if (result == MessageBoxResult.Cancel) return;
            UserLogin accessToken = null;
            try {
                accessToken = _client.GetAccessToken(); //Store this token for "remember me" function
                SaveUserLogin(accessToken);
            }
            catch (DropboxException ex) { //Fails if user doesn't authorize the app
                SBTErrorLogger.Log(ex.Message);
                MessageBox.Show(@"SaveMonkey not authorized by user");
            }
        }

        public void GetPermission(string url) {
            
        }

        //Not awaited
        public async Task Upload(string folderPath, FileInfo file) {
            var bytes = File.ReadAllBytes(file.FullName);
            _client.UploadFileAsync("/", file.Name, bytes, (response) => {
                var sb = new StringBuilder();
                sb.AppendLine(response.Contents.ToString());
                sb.AppendLine(response.Extension.ToString(CultureInfo.InvariantCulture));
                File.WriteAllText(@"C:\Users\Rob\Desktop\response.txt", sb.ToString()); //Remove after testing
            }, (error) => SBTErrorLogger.Log(error.Message));
        }

        public async Task Download(string filePath, string targetFile) {
            _client.GetFileAsync(filePath,
            (response) => File.WriteAllBytes(targetFile, response.RawBytes),
            (error) => SBTErrorLogger.Log(error.Message) );
        }


    }
}
