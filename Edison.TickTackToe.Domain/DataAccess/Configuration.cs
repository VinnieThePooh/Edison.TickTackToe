using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using Edison.TickTackToe.Domain.Models;

namespace Edison.TickTackToe.Domain.DataAccess
{
    
    public class UserProfileConfiguration : EntityTypeConfiguration<UserProfile>
    {
        public UserProfileConfiguration()
        {
            ToTable("Profiles");
            Property(e => e.UserName)
                .HasMaxLength(DefaultConstraints.DefaultStringMaxLength)
                .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute() {IsUnique = true}))
                .IsRequired();
            Property(e => e.NickName)
                .HasMaxLength(DefaultConstraints.DefaultStringMaxLength)
                .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute() {IsUnique = true}))
                .IsRequired();
            HasMany(u => u.Games).WithOptional(g => g.PlayerInitiator).HasForeignKey(g => g.IdPlayerInitiator).WillCascadeOnDelete(false);
        }
    }

    public class GameStepConfiguration : EntityTypeConfiguration<GameStep>
    {
        public GameStepConfiguration()
        {
            HasRequired(gs => gs.ParentGame).WithMany(g => g.GameSteps).HasForeignKey(gs => gs.IdGame).WillCascadeOnDelete(true);
            Property(gs => gs.PlayerName).IsRequired().HasMaxLength(DefaultConstraints.DefaultStringMaxLength);
        }
    }


    public class GameFigureConfiguration : EntityTypeConfiguration<GameFigure>
    {
        public GameFigureConfiguration()
        {
            ToTable("Figures", "handbooks");
        }
    }

}
