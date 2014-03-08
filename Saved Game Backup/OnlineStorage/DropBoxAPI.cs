using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DropNet;
using DropNet.Models;
using Xceed.Wpf.DataGrid.FilterCriteria;

namespace Saved_Game_Backup.OnlineStorage
{
    public class DropBoxAPI {

        private const string apiKey = @"zv4hgyhmkkr90xz";
        private const string apiSecret = @"1h4y0lb43hiu8ci";
        private DropNetClient _client;
        private UserLogin userLogin;

        public DropBoxAPI() {
            _client = new DropNetClient(apiKey, apiSecret);
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

        public async Task Initialize() { //Use async methods
            if (!LoadUserLogin()) return;
            var login = _client.GetToken(); //Gets oauth token
            var authUrl = _client.BuildAuthorizeUrl();
            Process.Start(authUrl);
            var result = MessageBox.Show(@"Please log in to DropBox and authorize SaveMonkey", "Authorize App",
                MessageBoxButton.OKCancel, MessageBoxImage.Information);
            if (result == MessageBoxResult.Cancel) return;
            var accessToken = _client.GetAccessToken(); //Store this token for "remember me" function
            SaveUserLogin(accessToken);
        }

        public void GetPermission(string url) {
            
        }

        public async Task Upload(string folderPath, FileInfo file) {
            var bytes = File.ReadAllBytes(file.FullName);
            //await _client.UploadFileAsync(folderPath, file.Name, bytes);
            _client.UploadFileAsync("/", "test.txt", bytes, (response) => { }, (error) => SBTErrorLogger.Log(error.Message));
        }

        public async Task Download(string filePath, string targetFile) {
            _client.GetFileAsync("/Getting Started.rtf",
            (response) => File.WriteAllBytes(targetFile, response.RawBytes),
            (error) =>
            {
                //Do something on error
            });
            
        }


    }
}
