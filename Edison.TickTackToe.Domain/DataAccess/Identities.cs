using Microsoft.AspNet.Identity.EntityFramework;

namespace Edison.TickTackToe.Domain.DataAccess
{
    public class CustomUserLogin : IdentityUserLogin<int> { }
    public class CustomUserRole : IdentityUserRole<int> { }
    public class CustomUserClaim: IdentityUserClaim<int> { }
    public class Role: IdentityRole<int, CustomUserRole> { }
}
