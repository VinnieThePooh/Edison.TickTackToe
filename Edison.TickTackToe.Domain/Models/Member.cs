using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Edison.TickTackToe.Domain.DataAccess;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Edison.TickTackToe.Domain.Models
{
    public class Member: IdentityUser<int, CustomUserLogin, CustomUserRole,CustomUserClaim>
    {
        [ForeignKey(nameof(Status))]
        public int? IdGameStatus { get; set; }

        public string NickName { get; set; }

        public virtual List<Game>  Games { get; set; }

        public MemberStatus Status { get; set; }
    }
}
