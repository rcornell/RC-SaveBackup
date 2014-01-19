using System;
using System.Collections.Generic;
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

        private string _queryString;
        private const string _apiKey = "ab63aeba2395b10932897115dc4bf3fa048e1734";
        private const string _stringBase = "http://www.giantbomb.com/api/";
        private const string _format = "json";
        private const string _fieldsRequested = "name,image";
        private const string _resource_Type = "game";
        private string _resource_ID;
        private string _responseString;
        
        private string _game_ID;

        public GiantBombAPI() {

        }

        public GiantBombAPI(string game_ID) {
            _game_ID = game_ID;
            BuildQueryString();
        }

        private void BuildQueryString() {
            _queryString = String.Format("{0}/{1}/{2}/?api_key={3}&format={4}&field_list={5}", _stringBase,
                _resource_Type, _resource_ID, _apiKey, _format, _fieldsRequested);
        }

        public async void DownloadThumbnail() {
            using (var client = new HttpClient())
                _responseString = await client.GetStringAsync(_queryString);

            var httpResponse = new HttpResponseMessage()
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

    public class Image
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
        public Image Image { get; set; }
    }


    public class ImageResponse : StandardResponse
    {
        [JsonProperty("results")]
        public ImageResult Results { get; set; }
    }


}
