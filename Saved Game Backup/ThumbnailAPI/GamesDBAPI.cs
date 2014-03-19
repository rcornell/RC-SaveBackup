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

