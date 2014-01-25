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

        private Image _thumbImage;
        public Image ThumbImage {
            get { return _thumbImage; }
            set { _thumbImage = value; }
        }

        private string _thumbnailPath;
        public string ThumbnailPath {
            get { return _thumbnailPath; }
            set { _thumbnailPath = value; }
        }

        private const string GameListPath = @"C:\Users\Rob\Documents\Save Backup Tool\Games.json";
        private const string ApiKey = "ab63aeba2395b10932897115dc4bf3fa048e1734";
        private const string StringBase = "http://www.giantbomb.com/api";
        private const string Format = "json";
        private const string FieldsRequested = "name,image";
        private const string ResourceType = "game";
        private Game _game;
        private string _responseString;
        private int _newGameId;

        public GiantBombAPI() {

        }

        //Not currently being used
        //public GiantBombAPI(int game_ID, Game game) {
        //    _game = game;
        //    _newGameId = game_ID;
        //    //CreateThumbnail(_game_ID);
        //}

        public GiantBombAPI(Game game) {
            _game = game;
            //GetGameID(_gameName);
        }

        //Retrieves GameID if it is the default value 999999 in json file.
        public async Task GetGameID() {
            var searchString = BuildSearchString(_game.Name);
           await DownloadData(searchString, false);
        }

        //Builds string to pull thumbnail data from API
        private string BuildThumbQueryString(int gameId) {
            var queryString = String.Format("{0}/{1}/{2}/?api_key={3}&format={4}&field_list={5}", StringBase,
                ResourceType, gameId, ApiKey, Format, FieldsRequested);
            return queryString;
        }

        //Builds string to search API for gameid
        private string BuildSearchString(string name) {
            //http://www.giantbomb.com/api/search/?api_key=ab63aeba2395b10932897115dc4bf3fa048e1734&format=json&query=%22skyrim%22&resources=game
            var searchString = String.Format("{0}/search/?api_key={1}&format={2}&query={3}&resources=game", StringBase,
                ApiKey, Format, name);
            return searchString;
        }

        /// <summary>
        /// This method starts the chain of checking for thumbnail on HDD
        /// and retrieving it from API if it is not found.
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public async Task GetThumb(Game game) {
            //Create path for thumbnails directory and get all files in directory
            var queryPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                            "\\Save Backup Tool\\Thumbnails\\";
            var files = Directory.GetFiles(queryPath);
            queryPath += game.Name;

            //search for thumbnail in directory
            //if found, set _thumbnailPath to path on HDD
            for (int i = 0; i < files.Count(); i++) {
                if (files[i].Contains(queryPath)){
                    _thumbnailPath = files[i];
                    break;
                }
            }

            //If thumbnail not found, retrieve gameID and thumbnail.
            if (game.ID == 999999) {
                await GetGameID();
                await UpdateGameID();
            }
            if (_thumbnailPath == null && game.ID != 999999)
                await CreateThumbnail();
        }

        //Requests query string and calls DownloadData
        private async Task CreateThumbnail() {
            var queryURL = BuildThumbQueryString(_newGameId);
            await DownloadData(queryURL, true);
        }

        //Downloads json code from GiantBomb's API and pulls out the requested
        //values, GameID or thumb url, depending on the bool value of thumbRequest
        private async Task DownloadData(string queryURL, bool thumbRequest) {
            using (var client = new HttpClient())
                _responseString = await client.GetStringAsync(queryURL);

            var blob = await JsonConvert.DeserializeObjectAsync<dynamic>(_responseString);

            if (thumbRequest) {
                string thumbURL = blob.results.image.thumb_url;
                if (!string.IsNullOrWhiteSpace(thumbURL))
                    BuildThumbnail(thumbURL);
            }
            else {
                _newGameId = blob.results[0].id;
              
            }
        }

        //Downloads thumbnail if it is not found in Thumbnails directory.
        private void BuildThumbnail(string url) {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            //Check for correct thumbnail file extension SHOULD YOU USE A SWITCH?
            var extension = "";
            if (url.Contains(".jpg"))
                extension = ".jpg";
            else if (url.Contains(".png"))
                extension = ".png";
            else if (url.Contains(".bmp"))
                extension = ".bmp";
            else if (url.Contains(".gif"))
                extension = ".gif";
            else 
                extension = ".jpg";
            
            //Create path for thumbnail on HDD
            _thumbnailPath = documentsPath + "\\Save Backup Tool\\Thumbnails\\" + _game.Name + extension;
            var fi = new FileInfo(_thumbnailPath);            

            //If File doesn't exist, download it.
            try {
                if (File.Exists(fi.ToString())) 
                    return;
                var webClient = new WebClient();
                webClient.DownloadFile(new Uri(url), fi.FullName);
            }
            catch (Exception ex) {
                Directory.CreateDirectory(documentsPath + "\\Save Backup Tool\\Error\\");
                var sb = new StringBuilder();
                sb.AppendLine(ex.Message);
                sb.AppendLine(ex.InnerException.Message);
                sb.AppendLine(ex.Source);
                sb.AppendLine(ex.StackTrace);
                using (var fs = File.OpenWrite(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)+"\\Save Backup Tool\\Error\\Log.txt")) {
                    using (var sw = new StreamWriter(fs)) {
                        sw.WriteLineAsync(sb.ToString());    
                    }
                    
                }
            }

            //Create thumbImage. Not necessary since Game.cs is updated with the path
            //But testing for future features.
            //Image test;
            //using (var fs = File.OpenRead(_thumbnailPath)) {
            //    test = Image.FromStream(fs);
            //}
            //_thumbImage = test;
        }

        /// Edits json to update game id once it has been found by DownloadData()
        /// Could be expanded to write Thumbnail path to json file
        private async Task UpdateGameID() {
            if (_game.ID != 999999) {
                return;
            }
            _game.ID = _newGameId;

            var listToReturn = new List<Game>();

            var gameJsonList =
                await JsonConvert.DeserializeObjectAsync<List<Game>>(File.ReadAllText(GameListPath));
            foreach (var g in gameJsonList) {
                listToReturn.Add(g.Name == _game.Name ? _game : g);
            }
            var fileToWrite = JsonConvert.SerializeObject(listToReturn);
            File.WriteAllText(GameListPath, fileToWrite);

            #region Old CSV parsing code

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
