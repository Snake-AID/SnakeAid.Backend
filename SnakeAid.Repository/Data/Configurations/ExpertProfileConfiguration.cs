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

            // One-to-One relationship with Account (configured in SnakeAidDbContext)
            builder.HasOne(ep => ep.Account)
                .WithOne(a => a.ExpertProfile)
                .HasForeignKey<ExpertProfile>(ep => ep.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many relationship with Specializations
            builder.HasMany(ep => ep.Specializations)
                .WithMany()
                .UsingEntity(j => j.ToTable("ExpertSpecializations"));
        }
    }
}