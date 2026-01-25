using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class UserFeedbackConfiguration : IEntityTypeConfiguration<UserFeedback>
    {
        public void Configure(EntityTypeBuilder<UserFeedback> builder)
        {
            builder.ToTable("UserFeedbacks");

            // Enum conversion
            builder.Property(f => f.Type)
                .HasConversion<int>()
                .IsRequired();

            // Relationship: UserFeedback -> Account (Rater)
            builder.HasOne(f => f.Rater)
                .WithMany()
                .HasForeignKey(f => f.RaterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: UserFeedback -> Account (TargetUser)
            builder.HasOne(f => f.TargetUser)
                .WithMany()
                .HasForeignKey(f => f.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(f => f.RaterId)
                .HasDatabaseName("IX_UserFeedbacks_RaterId");

            builder.HasIndex(f => f.TargetUserId)
                .HasDatabaseName("IX_UserFeedbacks_TargetUserId");

            builder.HasIndex(f => f.Type)
                .HasDatabaseName("IX_UserFeedbacks_Type");

            builder.HasIndex(f => new { f.ReferenceId, f.Type })
                .HasDatabaseName("IX_UserFeedbacks_ReferenceId_Type");
        }
    }
}
