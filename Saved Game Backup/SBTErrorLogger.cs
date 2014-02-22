using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saved_Game_Backup
{
// ReSharper disable once InconsistentNaming
    public class SBTErrorLogger {
        private static readonly string MyDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private const string SbtPath = "\\Save Backup Tool\\Error\\";

        public static void Log(Exception ex) {
            Directory.CreateDirectory(MyDocs + SbtPath);
            var sb = new StringBuilder();
            sb.AppendLine(ex.Message);
            //if (!string.IsNullOrWhiteSpace(ex.InnerException.Message)) sb.AppendLine(ex.InnerException.Message);
            //if (!string.IsNullOrWhiteSpace(ex.Source)) sb.AppendLine(ex.Source);
            // if (!string.IsNullOrWhiteSpace(ex.StackTrace)) sb.AppendLine(ex.StackTrace);
            var day = DateTime.Now.Day;
            var month = DateTime.Now.Month;
            var year = DateTime.Now.Year;
            var hour = DateTime.Now.Hour;
            var minute = DateTime.Now.Minute;
            var second = DateTime.Now.Second;
            var millisecond = DateTime.Now.Millisecond;
            var dateString = string.Format(@" {0}-{1}-{2} {3}-{4}-{5}-{6}", month, day, year, hour, minute, second, millisecond);
            var writePath = string.Format(@"{0}{1}Log {2}.txt", MyDocs, SbtPath, dateString);
            using (var fs = File.OpenWrite(writePath)) {
                using (var sw = new StreamWriter(fs)) {
                    sw.WriteLineAsync(sb.ToString());
                }

            }
        }
    }
}
