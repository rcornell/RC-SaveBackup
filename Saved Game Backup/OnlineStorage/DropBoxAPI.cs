﻿using System;
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
        private static FileInfo testfile = new FileInfo(@"C:\Users\Rob\Desktop\testimage.png");
        private static string userToken;
        private static string userSecret;
        private static DropNetClient _client;
        private static UserLogin userLogin;

        static DropBoxAPI() {
            _client = new DropNetClient(apiKey, apiSecret);
        }

        private static bool LoadUserLogin() {
            if (!PrefSaver.CheckForPrefs()) return false;
            var prefSaver = new PrefSaver();
            var prefs = prefSaver.LoadPrefs();
            if (!string.IsNullOrEmpty(prefs.UserSecret) && 
                !string.IsNullOrEmpty(prefs.UserToken))
                _client.UserLogin = new UserLogin() {Secret = prefs.UserSecret, Token=prefs.UserToken};
            return true;
        }

        public static async Task DropLogin() {
            if (!LoadUserLogin()) return;
            var login = _client.GetToken(); //Gets oauth token
            var authUrl = _client.BuildAuthorizeUrl();
            Process.Start(authUrl);
            var result = MessageBox.Show(@"Please log in to DropBox and authorize SaveMonkey", "Authorize App",
                MessageBoxButton.OKCancel, MessageBoxImage.Information);
            if (result == MessageBoxResult.Cancel) return;
            var accessToken = _client.GetAccessToken(); //Store this token for "remember me" function
        }

        public static void GetPermission(string url) {
            
        }

        public async Task Upload(string folderPath, FileInfo file) {
            
        }

        public async Task Download(string filePath, string targetFile) {
            
        }


    }
}
