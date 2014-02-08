using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;

namespace Saved_Game_Backup {   
    
    [Serializable]    
    public class Game {
        
        private string _name;
        public string Name {
            get { return _name; }
            set { _name = value; }
        }

        private string _path;
        public string Path {
            get { return _path; }
            set { _path = value; }
        }

        private int _id;
        public int ID {
            get { return _id; }
            set { _id = value; }
        }

        private string _thumbnailPath;
        public string ThumbnailPath {
            get { return _thumbnailPath; }
            set { _thumbnailPath = value; }
        }

        private bool _hasCustomPath;
        public bool HasCustomPath {
            get { return _hasCustomPath; } 
            set { _hasCustomPath = value; }
        }

        public bool HasThumb { get; set; }

        public Game() {
            
        }

        public Game(string name, string path) {
            Name = name;
            Path = path;
        }
        
        public Game(string name, string path, int id) {
            ID = id;
            Name = name;
            Path = path;
        }

        public Game(string name, string path, int id, string thumbnailpath) {
            ID = id;
            Name = name;
            Path = path;
            ThumbnailPath = thumbnailpath;
        }

        public Game(string name, string path, int id, string thumbnailPath, bool customPath, bool hasThumb)
        {
            ID = id;
            Name = name;
            Path = path;
            ThumbnailPath = thumbnailPath;
            HasCustomPath = customPath;
            HasThumb = hasThumb;
        }

        public override string ToString() {
            return Name;
        }
    }
}
