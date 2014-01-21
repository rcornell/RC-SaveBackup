using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Net;
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

        private const string _apiKey = "ab63aeba2395b10932897115dc4bf3fa048e1734";
        private const string _stringBase = "http://www.giantbomb.com/api";
        private const string _format = "json";
        private const string _fieldsRequested = "name,image";
        private const string _resource_Type = "game";
        private string _responseString;
        private string _gameName;
        private int _game_ID;

        public GiantBombAPI() {

        }

        public GiantBombAPI(int game_ID, string name) {
            _gameName = name;
            _game_ID = game_ID;
            //CreateThumbnail(_game_ID);
        }

        public GiantBombAPI(string name) {
            _gameName = name;
            //GetGameID(_gameName);
        }

        public async Task GetGameID() {
            var searchString = BuildSearchString(_gameName);
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
            var queryURL = BuildThumbQueryString(_game_ID);
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
                _game_ID = blob.results[0].id;
                //await CreateThumbnail(gameID);
            }
        }

        private void BuildThumbnail(string url) {
            
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(url, UriKind.Absolute);
            bitmap.EndInit();
            ThumbNail = bitmap;

            //Save thumb to HDD

            #region MyRegion
            //var webRequest = (HttpWebRequest)HttpWebRequest.Create(url);

            //using (var httpWebResponse = (HttpWebResponse)webRequest.GetResponse())
            //{
            //    using (var stream = httpWebResponse.GetResponseStream())
            //    {
            //        var bitmapImage = new BitmapImage();
            //        bitmapImage.BeginInit();
            //        bitmapImage.StreamSource = stream;
            //        bitmapImage.EndInit();
            //        ThumbNail = bitmapImage;
            //    }
            //}
            
            #endregion
            //var path = string.Format("{0}\\{1}.jpg", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), _gameName);

            //using (WebClient client = new WebClient()) {
            //    client.DownloadFile(url, path);
            //}
        }


        /// <summary>
        /// This method is completely broken and does not work.
        /// 
        /// Add try/catch for null reference exception
        /// </summary>
        public void UpdateGameID() {
            int row = 0;
            string line= "";

            var sr =
                new StreamReader(
                    @"C:\Users\Rob\Documents\Visual Studio 2012\Projects\Saved Game Backup\Saved Game Backup\Games.csv");
            var sw =
                new StreamWriter(
                    @"C:\Users\Rob\Documents\Visual Studio 2012\Projects\Saved Game Backup\Saved Game Backup\Games-New.csv");

            if (string.IsNullOrWhiteSpace(sr.Peek().ToString()))
                return;
                
            line = sr.ReadLine();

            if (line.StartsWith(_gameName)) {
                var data = line.Split(',');
                data[2] = _game_ID.ToString();
            }
            else {
                //This doesn't work because row keeps getting set back to 0.
                row++;
                UpdateGameID();
            }
                
            

            //sw.WriteLine();

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
