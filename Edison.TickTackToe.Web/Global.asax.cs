using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Edison.TickTackToe.Domain.DataAccess;
using Edison.TickTackToe.Web.Infrastructure;
using Edison.TickTackToe.Web.Models;
using Microsoft.AspNet.Identity.Owin;

namespace Edison.TickTackToe.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static IList<UserProjection> OnlineUsers { get; } = new List<UserProjection>();
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            Database.SetInitializer(new CustomCreateIfNotExist());
        }


        protected void Session_End()
        {
            var user = HttpContext.Current.User;
            var context = HttpContext.Current.GetOwinContext().Get<GameContext>();

            var toFind = context.Users.Single(u => u.UserName.Equals(user.Identity.Name));
            OnlineUsersTracker.RemoveOnlineUser(new UserProjection() { Name = toFind.UserName, Email = toFind.Email });
        }
    }
}
