using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using System.Xml.Serialization;
using Edison.TickTackToe.Domain.Infrastructure;
using Edison.TickTackToe.Domain.Infrastructure.Handbooks;
using Edison.TickTackToe.Domain.Models;

namespace Edison.TickTackToe.Domain.DataAccess
{
    
    public class UserProfileConfiguration : EntityTypeConfiguration<Member>
    {
        public UserProfileConfiguration()
        {
            ToTable("Profiles");
            Property(e => e.UserName)
                .HasMaxLength(DefaultConstraints.StringMaxLength)
                .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute() {IsUnique = true}))
                .IsRequired();
            Property(e => e.NickName)
                .HasMaxLength(DefaultConstraints.StringMaxLength)
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
            Property(gs => gs.PlayerName).IsRequired().HasMaxLength(DefaultConstraints.StringMaxLength);
        }
    }


    public class GameFigureConfiguration : EntityTypeConfiguration<GameFigure>
    {
        public GameFigureConfiguration()
        {
            ToTable("Figures", SchemaNames.Handbooks);
            HasKey(gf => gf.FigureId);

            HasMany(gf => gf.GameSteps)
                .WithRequired(m => m.GameFigure)
                .HasForeignKey(m => m.IdFigure)
                .WillCascadeOnDelete(false);

            Property(gf => gf.Name)
                .IsRequired()
                .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute() { IsUnique = true }))
                .HasMaxLength(DefaultConstraints.StringMaxLength);
        }
    }


    public class MemberStatusConfiguration : EntityTypeConfiguration<MemberStatus>
    {
        public MemberStatusConfiguration()
        {
            ToTable("MemberStatuses", SchemaNames.Handbooks);
            HasKey(ms => ms.StatusId);

            HasMany(gs => gs.Members)
                .WithOptional(m => m.Status)
                .HasForeignKey(m => m.IdGameStatus)
                .WillCascadeOnDelete(false);

            Property(ms => ms.Name)
                .IsRequired()
                .HasColumnAnnotation(IndexAnnotation.AnnotationName,new IndexAnnotation(new IndexAttribute() {IsUnique = true}))
                .HasMaxLength(DefaultConstraints.StringMaxLength);
        }
    }

}
