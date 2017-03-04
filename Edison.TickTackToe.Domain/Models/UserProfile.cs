using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Edison.TickTackToe.Domain.DataAccess;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Edison.TickTackToe.Domain.Models
{
    public class UserProfile: IdentityUser<int, CustomUserLogin, CustomUserRole,CustomUserClaim>
    {
        public string NickName { get; set; }

        public virtual List<Game>  Games { get; set; }
    }
}
