using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saved_Game_Backup
{
    public class GamesDBThumbResult
    {
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

        public class Images
        {
            public List<Fanart> fanart { get; set; }
            public Boxart boxart { get; set; }
            public Banner banner { get; set; }
        }

        public class Data
        {
            public static string baseImgUrl { get; set; }
            public static Images Images { get; set; }
        }
    }
}
