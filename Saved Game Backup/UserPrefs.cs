using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Saved_Game_Backup
{
    public class UserPrefs {
        
        private int _theme;
        public int Theme {
            get { return _theme; } 
            set { _theme = value; }
        }

        private int _maxBackups;
        public int MaxBackups {
            get { return _maxBackups; } 
            set { _maxBackups = value; }
        }

        public UserPrefs(){}

        public UserPrefs(int theme, int maxBackups) {
            _theme = theme;
            _maxBackups = maxBackups;
        }

        
    }
}
