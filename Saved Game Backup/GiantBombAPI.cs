using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Microsoft.CSharp.RuntimeBinder;
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

        private static bool gameDataChanged;
        private const string GameListPath = @"Assets\Games.json";
        private const string ApiKey = "ab63aeba2395b10932897115dc4bf3fa048e1734";
        private const string StringBase = "http://www.giantbomb.com/api";
        private const string Format = "json";
        private const string FieldsRequested = "name,image";
        private const string ResourceType = "game";

        public GiantBombAPI() {

        }

        //Not currently being used
        //public GiantBombAPI(int game_ID, Game game) {
        //    _game = game;
        //    _newGameId = game_ID;
        //    //CreateThumbnail(_game_ID);
        //}

        //public GiantBombAPI(Game game) {
        //    _game = game;
        //}

        public static async Task GetThumb(Game game)
        {
            //Create path for thumbnails directory and get all files in directory
            var thumbnailDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                            "\\Save Backup Tool\\Thumbnails\\";
            if (!Directory.Exists(thumbnailDirectory)) Directory.CreateDirectory(thumbnailDirectory);
            var files = Directory.GetFiles(thumbnailDirectory);
            thumbnailDirectory += game.Name;

            //search for thumbnail in directory
            //if found, set _thumbnailPath to path on HDD
            for (var i = 0; i < files.Count(); i++) {
                if (files[i].Contains(thumbnailDirectory)){ //could also check for game.Name
                    game.ThumbnailPath = files[i];
                    gameDataChanged = true;
                    break;
                }
            }

            //If thumbnailPath is found, leave method.
            if (!string.IsNullOrWhiteSpace(game.ThumbnailPath) && !game.ThumbnailPath.Contains("Loading"))
                return;

            //If thumbnail not found, retrieve gameID and thumbnail.
            if (game.ID == 999999) {
                await GetGameID(game);
            }
            //if (_thumbnailPath == null && game.ID != 999999)
            if (game.ThumbnailPath.Contains("Loading") || game.ThumbnailPath.Contains("NoThumb"))
                await GetThumbUrl(game);

            if (!string.IsNullOrWhiteSpace(game.ThumbnailPath) && !game.ThumbnailPath.Contains("NoThumb.jpg"))
                await DownloadThumbnail(game);

            if (gameDataChanged)
                await UpdateGameInJson(game);
        }

        //Retrieves GameID if it is the default value 999999 in json file.
        public static async Task GetGameID(Game game) {
            var searchString = BuildIdQueryString(game.Name);
            var responseString = "";
            
            using (var client = new HttpClient())
                responseString = await client.GetStringAsync(searchString);

            var blob = await JsonConvert.DeserializeObjectAsync<dynamic>(responseString);

            try {
                game.ID = blob.results[0].id;
                gameDataChanged = true;
            }
            catch (ArgumentOutOfRangeException ex) {
                SBTErrorLogger.Log(ex);
            }
        }

        //Gets the Giant Bomb thumbnail's web URL
        private static async Task GetThumbUrl(Game game)
        {
            var thumbQueryUrl = BuildThumbQueryString(game.ID);
            var responseString = "";
            
            using (var client = new HttpClient())
                responseString = await client.GetStringAsync(thumbQueryUrl);

            var blob = await JsonConvert.DeserializeObjectAsync<dynamic>(responseString);

            try {
                game.ThumbnailPath = blob.results.image.thumb_url;
            }
            catch (RuntimeBinderException ex) {
                SBTErrorLogger.Log(ex);
                game.ThumbnailPath = @"pack://application:,,,/Assets/NoThumb.jpg";
            }
        }

        //Downloads thumbnail using URL
        //And sets game.ThumbnailPath to local thumb cache
        private static Task DownloadThumbnail(Game game)
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            var extension = new FileInfo(game.ThumbnailPath).Extension;

            //Create path for thumbnail on HDD
            var thumbLocalPath = documentsPath + "\\Save Backup Tool\\Thumbnails\\" + game.Name + extension;

            var fi = new FileInfo(thumbLocalPath);

            //If File doesn't exist, download it.
            try {
                if (File.Exists(fi.ToString()))
                    return null;
                var webClient = new WebClient();
                webClient.DownloadFileAsync(new Uri(game.ThumbnailPath), fi.FullName);
                game.ThumbnailPath = thumbLocalPath;
                gameDataChanged = true;
            }
            catch (Exception ex) {
                SBTErrorLogger.Log(ex);
            }

            return null;
        }

        //If something has been changed in the game parameter,
        //Update the json
        private static async Task UpdateGameInJson(Game game)
        {

            var listToReturn = new List<Game>();

            var gameJsonList =
                await JsonConvert.DeserializeObjectAsync<List<Game>>(File.ReadAllText(GameListPath));
            listToReturn.AddRange(gameJsonList.Select(g => g.Name == game.Name ? game : g));
            var fileToWrite = JsonConvert.SerializeObject(listToReturn);
            File.WriteAllText(GameListPath, fileToWrite);
        }

        //Builds string to pull thumbnail URL from Giant Bomb
        private static string BuildThumbQueryString(int gameId) {
            var queryString = String.Format("{0}/{1}/{2}/?api_key={3}&format={4}&field_list={5}", StringBase,
                ResourceType, gameId, ApiKey, Format, FieldsRequested);
            return queryString;
        }

        //Builds string to search Giant Bomb for Game ID
        private static string BuildIdQueryString(string name) {
            //http://www.giantbomb.com/api/search/?api_key=ab63aeba2395b10932897115dc4bf3fa048e1734&format=json&query=%22skyrim%22&resources=game
            var searchString = String.Format("{0}/search/?api_key={1}&format={2}&query={3}&resources=game", StringBase,
                ApiKey, Format, name);
            return searchString;
        }


        //Could be modified to download the thumbnail data now.
        public static async Task AddToJson(Game newGameForJson) {
            try {
                var gameJsonList = await JsonConvert.DeserializeObjectAsync<List<Game>>(File.ReadAllText(GameListPath));
                gameJsonList.Add(newGameForJson);
                var listToReturn = new ObservableCollection<Game>(gameJsonList.OrderBy(x => x.Name));
                var fileToWrite = await JsonConvert.SerializeObjectAsync(listToReturn);
                File.WriteAllText(GameListPath, fileToWrite);
            }
            catch (Exception ex) {
                SBTErrorLogger.Log(ex);
            }
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
