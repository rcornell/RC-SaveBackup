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

        public static async Task AddToBackupList(ObservableCollection<Game> gamesToBackup, ObservableCollection<Game> gamesList, Game game) {
             await GetThumb(game);
            
            for (var i = 0; i < gamesList.Count(); i++) {
                if (game.Name != gamesList[i].Name) continue;
                game = gamesList[i];
                break;
            }

            if (game == null) return;
            gamesList.Remove(game);
            gamesToBackup.Add(game);
        }

        public static void AddToGamesList(ObservableCollection<Game> gamesToBackup,
            ObservableCollection<Game> gamesList, Game game)
        {
            for (var i = 0; i < gamesToBackup.Count(); i++)
            {
                if (game.Name != gamesToBackup[i].Name) continue;
                game = gamesToBackup[i];
                break;
            }

            if (game == null) return;
            gamesToBackup.Remove(game);
            gamesList.Add(game);
            gamesList = new ObservableCollection<Game>(gamesList.OrderBy(x => x.Name));
        }       

        public static void RemoveFromBackupList(ObservableCollection<Game> gamesToBackup, Game game){
            //for (var i = 0; i <= gamesToBackup.Count(); i++) {
            //    if (gamesToBackup[i].Game.Name != game.Name) continue;
            //    gamesToBackup.RemoveAt(i);
            //}
            gamesToBackup.Remove(game);
            gamesToBackup = new ObservableCollection<Game>(gamesToBackup.OrderBy(s => s.Name));
        }

        public static void RemoveFromGamesList(ObservableCollection<Game> gamesList, Game selectedGame) {
            //for (var i = 0; i <= gamesList.Count(); i++) {
            //    if (gamesList[i].Game.Name != selectedGame.Name) continue;
            //    gamesList.RemoveAt(i);
            //}

            gamesList.Remove(selectedGame);
            gamesList = new ObservableCollection<Game>(gamesList.OrderBy(s => s.Name));
        }

        private static async Task GetThumb(Game game) {
            var gb = new GiantBombAPI(game);
            await gb.GetThumb(game);
            game.ThumbnailPath = gb.ThumbnailPath;
        }
        
    }
}
