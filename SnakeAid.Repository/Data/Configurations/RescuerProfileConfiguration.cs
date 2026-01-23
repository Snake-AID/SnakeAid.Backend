using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SnakeAid.Repository.Data.Configurations
{
    public class RescuerProfileConfiguration : IEntityTypeConfiguration<RescuerProfile>
    {
        public void Configure(EntityTypeBuilder<RescuerProfile> builder)
        {
            builder.ToTable("RescuerProfiles");

            // Inherit from Account
            builder.HasBaseType<Account>();

            builder.Property(rp => rp.IsOnline)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(rp => rp.Rating)
                .HasColumnType("decimal(3,2)")
                .HasDefaultValue(0.0f);

            builder.Property(rp => rp.RatingCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(rp => rp.Type)
                .HasConversion<int>()
                .IsRequired()
                .HasDefaultValue(RescuerType.Emergency);

            // Index for finding online rescuers
            builder.HasIndex(rp => rp.IsOnline)
                .HasDatabaseName("IX_RescuerProfiles_IsOnline");

            // Index for rating queries
            builder.HasIndex(rp => rp.Rating)
                .HasDatabaseName("IX_RescuerProfiles_Rating");

            // Index for rescuer type
            builder.HasIndex(rp => rp.Type)
                .HasDatabaseName("IX_RescuerProfiles_Type");
        }
    }
}