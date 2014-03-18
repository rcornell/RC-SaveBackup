using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Ioc;
using Saved_Game_Backup.OnlineStorage;

namespace Saved_Game_Backup.Helper {
    class SingletonHelper {
        public static DropBoxAPI DropBoxAPI {
            get { return SimpleIoc.Default.GetInstance<DropBoxAPI>(); }
        }

        public static GamesDBAPI GamesDBAPI {
            get { return SimpleIoc.Default.GetInstance<GamesDBAPI>(); }
        }
    }
}
