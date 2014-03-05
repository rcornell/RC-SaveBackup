using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saved_Game_Backup.Helper
{
    public class ProgressHelper
    {
        public double TotalFiles { get; set; }
        public double FilesComplete { get; set; }

        public double PercentComplete {
            get {
                if (Double.IsNaN(FilesComplete/TotalFiles)) return 0.0;
                return (FilesComplete/TotalFiles);
            }
        }

    }
}
