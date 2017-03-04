using System;
using System.Data.Entity.Migrations;
using System.Linq;
using Edison.TickTackToe.Domain.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Edison.TickTackToe.Domain.DataAccess
{
    public static class DbSeeding
    {
        #region Interface

        public static void Seed(GameContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            SeedRoles(context);
            SeedUsers(context);
        }

        #endregion

        #region Implementation details

        private static void SeedUsers(GameContext context)
        {
            UserProfile admin = new UserProfile
            {
                UserName = "Admin",
                NickName = "Admin",
                Email = "admin@mail.ru"
            };


            var fooUser = new UserProfile()
            {
                UserName = "FooUser",
                NickName = "FooUser",
                Email = "foouser@mail.ru"
            };

            var barUser = new UserProfile()
            {
                UserName = "BarUser",
                NickName = "BarUser",
                Email = "baruser@mail.ru"
            };

            using (
                var userStore =
                    new UserStore<UserProfile, Role, int, CustomUserLogin, CustomUserRole, CustomUserClaim>(context))
            using (var userManager = new UserManager<UserProfile, int>(userStore))
            {
                string password = "adminpassword12";
                AddUser(userManager, admin, password);
                AddRole(userManager, admin, RoleNames.Admin);

                password = "fupass12";
                AddUser(userManager, fooUser, password);
                AddRole(userManager, fooUser, RoleNames.GeneralMember);

                password = "bupass12";
                AddUser(userManager, barUser, password);
                AddRole(userManager, barUser, RoleNames.GeneralMember);
            }
        }

        private static void AddUser(UserManager<UserProfile, int> userManager, UserProfile userProfile, string password)
        {
            var result = userManager.Create(userProfile);
            if (!result.Succeeded)
                throw new InvalidOperationException(result.Errors.First());

            userProfile = userManager.FindByEmail(userProfile.Email);

            result = userManager.AddPassword(userProfile.Id, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(result.Errors.First());
        }


        private static void SeedRoles(GameContext context)
        {
            context.Roles.AddOrUpdate(new[]
            {
                new Role() {Name = RoleNames.Admin},
                new Role() {Name = RoleNames.GeneralMember}
            });
        }


        private static void AddRole(UserManager<UserProfile, int> userManager, UserProfile userProfile, string roleName)
        {
            var result = userManager.AddToRole(userProfile.Id, roleName);
            if (!result.Succeeded)
                throw new InvalidOperationException(result.Errors.First());
        }

        #endregion
    }
}
