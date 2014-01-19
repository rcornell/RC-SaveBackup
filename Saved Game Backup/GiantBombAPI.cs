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

        //private string _queryString;
        private BitmapImage _thumbNail;
        public BitmapImage ThumbNail {
            get { return _thumbNail; }
            set {
                if (_thumbNail == value) return;
                _thumbNail = value;
            }
        }

        private const string _apiKey = "ab63aeba2395b10932897115dc4bf3fa048e1734";
        private const string _stringBase = "http://www.giantbomb.com/api/";
        private const string _format = "json";
        private const string _fieldsRequested = "name,image";
        private const string _resource_Type = "game";
        //private string _resource_ID;
        private string _responseString;
        
        private string _game_ID;

        public GiantBombAPI() {

        }

        public GiantBombAPI(string game_ID) {
            CreateThumbnail(game_ID);
        }

        private string BuildQueryString(string game_ID) {
            var queryString = String.Format("{0}/{1}/{2}/?api_key={3}&format={4}&field_list={5}", _stringBase,
                _resource_Type, game_ID, _apiKey, _format, _fieldsRequested);
            return queryString;
        }

        public void CreateThumbnail(string game_ID) {
            var queryURL = BuildQueryString(game_ID);
            DownloadThumbData(queryURL);
        }

        private async void DownloadThumbData(string queryURL) {
            using (var client = new HttpClient())
                _responseString = await client.GetStringAsync(queryURL);

            //var resultObject = await JsonConvert.DeserializeObjectAsync<ImageResponse>(_responseString);

            var blob = await JsonConvert.DeserializeObjectAsync<dynamic>(_responseString);
            string thumbURL = blob.results.image.thumb_url;

            if (!string.IsNullOrWhiteSpace(thumbURL))
                BuildThumbnail(thumbURL);
        }

        private void BuildThumbnail(string url) {
            
            var webRequest = (HttpWebRequest)HttpWebRequest.Create(url);

            using (var httpWebResponse = (HttpWebResponse)webRequest.GetResponse())
            {
                using (var stream = httpWebResponse.GetResponseStream())
                {
                    //BitmapImage source = System.Drawing.Image.FromStream(stream);
                    
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                    ThumbNail = bitmapImage;
                }
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
