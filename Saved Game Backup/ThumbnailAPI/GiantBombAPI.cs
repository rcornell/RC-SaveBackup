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
using System.Xml;
using System.Xml.Serialization;
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
                SBTErrorLogger.Log(ex.Message);
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
                SBTErrorLogger.Log(ex.Message);
                game.ThumbnailPath = @"pack://application:,,,/Assets/NoThumb.jpg";
            }
        }

        //Downloads thumbnail using URL
        //And sets game.ThumbnailPath to local thumb cache
        private static async Task DownloadThumbnail(Game game)
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            var extension = Path.GetExtension(game.ThumbnailPath);
                
                //new FileInfo(game.ThumbnailPath);

            //Create path for thumbnail on HDD
            var thumbLocalPath = documentsPath + "\\Save Backup Tool\\Thumbnails\\" + game.Name + extension;

            var fi = new FileInfo(thumbLocalPath);

            //If File doesn't exist, download it.
            try {
                if (File.Exists(fi.ToString())) return;
                var webClient = new WebClient();
                await webClient.DownloadFileTaskAsync(new Uri(game.ThumbnailPath), fi.FullName);
                game.ThumbnailPath = thumbLocalPath;
                gameDataChanged = true;
            }
            catch (Exception ex) {
                SBTErrorLogger.Log(ex.Message);
            }
        }

        //If something has been changed in the game parameter,
        //Update the json
        private static async Task UpdateGameInJson(Game game) {

            var listToReturn = new List<Game>();

            var gameJsonList =
                await JsonConvert.DeserializeObjectAsync<List<Game>>(File.ReadAllText(GameListPath));
            listToReturn.AddRange(gameJsonList.Select(g => g.Name == game.Name ? game : g));
            var fileToWrite = JsonConvert.SerializeObject(listToReturn);
            File.WriteAllText(GameListPath, fileToWrite);
        }


        private static string BuildThumbQueryString(int gameId) {
            var queryString = String.Format("{0}/{1}/{2}/?api_key={3}&format={4}&field_list={5}", StringBase,
                ResourceType, gameId, ApiKey, Format, FieldsRequested);
            return queryString;
        }

        private static string BuildIdQueryString(string name) {
            //http://www.giantbomb.com/api/search/?api_key=ab63aeba2395b10932897115dc4bf3fa048e1734&format=json&query=%22skyrim%22&resources=game
            var searchString = String.Format("{0}/search/?api_key={1}&format={2}&query={3}&resources=game", StringBase,
                ApiKey, Format, name);
            return searchString;
        }

        public static async Task AddToJson(Game newGameForJson) {
            try {
                var gameJsonList = await JsonConvert.DeserializeObjectAsync<List<Game>>(File.ReadAllText(GameListPath));
                gameJsonList.Add(newGameForJson);
                var listToReturn = new ObservableCollection<Game>(gameJsonList.OrderBy(x => x.Name));
                var fileToWrite = await JsonConvert.SerializeObjectAsync(listToReturn);
                File.WriteAllText(GameListPath, fileToWrite);
            }
            catch (Exception ex) {
                SBTErrorLogger.Log(ex.Message);
            }
        }


        private static readonly Uri GetArt = new Uri(@"http://thegamesdb.net/api/GetArt.php?id=");
        private static readonly Uri BannerBase = new Uri(@"http://thegamesdb.net/banners/");

        public static async Task xmlstuff() {
            var resultString = "";
            var url = new Uri(@"http://thegamesdb.net/api/GetGamesList.php?name=Witcher 2 Assassin of Kings");

            using (var httpClient = new HttpClient()) {
                resultString = await httpClient.GetStringAsync(url);
            }

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(resultString);
            var text = JsonConvert.SerializeXmlNode(xmlDoc);
            var t = JsonConvert.DeserializeObject<dynamic>(text);
            
            var blank = t.Data.Game[0].id;

            var combined = GetArt + blank.ToString();

            var artString = "";
            using (var httpClient = new HttpClient()) {
                artString = await httpClient.GetStringAsync(combined);    
            }
            

            var xmlArt = new XmlDocument();
            xmlArt.LoadXml(artString);

            var artText = JsonConvert.SerializeXmlNode(xmlArt);
            var newArtText = artText.Replace("@", "");
            var tt = JsonConvert.DeserializeObject<dynamic>(newArtText);

            var trimmedUrl = tt.Data.Images.boxart[1].thumb;
            var finalUrl = BannerBase + trimmedUrl.ToString();
            var webClient = new WebClient();
            webClient.DownloadFile(finalUrl, @"C:\Users\Rob\Desktop\NEWAPITEST.jpg");


            //using (Stream s = File.OpenRead(resultString)) {
            //    var xml = new XmlSerializer(typeof(string));
            //    dynamic x =  xml.Deserialize(s);
            //}


        }
    }

}
