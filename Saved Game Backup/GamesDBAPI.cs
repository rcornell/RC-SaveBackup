using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Xceed.Wpf.Toolkit.Primitives;

namespace Saved_Game_Backup {

    public class GamesDBAPI {

        private static bool gameDataChanged;
        private const string GameListPath = @"Assets\Games.json";
        private static readonly Uri SearchBase = new Uri(@"http://thegamesdb.net/api/GetGamesList.php?name=");
        private static readonly Uri SearchThumbUrlBase = new Uri(@"http://thegamesdb.net/api/GetArt.php?id=");
        private static readonly Uri BannerBase = new Uri(@"http://thegamesdb.net/banners/");

        public GamesDBAPI() {}



        public static async Task GetThumb(Game game) {
            //Create path for thumbnails directory and get all files in directory
            var thumbnailDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                                     "\\Save Backup Tool\\Thumbnails\\";
            if (!Directory.Exists(thumbnailDirectory)) Directory.CreateDirectory(thumbnailDirectory);
            var files = Directory.GetFiles(thumbnailDirectory);
            thumbnailDirectory += game.Name;

            //search for thumbnail in directory
            //if found, set _thumbnailPath to path on HDD
            for (var i = 0; i < files.Count(); i++) {
                if (!files[i].Contains(thumbnailDirectory)) continue; //could also check for game.Name
                game.ThumbnailPath = files[i];
                gameDataChanged = true;
                break;
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
            var fullSearchPath = SearchBase + game.Name;            
            try {
                var resultString = "";
                using (var httpClient = new HttpClient()) {
                    resultString = await httpClient.GetStringAsync(fullSearchPath);
                }

                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(resultString);

                var serializedXml = JsonConvert.SerializeXmlNode(xmlDoc); //XmlSerializer could be used here.
                var convertedJson = JsonConvert.DeserializeObject<dynamic>(serializedXml);

                game.ID = convertedJson.Data.Game[0].id;
                gameDataChanged = true;
            }
            catch (ArgumentOutOfRangeException ex) {
                SBTErrorLogger.Log(ex);
            }
            catch (Exception ex) {
                SBTErrorLogger.Log(ex);
            }
        }

        //Gets the GamesDB thumbnail's web URL
        private static async Task GetThumbUrl(Game game) {
            var thumbQueryUrl = SearchThumbUrlBase + game.ID.ToString();
            var resultString = "";
            try {
                using (var client = new HttpClient())
                    resultString = await client.GetStringAsync(thumbQueryUrl);

                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(resultString);

                var serializedXml = JsonConvert.SerializeXmlNode(xmlDoc); //XmlSerializer could be used here.
                var withouAtSignXml = serializedXml.Replace("@", "");
                var withoutPoundSignXml = withouAtSignXml.Replace("#", "");
                var withoutQuestionMarkXml = withoutPoundSignXml.Replace("?", "");
                var convertedJson = JsonConvert.DeserializeObject<GamesDBThumbResult>(withoutQuestionMarkXml);



                //var endOfUrl = convertedJson.Data.Images.boxart.thumb; //May not work in all cases. May want to check for an array of boxart 
                var something = convertedJson.Data.Images.boxart.thumb; //, then search for "front" 
                //JUST MAKE THE CLASS THAT JSON2CSHARP GIVES YOU AND HAVE JSONCONVERT COVNERT INTO THAT CLASS

                //game.ThumbnailPath = BannerBase + endOfUrl.ToString();

            }
            catch (RuntimeBinderException ex) {
                SBTErrorLogger.Log(ex);
                game.ThumbnailPath = @"pack://application:,,,/Assets/NoThumb.jpg";
            }
            catch (Exception ex) {
                SBTErrorLogger.Log(ex);
                game.ThumbnailPath = @"pack://application:,,,/Assets/NoThumb.jpg";
            }
            
        }

        //Downloads thumbnail using URL
        //And sets game.ThumbnailPath to local thumb cache
        private static async Task DownloadThumbnail(Game game) {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (string.IsNullOrWhiteSpace(game.ThumbnailPath)) return;

            var extension = Path.GetExtension(game.ThumbnailPath);

            //Create path for thumbnail on HDD
            var thumbLocalPath = String.Format("{0}\\Save Backup Tool\\Thumbnails\\{1}{2}", documentsPath, game.Name, extension);
            var fi = new FileInfo(thumbLocalPath);
            if (File.Exists(fi.ToString())) return;

            //If File doesn't exist, download it.
            try {
                var webClient = new WebClient();
                var downloadSourceUri = new Uri(game.ThumbnailPath);
                await webClient.DownloadFileTaskAsync(downloadSourceUri, fi.FullName);
                game.ThumbnailPath = thumbLocalPath;
                gameDataChanged = true;
            }
            catch (Exception ex) {
                SBTErrorLogger.Log(ex);
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
}

