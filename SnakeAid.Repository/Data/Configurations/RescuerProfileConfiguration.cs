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

            // One-to-Many: RescuerProfile -> CatchingMissions
            builder.HasMany(rp => rp.CatchingMissions)
                .WithOne(m => m.Rescuer)
                .HasForeignKey(m => m.RescuerId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-Many: RescuerProfile -> RescuerRequests
            builder.HasMany(rp => rp.RescuerRequests)
                .WithOne(r => r.Rescuer)
                .HasForeignKey(r => r.RescuerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index cho query performance
            builder.HasIndex(rp => rp.IsOnline)
                .HasDatabaseName("IX_RescuerProfiles_IsOnline");

            builder.HasIndex(rp => rp.Type)
                .HasDatabaseName("IX_RescuerProfiles_Type");
        }
    }
}