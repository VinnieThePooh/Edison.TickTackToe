using System;
using System.Collections.Generic;
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
    public class GamesHub : Hub
    {
        public async Task<IEnumerable<UserProjection>> GetOnlineUsers()
        {
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

               // we do not wait db updating
               Clients.All.statusChanged(new { UserEmail = userEmail, StatusCode = newStatus });

               user.IdGameStatus = targetStatus.StatusId;
               await manager.UpdateAsync(user);
            }
            catch (Exception e)
            {
                Clients.Caller.statusChanged(new {Exception = e.ToString()});
            }
        }
    }
}