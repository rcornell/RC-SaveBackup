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
using System.Windows.Forms;
using DropNet;
using DropNet.Exceptions;
using DropNet.Models;
using Xceed.Wpf.DataGrid.FilterCriteria;
using MessageBox = System.Windows.MessageBox;

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
            var saver = new PrefSaver();
            saver.SaveDropBoxToken(userLogin);
        }

        //Use async methods
        public async Task Initialize() { 
            var oauthLogin = _client.GetToken(); //Gets oauth token
            if (LoadUserLogin()) return; //True if user secret and token exist and were loaded
            var authUrl = _client.BuildAuthorizeUrl();
            Process.Start(authUrl);
            var result = MessageBox.Show(@"Please log in to DropBox and authorize SaveMonkey", "Authorize App",
                MessageBoxButton.OKCancel, MessageBoxImage.Information);
            if (result == MessageBoxResult.Cancel) return;
            try {
                var accessToken = _client.GetAccessToken();
                SaveUserLogin(accessToken);
            }
            catch (DropboxException ex) { //Fails if user doesn't authorize the app
                SBTErrorLogger.Log(ex.Message);
                PrefSaver.DeleteDropboxLogin();
                MessageBox.Show(@"SaveMonkey not authorized by user");
            }
        }

        public void GetMetadata() {
            var meta = _client.GetMetaData();
            DirectoryInfo dirs = null;
            foreach (var content in meta.Contents) {
                if (content.Is_Dir && dirs == null) {

                    dirs = new DirectoryInfo(content.Path);
                    continue;
                }
                if (!content.Is_Dir || dirs == null) continue;
                dirs.CreateSubdirectory(content.Name);
                continue;
            }      
        }

        //Not awaited
        public async Task Upload(string folderPath, FileInfo file) {
            var bytes = File.ReadAllBytes(file.FullName);
            var uploadPath = @"/SaveMonkey" + folderPath;
            await CheckFolderPath(uploadPath);
            _client.UploadFileAsync(uploadPath, file.Name, bytes, (response) => {
                var sb = new StringBuilder();
                sb.AppendLine(response.Contents.ToString());
                sb.AppendLine(response.Extension.ToString(CultureInfo.InvariantCulture));
                File.WriteAllText(@"C:\Users\Rob\Desktop\response.txt", sb.ToString()); //Remove after testing
            }, (error) => {
                if (error.Response.StatusCode.ToString() == @"Forbidden") {//Error with access/authorization. erase auth info and reinitialize.
                    PrefSaver.DeleteDropboxLogin();
                    Initialize();
                }
                Console.WriteLine(error.Response.StatusCode.ToString());
                MessageBox.Show(@"Look for error in output"); //Remove after testing
                SBTErrorLogger.Log(error.Response.StatusCode.ToString());

                //IF ERROR GET NEW AUTH
            });
        }

        public async Task Upload(string folderPath, string file) {
            var bytes = File.ReadAllBytes(file); //This fails because it's not a full path
            var uploadPath = @"/SaveMonkey" + folderPath;
            await CheckFolderPath(uploadPath);

            _client.UploadFileAsync(uploadPath, file, bytes, (response) => {
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

        private async Task CheckFolderPath(string path) { //NOT AUTHORIZED
            //var metaData = _client.CreateFolder(path);
        }


    }
}
