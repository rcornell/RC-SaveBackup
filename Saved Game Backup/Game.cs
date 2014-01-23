using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private BitmapImage _icon;
        public BitmapImage Icon {
            get { return _icon; }
            set { _icon = value; }
        }


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

        public Game(string name, string path, int id, BitmapImage icon)
        {
            ID = id;
            Name = name;
            Path = path;
            Icon = icon;
        }

        public override string ToString() {
            return Name;
        }
    }
}
