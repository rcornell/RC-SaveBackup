using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Saved_Game_Backup {   
    
    [Serializable]    
    public class Game {
        public string Name;
        public string Path;
        public int ID;
        public BitmapImage Icon;

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
