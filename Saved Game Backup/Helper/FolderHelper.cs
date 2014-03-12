using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saved_Game_Backup.Helper {
    public class FolderHelper {
        public string FolderPath { get; set; }

        public FolderHelper(string path) {
            FolderPath = path;
        }
    }
}
