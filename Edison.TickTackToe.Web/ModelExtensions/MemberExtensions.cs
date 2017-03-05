using System.Security.Claims;
using System.Threading.Tasks;
using Edison.TickTackToe.Domain.Models;
using Microsoft.AspNet.Identity;

namespace Edison.TickTackToe.Web.ModelExtensions
{
    public static class MemberExtensions
    {
        public static async Task<ClaimsIdentity> GenerateUserIdentityAsync(this Member member, UserManager<Member,int> manager)
        {
            var userIdentity = await manager.CreateIdentityAsync(member, DefaultAuthenticationTypes.ApplicationCookie);
            return userIdentity;
        }
    }
}