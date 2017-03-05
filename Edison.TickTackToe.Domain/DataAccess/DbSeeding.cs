using System;
using System.Data.Entity.Migrations;
using System.Linq;
using Edison.TickTackToe.Domain.Infrastructure.Handbooks;
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
            SeedFigures(context);
            SeedMemberStatuses(context);
            SeedUsers(context);
        }

        #endregion

        #region Implementation details

        private static void SeedUsers(GameContext context)
        {
            Member admin = new Member
            {
                UserName = "Admin",
                NickName = "Admin",
                Email = "admin@mail.ru"
            };

            var fooUser = new Member()
            {
                UserName = "FooUser",
                NickName = "FooUser",
                Email = "foouser@mail.ru"
            };

            var barUser = new Member()
            {
                UserName = "BarUser",
                NickName = "BarUser",
                Email = "baruser@mail.ru"
            };

            using (var userStore = new UserStore<Member, Role, int, CustomUserLogin, CustomUserRole, CustomUserClaim>(context))
            using (var userManager = new UserManager<Member, int>(userStore))
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

        private static void AddUser(UserManager<Member, int> userManager, Member member, string password)
        {
            var result = userManager.Create(member);
            if (!result.Succeeded)
                throw new InvalidOperationException(result.Errors.First());

            member = userManager.FindByEmail(member.Email);

            result = userManager.AddPassword(member.Id, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(result.Errors.First());
        }

        private static void SeedFigures(GameContext context)
        {
            context.GameFigures.AddOrUpdate(new GameFigure() { Name = FigureNames.Cross},
                                            new GameFigure() { Name = FigureNames.Nought});
        }


        private static void SeedRoles(GameContext context)
        {
            context.Roles.AddOrUpdate(new Role() {Name = RoleNames.Admin},
                                      new Role() {Name = RoleNames.GeneralMember});
        }


        private static void AddRole(UserManager<Member, int> userManager, Member member, string roleName)
        {
            var result = userManager.AddToRole(member.Id, roleName);
            if (!result.Succeeded)
                throw new InvalidOperationException(result.Errors.First());
        }


        private static void SeedMemberStatuses(GameContext context)
        {
                 context.MemberStatuses.AddOrUpdate(new MemberStatus() {Name = StatusNames.Idle},
                                                    new MemberStatus() { Name = StatusNames.Offline},
                                                    new MemberStatus() { Name = StatusNames.Playing });
        }

        #endregion
    }
}
