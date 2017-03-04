using System.Data.Entity;
using Edison.TickTackToe.Domain.Models;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Edison.TickTackToe.Domain.DataAccess
{
   public class GameContext: IdentityDbContext<Member,Role, int,CustomUserLogin, CustomUserRole, CustomUserClaim>
   {
       public GameContext() : base("DefaultConnection")
       {
           Games = Set<Game>();
           GameFigures = Set<GameFigure>();
           MemberStatuses = Set<MemberStatus>();
       }


       public DbSet<Game> Games { get; set; }

       public DbSet<GameFigure> GameFigures { get; set; }
       public DbSet<MemberStatus> MemberStatuses { get; set; } 


       protected override void OnModelCreating(DbModelBuilder modelBuilder)
       {
           base.OnModelCreating(modelBuilder);
           modelBuilder.Configurations.Add(new UserProfileConfiguration());
           modelBuilder.Configurations.Add(new GameStepConfiguration());
           modelBuilder.Configurations.Add(new GameFigureConfiguration());
           modelBuilder.Configurations.Add(new MemberStatusConfiguration());



           modelBuilder.Entity<CustomUserLogin>().ToTable("UserLogins");
           modelBuilder.Entity<CustomUserRole>().ToTable("UserRoles");
           modelBuilder.Entity<CustomUserClaim>().ToTable("UserClaims");
           modelBuilder.Entity<Role>().ToTable("Roles");
       }
    }
}
