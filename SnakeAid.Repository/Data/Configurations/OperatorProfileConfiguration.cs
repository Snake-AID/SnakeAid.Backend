using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class OperatorProfileConfiguration : IEntityTypeConfiguration<OperatorProfile>
    {
        public void Configure(EntityTypeBuilder<OperatorProfile> builder)
        {
            builder.ToTable("OperatorProfiles");

            builder.HasOne(o => o.Account)
                .WithMany()
                .HasForeignKey(o => o.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(o => o.AccountId)
                .IsUnique()
                .HasDatabaseName("UX_OperatorProfiles_AccountId");

            builder.HasIndex(o => o.IsOnDuty)
                .HasDatabaseName("IX_OperatorProfiles_IsOnDuty");

            builder.HasIndex(o => o.IsAcceptingNew)
                .HasDatabaseName("IX_OperatorProfiles_IsAcceptingNew");
        }
    }
}
