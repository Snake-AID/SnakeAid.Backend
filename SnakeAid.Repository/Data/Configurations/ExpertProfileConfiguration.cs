using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class ExpertProfileConfiguration : IEntityTypeConfiguration<ExpertProfile>
    {
        public void Configure(EntityTypeBuilder<ExpertProfile> builder)
        {
            builder.ToTable("ExpertProfiles");

            // One-to-One relationship with Account
            builder.HasOne(ep => ep.Account)
                .WithOne(a => a.ExpertProfile)
                .HasForeignKey<ExpertProfile>(ep => ep.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship với Specializations đã được config tại ExpertSpecializationConfiguration
            // (ExpertSpecialization là explicit join entity, không dùng implicit many-to-many)
        }
    }
}