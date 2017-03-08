using System;
using System.Threading.Tasks;
using Edison.TickTackToe.Domain.DataAccess;
using Edison.TickTackToe.Domain.Models;

namespace Edison.TickTackToe.Web.Infrastructure
{
    internal class GameResolver
    {
        private readonly GameContext _context;

        public GameResolver(GameContext context)
        {
            _context = context;
        }

        public async Task<GameState> CheckGameState(int gameId)
        {
            var game = _context.Games.Find(gameId);
            var invitatorWon = await CheckGameState(gameId, game.OpponentUserName);
            var oppWon =  await CheckGameState(gameId, game.PlayerInitiator.UserName);

            if (invitatorWon && oppWon)
                throw new ArgumentException("Could not be both users are winners");

            if (invitatorWon)
                return GameState.NoughtsVictory;
            if (oppWon)
                return GameState.CrossesVictory;

            return GameState.Draw;
        }

        public async Task<bool> CheckGameState(int gameId, string userName)
        {
            var game = _context.Games.Find(gameId);
            var targetDigit = GetTargetDigit(game, userName);
            var map = RestoreMap(game);
            return await ResolveGame(map, targetDigit);
        }

        #region Implementation details

        private int[,] RestoreMap(Game game)
        {
            var map = new int[game.FieldSize, game.FieldSize];
            map.Initialize();
            InitWithCustomValues(map);

            game.GameSteps.ForEach(gs =>
            {
                if (gs.PlayerName.Equals(game.PlayerInitiator.UserName))
                    map[gs.XCoordinate, gs.YCoordinate] = 0;
                else
                    map[gs.XCoordinate, gs.YCoordinate] = 1;
            });
            return map;
        }

        //todo: remove this method later
        private void InitWithCustomValues(int[,] map)
        {
            for(int i=0; i < map.GetLength(0); i++)
                for (int j = 0; j < map.GetLength(1); j++)
                    map[i, j] = -1;
        }

        private int GetTargetDigit(Game game, string userName)
        {
            if (game.PlayerInitiator.UserName.Equals(userName))
                return 0;
            if (game.OpponentUserName.Equals(userName))
                return 1;
            throw new ArgumentException("Invalid user name");
        }

        #region Game resolving itself

        private async Task<bool> ResolveGame(int[,] map, int targetDigit)
        {
            return await Task.FromResult(CheckHorizontalLines(map, targetDigit) |
                                           CheckVerticalLines(map, targetDigit) | 
                                           CheckDiagonalLines(map, targetDigit));
        }


        private bool CheckHorizontalLines(int[,] map, int targetDigit)
        {
            var flag = true;
            for (var i = 0; i < map.GetLength(0); i++)
            {
                flag = true;
                for (var j = 0; j < map.GetLength(1); j++)
                    if (map[i, j] != targetDigit)
                    {
                        flag = false;
                        break;
                    }
                if (flag)
                    return flag;
            }
            return flag;
        }

        // works only for 3 field size 
        private bool CheckDiagonalLines(int[,] map, int targetDigit)
        {
            return map[0, 0] == targetDigit &&
                   map[1,1] == targetDigit &&
                   map[2,2] == targetDigit ||
                   map[2,0] == targetDigit &&
                   map[1,1] == targetDigit &&
                   map[0,2] == targetDigit;
        }

        private bool CheckVerticalLines(int[,] map, int targetDigit)
        {
            var flag = true;
            for (var i = 0; i < map.GetLength(1); i++)
            {
                flag = true;
                for (var j = 0; j < map.GetLength(0); j++)
                    if (map[j, i] != targetDigit)
                    {
                        flag = false;
                        break;
                    }
                if (flag)
                    return flag;
            }
            return flag;
        }

        #endregion

        #endregion
    }
}