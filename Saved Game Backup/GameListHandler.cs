using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saved_Game_Backup
{
    public class GameListHandler {

        private static Game gameToBackup;

        public GameListHandler(){}

        //public static Dictionary<string, ObservableCollection<Game>>AddToBackupList() {
    
        //}

        public static List<ObservableCollection<Game>> AddToBackupList(ObservableCollection<Game> gamesToBackup, ObservableCollection<Game> gamesList, Game game) {
            List<ObservableCollection<Game>> listToReturn;

            gameToBackup = game;
            

            for (int i = 0; i < gamesList.Count(); i++) {
                if (gameToBackup.Name != gamesList[i].Name) continue;
                gameToBackup = gamesList[i];
                break;
            }

            if (gameToBackup != null)
            {
                gamesList.Remove(game);
                

                //Pull in Thumb data with GiantBombAPI
                //Used to be awaited
                //GetThumb();
                gamesToBackup.Add(gameToBackup);
                gamesToBackup = new ObservableCollection<Game>(gamesToBackup.OrderBy(x => x.Name));
            }

            listToReturn = new List<ObservableCollection<Game>>(){gamesToBackup, gamesList};
            return listToReturn;
        }

        private static async Task GetThumb() {
            var gb = new GiantBombAPI(gameToBackup);
            await gb.GetThumb(gameToBackup);
            gameToBackup.ThumbnailPath = gb.ThumbnailPath;
        }


    }
}
