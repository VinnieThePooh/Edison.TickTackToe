using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Edison.TickTackToe.Domain.DataAccess;
using Edison.TickTackToe.Domain.Infrastructure;
using Edison.TickTackToe.Domain.Models;
using Edison.TickTackToe.Web.Models;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.SignalR;

namespace Edison.TickTackToe.Web
{
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


        public async Task AcceptInvitation(string invitatorName)
        {


        }

        
        public async Task RejectInvitation(string invitatorName)
        {

        }

        private async Task<Member> GetUserByName(string userName)
        {
            var context = HttpContext.Current.GetOwinContext().Get<GameContext>();
            return await  context.Users.SingleAsync(u => u.UserName.Equals(userName));
        }

    }
}