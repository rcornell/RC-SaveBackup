using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saved_Game_Backup
{
    public class GamesDBThumbResultList {
        public DataWithList Data { get; set; }
    }

    public class GamesDBThumbResult
    {
        public Data Data { get; set; }      
    }

    public class Xml
    {
        public string version { get; set; }
        public string encoding { get; set; }
    }

    public class Original
    {
        public string width { get; set; }
        public string height { get; set; }
        public string text { get; set; }
    }

    public class Fanart
    {
        public Original original { get; set; }
        public string thumb { get; set; }
    }

    public class Boxart
    {
        public string side { get; set; }
        public string width { get; set; }
        public string height { get; set; }
        public string thumb { get; set; }
        public string text { get; set; }
    }

    public class Banner
    {
        public string width { get; set; }
        public string height { get; set; }
        public string text { get; set; }
    }

    public class ImagesWithList
    {
        public List<Fanart> fanart { get; set; }
        public List<Boxart> boxart { get; set; }
        public Banner banner { get; set; }
    }

    public class DataWithList
    {
        public string baseImgUrl { get; set; }
        public ImagesWithList Images { get; set; }
    }

    public class Images
    {
        public List<Fanart> fanart { get; set; }
        public Boxart boxart { get; set; }
        public Banner banner { get; set; }
    }

    public class Data
    {
        public string baseImgUrl { get; set; }
        public Images Images { get; set; }
    }
}
