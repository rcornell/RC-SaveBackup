using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Saved_Game_Backup.ViewModel;

namespace Saved_Game_Backup
{
    public class PrefSaver {

        public PrefSaver(UserPrefs prefs){}

        public static bool LoadPrefs()
        {
            string path = Environment.SpecialFolder.MyDocuments + "SaveBackupTool\\";
            if (Directory.Exists(path))
            {
                var bf = new BinaryFormatter();
                //bf.Deserialize();

                return true;
            }

            return false;
        }

        public static bool SavePrefs(MainViewModel main)
        {
            string path = Environment.SpecialFolder.MyDocuments + "SaveBackupTool\\";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);


            //REDO THIS TO JUST CAPTURE A FEW PROPERTIES
            using (Stream output = File.Create("UserPrefs.dat"))
            {
                var bf = new BinaryFormatter();
                bf.Serialize(output, main);

            }


            return false;
        }

    }
}
