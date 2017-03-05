using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Edison.TickTackToe.Web.Models;
using Microsoft.AspNet.SignalR;

namespace Edison.TickTackToe.Web.Infrastructure
{
    public class OnlineUsersTracker
    {

        static readonly IHubContext GamesHubContext = GlobalHost.ConnectionManager.GetHubContext<GamesHub>();

        internal static void RemoveOnlineUser(UserProjection projection)
        {
            var user = MvcApplication.OnlineUsers.FirstOrDefault(u => u.Email.Equals(projection.Email));
            if (user != null)
            {
                MvcApplication.OnlineUsers.Remove(user);
                GamesHubContext.Clients.All.userLeftSite(projection);
            }
        }

        internal static void AddOnlineUser(UserProjection projection)
        {
            var user = MvcApplication.OnlineUsers.FirstOrDefault(u => u.Email.Equals(projection.Email));
            if (user == null)
            {
                MvcApplication.OnlineUsers.Add(projection);
                GamesHubContext.Clients.All.userJoinedSite(projection);
            }
        }
    }
}