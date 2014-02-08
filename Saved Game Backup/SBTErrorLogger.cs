using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saved_Game_Backup
{
// ReSharper disable once InconsistentNaming
    public class SBTErrorLogger
    {
        public static void Log(Exception ex) {

            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            Directory.CreateDirectory(documentsPath + "\\Save Backup Tool\\Error\\");
            var sb = new StringBuilder();
            sb.AppendLine(ex.Message);
            //if (!string.IsNullOrWhiteSpace(ex.InnerException.Message)) sb.AppendLine(ex.InnerException.Message);
            //if (!string.IsNullOrWhiteSpace(ex.Source)) sb.AppendLine(ex.Source);
            // if (!string.IsNullOrWhiteSpace(ex.StackTrace)) sb.AppendLine(ex.StackTrace);
            using (var fs = File.OpenWrite(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Save Backup Tool\\Error\\Log.txt"))
            {
                using (var sw = new StreamWriter(fs))
                {
                    sw.WriteLineAsync(sb.ToString());
                }

            }
        }
    }
}
