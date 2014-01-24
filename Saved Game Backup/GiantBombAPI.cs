using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Windows;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace Saved_Game_Backup
{
    public class GiantBombAPI {
        private BitmapImage _thumbNail;
        public BitmapImage ThumbNail {
            get { return _thumbNail; }
            set {
                if (_thumbNail == value) return;
                _thumbNail = value;
            }
        }

        private const string _gameListPath = @"C:\Users\Rob\Documents\Save Backup Tool\Games.json";
        private const string _apiKey = "ab63aeba2395b10932897115dc4bf3fa048e1734";
        private const string _stringBase = "http://www.giantbomb.com/api";
        private const string _format = "json";
        private const string _fieldsRequested = "name,image";
        private const string _resource_Type = "game";
        private Game _game;
        private string _responseString;
        //private string _gameName;
        private int _newGameID;

        public GiantBombAPI() {

        }

        public GiantBombAPI(int game_ID, Game game) {
            _game = game;
            _newGameID = game_ID;
            //CreateThumbnail(_game_ID);
        }

        public GiantBombAPI(Game game) {
            _game = game;
            //GetGameID(_gameName);
        }

        public async Task GetGameID() {
            var searchString = BuildSearchString(_game.Name);
           await DownloadData(searchString, false);
        }

        private string BuildThumbQueryString(int game_ID) {
            var queryString = String.Format("{0}/{1}/{2}/?api_key={3}&format={4}&field_list={5}", _stringBase,
                _resource_Type, game_ID, _apiKey, _format, _fieldsRequested);
            return queryString;
        }

        private string BuildSearchString(string name) {
            //http://www.giantbomb.com/api/search/?api_key=ab63aeba2395b10932897115dc4bf3fa048e1734&format=json&query=%22skyrim%22&resources=game
            var searchString = String.Format("{0}/search/?api_key={1}&format={2}&query={3}&resources=game", _stringBase,
                _apiKey, _format, name);
            return searchString;
        }

        public async Task CreateThumbnail() {
            var queryURL = BuildThumbQueryString(_newGameID);
            await DownloadData(queryURL, true);
        }

        private async Task DownloadData(string queryURL, bool thumbRequest) {
            using (var client = new HttpClient())
                _responseString = await client.GetStringAsync(queryURL);

            //string b = _responseString;
            //var resultObject = await JsonConvert.DeserializeObjectAsync<ImageResponse>(_responseString);

            var blob = await JsonConvert.DeserializeObjectAsync<dynamic>(_responseString);

            if (thumbRequest) {
                string thumbURL = blob.results.image.thumb_url;
                if (!string.IsNullOrWhiteSpace(thumbURL))
                    BuildThumbnail(thumbURL);
            }
            else {
                _newGameID = blob.results[0].id;
                //await CreateThumbnail(gameID);
            }
        }

        private void BuildThumbnail(string url) {

            //Check for correct thumbnail file extension
            var extension = "";
            if (url.Contains(".jpg"))
                extension = ".jpg";
            else if (url.Contains(".png"))
                extension = ".png";

            else if (url.Contains(".bmp"))
                extension = ".bmp";

            //Create path for thumbnail on HDD
            var documentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                               "\\Save Backup Tool\\Thumbnails\\" + _game.Name + extension;

            //If file exists, don't download.
            if (File.Exists(documentPath)) {
                var im = Image.FromFile(documentPath);
                
            }

            //File doesn't exist on HDD, download file to HDD
            var webClient = new WebClient();
            webClient.DownloadFileAsync(new Uri(url), documentPath);

            //Create thumbnail BitmapImage
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(url, UriKind.Absolute);
            bitmap.EndInit();
            ThumbNail = bitmap;
        }


        /// <summary>
        /// Edits json to update game id once it has been found by
        /// DownloadData()
        /// </summary>
        public void UpdateGameID()
        {
            if (_game.ID == 999999) {
                _game.ID = _newGameID;
            }

            var listToReturn = new List<Game>();

            var gameJsonList = JsonConvert.DeserializeObject<List<Game>>(File.ReadAllText(_gameListPath));
            foreach (var g in gameJsonList) {
                listToReturn.Add(g.Name == _game.Name ? _game : g);
            }
            var fileToWrite = JsonConvert.SerializeObject(listToReturn);
            File.WriteAllText(_gameListPath, fileToWrite);

            #region Code that Eric made me give up

            //    string lineToEdit = "";
            //    var row = 0;
            //    StreamReader sr;
            //    string[] allRows;
            //    try {
            //        sr =
            //            new StreamReader(
            //                @"C:\Users\Rob\Documents\Visual Studio 2012\Projects\Saved Game Backup\Saved Game Backup\Games.csv");
            //        allRows =
            //            File.ReadAllLines(
            //                @"C:\Users\Rob\Documents\Visual Studio 2012\Projects\Saved Game Backup\Saved Game Backup\Games.csv");
            //    }
            //    catch (FileNotFoundException ex) {
            //        MessageBox.Show("Not able to find the Games.csv file this\r\nprogram uses for game data.\r\n\r\n"+ex.Message);
            //        return;
            //    }

            //    try {
            //        while (!sr.EndOfStream) {
            //            var line = sr.ReadLine();
            //            if (line.StartsWith(_gameName)) {
            //                var lineCells = line.Split(',');
            //                lineCells[2] = _game_ID.ToString(CultureInfo.InvariantCulture);
            //                for (var i = 0; i <= 2; i++) {
            //                    lineToEdit += lineCells[i];
            //                    if (i < 2)
            //                        lineToEdit += ",";
            //                }
            //                allRows[row] = lineToEdit;
            //                File.WriteAllLines(
            //                    @"C:\Users\Rob\Documents\Visual Studio 2012\Projects\Saved Game Backup\Saved Game Backup\Games - New.csv",
            //                    allRows);
            //            }
            //            else {
            //                row++;
            //            }
            //        }
            //        sr.Close();
            //        File.Replace(
            //            @"C:\Users\Rob\Documents\Visual Studio 2012\Projects\Saved Game Backup\Saved Game Backup\Games - New.csv",
            //            @"C:\Users\Rob\Documents\Visual Studio 2012\Projects\Saved Game Backup\Saved Game Backup\Games.csv",
            //            @"C:\Users\Rob\Documents\Visual Studio 2012\Projects\Saved Game Backup\Saved Game Backup\GamesBackup.csv");
            //        //File.Delete(@"C:\Users\Rob\Documents\Visual Studio 2012\Projects\Saved Game Backup\Saved Game Backup\GamesBackup.csv");
            //    }
            //    catch (Exception ex) {
            //        using (
            //            var sw =
            //                new StreamWriter(
            //                    string.Format(
            //                        @"C:\Users\Rob\Documents\Visual Studio 2012\Projects\Saved Game Backup\Saved Game Backup\Error Logs\Log {0}.txt",
            //                        DateTime.Now))) {
            //         sw.WriteLine(ex.Message);
            //         sw.WriteLine(ex.InnerException);
            //         sw.WriteLine(ex.Source);
            //         sw.WriteLine(ex.StackTrace);
            //         sw.WriteLine(ex.TargetSite);
            //        }
            //        MessageBox.Show("Unexpected error writing to Games.csv.\r\n\r\nLog file written.");
            //    }

            #endregion
        }
            
    }

    public class StandardResponse
    {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }

        [JsonProperty("offset")]
        public int Offset { get; set; }

        [JsonProperty("number_of_page_results")]
        public int NumberOfPageResults { get; set; }

        [JsonProperty("number_of_total_results")]
        public int NumberOfTotalResults { get; set; }

        [JsonProperty("status_code")]
        public int StatusCode { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class ImageURLs
    {
        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }

        [JsonProperty("medium_url")]
        public string MediumUrl { get; set; }

        [JsonProperty("screen_url")]
        public string ScreenUrl { get; set; }

        [JsonProperty("small_url")]
        public string SmallUrl { get; set; }

        [JsonProperty("super_url")]
        public string SuperUrl { get; set; }

        [JsonProperty("thumb_url")]
        public string ThumbUrl { get; set; }

        [JsonProperty("tiny_url")]
        public string TinyUrl { get; set; }
    }

    public class ImageResult
    {
        [JsonProperty("image")]
        public ImageURLs ImageURLs { get; set; }
    }

    public class ImageResponse : StandardResponse
    {
        [JsonProperty("results")]
        public ImageResult Results { get; set; }
    }


}
