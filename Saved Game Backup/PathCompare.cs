using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saved_Game_Backup
{
    public class PathCompare
    {
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }

        public PathCompare(string source, string destination) {
            SourcePath = source;
            DestinationPath = destination;
        }
    }
}
