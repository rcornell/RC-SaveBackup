using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saved_Game_Backup
{
    public class GameListHandler {

        public GameListHandler(){}

        //public static Dictionary<string, ObservableCollection<Game>>AddToBackupList() {
    
        //}

        public static async Task<Game> AddToBackupList(ObservableCollection<Game> gamesToBackup, ObservableCollection<Game> gamesList, Game game) {
             await GetThumb(game);
            
            for (int i = 0; i < gamesList.Count(); i++) {
                if (game.Name != gamesList[i].Name) continue;
                game = gamesList[i];
                break;
            }

            if (game == null) return null;
            gamesList.Remove(game);
            gamesToBackup.Add(game);
            return game;
        }

        private static async Task GetThumb(Game game)
        {
            var gb = new GiantBombAPI(game);
            await gb.GetThumb(game);
            game.ThumbnailPath = gb.ThumbnailPath;
        }

        public static Game RemoveFromGamesList(ObservableCollection<Game> gamesList, Game selectedGame) {
            gamesList.Remove(selectedGame);
            return selectedGame;
        }

        public static Game RemoveFromBackupList(ObservableCollection<Game> gamesToBackup, Game game) {
            gamesToBackup.Remove(game);
            return game;
        }

        public static void AddToGamesList(ObservableCollection<Game> gamesToBackup,
            ObservableCollection<Game> gamesList, Game game) {


            for (var i = 0; i < gamesToBackup.Count(); i++) {
                if (game.Name != gamesToBackup[i].Name) continue;
                game = gamesToBackup[i];
                break;
            }

            if (game == null) return;
            gamesToBackup.Remove(game);
            gamesList.Add(game);
            gamesList = new ObservableCollection<Game>(gamesList.OrderBy(x => x.Name));
        }
    }
}
