using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Edison.TickTackToe.Domain.DataAccess;
using Edison.TickTackToe.Domain.Infrastructure;
using Edison.TickTackToe.Domain.Infrastructure.Handbooks;
using Edison.TickTackToe.Domain.Models;
using Edison.TickTackToe.Web.Infrastructure;
using Edison.TickTackToe.Web.Models;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.SignalR;

namespace Edison.TickTackToe.Web
{
    // todo create some GameManager and aggregate methods
    [System.Web.Mvc.Authorize]
    public class GamesHub : Hub
    {
        private async Task ActualizeConnectionId()
        {
            var context = HttpContext.Current.GetOwinContext().Get<GameContext>();
            var userName = Context.User.Identity.Name;
            var user = context.Users.Single(u => u.UserName.Equals(userName));
            user.ConnectionId = Context.ConnectionId;
            await context.SaveChangesAsync();
        }

        public async Task<IEnumerable<UserProjection>> GetOnlineUsers()
        {
            await ActualizeConnectionId();
            return await Task.FromResult(MvcApplication.OnlineUsers);
        }

        // not tested
        // todo: test it
        public async Task ChangeStatus(string userEmail, int newStatus)
        {
            try
            {
               var owinContext = HttpContext.Current.GetOwinContext();
               var manager = owinContext.GetUserManager<ApplicationUserManager>();
               var user = await manager.FindByEmailAsync(userEmail);
                
               var enumValue = (StatusCode)newStatus;
               var enumName = Enum.GetName(typeof (StatusCode), enumValue);
               var gameContext = owinContext.Get<GameContext>();
               var targetStatus = gameContext.Set<MemberStatus>().Single(s => s.Name.Equals(enumName));

               var fromStaticList = MvcApplication.OnlineUsers.Single(u => u.Email.Equals(userEmail));
               fromStaticList.Status = targetStatus.Name;

               // do not wait db updating
               Clients.All.statusChanged(new { UserEmail = userEmail, StatusCode = newStatus });

               user.IdGameStatus = targetStatus.StatusId;
               await manager.UpdateAsync(user);
            }
            catch (Exception e)
            {
                Clients.Caller.handleException(new {Exception = e.ToString(), MethodName = nameof(ChangeStatus)});
            }
        }

        public async Task InviteUser(string userName)
        {
            try
            {
                if (string.IsNullOrEmpty(userName))
                      throw new ArgumentNullException(nameof(userName));

                var initiator = Context.User.Identity.Name;
                var toBeInvited = await GetUserByName(userName);
                Clients.Client(toBeInvited.ConnectionId).invitationArrived(new {UserName = initiator});
            }

            catch (Exception e)
            {
                Clients.Caller.handleException(new {MethodName = nameof(InviteUser), Exception = e.ToString()});
            }
        }



        // todo: create make some manager for game
        public async Task AcceptInvitation(string invitatorName)
        {
            try
            {
               var invitator = await GetUserByName(invitatorName);
               var opponent = await GetUserByName(Context.User.Identity.Name);

                var oppId = opponent.ConnectionId;
                var invId = invitator.ConnectionId;

               var id = await CreateNewGame(invitator, opponent);

                // on this client beginNewGame will be called inside this accepting callback
                // his own name will be taken from client
                Clients.Client(invId).userAcceptedInvitation(new { OpponentName = Context.User.Identity.Name, GameId = id });
                Clients.Caller.beginNewGame(new { InvitatorName = invitatorName, GameId = id });
                Clients.AllExcept(oppId, invId).playersStatusChanged(new {InvitatorName = invitatorName, OpponentName = opponent.UserName, StatusCode = (int)StatusCode.Playing});
            }
            catch (Exception e)
            {
                Clients.Caller.handleException(new { MethodName = nameof(AcceptInvitation), Exception = e.ToString() });
            }
        }


        public async Task MakeStep(int rowIndex, int colIndex, int gameId)
        {
            try
            {
                var context = HttpContext.Current.GetOwinContext().Get<GameContext>();
                var userName = Context.User.Identity.Name;
                var opponent = await AddStepToGame(rowIndex, colIndex, gameId, userName);

                var gameResolver = new GameResolver(context);
                var result = await gameResolver.CheckGameState(gameId, userName);
                if (result)
                {
                    var game = context.Games.Find(gameId);
                    game.WinnerUserName = userName;
                    game.GameEndingDate = DateTime.Now;
                    await context.SaveChangesAsync();
                }

                Clients.Client(opponent.ConnectionId).userMadeStep(new {RowIndex = rowIndex, ColumnIndex = colIndex});
            }
            catch (Exception e)
            {
                Clients.Caller.handleException(new { MethodName = nameof(MakeStep), Exception = e.ToString() });
            }
        }

        public async Task RejectInvitation(string invitatorName)
        {
            var targetUser = await GetUserByName(invitatorName);
            Clients.Client(targetUser.ConnectionId).userRejectedInvitation(new { UserName = Context.User.Identity.Name });
        }

        #region Implementation details

        /// <summary>
        /// Returns opponent
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="columnIndex"></param>
        /// <param name="gameId"></param>
        /// <param name="userName">Name of a use who made a step</param>
        /// <returns></returns>
        //todo: refactor
        // figure name in not necessary in GameStep entity
        private async Task<Member> AddStepToGame(int rowIndex, int columnIndex, int gameId, string userName)
        {
            Member opponent;
            var context = HttpContext.Current.GetOwinContext().Get<GameContext>();
            var game = context.Games.Find(gameId);

            // this method is not necessary
            var figure = await GetFigure(userName, context, game);

            var step = new GameStep()
            {
                PlayerName = userName,
                XCoordinate = rowIndex,
                YCoordinate = columnIndex,
                IdFigure = figure.FigureId
            };
            game.GameSteps.Add(step);
            await context.SaveChangesAsync();

            if (game.PlayerInitiator.UserName.Equals(userName))
                opponent = await GetUserByName(game.OpponentUserName);
            else if (game.OpponentUserName.Equals(userName))
                opponent = game.PlayerInitiator;
            else throw new ArgumentException("Invalid user name");
            return opponent;
        }

        private async Task<GameFigure> GetFigure(string userName, GameContext context, Game game)
        {
            if (userName.Equals(game.OpponentUserName))
                   return await context.GameFigures.SingleOrDefaultAsync(f => f.Name.Equals(FigureNames.Cross));
            return await context.GameFigures.SingleOrDefaultAsync(f => f.Name.Equals(FigureNames.Nought));
        }


        

        private async Task<Member> GetUserByName(string userName)
        {
            var context = HttpContext.Current.GetOwinContext().Get<GameContext>();
            return await context.Users.SingleAsync(u => u.UserName.Equals(userName));
        }

        /// <summary>
        /// Returns newly created game id
        /// </summary>
        /// <param name="invitator"></param>
        /// <param name="opponent"></param>
        /// <param name="fieldSize"></param>
        /// <returns></returns>
        private async Task<int> CreateNewGame(Member invitator, Member opponent, int fieldSize = 3)
        {
            // quess context is a singletone
            var context = HttpContext.Current.GetOwinContext().Get<GameContext>();
            var game = new Game
            {
                OpponentUserName = opponent.UserName,
                GameBeginningDate = DateTime.Now,
                // 3 by default now
                FieldSize = fieldSize
            };
            invitator.Games.Add(game);
            await context.SaveChangesAsync();
            return game.GameId;
        }
        #endregion
    }
}