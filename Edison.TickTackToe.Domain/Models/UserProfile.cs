using System.Collections.Generic;
using Edison.TickTackToe.Domain.DataAccess;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Edison.TickTackToe.Domain.Models
{
    public class UserProfile: IdentityUser<int, CustomUserLogin, CustomUserRole,CustomUserClaim>
    {
        public string NickName { get; set; }

        public virtual List<Game>  Games { get; set; }
    }
}
