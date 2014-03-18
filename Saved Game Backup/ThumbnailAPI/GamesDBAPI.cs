using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting.Proxies;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Xceed.Wpf.Toolkit.Primitives;

namespace Saved_Game_Backup {

    public class GamesDBAPI {

        private bool gameDataChanged;
        private const string GameListPath = @"Assets\Games.json";
        private readonly Uri SearchBase = new Uri(@"http://thegamesdb.net/api/GetGamesList.php?name=");
        private readonly Uri SearchThumbUrlBase = new Uri(@"http://thegamesdb.net/api/GetArt.php?id=");
        private readonly Uri BannerBase = new Uri(@"http://thegamesdb.net/banners/");

        public GamesDBAPI() {}



        public async Task GetThumb(Game game) {
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
        public async Task GetGameID(Game game) {
            var fullSearchPath = new Uri(SearchBase + game.Name);

            #region JsonMethod
            //try {
            //    var resultString = "";
            //    using (var httpClient = new HttpClient()) {
            //        resultString = await httpClient.GetStringAsync(fullSearchPath);
            //    }

            //    var xmlDoc = new XmlDocument();
            //    xmlDoc.LoadXml(resultString);

            //    var serializedXml = JsonConvert.SerializeXmlNode(xmlDoc); //XmlSerializer could be used here.
            //    var convertedJson = JsonConvert.DeserializeObject<dynamic>(serializedXml);

                

            //    if (convertedJson.Data.Game.GetType() == typeof (List<>))
            //        return;

            //    game.ID = convertedJson.Data.Game[0].id;
            //    gameDataChanged = true;
            //}
            //catch (ArgumentOutOfRangeException ex) {
            //    SBTErrorLogger.Log(ex);
            //}
            //catch (Exception ex) {
            //    SBTErrorLogger.Log(ex);
            //}
            #endregion

            var client = new WebClient() { Proxy = null };
            var result = await client.DownloadStringTaskAsync(fullSearchPath);         
            var indexOfFirstTag = result.IndexOf("<id>");
            var resultExFirstTag = result.Substring(indexOfFirstTag + 4);
            var indexOfSecondTag = resultExFirstTag.IndexOf("</id>");
            game.ID = int.Parse(resultExFirstTag.Remove(indexOfSecondTag));

        }

        //Gets the GamesDB thumbnail's web URL
        private async Task GetThumbUrl(Game game) {
            var thumbQueryUrl = SearchThumbUrlBase + game.ID.ToString();
            

            #region JsonMethod
            //try {
            //    var resultString = "";
            //    using (var client = new HttpClient())
            //        resultString = await client.GetStringAsync(thumbQueryUrl);

            //    var xmlDoc = new XmlDocument();
            //    xmlDoc.LoadXml(resultString);

            //    var serializedXml = JsonConvert.SerializeXmlNode(xmlDoc); //XmlSerializer could be used here.
            //    var withouAtSignXml = serializedXml.Replace("@", "");
            //    var withoutPoundSignXml = withouAtSignXml.Replace("#", "");
            //    var withoutQuestionMarkXml = withoutPoundSignXml.Replace("?", "");
            //    var dynamicResult = JsonConvert.DeserializeObject<dynamic>(withoutQuestionMarkXml);

            //    string gamesDBString = dynamicResult.ToString();
            //    var count = Regex.Matches(gamesDBString, "boxart").Count;
            //    var endOfUrl = "";
            //    if (count > 3) { //If true, the boxart is a List<Boxart>
            //        withoutQuestionMarkXml.Replace("Images", "ImagesWithList");
            //        withoutQuestionMarkXml.Replace("Data", "DataWithList");
            //        var gamesDBResult = JsonConvert.DeserializeObject<GamesDBThumbResultList>(withoutQuestionMarkXml);
            //        var boxList = gamesDBResult.Data.Images.boxart;
            //        foreach (var boxItem in boxList.Where(boxItem => boxItem.thumb.Contains("front")))
            //            endOfUrl = boxItem.thumb;                 
            //    } else { //else, there is only one boxart in the result
            //        var gamesDBResult = JsonConvert.DeserializeObject<GamesDBThumbResult>(withoutQuestionMarkXml);
            //        endOfUrl = gamesDBResult.Data.Images.boxart.thumb;
            //    }
              
            
            //    game.ThumbnailPath = BannerBase + endOfUrl;

            //}
            //catch (RuntimeBinderException ex) {
            //    SBTErrorLogger.Log(ex);
            //    game.ThumbnailPath = @"pack://application:,,,/Assets/NoThumb.jpg";
            //}
            //catch (Exception ex) {
            //    SBTErrorLogger.Log(ex);
            //    game.ThumbnailPath = @"pack://application:,,,/Assets/NoThumb.jpg";
            //}
            #endregion

            var client = new WebClient() { Proxy = null }; ;
            var artResponseString = await client.DownloadStringTaskAsync(thumbQueryUrl);
            var index = artResponseString.IndexOf("boxart/thumb/original/front/");
            var beginningTrimmed = artResponseString.Substring(index);
            var endIndex = beginningTrimmed.IndexOf("\">");
            var endTrimmed = beginningTrimmed.Remove(endIndex);
            var finalUrl = BannerBase + endTrimmed;
            game.ThumbnailPath = finalUrl;
            gameDataChanged = true;

        }

        //Downloads thumbnail using URL
        //And sets game.ThumbnailPath to local thumb cache
        private async Task DownloadThumbnail(Game game) {
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
                SBTErrorLogger.Log(ex.Message);
            }
        }

        //If something has been changed in the game parameter,
        //Update the json
        private async Task UpdateGameInJson(Game game) {
            var listToReturn = new List<Game>();
            var gameJsonList =
                await JsonConvert.DeserializeObjectAsync<List<Game>>(File.ReadAllText(GameListPath));
            listToReturn.AddRange(gameJsonList.Select(g => g.Name == game.Name ? game : g));
            var fileToWrite = JsonConvert.SerializeObject(listToReturn);
            File.WriteAllText(GameListPath, fileToWrite);
        }

        public async Task AddToJson(Game newGameForJson) {
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
    }
}

