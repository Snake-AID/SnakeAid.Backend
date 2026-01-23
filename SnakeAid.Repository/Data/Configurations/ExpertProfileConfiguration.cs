using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SnakeAid.Repository.Data.Configurations
{
    public class ExpertProfileConfiguration : IEntityTypeConfiguration<ExpertProfile>
    {
        public void Configure(EntityTypeBuilder<ExpertProfile> builder)
        {
            builder.ToTable("ExpertProfiles");

            // Inherit from Account
            builder.HasBaseType<Account>();

            builder.Property(ep => ep.Biography)
                .HasMaxLength(2000);

            builder.Property(ep => ep.IsOnline)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(ep => ep.Rating)
                .HasColumnType("decimal(3,2)")
                .HasDefaultValue(0.0f);

            builder.Property(ep => ep.RatingCount)
                .IsRequired()
                .HasDefaultValue(0);

            // Configure many-to-many relationship with Specialization
            builder.HasMany(ep => ep.Specializations)
                .WithMany()
                .UsingEntity("ExpertSpecializations",
                    l => l.HasOne(typeof(Specialization)).WithMany().HasForeignKey("SpecializationId"),
                    r => r.HasOne(typeof(ExpertProfile)).WithMany().HasForeignKey("ExpertProfileId"),
                    j => j.HasKey("ExpertProfileId", "SpecializationId"));
        }
    }
}