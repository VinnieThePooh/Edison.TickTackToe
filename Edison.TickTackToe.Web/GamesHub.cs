﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
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
using Microsoft.AspNet.SignalR.Hubs;
using WebGrease.Css.Extensions;

namespace Edison.TickTackToe.Web
{
    // todo create some GameManager and aggregate methods
    [System.Web.Mvc.Authorize]
    public class GamesHub : Hub
    {
        private async Task ActualizeConnectionId()
        {
            var context = Context.Request.GetHttpContext().GetOwinContext().Get<GameContext>();
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

        public async Task ChangeStatus(string userEmail, int newStatus)
        {
            try
            {
                var gameContext = Context.Request.GetHttpContext().GetOwinContext().Get<GameContext>();
                var user = await gameContext.Users.SingleAsync(u => u.Email.Equals(userEmail));

                var enumValue = (StatusCode) newStatus;
                var enumName = Enum.GetName(typeof (StatusCode), enumValue);
                var targetStatus = gameContext.Set<MemberStatus>().Single(s => s.Name.Equals(enumName));

                var fromStaticList = MvcApplication.OnlineUsers.Single(u => u.Email.Equals(userEmail));
                fromStaticList.Status = targetStatus.Name;

                // do not wait db updating
                Clients.All.statusChanged(new {UserEmail = userEmail, StatusCode = newStatus});
                
                user.IdGameStatus = targetStatus.StatusId;
                await gameContext.SaveChangesAsync();
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
                var toBeInvited = await GetUserByName(userName, Context);
                Clients.Client(toBeInvited.ConnectionId).invitationArrived(new {UserName = initiator});
            }

            catch (Exception e)
            {
                Clients.Caller.handleException(new {MethodName = nameof(InviteUser), Exception = e.ToString()});
            }
        }

        // todo: make some manager for game or not?
        public async Task AcceptInvitation(string invitatorName)
        {
            try
            {
                var invitator = await GetUserByName(invitatorName, Context);
                var opponent = await GetUserByName(Context.User.Identity.Name, Context);

                var oppId = opponent.ConnectionId;
                var invId = invitator.ConnectionId;

                var id = await CreateNewGame(invitator, opponent);


                // change status in db
                // change status in static list


                var context = Context.Request.GetHttpContext().GetOwinContext().Get<GameContext>();

                var playingStatus = await context.MemberStatuses.SingleAsync(ms => ms.Name.Equals(StatusNames.Playing));
                invitator.IdGameStatus = playingStatus.StatusId;
                opponent.IdGameStatus = playingStatus.StatusId;
                await context.SaveChangesAsync();

                MvcApplication.OnlineUsers.Where(
                    u => u.Name.Equals(opponent.UserName) || u.Name.Equals(invitator.UserName))
                    .ForEach(u => u.Status = playingStatus.Name);


                // on this client beginNewGame will be called inside this accepting callback
                // his own name will be taken from client
                Clients.Client(invId).userAcceptedInvitation(new {OpponentName = Context.User.Identity.Name, GameId = id});
                Clients.Caller.beginNewGame(new {InvitatorName = invitatorName, OpponentName = Context.User.Identity.Name, GameId = id});
                Clients.AllExcept(oppId, invId).playersStatusChanged(new
                                                                        {
                                                                            InvitatorName = invitatorName,
                                                                            OpponentName = opponent.UserName,
                                                                            StatusCode = (int) StatusCode.Playing
                                                                        });
            }
            catch (Exception e)
            {
                Clients.Caller.handleException(new {MethodName = nameof(AcceptInvitation), Exception = e.ToString()});
            }
        }


        public async Task MakeStep(int rowIndex, int colIndex, int gameId)
        {
            try
            {
                Game game;
                var context = Context.Request.GetHttpContext().GetOwinContext().Get<GameContext>();
                var userName = Context.User.Identity.Name;
                Debug.WriteLine($"{userName} made a step in [{rowIndex},{colIndex}].");
                var opponent = await AddStepToGame(rowIndex, colIndex, gameId, userName);

                var gameResolver = new GameResolver(context);
                var result = await gameResolver.CheckGameState(gameId, userName);
                if (result)
                {
                    game = context.Games.Find(gameId);
                    game.WinnerUserName = userName;
                    game.GameEndingDate = DateTime.Now;
                    await context.SaveChangesAsync();
                }

                game = context.Games.Find(gameId);

                // plain method of determing
                //todo: remake it later
                var stepsNumber = game.GameSteps.Count;
                if (!result && stepsNumber == game.FieldSize*game.FieldSize)
                {
                    game.WinnerUserName = ConstantStrings.NobodyWon;
                    game.GameEndingDate = DateTime.Now;
                    await context.SaveChangesAsync();
                }
                Clients.Client(opponent.ConnectionId).userMadeStep(new {RowIndex = rowIndex, ColumnIndex = colIndex});
            }
            catch (Exception e)
            {
                Clients.Caller.handleException(new {MethodName = nameof(MakeStep), Exception = e.ToString()});
            }
        }

        public async Task RejectInvitation(string invitatorName)
        {
            var targetUser = await GetUserByName(invitatorName, Context);
            Clients.Client(targetUser.ConnectionId).userRejectedInvitation(new {UserName = Context.User.Identity.Name});
        }

        // todo: refactor if possible
        public async Task BeginNewGame(int gameId, int? toCont)
        {
            var gameContext = Context.Request.GetHttpContext().GetOwinContext().Get<GameContext>();
            var userName = Context.User.Identity.Name;
            var game = await gameContext.Games.FindAsync(gameId);


            if (!toCont.HasValue)
            {
                var fromList = MvcApplication.OnlineUsers.Single(u => u.Name.Equals(userName));
                fromList.Status = (await GetStatusByName(StatusNames.Idle, Context)).Name;
            }


            try
            {
                if (game.OpponentUserName.Equals(userName))
                {
                    game.OppWannaProceed = toCont.HasValue;

                    if (!toCont.HasValue)
                    {
                        var user = await GetUserByName(userName, Context);
                        await ChangeStatus(user.Email, (int) StatusCode.Idle);
                        if (game.InvWannaProceed.HasValue)
                        {
                            if (game.InvWannaProceed.Value)
                            {
                                await ChangeStatus(game.PlayerInitiator.Email, (int) StatusCode.Idle);
                                Clients.Client(game.PlayerInitiator.ConnectionId).playerRejectedToProceed();
                            }
                        }
                    }
                    else if (game.InvWannaProceed.HasValue)
                    {
                        var opp = await GetUserByName(game.OpponentUserName, Context);
                        if (game.InvWannaProceed.Value)
                        {
                            var newInv = DefineNewInvitator(game, game.PlayerInitiator, opp);
                            var newId = await CreateNewGame(newInv, newInv == opp ? game.PlayerInitiator : opp);
                            Clients.Clients(new[] {opp.ConnectionId, game.PlayerInitiator.ConnectionId})
                                .beginNewGame(new {InvitatorName = newInv.UserName, OpponentName = newInv == opp ? game.PlayerInitiator.UserName: opp.UserName , GameId = newId});
                        }
                        else // no-yes case
                        {
                            Clients.Client(opp.ConnectionId).playerRejectedToProceed();
                            await ChangeStatus(opp.Email, (int)StatusCode.Idle);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Clients.Caller.handleException(new { MethodName = nameof(MakeStep), Exception = e.ToString()});
            }

            try
            {
                if (game.PlayerInitiator.UserName.Equals(userName))
                {
                    game.InvWannaProceed = toCont.HasValue;

                    if (!toCont.HasValue)
                    {
                        //  context doesn't allow for  wait
                        await ChangeStatus(game.PlayerInitiator.Email, (int) StatusCode.Idle);
                        if (game.OppWannaProceed.HasValue)
                        {
                            if (game.OppWannaProceed.Value)
                            {
                                var opp = await GetUserByName(game.OpponentUserName, Context);
                                await ChangeStatus(opp.Email, (int) StatusCode.Idle);
                                Clients.Client(opp.ConnectionId).playerRejectedToProceed();
                            }
                        }
                    }
                    else if (game.OppWannaProceed.HasValue)
                    {
                        var opp = await GetUserByName(game.OpponentUserName, Context);
                        if (game.OppWannaProceed.Value)
                        {
                            var newInv = DefineNewInvitator(game, game.PlayerInitiator, opp);
                            var newId = await CreateNewGame(newInv, newInv == opp ? game.PlayerInitiator : opp);
                            Clients.Clients(new[] {opp.ConnectionId, game.PlayerInitiator.ConnectionId})
                                .beginNewGame(new {InvitatorName = newInv.UserName, OpponentName = newInv == opp ? game.PlayerInitiator.UserName: opp.UserName,  GameId = newId});
                        }
                        // no-yes case
                        else
                        {
                            Clients.Client(game.PlayerInitiator.ConnectionId).playerRejectedToProceed();
                            await ChangeStatus(game.PlayerInitiator.Email, (int) StatusCode.Idle);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Clients.Caller.handleException(new {MethodName = nameof(BeginNewGame), Exception = e.ToString()});
            }

            await gameContext.SaveChangesAsync();
        }

        #region Implementation details

        /// <summary>
        ///     Returns opponent
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
            var context = Context.Request.GetHttpContext().GetOwinContext().Get<GameContext>();
            var game = context.Games.Find(gameId);

            // this method is not necessary
            var figure = await GetFigure(userName, context, game);

            var step = new GameStep
            {
                PlayerName = userName,
                XCoordinate = rowIndex,
                YCoordinate = columnIndex,
                IdFigure = figure.FigureId
            };
            game.GameSteps.Add(step);
            await context.SaveChangesAsync();

            if (game.PlayerInitiator.UserName.Equals(userName))
                opponent = await GetUserByName(game.OpponentUserName, Context);
            else if (game.OpponentUserName.Equals(userName))
                opponent = game.PlayerInitiator;
            else throw new ArgumentException("Invalid user name");
            return opponent;
        }


        private Member DefineNewInvitator(Game game, Member prevInv, Member prevOpp)
        {
            if (game.WinnerUserName.Equals(ConstantStrings.NobodyWon))
                return prevOpp;
            if (game.WinnerUserName.Equals(prevInv.UserName))
                return prevOpp;
            return prevInv;
        }


        private async Task<MemberStatus> GetStatusByName(string statusName, HubCallerContext hubContext)
        {
            var gameContext = hubContext.Request.GetHttpContext().GetOwinContext().Get<GameContext>();
            return await gameContext.MemberStatuses.SingleAsync(ms => ms.Name.Equals(statusName));
        }

        private async Task<GameFigure> GetFigure(string userName, GameContext context, Game game)
        {
            if (userName.Equals(game.OpponentUserName))
                return await context.GameFigures.SingleOrDefaultAsync(f => f.Name.Equals(FigureNames.Cross));
            return await context.GameFigures.SingleOrDefaultAsync(f => f.Name.Equals(FigureNames.Nought));
        }

        private async Task<Member> GetUserByName(string userName, HubCallerContext hubContext)
        {
            var context = hubContext.Request.GetHttpContext().GetOwinContext().Get<GameContext>();
            return await context.Users.SingleAsync(u => u.UserName.Equals(userName));
        }

        /// <summary>
        ///     Returns newly created game id
        /// </summary>
        /// <param name="invitator"></param>
        /// <param name="opponent"></param>
        /// <param name="fieldSize"></param>
        /// <returns></returns>
        private async Task<int> CreateNewGame(Member invitator, Member opponent, int fieldSize = 3)
        {
            // quess context is a singletone
            var context = Context.Request.GetHttpContext().GetOwinContext().Get<GameContext>();
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