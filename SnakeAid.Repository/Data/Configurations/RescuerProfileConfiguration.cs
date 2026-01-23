using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class RescuerProfileConfiguration : IEntityTypeConfiguration<RescuerProfile>
    {
        public void Configure(EntityTypeBuilder<RescuerProfile> builder)
        {
            builder.ToTable("RescuerProfiles");

            // Primary key
            builder.HasKey(rp => rp.AccountId);

            builder.Property(rp => rp.Type)
                .HasConversion<int>()
                .IsRequired()
                .HasDefaultValue(RescuerType.Emergency);

            // One-to-One relationship with Account (configured in SnakeAidDbContext)
            builder.HasOne(rp => rp.Account)
                .WithOne(a => a.RescuerProfile)
                .HasForeignKey<RescuerProfile>(rp => rp.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index cho query performance
            builder.HasIndex(rp => rp.IsOnline)
                .HasDatabaseName("IX_RescuerProfiles_IsOnline");

            builder.HasIndex(rp => rp.Type)
                .HasDatabaseName("IX_RescuerProfiles_Type");
        }
    }
}